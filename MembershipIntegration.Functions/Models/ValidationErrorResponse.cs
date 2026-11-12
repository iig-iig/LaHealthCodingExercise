using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MembershipIntegration.Functions.Models;

public sealed record ValidationErrorResponse(
    [property: JsonPropertyName("errors")] IReadOnlyCollection<string> Errors);
