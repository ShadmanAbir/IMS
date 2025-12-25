using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

public class ApplicationUserLogin : IdentityUserLogin<Guid>
{
    // Parameterless ctor for EF
    private ApplicationUserLogin() { }

    public ApplicationUserLogin(Guid userId, string loginProvider, string providerKey)
    {
        UserId = userId;
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
    }
}
