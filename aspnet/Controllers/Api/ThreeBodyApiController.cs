using aspnet.Models;
using aspnet.Models.DTO;
using aspnet.Repositories;
using aspnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

[Route("api/threebody")]
[ApiController]
[Authorize]
public class ThreeBodyApiController : ControllerBase
{
    private readonly ThreeBodyGameService _game;
    private readonly IPlayerRepository _players;

    public ThreeBodyApiController(ThreeBodyGameService game, IPlayerRepository players)
    {
        _game = game;
        _players = players;
    }

    public record ThreeBodyBetRequest(int Amount, string Planet);

    private Player? FindPlayer()
    {
        var email = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(email) ? null : _players.GetByEmail(email);
    }

    private ActionResult<ThreeBodyStateDTO> WithPlayer(Func<Player, ThreeBodyStateDTO> action)
    {
        var player = FindPlayer();
        if (player is null)
        {
            return Conflict(new { error = "Tvoj račun nema zapis igrača. Spremi svoj profil (Moj profil) pa pokušaj ponovno." });
        }
        return Ok(action(player));
    }

    [HttpGet("state")]
    public ActionResult<ThreeBodyStateDTO> State()
        => WithPlayer(p => _game.GetState(p.Id, p));

    [HttpPost("bet")]
    public ActionResult<ThreeBodyStateDTO> Bet([FromBody] ThreeBodyBetRequest req)
    {
        if (req.Amount <= 0) return BadRequest(new { error = "amount must be positive." });
        if (req.Planet is not ("A" or "B" or "C")) return BadRequest(new { error = "planet must be A, B, or C." });
        return WithPlayer(p => _game.SetBet(p.Id, req.Amount, req.Planet, p));
    }

    [HttpPost("start")]
    public ActionResult<ThreeBodyStateDTO> Start()
        => WithPlayer(p => _game.Start(p.Id, p));

    [HttpPost("skip")]
    public ActionResult<ThreeBodyStateDTO> SkipToEnd()
        => WithPlayer(p => _game.SkipToEnd(p.Id, p));

    [HttpPost("reset")]
    public ActionResult<ThreeBodyStateDTO> Reset()
        => WithPlayer(p => _game.Reset(p.Id, p));
}
