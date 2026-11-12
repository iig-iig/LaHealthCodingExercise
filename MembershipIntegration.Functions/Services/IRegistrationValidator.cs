using MembershipIntegration.Functions.Models;

namespace MembershipIntegration.Functions.Services;

public interface IRegistrationValidator
{
    ValidationErrorResponse Validate(RegistrationRequest request);
}
