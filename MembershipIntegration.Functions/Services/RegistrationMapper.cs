using System.Globalization;
using MembershipIntegration.Functions.Models;

namespace MembershipIntegration.Functions.Services;

public sealed class RegistrationMapper : IRegistrationMapper
{
    public LegacyMemberRequest Map(RegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfBirth))
        {
            throw new InvalidOperationException("Input dateOfBirth is invalid for legacy mapping.");
        }

        var planCode = request.MembershipType switch
        {
            "Single" => "S",
            "Family" => "F",
            "Couple" => "C",
            _ => throw new InvalidOperationException($"Unsupported membership type: {request.MembershipType}")
        };

        var member = new LegacyMember(
            GivenName: request.FirstName!,
            FamilyName: request.LastName!,
            Dob: dateOfBirth.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            Contact: new Contact(request.Email!),
            PlanCode: planCode,
            Source: "MILKYWAY");

        return new LegacyMemberRequest(member);
    }
}
