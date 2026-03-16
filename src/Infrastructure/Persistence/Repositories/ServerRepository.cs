using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Interfaces.Repositories;
using Infrastructure.Persistence.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ServerRepository> _logger;

    public ServerRepository(ApplicationDbContext dbContext, ILogger<ServerRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Server?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching server by ID {ServerId}", id);
        var server = await _dbContext.Servers
            .Include(s => s.CurrentRental)
            .FirstOrDefaultAsync(s => s.Id == id);
        
        if (server is null)
            _logger.LogDebug("Server with ID {ServerId} not found", id);
        else
            _logger.LogDebug("Server {ServerId} retrieved successfully", id);
        
        return server;
    }
    

    public async Task<ICollection<Server>> GetFreeByCriteriaAsync(string? os, int? minRam, int? maxRam, int? minDisk, int? maxDisk, int? minCpu)
    {
        _logger.LogDebug("Searching servers with criteria: OS={OS}, MinRAM={MinRam}, MaxRAM={MaxRam}, MinDisk={MinDisk}, MaxDisk={MaxDisk}, MinCPU={MinCpu}",
            os, minRam, maxRam, minDisk, maxDisk, minCpu);
        var query = _dbContext.Servers
            .Where(s => s.Status == ServerStatus.Available || s.Status == ServerStatus.Off);
        if (!string.IsNullOrEmpty(os))
            query = query.Where(s => s.OS.Contains(os));
        if (minRam.HasValue)
            query = query.Where(s => s.RAM >= minRam);
        if (maxRam.HasValue)
            query = query.Where(s => s.RAM <= maxRam);
        if (minDisk.HasValue)
            query = query.Where(s => s.Disk >= minDisk);
        if (maxDisk.HasValue)
            query = query.Where(s => s.Disk <= maxDisk);
        if (minCpu.HasValue)
            query = query.Where(s => s.CpuCores >= minCpu);
        var servers = await query.ToListAsync();
        _logger.LogDebug("Found {Count} servers matching criteria", servers.Count);
        return servers;
    }

    public void Add(Server server)
    {
        _logger.LogDebug("Adding new server with OS {OS}, RAM {Ram}GB, Disk {Disk}GB, CPU {Cpu} cores", 
            server.OS, server.RAM, server.Disk, server.CpuCores);
        _dbContext.Servers.Add(server);
    }

    public void Update(Server server)
    {
        _logger.LogDebug("Updating server ID {ServerId}", server.Id);
        _dbContext.Entry(server).State = EntityState.Modified;
    }
}