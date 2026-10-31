# AI Buddy for Windows

A Windows 10/11 desktop companion with a front-facing spider hanging from the top edge of the screen, screen-aware chat, and optional proactive screen watching.

## Run it

1. Install the .NET 8 SDK.
2. For free local AI, install [Ollama for Windows](https://ollama.com/download/windows).
3. Open PowerShell in this folder and download/start the default vision model:

   ```powershell
   ollama run qwen3-vl:2b
   ```

   The first run downloads the model (about 1.9 GB). It runs on your computer. The 2B model is relatively small, so responses can be slower on CPU and may be less capable than larger hosted models.
4. In this folder, run:

   ```powershell
   dotnet run
   ```

AI Buddy defaults to local Ollama at `http://localhost:11434`; no OpenAI API key or paid credits are needed in this mode. In Settings, you can switch to OpenAI if you choose to use a key.

## Spider controls

- The spider is a transparent, always-on-top overlay near the right edge of the primary display. Its web drops 60% of the usable screen height before the spider; it has no card or panel behind it.
- Left-click the spider to open chat. Right-click it for chat, settings, screen-watching, hide, and exit controls.
- To hide or bring the spider back, use **Hide spider** or **Show spider** from the AI Buddy tray icon menu. It starts visible when the app starts.
- The tray menu also lets you pause or resume screen watching without opening a panel.

## Ask about the screen

- Click **Chat**, enter a question, and click **Ask** (or press Ctrl+Enter). Each question captures the current desktop and includes it with the question.
- Chat history is held in memory during that chat session.
- In local Ollama mode, the image is sent to the Ollama service on this PC. In OpenAI mode, it is sent to the configured OpenAI API.
- Captures are held in memory only and are not written as image or video files.

## Pop-up coding advice

- Enable **pop-up coding advice** in Settings or from the spider's right-click menu or tray icon menu.
- While active, AI Buddy samples the desktop every 3 seconds and compares a small in-memory image sample. It sends a changed full frame to the selected AI provider no more than once every 45 seconds.
- The assistant checks whether the screen clearly shows an IDE, code, terminal, debugger, build error, or coding documentation. It stays quiet outside coding contexts and shows one brief, actionable tip in a dismissible pop-up when useful.
- It never clicks, types into, or controls other apps. This is screen observation, not a saved video recording. Pause watching at any time from the spider's right-click menu or tray icon menu.

## Settings and privacy

- Choose **Ollama (local, free)** for local processing. Ollama must be installed and running, and the selected vision model must be downloaded.
- Choose **OpenAI (uses API credits)** to send questions and screenshots to OpenAI. Add the API key in Settings; it is saved in Windows Credential Manager, not in source code or the settings file.
- The model, Ollama address, and whether watching starts with the app are stored in `%APPDATA%\AiBuddy\settings.json`. API keys are not stored there.
- Proactive requests ask for a useful suggestion only. The spider remains quiet when there is nothing useful to add.

## Build a Windows executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

The framework-dependent output is placed under `bin\Release\net8.0-windows\win-x64\publish`. This MVP does not include an installer.

## Customize the appearance

- Edit `MainWindow.xaml` and `MainWindow.xaml.cs` to change the hanging spider artwork, overlay position, thread length, and colors.
- Edit `CodingActivityDetector.cs` to change which foreground coding tools wake the spider. The spider wakes during a question even when screen watching is off; coding-app detection only runs while screen watching is enabled.
- Edit `ChatWindow.xaml` and `SettingsWindow.xaml` to adjust the chat and settings panels.
- Edit `SuggestionPopup.cs` to change the proactive suggestion bubble.
- Rebuild and run with `dotnet run` to preview your changes.
