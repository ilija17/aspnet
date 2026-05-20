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

    public void Create(Transaction transaction)
    {
        _db.Transactions.Add(transaction);
        _db.SaveChanges();
    }

    public List<Transaction> Search(string q) =>
        _db.Transactions
           .Include(t => t.Player)
           .Where(t => t.Player.FirstName.Contains(q) || t.Player.LastName.Contains(q))
           .OrderByDescending(t => t.CreatedAt)
           .Take(20)
           .ToList();
}
