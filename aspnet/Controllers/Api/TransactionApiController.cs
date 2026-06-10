using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers.Api;

[Route("api/transaction")]
[ApiController]
public class TransactionApiController : ControllerBase
{
    private readonly CasinoDbContext _dbContext;

    public TransactionApiController(CasinoDbContext dbContext) => _dbContext = dbContext;

    /// GET /api/transaction?playerId=1&type=Deposit
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionDTO>>> Get(int? playerId, TransactionType? type)
    {
        var query = _dbContext.Transactions.Include(t => t.Player).AsQueryable();

        if (playerId.HasValue) query = query.Where(t => t.PlayerId == playerId.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);

        var transactions = await query.ToListAsync();
        return Ok(transactions.Select(t => t.ToDTO()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionDTO>> Get(int id)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null) return NotFound();

        return Ok(transaction.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TransactionDTO>> Post([FromBody] TransactionInputDTO model)
    {
        if (!await _dbContext.Players.AnyAsync(p => p.Id == model.PlayerId))
        {
            ModelState.AddModelError(nameof(model.PlayerId), "Igrač ne postoji");
            return ValidationProblem(ModelState);
        }

        var transaction = new Transaction
        {
            Amount = model.Amount,
            Type = model.Type,
            CreatedAt = model.CreatedAt,
            PlayerId = model.PlayerId
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(transaction).Reference(t => t.Player).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, transaction.ToDTO());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TransactionDTO>> Put(int id, [FromBody] TransactionInputDTO model)
    {
        if (model.Id != 0 && model.Id != id) return BadRequest();

        var transaction = await _dbContext.Transactions
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null) return NotFound();

        if (!await _dbContext.Players.AnyAsync(p => p.Id == model.PlayerId))
        {
            ModelState.AddModelError(nameof(model.PlayerId), "Igrač ne postoji");
            return ValidationProblem(ModelState);
        }

        transaction.Amount = model.Amount;
        transaction.Type = model.Type;
        transaction.CreatedAt = model.CreatedAt;
        transaction.PlayerId = model.PlayerId;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(transaction).Reference(t => t.Player).LoadAsync();

        return Ok(transaction.ToDTO());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null) return NotFound();

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
