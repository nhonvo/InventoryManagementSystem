using FluentValidation.TestHelper;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Validators;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validations;

public class CreatePositionRequestValidatorTests
{
    private readonly CreatePositionRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var request = new CreatePositionRequest("AAPL", 10.5m, 150.25m, DateTime.UtcNow.AddMinutes(-5));
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("aapl")] // Lowercase not allowed per regex
    [InlineData("AAPL#1")]
    [InlineData("VERYLONGSYMBOLNAME")]
    public void Validate_InvalidTickerSymbol_FailsValidation(string ticker)
    {
        var request = new CreatePositionRequest(ticker, 10m, 100m, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TickerSymbol);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_InvalidQuantity_FailsValidation(decimal quantity)
    {
        var request = new CreatePositionRequest("AAPL", quantity, 100m, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(1_000_000)]
    public void Validate_InvalidUnitPrice_FailsValidation(decimal price)
    {
        var request = new CreatePositionRequest("AAPL", 10m, price, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Fact]
    public void Validate_FutureTradedAtDate_FailsValidation()
    {
        var request = new CreatePositionRequest("AAPL", 10m, 100m, DateTime.UtcNow.AddDays(1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TradedAt);
    }
}
