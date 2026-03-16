using Application.DTO;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ServerService : IServerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ServerService> _logger;
    
    public ServerService(IUnitOfWork unitOfWork, ILogger<ServerService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<ServerDto> AddServerAsync(ServerCreateDto dto)
    {
        _logger.LogInformation("Adding new server: OS={OS}, RAM={Ram}GB, Disk={Disk}GB, Cores={Cores}, IsOn={IsOn}",
            dto.OS, dto.RAM, dto.Disk, dto.CpuCores, dto.IsOn);
        
        var server = new Server
        {
            OS = dto.OS,
            RAM = dto.RAM,
            Disk = dto.Disk,
            CpuCores = dto.CpuCores,
            Status = dto.IsOn ? ServerStatus.Available : ServerStatus.Off
        };

        _unitOfWork.ServerRepository.Add(server);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Server added successfully with ID {ServerId}", server.Id);
        
        return new ServerDto
        {
            Id = server.Id,
            OS = server.OS,
            RAM = server.RAM,
            Disk = server.Disk,
            CpuCores = server.CpuCores,
            Status = server.Status.ToString(),
            EstimatedReadyMinutes = server.Status == ServerStatus.Off ? 5 : 0
        };
    }

    public async Task<ICollection<ServerDto>> GetFreeServersAsync(string? os, int? minRam, int? maxRam, int? minDisk, int? maxDisk, int? minCpu)
    {
        _logger.LogInformation("Searching free servers with filters: OS={OS}, MinRAM={MinRam}, MaxRAM={MaxRam}, MinDisk={MinDisk}, MaxDisk={MaxDisk}, MinCPU={MinCpu}",
            os, minRam, maxRam, minDisk, maxDisk, minCpu);
        
        var servers = await _unitOfWork.ServerRepository.GetFreeByCriteriaAsync(os, minRam, maxRam, minDisk, maxDisk, minCpu);
        
        _logger.LogInformation("Found {Count} free servers matching criteria", servers.Count);
        
        return servers.Select(s => new ServerDto
        {
            Id = s.Id,
            OS = s.OS,
            RAM = s.RAM,
            Disk = s.Disk,
            CpuCores = s.CpuCores,
            Status = s.Status.ToString(),
            EstimatedReadyMinutes = s.Status == ServerStatus.Off ? 5 : 0
        }).ToList();
    }
}