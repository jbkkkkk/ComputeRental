using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IRentalRepository
{
    Task<Rental?> GetActiveByIdAsync(int id);
    Task<Rental?> GetByIdWithServerAsync(int id);
    Task<ICollection<Rental>> GetExpiredActiveRentalsAsync(DateTime now);
    void Add(Rental rental);
    void Update(Rental rental);
}