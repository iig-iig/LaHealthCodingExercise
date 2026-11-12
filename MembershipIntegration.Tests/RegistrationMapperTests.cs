using System.Text.Json;
using FluentAssertions;
using MembershipIntegration.Functions.Models;
using MembershipIntegration.Functions.Services;

namespace MembershipIntegration.Tests;

public class RegistrationMapperTests
{
    private readonly IRegistrationMapper _mapper = new RegistrationMapper();

    [Fact]
    public void ValidRegistrationMapsCorrectly()
    {
        var request = CreateValidRequest();

        var result = _mapper.Map(request);

        result.Member.GivenName.Should().Be("Alex");
        result.Member.FamilyName.Should().Be("Nguyen");
        result.Member.Dob.Should().Be("12/04/1990");
        result.Member.Contact.Email.Should().Be("alex.nguyen@example.com");
        result.Member.PlanCode.Should().Be("S");
        result.Member.Source.Should().Be("MILKYWAY");
    }

    [Theory]
    [InlineData("Single", "S")]
    [InlineData("Family", "F")]
    [InlineData("Couple", "C")]
    public void MembershipTypeMapsToExpectedPlanCode(string membershipType, string expectedPlanCode)
    {
        var request = CreateValidRequest() with { MembershipType = membershipType };

        var result = _mapper.Map(request);

        result.Member.PlanCode.Should().Be(expectedPlanCode);
    }

    [Fact]
    public void SourceAlwaysEqualsMilkyway()
    {
        var result = _mapper.Map(CreateValidRequest());

        result.Member.Source.Should().Be("MILKYWAY");
    }

    [Fact]
    public void RegisteredAtDoesNotAppearInLegacyPayload()
    {
        var result = _mapper.Map(CreateValidRequest());

        var json = JsonSerializer.Serialize(result);

        json.Should().NotContain("registeredAt");
        json.Should().Contain("\"member\"");
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
