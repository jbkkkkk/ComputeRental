using Application.DTO;

namespace Application.Interfaces.Services;

public interface IServerService
{
    Task<ServerDto> AddServerAsync(ServerCreateDto dto);
    Task<ICollection<ServerDto>> GetFreeServersAsync(string? os, int? minRam, int? maxRam, int? minDisk, int? maxDisk, int? minCpu);
}