using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Domain.Entities;
using Application.DTO;
using Application.Interfaces.Repositories;
using Application.Services;

namespace Application.UnitTests;

public class ServerServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IServerRepository> _serverRepoMock;
    private readonly ServerService _service;

    public ServerServiceTests()
    {
        _serverRepoMock = new Mock<IServerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ServerRepository).Returns(_serverRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.RentalRepository).Returns(new Mock<IRentalRepository>().Object);

        var loggerMock = new Mock<ILogger<ServerService>>();
        _service = new ServerService(_unitOfWorkMock.Object, loggerMock.Object);
    }
    
    [Fact]
    public async Task AddServerAsync_ShouldAddServerAndReturnDto()
    {
        //Arrange
        var dto = new ServerCreateDto
        {
            OS = "Ubuntu 22.04",
            RAM = 16,
            Disk = 256,
            CpuCores = 8,
            IsOn = true
        };
        
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);
        
        //Act
        var result = await _service.AddServerAsync(dto);
        
        //Assert
        result.Should().NotBeNull();
        result.OS.Should().Be(dto.OS);
        result.RAM.Should().Be(dto.RAM);
        result.Disk.Should().Be(dto.Disk);
        result.CpuCores.Should().Be(dto.CpuCores);
        result.Status.Should().Be(ServerStatus.Available.ToString());

        _serverRepoMock.Verify(r => r.Add(It.IsAny<Server>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetFreeServersAsync_ShouldReturnFilteredServers()
    {
        //Arrange
        var servers = new List<Server>
        {
            new() { Id = 1, OS = "Ubuntu", RAM = 8, Disk = 100, CpuCores = 4, Status = ServerStatus.Available },
            new() { Id = 2, OS = "Debian", RAM = 16, Disk = 200, CpuCores = 8, Status = ServerStatus.Off },
            new() { Id = 3, OS = "CentOS", RAM = 32, Disk = 500, CpuCores = 16, Status = ServerStatus.Rented }
        };

        _serverRepoMock.Setup(r => r.GetFreeByCriteriaAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(servers.Where(s => s.Status == ServerStatus.Available || s.Status == ServerStatus.Off).ToList());
        
        //Act
        var result = await _service.GetFreeServersAsync(null, null, null, 
            null, null, null);
        
        //Assert
        result.Should().HaveCount(2);
        result.Select(s => s.Id).Should().Contain(new[] { 1, 2 });
    }
}