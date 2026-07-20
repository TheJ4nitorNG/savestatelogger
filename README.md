# Savestate Logger

Savestate Logger is a background service for Windows that automatically captures and saves the state of your work every 10 minutes. 

If your PC loses power, you forget to save, or you accidentally ruin a script, you don't lose your progress. You can just load the previous save state. It currently supports capturing active **Notepad** instances and **PowerShell/CMD** terminal environments.

## Features

* **Automatic Background Saves:** Runs quietly in the background, polling at a default 10-minute interval.
* **Notepad State Capture:** Extracts the raw text from any open, active Notepad window using Windows UIAutomation.
* **Terminal State Capture:** Captures command history and terminal states for PowerShell/CMD.
* **Smart Storage:** Saves text data cleanly into timestamped `.txt` files categorized by application.
* **Auto-Cleanup:** Automatically manages the backup directory, deleting states older than 7 days to save disk space.
* **CLI Management:** Built-in commands to easily list and restore your saved states.

## Prerequisites

* Windows OS (Required for UIAutomation and Terminal API access)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed on your machine.

## Installation & Setup

1. **Clone the repository:**
   ```powershell
   git clone https://github.com/TheJ4nitorNG/savestatelogger.git
   cd savestatelogger
   ```

2. **Build the project:**
   ```powershell
   dotnet build src/SavestateLogger.slnx
   ```

*(Optional)* **Publish as a standalone executable:**
If you want to run this without the `dotnet` command or run it on startup, publish a self-contained executable:
```powershell
dotnet publish src/SavestateLogger/SavestateLogger.csproj -c Release -r win-x64 --self-contained
```

## Usage

### 1. Run the Background Logger
To start the daemon with the default 10-minute (600 seconds) interval:
```powershell
dotnet run --project src/SavestateLogger/SavestateLogger.csproj
```

**Custom Interval:**
If you want the logger to save more frequently (e.g., every 60 seconds), use the `--interval` or `-i` flag:
```powershell
dotnet run --project src/SavestateLogger/SavestateLogger.csproj -- --interval 60
```

### 2. View Saved States
To see a list of all your available backups across all supported applications:
```powershell
dotnet run --project src/SavestateLogger/SavestateLogger.csproj -- --list
```

### 3. Restore a Saved State
To launch the interactive restore menu and open a previous savestate:
```powershell
dotnet run --project src/SavestateLogger/SavestateLogger.csproj -- --restore
```

## Where are my files?

The application stores everything in your local user roaming data. You can access them anytime via the File Explorer by typing:
`%APPDATA%\SavestateLogger`

Inside, you will find:
* `NotepadBackups/` - Contains timestamped text files of your Notepad sessions.
* `TerminalBackups/` - Contains timestamped command histories.
* `service.log` - Health monitoring and error logging for the background service.

## Development & Testing

To run the automated test suite (verifying UI extraction logic, pathing, and backup management):

```powershell
dotnet test src/SavestateLogger.Tests/SavestateLogger.Tests.csproj
```
