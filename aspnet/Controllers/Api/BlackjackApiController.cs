using aspnet.Models;
using aspnet.Models.DTO;
using aspnet.Repositories;
using aspnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

// API za singleplayer blackjack (wwwroot/kocka). Sva pravila igre su u
// BlackjackGameService; igrač se identificira prijavljenim računom, a ulozi
// i isplate idu na njegov stvarni Player.Balance.
[Route("api/blackjack")]
[ApiController]
[Authorize]
public class BlackjackApiController : ControllerBase
{
    private readonly BlackjackGameService _game;
    private readonly IPlayerRepository _players;

    public BlackjackApiController(BlackjackGameService game, IPlayerRepository players)
    {
        _game = game;
        _players = players;
    }

    public record BlackjackActionRequest(int? Amount);

    // Username je email, a zapis igrača nastaje pri registraciji; stariji
    // računi bez njega dobivaju ga spremanjem profila
    private Player? FindPlayer()
    {
        var email = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(email) ? null : _players.GetByEmail(email);
    }

    private ActionResult<BlackjackStateDTO> WithPlayer(Func<int, BlackjackStateDTO> action)
    {
        var player = FindPlayer();
        if (player is null)
        {
            return Conflict(new { error = "Tvoj račun nema zapis igrača. Spremi svoj profil (Moj profil) pa pokušaj ponovno." });
        }
        return Ok(action(player.Id));
    }

    [HttpGet("state")]
    public ActionResult<BlackjackStateDTO> State() => WithPlayer(_game.GetState);

    [HttpPost("bet")]
    public ActionResult<BlackjackStateDTO> Bet([FromBody] BlackjackActionRequest req)
    {
        if (req.Amount is not > 0) return BadRequest(new { error = "amount must be positive." });
        return WithPlayer(id => _game.SetBet(id, req.Amount.Value));
    }

    [HttpPost("deal")]
    public ActionResult<BlackjackStateDTO> Deal() => WithPlayer(_game.Deal);

    [HttpPost("hit")]
    public ActionResult<BlackjackStateDTO> Hit() => WithPlayer(_game.Hit);

    [HttpPost("stand")]
    public ActionResult<BlackjackStateDTO> Stand() => WithPlayer(_game.Stand);

    [HttpPost("double")]
    public ActionResult<BlackjackStateDTO> Double() => WithPlayer(_game.Double);
}
