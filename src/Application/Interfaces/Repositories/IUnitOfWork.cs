namespace Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IServerRepository ServerRepository { get; }
    IRentalRepository RentalRepository { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}