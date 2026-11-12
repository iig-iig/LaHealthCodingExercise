using MembershipIntegration.Functions.Models;

namespace MembershipIntegration.Functions.Services;

public interface IRegistrationMapper
{
    LegacyMemberRequest Map(RegistrationRequest request);
}
