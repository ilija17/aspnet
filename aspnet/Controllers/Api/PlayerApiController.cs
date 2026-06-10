using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/player")]
[ApiController]
public class PlayerApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public PlayerApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/player?q=marko&minBalance=1000
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDTO>>> Get(string? q, decimal? minBalance)
    {
        var query = _dbContext.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                p.FirstName.Contains(q) ||
                p.LastName.Contains(q) ||
                p.Email.Contains(q));
        }

        if (minBalance.HasValue)
        {
            query = query.Where(p => p.Balance >= minBalance.Value);
        }

        var players = await query.ToListAsync();
        return Ok(players.Select(p => p.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDTO>> Get(int id)
    {
        var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null) return NotFound();

        return Ok(player.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PlayerDTO>> Post([FromBody] PlayerInputDTO model)
    {
        var player = new Player
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            DateOfBirth = model.DateOfBirth,
            Balance = model.Balance
        };

        _dbContext.Players.Add(player);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = player.Id }, player.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PlayerDTO>> Put(int id, [FromBody] PlayerInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null) return NotFound();

        player.FirstName = model.FirstName;
        player.LastName = model.LastName;
        player.Email = model.Email;
        player.DateOfBirth = model.DateOfBirth;
        player.Balance = model.Balance;

        await _dbContext.SaveChangesAsync();

        return Ok(player.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null) return NotFound();

        _dbContext.Players.Remove(player);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
