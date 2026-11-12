using System.Text.Json.Serialization;

namespace MembershipIntegration.Functions.Models;

public sealed record LegacyMember(
    [property: JsonPropertyName("given_name")] string GivenName,
    [property: JsonPropertyName("family_name")] string FamilyName,
    [property: JsonPropertyName("dob")] string Dob,
    [property: JsonPropertyName("contact")] Contact Contact,
    [property: JsonPropertyName("plan_code")] string PlanCode,
    [property: JsonPropertyName("source")] string Source);
