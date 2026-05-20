using aspnet.Models;

namespace aspnet.Repositories;

public interface ITransactionRepository
{
    List<Transaction> GetAll();
    Transaction? GetById(int id);
    void Create(Transaction transaction);
    List<Transaction> Search(string q);
}
