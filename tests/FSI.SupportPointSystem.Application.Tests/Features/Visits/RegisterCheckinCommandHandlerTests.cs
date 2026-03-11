using FluentAssertions;
using FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckin;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace FSI.SupportPointSystem.Application.Tests.Features.Visits;

public sealed class RegisterCheckinCommandHandlerTests
{
    private readonly Mock<IVisitRepository> _visitRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ISellerRepository> _sellerRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RegisterCheckinCommandHandler _sut;

    // Cliente posicionado em coordenadas conhecidas
    private static readonly Customer ValidCustomer =
        Customer.Create("Padaria Silva", "11222333000181", -23.550520m, -46.633308m);

    // Vendedor ativo
    private static readonly Seller ActiveSeller = BuildActiveSeller();

    public RegisterCheckinCommandHandlerTests()
    {
        _sut = new RegisterCheckinCommandHandler(
            _visitRepo.Object,
            _customerRepo.Object,
            _sellerRepo.Object,
            _unitOfWork.Object);
    }

    // -------------------------------------------------------
    // Cenário de sucesso
    // -------------------------------------------------------
    [Fact]
    public async Task Handle_ValidCheckin_ShouldReturnSuccess()
    {
        var sellerId = ActiveSeller.Id;
        ArrangeValidState(sellerId, hasActiveVisit: false);

        var command = new RegisterCheckinCommand(
            SellerId: sellerId,
            CustomerId: ValidCustomer.Id,
            Latitude: -23.550600m,  // dentro do raio de 100m
            Longitude: -46.633400m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SellerId.Should().Be(sellerId);
        result.Value.Message.Should().Contain("sucesso");
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------
    // Cenário: check-in fora do raio -> OUTSIDE_RADIUS
    // -------------------------------------------------------
    [Fact]
    public async Task Handle_OutsideRadius_ShouldReturnFailure()
    {
        var sellerId = ActiveSeller.Id;
        ArrangeValidState(sellerId, hasActiveVisit: false);

        var command = new RegisterCheckinCommand(
            SellerId: sellerId,
            CustomerId: ValidCustomer.Id,
            Latitude: -23.555m,    // ~500m - fora do raio
            Longitude: -46.638m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OUTSIDE_RADIUS");
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------
    // Cenário: vendedor já tem check-in ativo -> CONFLICT
    // -------------------------------------------------------
    [Fact]
    public async Task Handle_SellerHasActiveVisit_ShouldReturnConflict()
    {
        var sellerId = ActiveSeller.Id;
        ArrangeValidState(sellerId, hasActiveVisit: true);

        var command = new RegisterCheckinCommand(
            SellerId: sellerId,
            CustomerId: ValidCustomer.Id,
            Latitude: -23.550600m,
            Longitude: -46.633400m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CONFLICT_CHECKIN");
    }

    // -------------------------------------------------------
    // Cenário: vendedor não encontrado
    // -------------------------------------------------------
    [Fact]
    public async Task Handle_SellerNotFound_ShouldReturnFailure()
    {
        _sellerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Seller?)null);

        var command = new RegisterCheckinCommand(
            SellerId: Guid.NewGuid(),
            CustomerId: ValidCustomer.Id,
            Latitude: -23.550600m,
            Longitude: -46.633400m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SELLER_NOT_FOUND");
    }

    // -------------------------------------------------------
    // Cenário: cliente não encontrado
    // -------------------------------------------------------
    [Fact]
    public async Task Handle_CustomerNotFound_ShouldReturnFailure()
    {
        var sellerId = ActiveSeller.Id;
        _sellerRepo.Setup(r => r.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveSeller);
        _customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new RegisterCheckinCommand(
            SellerId: sellerId,
            CustomerId: Guid.NewGuid(),
            Latitude: -23.550600m,
            Longitude: -46.633400m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CUSTOMER_NOT_FOUND");
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private void ArrangeValidState(Guid sellerId, bool hasActiveVisit)
    {
        _sellerRepo.Setup(r => r.GetByIdAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveSeller);
        _customerRepo.Setup(r => r.GetByIdAsync(ValidCustomer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidCustomer);
        _visitRepo.Setup(r => r.HasActiveVisitAsync(sellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasActiveVisit);
        _visitRepo.Setup(r => r.AddAsync(It.IsAny<Visit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static Seller BuildActiveSeller()
    {
        var user = User.CreateSeller(
            Cpf.Create("52998224725"),
            "hash_qualquer");
        return Seller.Create(user, "Carlos Vendedor", "11999999999", null);
    }
}
