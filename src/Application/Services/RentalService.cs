using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services;

public class RentalService : IRentalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RentalService> _logger;

    public RentalService(IUnitOfWork unitOfWork, ILogger<RentalService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<RentalResponseDto> RentServerAsync(int serverId)
    {
        _logger.LogInformation("Attempting to rent server ID {ServerId}", serverId);
        
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var server = await _unitOfWork.ServerRepository.GetByIdAsync(serverId);
            if (server is null)
            {
                _logger.LogWarning("Rent failed: server ID {ServerId} not found", serverId);
                throw new NotFoundException($"Server (id = {serverId}) not found");
            }

            if (server.Status != ServerStatus.Available && server.Status != ServerStatus.Off)
            {
                _logger.LogWarning("Rent failed: server ID {ServerId} is not available (current status: {Status})", 
                    serverId, server.Status);
                throw new InvalidOperationException($"Server (id = {serverId}) is not available for rent");
            }

            var rental = new Rental
            {
                ServerId = server.Id,
                RentedAt = DateTime.UtcNow,
                Status = RentalStatus.Active
            };

            if (server.Status == ServerStatus.Available)
            {
                rental.ReadyAt = DateTime.UtcNow;
                rental.WillBeOffAt = DateTime.UtcNow.AddMinutes(20);
                server.Status = ServerStatus.Rented;
                _logger.LogInformation("Server ID {ServerId} is already on, rented immediately", serverId);
            }
            else
            {
                server.Status = ServerStatus.TurningOn;
                _logger.LogInformation("Server ID {ServerId} is off, will be turning on for 5 minutes", serverId);
            }
            
            _unitOfWork.RentalRepository.Add(rental);
            server.CurrentRental = rental;
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            _logger.LogInformation("Server ID {ServerId} rented successfully, rental ID {RentalId}", serverId, rental.Id);

            return new RentalResponseDto
            {
                RentalId = rental.Id,
                ServerId = server.Id,
                Status = server.Status.ToString(),
                ReadyAt = rental.ReadyAt,
                WillBeOffAt = rental.WillBeOffAt
            };
        }

        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Concurrency conflict while renting server ID {ServerId}", serverId);
            throw;
        }

        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Unexpected error while renting server ID {ServerId}", serverId);
            throw;
        }
    }

    public async Task<RentalStatusDto> GetRentalStatusAsync(int rentalId)
    {
        _logger.LogInformation("Checking status for rental ID {RentalId}", rentalId);
        
        var rental = await _unitOfWork.RentalRepository.GetByIdWithServerAsync(rentalId);
        if (rental is null)
        {
            _logger.LogWarning("Rental ID {RentalId} not found", rentalId);
            throw new NotFoundException($"Rental (id = {rentalId}) not found");
        }

        if (rental.Status != RentalStatus.Active)
        {
            _logger.LogInformation("Rental ID {RentalId} is already completed", rentalId);
            return new RentalStatusDto
            {
                RentalId = rental.Id,
                ServerId = rental.ServerId,
                IsReady = false,
                Status = "Completed"
            };
        }
        
        var server = rental.Server;

        if (server.Status == ServerStatus.TurningOn)
        {
            var elapsed = DateTime.UtcNow - rental.RentedAt;
            if (elapsed.TotalMinutes >= 5)
            {
                _logger.LogInformation("Server ID {ServerId} turning on completed, now rented", server.Id);
                
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    server.Status = ServerStatus.Rented;
                    rental.ReadyAt = DateTime.UtcNow;
                    rental.WillBeOffAt = DateTime.UtcNow.AddMinutes(20);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error while completing turn-on for rental ID {RentalId}", rentalId);
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Server ID {ServerId} still turning on.", server.Id);
                return new RentalStatusDto
                {
                    RentalId = rental.Id,
                    ServerId = server.Id,
                    IsReady = false,
                    Status = server.Status.ToString(),
                    ReadyAt = rental.RentedAt.AddMinutes(5),
                    MinutesUntilOff = null
                };
            }
        }

        if (server.Status == ServerStatus.Rented && rental.WillBeOffAt.HasValue)
        {
            var minutesUntilOff = (int)(rental.WillBeOffAt.Value - DateTime.UtcNow).TotalMinutes;
            if (minutesUntilOff < 0) 
                minutesUntilOff = 0;
            
            _logger.LogInformation("Server ID {ServerId} is rented, will be off in {MinutesUntilOff} minutes", 
                server.Id, minutesUntilOff);
            
            return new RentalStatusDto
            {
                RentalId = rental.Id,
                ServerId = server.Id,
                IsReady = true,
                Status = server.Status.ToString(),
                ReadyAt = rental.ReadyAt,
                WillBeOffAt = rental.WillBeOffAt,
                MinutesUntilOff = minutesUntilOff
            };
        }
        
        _logger.LogWarning("Unexpected state for rental ID {RentalId}: server status {ServerStatus}, rental status {RentalStatus}", 
            rentalId, server.Status, rental.Status);
        
        return new RentalStatusDto
        {
            RentalId = rental.Id,
            ServerId = server.Id,
            IsReady = false,
            Status = server.Status.ToString()
        };
    }

    public async Task ReleaseServerAsync(int rentalId)
    {
        _logger.LogInformation("Releasing server for rental ID {RentalId}", rentalId);
        
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var rental = await _unitOfWork.RentalRepository.GetActiveByIdAsync(rentalId);
            if (rental is null)
            {
                _logger.LogWarning("Release failed: active rental ID {RentalId} not found", rentalId);
                throw new NotFoundException($"Active rental (id = {rentalId}) not found");
            }

            var server = rental.Server;

            server.Status = ServerStatus.Off;
            server.CurrentRentalId = null;
            rental.Status = RentalStatus.Completed;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Concurrency conflict while releasing rental ID {RentalId}", rentalId);
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Unexpected error while releasing rental ID {RentalId}", rentalId);
            throw;
        }
    }
}