using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IServerRepository
{
    Task<Server?> GetByIdAsync(int id);
    Task<ICollection<Server>> GetFreeByCriteriaAsync(string? os, int? minRam, int? maxRam, int? minDisk, 
        int? maxDisk, int? minCpu);
    void Add(Server server);
    void Update(Server server);
}