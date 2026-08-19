using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace StaleAuthStateSample.Services;

public sealed class CachedUserService(AuthenticationStateProvider authenticationStateProvider)
{
    private ClaimsPrincipal? _cachedUser;

    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        if (_cachedUser is null)
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            _cachedUser = state.User;
        }

        return _cachedUser;
    }
}