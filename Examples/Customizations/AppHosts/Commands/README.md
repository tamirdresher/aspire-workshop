# Resource command sample

This AppHost demonstrates current Aspire resource-command APIs: typed inputs, named
CLI options, validation, structured JSON results, command visibility, health-aware
state, and persistent container lifetime.

After starting the AppHost, validate the command arguments without changing Redis:

```powershell
aspire resource cache preview-clear-cache --database 0 --mode async --show-result --apphost .\apphost.cs
```

`preview-clear-cache` is API-only. The dashboard shows `clear-cache` when Redis is
healthy; that command asks for confirmation, flushes the selected database, and
returns a JSON summary.
