using FluentAssertions;
using InventoryAlert.Domain.Common.Exceptions;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Validations;

public class DomainExceptionsTests
{
    [Fact]
    public void NotFoundException_Constructors_SetPropertiesCorrectly()
    {
        var ex1 = new NotFoundException();
        ex1.Message.Should().NotBeNull();

        var ex2 = new NotFoundException("Custom message");
        ex2.Message.Should().Be("Custom message");

        var inner = new Exception("Inner");
        var ex3 = new NotFoundException("Custom message", inner);
        ex3.InnerException.Should().Be(inner);

        var ex4 = new NotFoundException("StockListing", "AAPL");
        ex4.Message.Should().Be("Entity \"StockListing\" (AAPL) was not found.");
    }

    [Fact]
    public void ValidationException_Constructors_SetPropertiesCorrectly()
    {
        var ex1 = new ValidationException();
        ex1.Message.Should().Be("One or more validation failures have occurred.");

        var ex2 = new ValidationException("Invalid payload");
        ex2.Message.Should().Be("Invalid payload");

        var inner = new Exception("Inner");
        var ex3 = new ValidationException("Invalid payload", inner);
        ex3.InnerException.Should().Be(inner);
    }

    [Fact]
    public void UserFriendlyException_Constructors_SetPropertiesCorrectly()
    {
        var inner = new Exception("Root cause");

        var ex1 = new UserFriendlyException(ErrorCode.NotFound, "Item not found", inner);
        ex1.ErrorCode.Should().Be(ErrorCode.NotFound);
        ex1.UserFriendlyMessage.Should().Be("Item not found");
        ex1.InnerException.Should().Be(inner);

        var ex2 = new UserFriendlyException("System message", "User display message", inner);
        ex2.Message.Should().Be("System message");
        ex2.UserFriendlyMessage.Should().Be("User display message");

        var ex3 = new UserFriendlyException(ErrorCode.Conflict, "System conflict", "Conflict message", inner);
        ex3.ErrorCode.Should().Be(ErrorCode.Conflict);
        ex3.Message.Should().Be("System conflict");
        ex3.UserFriendlyMessage.Should().Be("Conflict message");
    }
}
