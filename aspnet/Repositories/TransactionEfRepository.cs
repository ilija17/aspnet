using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Repositories;

public class TransactionEfRepository : ITransactionRepository
{
    private readonly CasinoDbContext _db;

    public TransactionEfRepository(CasinoDbContext db) => _db = db;

    public List<Transaction> GetAll() =>
        _db.Transactions
           .Include(t => t.Player)
           .OrderByDescending(t => t.CreatedAt)
           .ToList();

    public Transaction? GetById(int id) =>
        _db.Transactions
           .Include(t => t.Player)
           .FirstOrDefault(t => t.Id == id);
}
