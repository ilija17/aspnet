using System.Text.Json;
using aspnet.Models.DTO;
using aspnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

[Route("api/chat")]
[ApiController]
public class ChatApiController : ControllerBase
{
    private const string SystemPrompt =
        "You are the AI concierge of a casino management web app (casinos, tables, games, " +
        "players, reservations, transactions). Answer briefly and helpfully, in the language " +
        "the user writes in. Keep the playful casino-concierge tone, but give real answers. " +
        "Use the provided tools to look up real data instead of guessing; if a tool returns " +
        "an error, tell the user what you can't access and why. " +
        "Never give gambling, financial or legal advice beyond general information.";

    // Više rundi tool-callinga: model smije lančano pozivati alate prije odgovora
    private const int MaxToolRounds = 6;

    // DeepSeek šalje cijeli odgovor odjednom (bez streaminga), pa duži razgovori
    // znaju trajati i preko default timeouta
    private static readonly TimeSpan UpstreamTimeout = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ChatToolService _toolService;
    private readonly ILogger<ChatApiController> _logger;

    public ChatApiController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ChatToolService toolService,
        ILogger<ChatApiController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _toolService = toolService;
        _logger = logger;
    }

    /// POST /api/chat — proslijedi razgovor DeepSeek modelu i vrati odgovor.
    /// Model kroz function calling smije čitati podatke, ali samo one na koje
    /// pozivatelj ima pravo (vidi ChatToolService).
    [HttpPost]
    public async Task<ActionResult<ChatReplyDTO>> Post([FromBody] ChatRequestDTO request)
    {
        // DeepSeek__ApiKey (compose) ili DEEPSEEK_API_KEY (raw env / .env)
        var apiKey = _config["DeepSeek:ApiKey"] ?? _config["DEEPSEEK_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "AI chat is not configured." });
        }

        var model = _config["DeepSeek:Model"] ?? "deepseek-chat";
        var baseUrl = _config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var userContext = new ChatUserContext(
            isAuthenticated,
            IsStaff: isAuthenticated && (User.IsInRole("Admin") || User.IsInRole("Manager")),
            Email: isAuthenticated ? User.Identity!.Name : null);

        var identityNote = userContext switch
        {
            { IsStaff: true } => $" The user is signed in as staff ({userContext.Email}) and may see all data.",
            { IsAuthenticated: true } => $" The user is signed in as {userContext.Email} and may see public data and only their own player data.",
            _ => " The visitor is not signed in and may only see public catalog data."
        };

        var messages = new List<object> { new { role = "system", content = SystemPrompt + identityNote } };
        // Ograniči povijest da prompt ne raste bez kontrole
        foreach (var m in request.Messages.TakeLast(20))
        {
            var role = m.Role == "assistant" ? "assistant" : "user";
            messages.Add(new { role, content = m.Content });
        }

        var tools = _toolService.GetToolDefinitions(userContext);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = UpstreamTimeout;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            for (var round = 0; round < MaxToolRounds; round++)
            {
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl.TrimEnd('/')}/chat/completions",
                    new { model, messages, tools, max_tokens = 1024 });

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("DeepSeek returned {Status}: {Detail}", response.StatusCode, detail);
                    return StatusCode(StatusCodes.Status502BadGateway,
                        new { error = "AI service returned an error." });
                }

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var message = json.RootElement.GetProperty("choices")[0].GetProperty("message");

                if (message.TryGetProperty("tool_calls", out var toolCalls) &&
                    toolCalls.ValueKind == JsonValueKind.Array &&
                    toolCalls.GetArrayLength() > 0)
                {
                    // Assistant poruka s tool_calls mora se vratiti modelu netaknuta
                    messages.Add(message.Clone());

                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var callId = toolCall.GetProperty("id").GetString();
                        var function = toolCall.GetProperty("function");
                        var toolName = function.GetProperty("name").GetString() ?? "";
                        var result = await _toolService.ExecuteAsync(
                            toolName, ParseArguments(function), userContext);

                        messages.Add(new { role = "tool", tool_call_id = callId, content = result });
                    }
                    continue;
                }

                var reply = message.GetProperty("content").GetString();
                return Ok(new ChatReplyDTO { Reply = reply ?? string.Empty });
            }

            // Model se zapleo u alate — bolje graciozan odgovor nego fallback na canned replies
            return Ok(new ChatReplyDTO
            {
                Reply = "I dug through the data but couldn't wrap that one up — try asking a bit more specifically. 🎲"
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "DeepSeek request failed");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "AI service is unreachable." });
        }
    }

    /// function.arguments je JSON zapisan kao string; neispravan JSON tretiraj kao prazan
    private static JsonElement ParseArguments(JsonElement function)
    {
        var raw = function.TryGetProperty("arguments", out var a) ? a.GetString() : null;
        if (string.IsNullOrWhiteSpace(raw)) return default;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
