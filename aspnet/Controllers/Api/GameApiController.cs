using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/game")]
[ApiController]
public class GameApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public GameApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/game?q=poker&type=Blackjack
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameDTO>>> Get(string? q, GameType? type)
    {
        var query = _dbContext.Games.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(g =>
                g.Name.Contains(q) ||
                g.Description.Contains(q));
        }

        if (type.HasValue)
        {
            query = query.Where(g => g.Type == type.Value);
        }

        var games = await query.ToListAsync();
        return Ok(games.Select(g => g.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GameDTO>> Get(int id)
    {
        var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.Id == id);
        if (game is null) return NotFound();

        return Ok(game.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<GameDTO>> Post([FromBody] GameInputDTO model)
    {
        var game = new Game
        {
            Name = model.Name,
            Type = model.Type,
            MinBet = model.MinBet,
            MaxBet = model.MaxBet,
            Description = model.Description ?? string.Empty
        };

        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = game.Id }, game.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<GameDTO>> Put(int id, [FromBody] GameInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.Id == id);
        if (game is null) return NotFound();

        game.Name = model.Name;
        game.Type = model.Type;
        game.MinBet = model.MinBet;
        game.MaxBet = model.MaxBet;
        game.Description = model.Description ?? string.Empty;

        await _dbContext.SaveChangesAsync();

        return Ok(game.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.Id == id);
        if (game is null) return NotFound();

        _dbContext.Games.Remove(game);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
