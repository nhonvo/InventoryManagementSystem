using FluentValidation.TestHelper;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Validators;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validations;

public class AlertRuleRequestValidatorTests
{
    private readonly AlertRuleRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidPriceAboveRule_PassesValidation()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 200.00m, true);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidPercentDropFromCost_PassesValidation()
    {
        var request = new AlertRuleRequest("GOOGL", AlertCondition.PercentDropFromCost, 15.50m, false);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidLowHoldingsCount_PassesValidation()
    {
        var request = new AlertRuleRequest("MSFT", AlertCondition.LowHoldingsCount, 5.00m, true);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", AlertCondition.PriceAbove, 100)]
    [InlineData("VERYLONGSYMBOLNAME", AlertCondition.PriceAbove, 100)]
    public void Validate_InvalidTickerSymbol_FailsValidation(string symbol, AlertCondition condition, decimal target)
    {
        var request = new AlertRuleRequest(symbol, condition, target, true);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TickerSymbol);
    }

    [Fact]
    public void Validate_ZeroTargetValue_FailsValidation()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 0m, true);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetValue);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(100.01)]
    public void Validate_InvalidPercentDropFromCostRange_FailsValidation(decimal percentDrop)
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PercentDropFromCost, percentDrop, true);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetValue);
    }

    [Fact]
    public void Validate_NonWholeNumberLowHoldingsCount_FailsValidation()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.LowHoldingsCount, 5.50m, true);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetValue);
    }

    [Fact]
    public void Validate_InvalidEnumCondition_FailsValidation()
    {
        var request = new AlertRuleRequest("AAPL", (AlertCondition)999, 100m, true);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Condition);
    }
}
