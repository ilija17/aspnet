using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/employee")]
[ApiController]
public class EmployeeApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public EmployeeApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/employee?q=luka&casinoId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDTO>>> Get(string? q, int? casinoId)
    {
        var query = _dbContext.Employees.Include(e => e.Casino).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e =>
                e.FirstName.Contains(q) ||
                e.LastName.Contains(q) ||
                e.Position.Contains(q));
        }

        if (casinoId.HasValue)
        {
            query = query.Where(e => e.CasinoId == casinoId.Value);
        }

        var employees = await query.ToListAsync();
        return Ok(employees.Select(e => e.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDTO>> Get(int id)
    {
        var employee = await _dbContext.Employees
            .Include(e => e.Casino)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return NotFound();

        return Ok(employee.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EmployeeDTO>> Post([FromBody] EmployeeInputDTO model)
    {
        if (!await _dbContext.Casinos.AnyAsync(c => c.Id == model.CasinoId))
        {
            ModelState.AddModelError(nameof(model.CasinoId), "Casino ne postoji");
            return ValidationProblem(ModelState);
        }

        var employee = new Employee
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Position = model.Position,
            CasinoId = model.CasinoId
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(employee).Reference(e => e.Casino).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = employee.Id }, employee.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EmployeeDTO>> Put(int id, [FromBody] EmployeeInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var employee = await _dbContext.Employees
            .Include(e => e.Casino)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return NotFound();

        if (!await _dbContext.Casinos.AnyAsync(c => c.Id == model.CasinoId))
        {
            ModelState.AddModelError(nameof(model.CasinoId), "Casino ne postoji");
            return ValidationProblem(ModelState);
        }

        employee.FirstName = model.FirstName;
        employee.LastName = model.LastName;
        employee.Position = model.Position;
        employee.CasinoId = model.CasinoId;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(employee).Reference(e => e.Casino).LoadAsync();

        return Ok(employee.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return NotFound();

        _dbContext.Employees.Remove(employee);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
