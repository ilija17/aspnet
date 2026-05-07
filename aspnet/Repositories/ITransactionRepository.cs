using aspnet.Models;

namespace aspnet.Repositories;

public interface ITransactionRepository
{
    List<Transaction> GetAll();
    Transaction? GetById(int id);
}
