using System.Text.Json.Serialization;

namespace MembershipIntegration.Functions.Models;

public sealed record RegistrationRequest
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("membershipType")]
    public string? MembershipType { get; init; }

    [JsonPropertyName("registeredAt")]
    public string? RegisteredAt { get; init; }
}
