using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services;

public class AutoShutdownService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoShutdownService> _logger;

    public AutoShutdownService(IServiceScopeFactory scopeFactory, ILogger<AutoShutdownService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoShutdownService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                await ShutdownExpiredRentals(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoShutdownService main loop.");
            }
        }

        _logger.LogInformation("AutoShutdownService stopped.");
    }
    
    private async Task ShutdownExpiredRentals(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var expiredRentals = await unitOfWork.RentalRepository.GetExpiredActiveRentalsAsync(now);

        foreach (var rental in expiredRentals)
        {
            try
            {
                await unitOfWork.BeginTransactionAsync();

                var server = rental.Server;

                if (server.Status != Domain.Entities.ServerStatus.Rented || 
                    rental.Status != Domain.Entities.RentalStatus.Active)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    continue;
                }

                server.Status = Domain.Entities.ServerStatus.Off;
                server.CurrentRentalId = null;
                rental.Status = Domain.Entities.RentalStatus.Completed;

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Server {ServerId} automatically turned off (rental {RentalId})", 
                    server.Id, rental.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogWarning(
                    "Concurrency conflict while turning off rental {RentalId}. Skipping.", 
                    rental.Id);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error turning off rental {RentalId}", rental.Id);
            }
        }
    }
}