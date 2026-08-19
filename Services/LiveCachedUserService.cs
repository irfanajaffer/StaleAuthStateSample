using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace StaleAuthStateSample.Services;

public sealed class LiveCachedUserService : IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private ClaimsPrincipal? _cachedUser;

    public LiveCachedUserService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        if (_cachedUser is null)
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _cachedUser = state.User;
        }

        return _cachedUser;
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _cachedUser = (await authenticationStateTask).User;
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}