using System.Text.Json.Serialization;

namespace MembershipIntegration.Functions.Models;

public sealed record LegacyMemberRequest(
    [property: JsonPropertyName("member")] LegacyMember Member);
