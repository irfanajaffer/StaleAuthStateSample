# Validation — Analyzer diagnostics for stale authentication state (BL0013)

Scenario: [dotnet/aspnetcore #68488](https://github.com/dotnet/aspnetcore/issues/68488)

## Build tested

```
.NET SDK 11.0.100-preview.7.26381.103
```

The SDK is pinned in `global.json` at the repository root.

## Sample application

`StaleAuthStateSample.csproj` — Blazor Web App (Interactive Server)

| Component/Service | Route/Page | Purpose |
|---|---|---|
| `StaleUserBadge` | `/stale-auth-demo` | Reads auth state once in `OnInitializedAsync`, never subscribes — triggers BL0013 |
| `LiveUserBadge` | `/stale-auth-demo` | Subscribes to `AuthenticationStateChanged` — BL0013 resolved |
| `CascadingUserBadge` | `/stale-auth-demo` | Consumes `[CascadingParameter] Task<AuthenticationState>` — no BL0013 expected |
| `CachedUserService` | `/stale-auth-demo` | Scoped DI service that caches the user once — triggers BL0013 |
| `LiveCachedUserService` | `/stale-auth-demo` | Scoped DI service that stays in sync with auth changes — no BL0013 expected |
| `<AuthorizeView>` / `<CascadingAuthenticationState>` | `/stale-auth-demo` | Framework consumers, always live — no BL0013 expected |

## How to run

```powershell
cd StaleAuthStateSample
dotnet run --launch-profile http
```

Open `http://localhost:5093/stale-auth-demo` and use the sign-in/sign-out buttons to reach each validation consumer.

## How to verify the build diagnostic

```powershell
cd StaleAuthStateSample
dotnet build | Tee-Object evidence/build-debug.txt
```

Expected output: two unique `BL0013` warnings — one on `StaleUserBadge.razor` and one on `CachedUserService.cs`. `LiveUserBadge`, `LiveCachedUserService`, `CascadingUserBadge`, `<AuthorizeView>`, and `<CascadingAuthenticationState>` must not be flagged.

## Runtime checks

1. Load the page while anonymous. All consumers initially show anonymous.
2. Sign in as Alice. The stale component and stale service remain anonymous; all live consumers show Alice and Administrator.
3. Sign in as Bob. The stale consumers remain unchanged; all live consumers show Bob and Support.
4. Sign out. The stale consumers remain unchanged; all live consumers show anonymous.
5. Navigate to Counter and back after changing users. Record which values are reconstructed and which remain current.
6. Repeat after replacing each stale consumer with its subscribed counterpart. Both then track changes without reload and BL0013 disappears.

Capture screenshots after steps 2-5, browser console output, and the rendered markup for each consumer in `evidence/`.

## Manual validation matrix

Run each command in both the command line and the preview IDE and save full output. Do not mark an item passed without attaching evidence.

| Exercise | Procedure |
|---|---|
| Debug source | `dotnet build` then `dotnet run --launch-profile http` |
| IDE and command line | Build in the IDE and via `dotnet build`; confirm both report the same BL0013 diagnostic |

The scenario does not require a render-mode matrix because BL0013 is compile-time-identical across Static SSR, Interactive Server, Interactive WebAssembly, Interactive Auto, standalone WebAssembly, and MAUI Hybrid. This sample uses the one Interactive Server configuration needed to reproduce stale state at runtime.

Use [VALIDATION_REPORT.md](test-report/VALIDATION_REPORT.md) to record results. The `evidence/` directory is intentionally kept out of container builds but should be committed when publishing the validation repository.

## Configuration tested

- Blazor Web App — Interactive Server
- Published Release output (`dotnet publish -c Release`)

## Evidence

Captured artifacts are in `evidence/`.