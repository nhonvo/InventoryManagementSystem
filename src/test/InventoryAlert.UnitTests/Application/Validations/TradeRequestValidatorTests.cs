using FluentValidation.TestHelper;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Validators;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validations;

public class TradeRequestValidatorTests
{
    private readonly TradeRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidTradeRequest_PassesValidation()
    {
        var request = new TradeRequest(TradeType.Buy, 50m, 120.50m, "Buying dip");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidTradeType_FailsValidation()
    {
        var request = new TradeRequest((TradeType)999, 10m, 100m, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_InvalidQuantity_FailsValidation(decimal quantity)
    {
        var request = new TradeRequest(TradeType.Buy, quantity, 100m, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(1_000_000)]
    public void Validate_InvalidUnitPrice_FailsValidation(decimal price)
    {
        var request = new TradeRequest(TradeType.Buy, 10m, price, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Fact]
    public void Validate_ExcessiveNotesLength_FailsValidation()
    {
        var longNotes = new string('A', 501);
        var request = new TradeRequest(TradeType.Buy, 10m, 100m, longNotes);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
