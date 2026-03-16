using Microsoft.EntityFrameworkCore.Storage;
using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    
    public IServerRepository ServerRepository { get; }
    public IRentalRepository RentalRepository { get; }

    public UnitOfWork(ApplicationDbContext context, IServerRepository serverRepository, 
        IRentalRepository rentalRepository, ILogger<UnitOfWork> logger)
    {
        _context = context;
        ServerRepository = serverRepository;
        RentalRepository = rentalRepository;
        _logger = logger;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            var result = await _context.SaveChangesAsync();
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes");
            throw;
        }
    }

    public async Task BeginTransactionAsync()
    {
        _logger.LogDebug("Beginning database transaction");
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            _logger.LogDebug("Transaction committed");
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            _logger.LogDebug("Transaction rolled back");
        }
            
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}