using Microsoft.AspNetCore.Identity;

namespace IMS.Api.Domain.Entities;

public class ApplicationUserToken : IdentityUserToken<Guid>
{
    // Parameterless ctor for EF
    private ApplicationUserToken() { }

    public ApplicationUserToken(Guid userId, string loginProvider, string name, string value)
    {
        UserId = userId;
        LoginProvider = loginProvider;
        Name = name;
        Value = value;
    }
}
