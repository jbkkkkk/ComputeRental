namespace Domain.Entities;

public class Server
{
    public int Id { get; set; }
    public string OS { get; set; } = string.Empty;
    public int RAM { get; set; }
    public int Disk { get; set; }
    public int CpuCores { get; set; }
    public ServerStatus Status { get; set; }
    public uint RowVersion { get; set; }
    public int? CurrentRentalId { get; set; }
    public Rental? CurrentRental { get; set; } 
}

public enum ServerStatus
{
    Available,
    TurningOn,
    Rented,
    Off
}