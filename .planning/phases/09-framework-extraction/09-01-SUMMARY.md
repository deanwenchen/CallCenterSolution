# Plan 01 Summary — CallCenterOptions + Extensions.cs

## Completed
- ✅ `CallCenterOptions.cs` — 5 properties + `ApplyDefaults()` as sole env var reader
- ✅ `Extensions.cs` — `AddCallCenter()` + 3 override methods (`AddCallCenterOrderService<T>`, `AddCallCenterFinanceService<T>`, `AddCallCenterMemberService<T>`)

## Verification
- ✅ `dotnet build CallCenter.Framework` — 0 errors, 0 warnings
- ✅ `dotnet build CallCenter.AgentHost` — 0 errors, 0 warnings
- ✅ `AddKeyedSingleton<IChatClient>("base", ...)` registered
- ✅ Pipeline client registered as default `IChatClient`
- ✅ Mock services registered when `UseMockServices = true`
- ✅ Workflow registered via factory lambda
