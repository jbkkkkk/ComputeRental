using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class RentalRepository : IRentalRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RentalRepository> _logger;

    public RentalRepository(ApplicationDbContext dbContext, ILogger<RentalRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Rental?> GetActiveByIdAsync(int id)
    {
        _logger.LogDebug("Fetching active rental {RentalId} with server details", id);
        var rental = await _dbContext.Rentals.Include(r => r.Server)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == RentalStatus.Active);
        
        if (rental is null)
            _logger.LogDebug("Active rental {RentalId} not found", id);
        else
            _logger.LogDebug("Active rental {RentalId} retrieved", id);

        return rental;
    }


    public async Task<Rental?> GetByIdWithServerAsync(int id)
    {
        _logger.LogDebug("Fetching rental {RentalId} with server details", id);
       var rental = await _dbContext.Rentals.Include(r => r.Server)
            .FirstOrDefaultAsync(r => r.Id == id);
       if (rental is null)
           _logger.LogDebug("Rental {RentalId} not found", id);
       else
           _logger.LogDebug("Rental {RentalId} retrieved successfully", id);

       return rental;
    }


    public async Task<ICollection<Rental>> GetExpiredActiveRentalsAsync(DateTime now)
    {
        _logger.LogDebug("Checking for expired active rentals as of {Now}", now);
        var rentals = await _dbContext.Rentals
            .Include(r => r.Server)
            .Where(r => r.Status == RentalStatus.Active
            && r.Server.Status == ServerStatus.Rented
            && r.WillBeOffAt <= now)
            .ToListAsync();
        
        _logger.LogDebug("Found {Count} expired rentals", rentals.Count);
        return rentals;
    }
    
    public void Add(Rental rental)
    {
        _logger.LogDebug("Adding new rental for server ID {ServerId}", rental.ServerId);
        _dbContext.Rentals.Add(rental);
    }

    public void Update(Rental rental)
    {
        _logger.LogDebug("Updating rental ID {RentalId}", rental.Id);
        _dbContext.Entry(rental).State = EntityState.Modified;
    }
    
}