namespace Application.DTO;

public class ServerDto
{
    public int Id { get; set; }
    public string OS { get; set; } = string.Empty;
    public int RAM { get; set; }
    public int Disk { get; set; }
    public int CpuCores { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EstimatedReadyMinutes { get; set; }
}

public class ServerCreateDto
{
    public string OS { get; set; } = string.Empty;
    public int RAM { get; set; }
    public int Disk { get; set; }
    public int CpuCores { get; set; }
    public bool IsOn { get; set; } = false;
}