using Microsoft.AspNetCore.Mvc;
using Application.DTO;
using Application.Interfaces.Services;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;
    private readonly ILogger<ServersController> _logger;

    public ServersController(IServerService serverService, ILogger<ServersController> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServerDto>>> GetFreeServers(
        [FromQuery] string? os,
        [FromQuery] int? minRam, [FromQuery] int? maxRam,
        [FromQuery] int? minDisk, [FromQuery] int? maxDisk,
        [FromQuery] int? minCpu)
    {
        var result = await _serverService.GetFreeServersAsync(os, minRam, maxRam, minDisk, maxDisk, minCpu);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ServerDto>> AddServer(ServerCreateDto dto)
    {
        var result = await _serverService.AddServerAsync(dto);
        return CreatedAtAction(nameof(GetFreeServers), new { id = result.Id }, result);
    }
}