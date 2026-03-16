namespace Domain.Entities;

public class Rental
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public DateTime RentedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? WillBeOffAt { get; set; }
    public RentalStatus Status { get; set; }
}

public enum RentalStatus
{
    Active,
    Completed
}