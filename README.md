# AI Buddy for Windows

A Windows 10/11 desktop companion with a floating rabbit, screen-aware chat, and optional proactive screen watching.

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

## Ask about the screen

- Click **Chat**, enter a question, and click **Ask** (or press Ctrl+Enter). Each question captures the current desktop and includes it with the question.
- Chat history is held in memory during that chat session.
- In local Ollama mode, the image is sent to the Ollama service on this PC. In OpenAI mode, it is sent to the configured OpenAI API.
- Captures are held in memory only and are not written as image or video files.

## Screen watching

- Screen watching is off by default. Click **Watch off** or enable it in Settings. The button changes to **Watching · pause** while active.
- While active, AI Buddy samples the desktop every 3 seconds and compares a small in-memory image sample. It sends a full frame for analysis only after a meaningful change and at most once every 30 seconds.
- The assistant offers a short suggestion only when it sees something useful. It never clicks, types into, or controls other apps.
- This is screen observation, not a saved screen recording. Pause watching at any time using the rabbit's button.

## Settings and privacy

- Choose **Ollama (local, free)** for local processing. Ollama must be installed and running, and the selected vision model must be downloaded.
- Choose **OpenAI (uses API credits)** to send questions and screenshots to OpenAI. Add the API key in Settings; it is saved in Windows Credential Manager, not in source code or the settings file.
- The model, Ollama address, and whether watching starts with the app are stored in `%APPDATA%\AiBuddy\settings.json`. API keys are not stored there.
- Proactive requests ask for a useful suggestion only. The rabbit remains quiet when there is nothing useful to add.

## Build a Windows executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

The framework-dependent output is placed under `bin\Release\net8.0-windows\win-x64\publish`. This MVP does not include an installer.

## Customize the appearance

- Edit `MainWindow.xaml` to change the rabbit artwork, layout, colors, and buttons.
- Edit `ChatWindow.xaml` and `SettingsWindow.xaml` to adjust the chat and settings panels.
- Edit `SuggestionPopup.cs` to change the proactive suggestion bubble.
- Rebuild and run with `dotnet run` to preview your changes.
