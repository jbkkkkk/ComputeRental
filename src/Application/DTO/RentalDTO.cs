namespace Application.DTO;

public class RentalRequestDto
{
    public int ServerId { get; set; }
}

public class RentalResponseDto
{
    public int RentalId { get; set; }
    public int ServerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReadyAt { get; set; }
    public DateTime? WillBeOffAt { get; set; }
}

public class RentalStatusDto
{
    public int RentalId { get; set; }
    public int ServerId { get; set; }
    public bool IsReady { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReadyAt { get; set; }
    public DateTime? WillBeOffAt { get; set; }
    public int? MinutesUntilOff { get; set; }
}