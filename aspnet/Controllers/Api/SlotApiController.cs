using aspnet.Models;
using aspnet.Models.DTO;
using aspnet.Repositories;
using aspnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers.Api;

// API za singleplayer slot (wwwroot/slot). Sva pravila igre su u
// SlotGameService; igrač se identificira prijavljenim računom, a ulozi i
// isplate idu na njegov stvarni Player.Balance.
[Route("api/slot")]
[ApiController]
[Authorize]
public class SlotApiController : ControllerBase
{
    private readonly SlotGameService _game;
    private readonly IPlayerRepository _players;

    public SlotApiController(SlotGameService game, IPlayerRepository players)
    {
        _game = game;
        _players = players;
    }

    public record SlotBetRequest(int? Amount);

    private Player? FindPlayer()
    {
        var email = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(email) ? null : _players.GetByEmail(email);
    }

    private ActionResult<SlotStateDTO> WithPlayer(Func<int, SlotStateDTO> action)
    {
        var player = FindPlayer();
        if (player is null)
        {
            return Conflict(new { error = "Tvoj račun nema zapis igrača. Spremi svoj profil (Moj profil) pa pokušaj ponovno." });
        }
        return Ok(action(player.Id));
    }

    [HttpGet("state")]
    public ActionResult<SlotStateDTO> State() => WithPlayer(_game.GetState);

    [HttpPost("bet")]
    public ActionResult<SlotStateDTO> Bet([FromBody] SlotBetRequest req)
    {
        if (req.Amount is not > 0) return BadRequest(new { error = "amount must be positive." });
        return WithPlayer(id => _game.SetBet(id, req.Amount.Value));
    }

    [HttpPost("spin")]
    public ActionResult<SlotStateDTO> Spin() => WithPlayer(_game.Spin);
}
