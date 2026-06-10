using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/table")]
[ApiController]
public class TableApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public TableApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/table?casinoId=1&gameId=2&available=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDTO>>> Get(int? casinoId, int? gameId, bool? available)
    {
        var query = _dbContext.Tables
            .Include(t => t.Casino)
            .Include(t => t.Game)
            .AsQueryable();

        if (casinoId.HasValue) query = query.Where(t => t.CasinoId == casinoId.Value);
        if (gameId.HasValue) query = query.Where(t => t.GameId == gameId.Value);
        if (available.HasValue) query = query.Where(t => t.IsAvailable == available.Value);

        var tables = await query.ToListAsync();
        return Ok(tables.Select(t => t.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableDTO>> Get(int id)
    {
        var table = await _dbContext.Tables
            .Include(t => t.Casino)
            .Include(t => t.Game)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table is null) return NotFound();

        return Ok(table.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TableDTO>> Post([FromBody] TableInputDTO model)
    {
        if (!await _dbContext.Casinos.AnyAsync(c => c.Id == model.CasinoId))
        {
            ModelState.AddModelError(nameof(model.CasinoId), "Casino ne postoji");
        }
        if (!await _dbContext.Games.AnyAsync(g => g.Id == model.GameId))
        {
            ModelState.AddModelError(nameof(model.GameId), "Igra ne postoji");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var table = new Table
        {
            TableNumber = model.TableNumber,
            IsAvailable = model.IsAvailable,
            MinBet = model.MinBet,
            MaxBet = model.MaxBet,
            CasinoId = model.CasinoId,
            GameId = model.GameId
        };

        _dbContext.Tables.Add(table);
        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(table).Reference(t => t.Casino).LoadAsync();
        await _dbContext.Entry(table).Reference(t => t.Game).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = table.Id }, table.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TableDTO>> Put(int id, [FromBody] TableInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var table = await _dbContext.Tables
            .Include(t => t.Casino)
            .Include(t => t.Game)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table is null) return NotFound();

        if (!await _dbContext.Casinos.AnyAsync(c => c.Id == model.CasinoId))
        {
            ModelState.AddModelError(nameof(model.CasinoId), "Casino ne postoji");
        }
        if (!await _dbContext.Games.AnyAsync(g => g.Id == model.GameId))
        {
            ModelState.AddModelError(nameof(model.GameId), "Igra ne postoji");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        table.TableNumber = model.TableNumber;
        table.IsAvailable = model.IsAvailable;
        table.MinBet = model.MinBet;
        table.MaxBet = model.MaxBet;
        table.CasinoId = model.CasinoId;
        table.GameId = model.GameId;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(table).Reference(t => t.Casino).LoadAsync();
        await _dbContext.Entry(table).Reference(t => t.Game).LoadAsync();

        return Ok(table.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var table = await _dbContext.Tables.FirstOrDefaultAsync(t => t.Id == id);
        if (table is null) return NotFound();

        _dbContext.Tables.Remove(table);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
