using Application.DTO;

namespace Application.Interfaces.Services;

public interface IRentalService
{
    Task<RentalResponseDto> RentServerAsync(int serverId);
    Task<RentalStatusDto> GetRentalStatusAsync(int rentalId);
    Task ReleaseServerAsync(int rentalId);
}