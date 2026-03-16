using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces.Services;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _rentalService;
    private readonly ILogger<RentalsController> _logger;

    public RentalsController(IRentalService rentalService, ILogger<RentalsController> logger)
    {
        _rentalService = rentalService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<RentalResponseDto>> RentServer(RentalRequestDto dto)
    {
        try
        {
            var result = await _rentalService.RentServerAsync(dto.ServerId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Server was modified by another request. Please retry.");
        }
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<RentalStatusDto>> GetRentalStatus(int id)
    {
        try
        {
            var result = await _rentalService.GetRentalStatusAsync(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id}/release")]
    public async Task<IActionResult> ReleaseServer(int id)
    {
        try
        {
            await _rentalService.ReleaseServerAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Server was modified concurrently. Please retry.");
        }
    }
}