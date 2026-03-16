using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using FluentAssertions;


namespace Application.UnitTests;

public class RentalServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IServerRepository> _serverRepoMock;
    private readonly Mock<IRentalRepository> _rentalRepoMock;
    private readonly RentalService _service;
    
    public RentalServiceTests()
    {
        _serverRepoMock = new Mock<IServerRepository>();
        _rentalRepoMock = new Mock<IRentalRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ServerRepository).Returns(_serverRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.RentalRepository).Returns(_rentalRepoMock.Object);

        var loggerMock = new Mock<ILogger<RentalService>>();
        _service = new RentalService(_unitOfWorkMock.Object, loggerMock.Object);
    }
    
    [Fact]
    public async Task RentServerAsync_WhenServerAvailable_ShouldCreateRentalAndSetRented()
    {
        // Arrange
        var server = new Server { Id = 1, Status = ServerStatus.Available };
        _serverRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        Rental? addedRental = null;
        _rentalRepoMock.Setup(r => r.Add(It.IsAny<Rental>()))
            .Callback<Rental>(r => addedRental = r);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1)
            .Callback(() =>
            {
                if (addedRental != null)
                    addedRental.Id = 100;
            });

        // Act
        var result = await _service.RentServerAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RentalId.Should().Be(100);
        result.ServerId.Should().Be(1);
        result.Status.Should().Be(ServerStatus.Rented.ToString());
        result.ReadyAt.Should().NotBeNull();
        result.WillBeOffAt.Should().NotBeNull();

        server.Status.Should().Be(ServerStatus.Rented);
        _rentalRepoMock.Verify(r => r.Add(It.IsAny<Rental>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
    
    [Fact]
    public async Task RentServerAsync_WhenServerOff_ShouldSetTurningOn()
    {
        // Arrange
        var server = new Server { Id = 1, Status = ServerStatus.Off };
        _serverRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        Rental? addedRental = null;
        _rentalRepoMock.Setup(r => r.Add(It.IsAny<Rental>()))
            .Callback<Rental>(r => addedRental = r);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1)
            .Callback(() => addedRental!.Id = 100);

        // Act
        var result = await _service.RentServerAsync(1);

        // Assert
        result.Status.Should().Be(ServerStatus.TurningOn.ToString());
        result.ReadyAt.Should().BeNull();
        result.WillBeOffAt.Should().BeNull();

        server.Status.Should().Be(ServerStatus.TurningOn);
    }
    
    [Fact]
    public async Task RentServerAsync_WhenServerNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _serverRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Server?)null);

        // Act
        Func<Task> act = async () => await _service.RentServerAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Server (id = 999) not found");
    }
    
    [Fact]
    public async Task RentServerAsync_WhenServerNotAvailable_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var server = new Server { Id = 1, Status = ServerStatus.Rented };
        _serverRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        // Act
        Func<Task> act = async () => await _service.RentServerAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Server (id = 1) is not available for rent");
    }
    
    [Fact]
    public async Task RentServerAsync_WhenConcurrencyConflict_ShouldThrow()
    {
        // Arrange
        var server = new Server { Id = 1, Status = ServerStatus.Available };
        _serverRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(server);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        Func<Task> act = async () => await _service.RentServerAsync(1);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetRentalStatusAsync_WhenTurningOnNotComplete_ShouldReturnNotReady()
    {
        // Arrange
        var rental = new Rental
        {
            Id = 1,
            ServerId = 1,
            RentedAt = DateTime.UtcNow.AddMinutes(-2),
            Status = RentalStatus.Active,
            Server = new Server { Id = 1, Status = ServerStatus.TurningOn }
        };

        _rentalRepoMock.Setup(r => r.GetByIdWithServerAsync(1))
            .ReturnsAsync(rental);

        // Act
        var result = await _service.GetRentalStatusAsync(1);

        // Assert
        result.IsReady.Should().BeFalse();
        result.Status.Should().Be(ServerStatus.TurningOn.ToString());
        result.ReadyAt.Should().BeCloseTo(rental.RentedAt.AddMinutes(5), TimeSpan.FromSeconds(1));
        result.MinutesUntilOff.Should().BeNull();
    }
    
    [Fact]
    public async Task GetRentalStatusAsync_WhenTurningOnComplete_ShouldSetRented()
    {
        // Arrange
        var rental = new Rental
        {
            Id = 1,
            ServerId = 1,
            RentedAt = DateTime.UtcNow.AddMinutes(-6),
            Status = RentalStatus.Active,
            Server = new Server { Id = 1, Status = ServerStatus.TurningOn }
        };

        _rentalRepoMock.Setup(r => r.GetByIdWithServerAsync(1))
            .ReturnsAsync(rental);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetRentalStatusAsync(1);

        // Assert
        result.IsReady.Should().BeTrue();
        result.Status.Should().Be(ServerStatus.Rented.ToString());
        result.WillBeOffAt.Should().NotBeNull();

        rental.Server.Status.Should().Be(ServerStatus.Rented);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
    
    [Fact]
    public async Task ReleaseServerAsync_ShouldSetServerOffAndCompleteRental()
    {
        // Arrange
        var rental = new Rental
        {
            Id = 1,
            ServerId = 1,
            Status = RentalStatus.Active,
            Server = new Server { Id = 1, Status = ServerStatus.Rented }
        };

        _rentalRepoMock.Setup(r => r.GetActiveByIdAsync(1))
            .ReturnsAsync(rental);

        // Act
        await _service.ReleaseServerAsync(1);

        // Assert
        rental.Server.Status.Should().Be(ServerStatus.Off);
        rental.Server.CurrentRentalId.Should().BeNull();
        rental.Status.Should().Be(RentalStatus.Completed);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }
    
    [Fact]
    public async Task ReleaseServerAsync_WhenRentalNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _rentalRepoMock.Setup(r => r.GetActiveByIdAsync(999))
            .ReturnsAsync((Rental?)null);

        // Act
        Func<Task> act = async () => await _service.ReleaseServerAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Active rental (id = 999) not found");
    }
}