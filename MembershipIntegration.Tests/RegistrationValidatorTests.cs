using FluentAssertions;
using MembershipIntegration.Functions.Models;
using MembershipIntegration.Functions.Services;

namespace MembershipIntegration.Tests;

public class RegistrationValidatorTests
{
    private readonly IRegistrationValidator _validator = new RegistrationValidator();

    [Fact]
    public void ValidRegistrationHasNoValidationErrors()
    {
        var request = CreateValidRequest();

        var result = _validator.Validate(request);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void MissingFirstNameReturnsRequiredError()
    {
        var request = CreateValidRequest() with { FirstName = null };

        var result = _validator.Validate(request);

        result.Errors.Should().ContainSingle().Which.Should().Be("firstName is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyOrWhitespaceFirstNameReturnsRequiredError(string value)
    {
        var request = CreateValidRequest() with { FirstName = value };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("firstName is required");
    }

    [Fact]
    public void MissingLastNameReturnsRequiredError()
    {
        var request = CreateValidRequest() with { LastName = null };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("lastName is required");
    }

    [Fact]
    public void MissingDateOfBirthReturnsRequiredError()
    {
        var request = CreateValidRequest() with { DateOfBirth = null };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("dateOfBirth is required");
    }

    [Fact]
    public void MissingEmailReturnsRequiredError()
    {
        var request = CreateValidRequest() with { Email = null };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("email is required");
    }

    [Fact]
    public void MissingMembershipTypeReturnsRequiredError()
    {
        var request = CreateValidRequest() with { MembershipType = null };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("membershipType is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("alex@")]
    [InlineData("@@example.com")]
    public void InvalidEmailFormatReturnsValidationError(string email)
    {
        var request = CreateValidRequest() with { Email = email };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("email is invalid");
    }

    [Theory]
    [InlineData("Singles")]
    [InlineData("FamilyMember")]
    [InlineData("Couples")]
    [InlineData("single")]
    public void InvalidMembershipTypeReturnsValidationError(string membershipType)
    {
        var request = CreateValidRequest() with { MembershipType = membershipType };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("membershipType must be Single, Family or Couple");
    }

    [Theory]
    [InlineData("2026-06-01T09:00:00Z")]
    [InlineData("2026/06/01")]
    [InlineData("2026-02-30")]
    public void InvalidDobFormatReturnsValidationError(string dateOfBirth)
    {
        var request = CreateValidRequest() with { DateOfBirth = dateOfBirth };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain("dateOfBirth must be in yyyy-MM-dd format");
    }

    [Fact]
    public void MultipleInvalidPropertiesReturnAllApplicableErrors()
    {
        var request = new RegistrationRequest
        {
            FirstName = string.Empty,
            LastName = null,
            DateOfBirth = "2026/06/01",
            Email = "not-an-email",
            MembershipType = "single"
        };

        var result = _validator.Validate(request);

        result.Errors.Should().BeEquivalentTo(new[]
        {
            "firstName is required",
            "lastName is required",
            "dateOfBirth must be in yyyy-MM-dd format",
            "email is invalid",
            "membershipType must be Single, Family or Couple"
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void RequiredFieldsDoNotAlsoProduceFormatErrors()
    {
        var request = new RegistrationRequest
        {
            FirstName = string.Empty,
            LastName = "Nguyen",
            DateOfBirth = string.Empty,
            Email = "",
            MembershipType = string.Empty
        };

        var result = _validator.Validate(request);

        result.Errors.Should().BeEquivalentTo(new[]
        {
            "firstName is required",
            "dateOfBirth is required",
            "email is required",
            "membershipType is required"
        }, options => options.WithStrictOrdering());
    }

    private static RegistrationRequest CreateValidRequest() => new()
    {
        FirstName = "Alex",
        LastName = "Nguyen",
        DateOfBirth = "1990-04-12",
        Email = "alex.nguyen@example.com",
        MembershipType = "Single",
        RegisteredAt = "2026-06-01T09:00:00Z"
    };
}
