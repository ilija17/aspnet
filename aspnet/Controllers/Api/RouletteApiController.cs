using aspnet.Models;
using aspnet.Models.DTO;
using aspnet.Repositories;
using aspnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

// API za singleplayer rulet (wwwroot/rulet). Sva pravila igre su u
// RouletteGameService; igrač se identificira prijavljenim računom, a ulozi
// i isplate idu na njegov stvarni Player.Balance.
[Route("api/roulette")]
[ApiController]
[Authorize]
public class RouletteApiController : ControllerBase
{
    private readonly RouletteGameService _game;
    private readonly IPlayerRepository _players;

    public RouletteApiController(RouletteGameService game, IPlayerRepository players)
    {
        _game = game;
        _players = players;
    }

    public record RouletteBetRequest(string? Kind, int? Number, int? Amount);

    private Player? FindPlayer()
    {
        var email = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(email) ? null : _players.GetByEmail(email);
    }

    private ActionResult<RouletteStateDTO> WithPlayer(Func<int, RouletteStateDTO> action)
    {
        var player = FindPlayer();
        if (player is null)
        {
            return Conflict(new { error = "Tvoj račun nema zapis igrača. Spremi svoj profil (Moj profil) pa pokušaj ponovno." });
        }
        return Ok(action(player.Id));
    }

    [HttpGet("state")]
    public ActionResult<RouletteStateDTO> State() => WithPlayer(_game.GetState);

    [HttpPost("bet")]
    public ActionResult<RouletteStateDTO> Bet([FromBody] RouletteBetRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Kind)) return BadRequest(new { error = "kind is required." });
        if (req.Amount is not > 0) return BadRequest(new { error = "amount must be positive." });
        return WithPlayer(id => _game.PlaceBet(id, req.Kind, req.Number, req.Amount.Value));
    }

    [HttpPost("clear")]
    public ActionResult<RouletteStateDTO> Clear() => WithPlayer(_game.ClearBets);

    [HttpPost("spin")]
    public ActionResult<RouletteStateDTO> Spin() => WithPlayer(_game.Spin);
}
