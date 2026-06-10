using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/reservation")]
[ApiController]
public class ReservationApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public ReservationApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/reservation?playerId=1&tableId=2&from=2024-04-01
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDTO>>> Get(int? playerId, int? tableId, DateTime? from)
    {
        var query = _dbContext.Reservations
            .Include(r => r.Player)
            .Include(r => r.Table).ThenInclude(t => t.Casino)
            .Include(r => r.Table).ThenInclude(t => t.Game)
            .AsQueryable();

        if (playerId.HasValue) query = query.Where(r => r.PlayerId == playerId.Value);
        if (tableId.HasValue) query = query.Where(r => r.TableId == tableId.Value);
        if (from.HasValue) query = query.Where(r => r.ReservedAt >= from.Value);

        var reservations = await query.ToListAsync();
        return Ok(reservations.Select(r => r.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDTO>> Get(int id)
    {
        var reservation = await _dbContext.Reservations
            .Include(r => r.Player)
            .Include(r => r.Table).ThenInclude(t => t.Casino)
            .Include(r => r.Table).ThenInclude(t => t.Game)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null) return NotFound();

        return Ok(reservation.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ReservationDTO>> Post([FromBody] ReservationInputDTO model)
    {
        if (!await _dbContext.Players.AnyAsync(p => p.Id == model.PlayerId))
        {
            ModelState.AddModelError(nameof(model.PlayerId), "Igrač ne postoji");
        }
        if (!await _dbContext.Tables.AnyAsync(t => t.Id == model.TableId))
        {
            ModelState.AddModelError(nameof(model.TableId), "Stol ne postoji");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var reservation = new Reservation
        {
            ReservedAt = model.ReservedAt,
            PlayerId = model.PlayerId,
            TableId = model.TableId
        };

        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(reservation).Reference(r => r.Player).LoadAsync();
        await _dbContext.Entry(reservation).Reference(r => r.Table).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ReservationDTO>> Put(int id, [FromBody] ReservationInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var reservation = await _dbContext.Reservations
            .Include(r => r.Player)
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null) return NotFound();

        if (!await _dbContext.Players.AnyAsync(p => p.Id == model.PlayerId))
        {
            ModelState.AddModelError(nameof(model.PlayerId), "Igrač ne postoji");
        }
        if (!await _dbContext.Tables.AnyAsync(t => t.Id == model.TableId))
        {
            ModelState.AddModelError(nameof(model.TableId), "Stol ne postoji");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        reservation.ReservedAt = model.ReservedAt;
        reservation.PlayerId = model.PlayerId;
        reservation.TableId = model.TableId;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(reservation).Reference(r => r.Player).LoadAsync();
        await _dbContext.Entry(reservation).Reference(r => r.Table).LoadAsync();

        return Ok(reservation.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation is null) return NotFound();

        _dbContext.Reservations.Remove(reservation);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
