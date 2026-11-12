using System.Text.Json.Serialization;

namespace MembershipIntegration.Functions.Models;

public sealed record Contact(
    [property: JsonPropertyName("email")] string Email);
