# AI Buddy for Windows

A small Windows 10/11 desktop companion with a floating rabbit, an OpenAI powered chat, and optional screen watching.

## Run it

1. Install the **.NET 8 SDK** on Windows 10 or 11.
2. Open PowerShell in this folder and run:

   ```powershell
   dotnet run
   ```

The rabbit appears near the lower-right corner. Use **Chat** to ask about the current screen. Use **Watch off** to explicitly enable watching. Right-click the rabbit for settings and exit; the app also has a notification-area icon.

## Screen and key handling

- Screen watching is off on first launch. When enabled, the app samples the desktop locally every 3 seconds and looks for changes. It sends a changed screen no more than once every 30 seconds for proactive analysis.
- A question you submit always captures and sends the current desktop along with your text, whether screen watching is on or off.
- Captures are held in memory and are not written to disk. They are sent to OpenAI for analysis. OpenAI service-side data handling follows the account and API settings in effect for your account.
- Add the API key in **Settings**. The app saves it as a Windows Credential Manager generic credential for the current Windows user. It is not included in the source or settings file.
- The default model is `gpt-6-astra`; Settings lets you change it to another vision-capable model available to your API account.
- Proactive requests ask for a useful suggestion only. The rabbit stays quiet when there is nothing useful to say. It never clicks, types into, or controls other apps.

## Build a Windows executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

The framework-dependent output is placed under `bin\Release\net8.0-windows\win-x64\publish`. This MVP does not include an installer.

## Customize the appearance

- Edit `MainWindow.xaml` to change the rabbit artwork, layout, colors, and buttons.
- Edit `ChatWindow.xaml` and `SettingsWindow.xaml` to adjust the chat and settings panels.
- Edit `SuggestionPopup.cs` to change the proactive suggestion bubble.
- Rebuild and run with `dotnet run` to preview your changes. The OpenAI API key is not part of the source; it stays in Windows Credential Manager.
