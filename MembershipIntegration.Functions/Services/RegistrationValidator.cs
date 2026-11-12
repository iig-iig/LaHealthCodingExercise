using System.Globalization;
using System.Net.Mail;
using MembershipIntegration.Functions.Models;

namespace MembershipIntegration.Functions.Services;

public sealed class RegistrationValidator : IRegistrationValidator
{
    public ValidationErrorResponse Validate(RegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors.Add("firstName is required");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors.Add("lastName is required");
        }

        if (string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            errors.Add("dateOfBirth is required");
        }
        else if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            errors.Add("dateOfBirth must be in yyyy-MM-dd format");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("email is required");
        }
        else if (!IsPlausibleEmail(request.Email))
        {
            errors.Add("email is invalid");
        }

        if (string.IsNullOrWhiteSpace(request.MembershipType))
        {
            errors.Add("membershipType is required");
        }
        else if (!IsSupportedMembershipType(request.MembershipType))
        {
            errors.Add("membershipType must be Single, Family or Couple");
        }

        return new ValidationErrorResponse(errors);
    }

    private static bool IsPlausibleEmail(string value)
    {
        return MailAddress.TryCreate(value, out _);
    }

    private static bool IsSupportedMembershipType(string value)
    {
        return string.Equals(value, "Single", StringComparison.Ordinal)
            || string.Equals(value, "Family", StringComparison.Ordinal)
            || string.Equals(value, "Couple", StringComparison.Ordinal);
    }
}
