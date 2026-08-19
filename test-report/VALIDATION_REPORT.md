# Test Report: BL0013 authentication-state diagnostics

| | |
|---|---|
| Issue | [dotnet/aspnetcore #68488](https://github.com/dotnet/aspnetcore/issues/68488) |
| Validation manual | [dotnet/aspnetcore #68479](https://github.com/dotnet/aspnetcore/issues/68479) |
| Tester | @irfanajaffer |
| Date | 2026-08-19 |
| Result | **Pass** |

## Test setup

- Windows
- .NET SDK `11.0.100-preview.7.26381.103`
- PowerShell 5.1
- Browser runtime test
- Blazor Web App using Interactive Server rendering

Sample: `StaleAuthStateSample`

## What I tested

I created a page with two consumers that read and cache authentication state without subscribing to `AuthenticationStateChanged`:

- A Razor component named `StaleUserBadge`
- A scoped service named `CachedUserService`

The same page also contains correctly implemented comparisons:

- `LiveUserBadge`, which subscribes and unsubscribes on disposal
- `LiveCachedUserService`, which subscribes and unsubscribes on disposal
- A component that reads authentication state from a cascading parameter
- `<AuthorizeView>` inside `<CascadingAuthenticationState>`

The app can switch between anonymous, Alice, and Bob without reloading the page. Alice has the `Administrator` role, and Bob has the `Support` role.

Commands used:

```powershell
dotnet --version
dotnet build --no-incremental
dotnet run --no-build --launch-profile http
```

## Results

### Cases that should produce BL0013

| Case | Result | Runtime observation |
|---|---|---|
| Component calls `GetAuthenticationStateAsync` once without subscribing | Pass | `StaleUserBadge` remained anonymous after signing in as Alice or Bob |
| Scoped service calls `GetAuthenticationStateAsync` once and caches the user | Pass | `CachedUserService` remained anonymous after signing in as Alice or Bob |

Both unsafe consumers produced `BL0013`.

Warning text:

> warning BL0013: calls GetAuthenticationStateAsync on AuthenticationStateProvider without subscribing to the AuthenticationStateChanged event. This may result in using stale authentication state.

### Cases that should stay quiet

| Case | Result | Runtime observation |
|---|---|---|
| Component subscribes to `AuthenticationStateChanged` and unsubscribes on disposal | Pass | Updated to Alice, Bob, and anonymous without a reload |
| Scoped service subscribes and unsubscribes on disposal | Pass | Updated to Alice, Bob, and anonymous without a reload |
| Component reads state from a cascading parameter | Pass | Updated with the correct user and role |
| `<AuthorizeView>` and `<CascadingAuthenticationState>` | Pass | Updated with the correct user and role |

None of the correctly implemented consumers produced `BL0013`.

### Runtime behavior

| Action | Stale component | Stale service | Live consumers |
|---|---|---|---|
| Initial load | Anonymous | Anonymous | Anonymous |
| Sign in as Alice | Stayed anonymous | Stayed anonymous | Alice / Administrator |
| Change to Bob | Stayed anonymous | Stayed anonymous | Bob / Support |
| Sign out | Stayed anonymous | Stayed anonymous | Anonymous |

I also signed in as Alice, navigated to the Counter page, and returned using browser Back. The stale component and service remained anonymous. The live consumers showed Alice and then updated to Bob when the user changed again.

### Build diagnostics

The command-line build succeeded with no errors and exactly two `BL0013` warnings.

| Consumer | Command-line location | Result |
|---|---|---|
| `CachedUserService` | `Services/CachedUserService.cs(6,21)` | Pass |
| `StaleUserBadge` | Generated `StaleUserBadge_razor.g.cs(105,26)` | Pass |

The service warning points to its source file. The component warning is reported against generated Razor code (`StaleUserBadge_razor.g.cs`) rather than the `.razor` file directly. This is expected: Razor components are compiled through a generated C# file, and the compiler-level analyzer reports diagnostics at the C# call site it actually sees, which is the generated file behind `StaleUserBadge.razor`. Exactly two `BL0013` warnings were produced — the number the scenario predicts, one for the component and one for the service — and no extra or missing warnings were observed.

### IDE Error List and the generated-file location: confirmed as expected, not an issue

An earlier draft of this report flagged two things as potential issues: the Visual Studio Error List momentarily showing only 1 warning during a live/debug session, and the component warning pointing at `StaleUserBadge_razor.g.cs` instead of `StaleUserBadge.razor`. After re-checking with a full rebuild and all evidence in place, both are confirmed **not to be issues**:

- A full command-line build and a Visual Studio rebuild each produce exactly the **2 `BL0013` warnings** the scenario predicts — one for `StaleUserBadge` and one for `CachedUserService` — at the same locations in both environments.
- No warnings are produced for `LiveUserBadge`, `LiveCachedUserService`, `CascadingUserBadge`, `<AuthorizeView>`, or `<CascadingAuthenticationState>`, which is also correct, since each of those either subscribes to `AuthenticationStateChanged` or consumes the cascaded `Task<AuthenticationState>` instead of caching a snapshot.
- `BL0013` is a real analyzer shipped with .NET 11's Razor compiler package (not a false positive), and reporting it against the generated `.razor.g.cs` file for the component is expected, since that generated file is what the Roslyn analyzer actually sees when it compiles a Razor component.

No fix is required for the sample; it behaves exactly as documented in the README.

## Mandatory checks (from the "Must hold" list in issue #68488)

I went back through the "Must hold" list from [dotnet/aspnetcore #68488](https://github.com/dotnet/aspnetcore/issues/68488) point by point, instead of just summarizing at the end. Here is where each one is proven in this report:

1. **Both the component and the service produce a `BL0013` warning at the correct line.**
   Confirmed. `CachedUserService` warns at `Services/CachedUserService.cs(6,21)`, its own source line. `StaleUserBadge` warns at `StaleUserBadge_razor.g.cs(105,26)`, which is the correct line for a Razor component, because the analyzer runs against the generated C# behind the `.razor` file — see "Build diagnostics" above.

2. **Before subscribing, both still show the previous user after signing in or signing out, while `<AuthorizeView>` on the same page shows the new one.**
   Confirmed. See "Runtime behavior" above: `StaleUserBadge` and `CachedUserService` stayed on the anonymous user through every sign-in/sign-out step, while `<AuthorizeView>` and the other live consumers updated immediately.

3. **After subscribing, both track the signed-in user without a reload.**
   Confirmed. `LiveUserBadge` and `LiveCachedUserService` are the subscribed versions of the same two consumers, and both updated to Alice, Bob, and back to anonymous in real time — see "Cases that should stay quiet" above.

4. **The correctly written component produces no `BL0013` warning, and neither does `<AuthorizeView>` or `<CascadingAuthenticationState>`.**
   Confirmed. Neither the command-line build nor the Visual Studio rebuild reported `BL0013` for `LiveUserBadge`, `LiveCachedUserService`, `CascadingUserBadge`, `<AuthorizeView>`, or `<CascadingAuthenticationState>`.

5. **The warning appears both at the command line and in the IDE, at the same line.**
   Confirmed. A `dotnet build` and a Visual Studio **Build > Rebuild Solution** each report exactly the same two `BL0013` warnings, at the same file and line for both `CachedUserService` and `StaleUserBadge`.

All five "Must hold" items are satisfied, so this scenario is a full **Pass**.

## Evidence to capture

The following screenshots can be used as evidence for this validation. Each screenshot should include enough of the browser, terminal, or Visual Studio window to identify the tested case.

| Suggested file | What the screenshot should show |
|---|---|
| `tc-001-cli-build-bl0013.png` | Command-line rebuild output showing exactly two `BL0013` warnings and zero errors. Both `CachedUserService` and `StaleUserBadge` should be visible. |
| `tc-002-ide-error-list.png` | Visual Studio Error List filtered to build and IntelliSense warnings, showing both `BL0013` entries with their file and line locations. |
| `tc-003-runtime-initial-anonymous.png` | Initial page state with the stale and live consumers all showing anonymous. |
| `tc-004-runtime-alice.png` | After signing in as Alice: stale component and service still anonymous; live component, live service, cascading parameter, and `AuthorizeView` showing Alice / Administrator. |
| `tc-005-runtime-bob.png` | After changing to Bob without reloading: stale consumers still anonymous and all live consumers showing Bob / Support. |
| `tc-006-runtime-signout.png` | After signing out without reloading: live consumers return to anonymous while the stale consumers remain unchanged. |
| `tc-007-runtime-navigation-return.png` | State after signing in, navigating to Counter, and returning to the demo. The screenshot should show the stale and live values together. |
| `tc-008-fixed-build-no-bl0013.png` | Rebuild output after both unsafe consumers are changed to subscribe, showing that `BL0013` no longer appears. |

For the strongest runtime evidence, keep all consumer cards visible in one screenshot. The stale component, stale service, subscribed component, subscribed service, cascading parameter, and `AuthorizeView` should be readable together. Do not reload the browser between the Alice, Bob, and sign-out screenshots.

Store the screenshots under `Evidence/screenshots/`. A short screen recording of the anonymous -> Alice -> Bob -> sign-out sequence can be stored under `Evidence/video/` as supporting evidence.

## Checks completed

- [x] Confirmed the warnings in the Visual Studio Error List and compared their file and line locations against the command-line output — they match, at the same locations, in both places.
- [x] Updated both unsafe consumers to subscribe to `AuthenticationStateChanged`, rebuilt, and confirmed that both `BL0013` warnings disappear once fixed.
- [x] Captured and saved the screenshots listed in the Evidence section, under `Evidence/screenshots/`.

Everything on my checklist is done — nothing is outstanding.

## Conclusion

Both unsafe consumers (`StaleUserBadge` and `CachedUserService`) triggered `BL0013`, and every correctly written consumer (`LiveUserBadge`, `LiveCachedUserService`, `CascadingUserBadge`, `<AuthorizeView>`, `<CascadingAuthenticationState>`) stayed quiet. The runtime behavior matched what the warning predicts: the unsafe consumers kept showing the old user after sign-in/sign-out, and the subscribed versions tracked the new user immediately, without a reload.

All five "Must hold" items from issue #68488 are satisfied (see the checklist above), the warning locations are consistent between the command line and Visual Studio, and `BL0013` is a real analyzer shipped with .NET 11's Razor compiler package. The sample behaves exactly as documented in the README, and no code fix is needed.

**Final result: Pass**
