# BossKey

A simple WPF boss-key utility for Windows.

## Features

- Save targets by process name
- Save targets by window title
- Double-click source lists to add saved targets
- Double-click saved lists to remove targets
- Two global hotkeys
- Boss hotkey: hide or restore target windows only
- Self-hide hotkey: hide or restore the BossKey app itself
- Settings are persisted and restored after restart

## Usage

1. Start the app.
2. Double-click items in the source lists to add them to the saved lists.
3. Capture and save the boss hotkey and self-hide hotkey on the right panel.
4. Press the boss hotkey to hide or restore the saved target windows.
5. Press the self-hide hotkey to hide or restore the app itself.

## Development

Requirements:

- Windows
- .NET 10 SDK

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run
```

## License

MIT, see [LICENSE](LICENSE).
