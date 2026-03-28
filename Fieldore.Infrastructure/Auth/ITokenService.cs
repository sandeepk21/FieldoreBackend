using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Entities;

namespace Fieldore.Infrastructure.Auth;

public interface ITokenService
{
    AuthResponse CreateToken(AuthUser authUser, AppUserProfile profile, Guid? businessId = null);
}
