using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace StaleAuthStateSample.Services;

/// <summary>
/// A minimal, in-memory <see cref="AuthenticationStateProvider"/> used purely to demonstrate
/// how the current user changes over the lifetime of a single Blazor Server circuit.
///
/// Real apps swap the signed-in user this way when, for example, a background revalidation
/// check (see RevalidatingServerAuthenticationStateProvider) fails the user's security stamp,
/// or another tab signs the user in/out and the change is pushed to this circuit.
///
/// Calling NotifyAuthenticationStateChanged is the ONLY supported way to tell already-rendered
/// components that the user changed. Components that fetch the state once via
/// GetAuthenticationStateAsync and never observe this event (or the equivalent
/// [CascadingParameter] Task&lt;AuthenticationState&gt;) will keep rendering the user that was
/// signed in when they last read the state.
/// </summary>
public sealed class DemoAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private AuthenticationState _currentState = new(Anonymous);

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_currentState);

    public void SignIn(string userName)
    {
        var role = userName == "alice" ? "Administrator" : "Support";
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Role, role)],
            authenticationType: "Demo");
        _currentState = new AuthenticationState(new ClaimsPrincipal(identity));

        // This is the part a lot of components forget to listen for.
        NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
    }

    public void SignOut()
    {
        _currentState = new AuthenticationState(Anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
    }
}
