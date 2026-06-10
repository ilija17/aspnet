using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/casino")]
[ApiController]
public class CasinoApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public CasinoApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/casino?q=vegas&foundedAfter=2010-01-01
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CasinoDTO>>> Get(string? q, DateTime? foundedAfter)
    {
        var query = _dbContext.Casinos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.Name.Contains(q) ||
                c.Address.Contains(q) ||
                c.LicenseNumber.Contains(q));
        }

        if (foundedAfter.HasValue)
        {
            query = query.Where(c => c.FoundedDate >= foundedAfter.Value);
        }

        var casinos = await query.ToListAsync();
        return Ok(casinos.Select(c => c.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CasinoDTO>> Get(int id)
    {
        var casino = await _dbContext.Casinos.FirstOrDefaultAsync(c => c.Id == id);
        if (casino is null) return NotFound();

        return Ok(casino.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CasinoDTO>> Post([FromBody] CasinoInputDTO model)
    {
        var casino = new Casino
        {
            Name = model.Name,
            Address = model.Address,
            LicenseNumber = model.LicenseNumber,
            FoundedDate = model.FoundedDate
        };

        _dbContext.Casinos.Add(casino);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = casino.Id }, casino.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CasinoDTO>> Put(int id, [FromBody] CasinoInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var casino = await _dbContext.Casinos.FirstOrDefaultAsync(c => c.Id == id);
        if (casino is null) return NotFound();

        casino.Name = model.Name;
        casino.Address = model.Address;
        casino.LicenseNumber = model.LicenseNumber;
        casino.FoundedDate = model.FoundedDate;

        await _dbContext.SaveChangesAsync();

        return Ok(casino.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var casino = await _dbContext.Casinos.FirstOrDefaultAsync(c => c.Id == id);
        if (casino is null) return NotFound();

        _dbContext.Casinos.Remove(casino);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
