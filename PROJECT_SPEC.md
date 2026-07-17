# Project Specification: Savestate Logger

## Goal
Create a background service for Windows that automatically saves the state of Notepad and PowerShell/CMD every 10 minutes. This provides a safety net against crashes, power outages, and user errors without relying on keylogging.

## Tech Stack
*   **Language:** C#
*   **Framework:** .NET 8 (Console Application / Background Service)
*   **Libraries:** 
    *   `UIAutomationClient` / `UIAutomationTypes` (for GUI scraping)
    *   `System.Management.Automation` (for PowerShell hooking/history)
    *   P/Invoke / Win32 APIs (for ConPTY or process management if needed)

## Requirements & Constraints
*   **State Capture, Not Keylogging:** The application must capture the literal text buffer of editors and terminal streams, adhering strictly to `autosave_architecture.md`.
*   **Production Readiness:** Code must be robust, handle exceptions (e.g., processes closing mid-read), and be ready for deployment as a background task.
*   **Zero Placeholders:** No mock data or stubbed implementations.
*   **Persistence:** State must be written to a predictable local directory (e.g., `%APPDATA%\SavestateLogger`).

## Development Tracks

### Track 1: Core Service & Notepad State Capture (track_001)
*   **Objective:** Set up the C# background loop and implement UI Automation to extract text from running Notepad processes.
*   **Tasks:**
    1. Initialize a .NET 8 Worker Service or Console App with a robust 10-minute timer loop.
    2. Implement process discovery to find all active `notepad.exe` instances.
    3. Use `UIAutomationClient` to target the main text `Document` or `Edit` control within each Notepad instance.
    4. Extract the full text string and write it to a timestamped backup file in a local app data folder.

### Track 2: Terminal State Capture (track_002)
*   **Objective:** Implement state capture for PowerShell/CMD environments.
*   **Tasks:**
    1. Research and select the best method for terminal capture (PSReadLine history hooking vs. ConPTY wrapping) as outlined in the architecture document.
    2. Implement the chosen method to ensure executed commands and outputs (where possible) are saved periodically.
    3. Integrate terminal backup logic into the main 10-minute service loop alongside the Notepad backups.

### Track 3: Restoration & Polish (track_003)
*   **Objective:** Ensure users can easily find and restore their saved states.
*   **Tasks:**
    1. Implement a cleanup routine to manage the size of the backup folder (e.g., delete states older than 7 days).
    2. Add logging to monitor the service's health and capture errors (e.g., permission denied on certain UI elements).
    3. (Optional) Provide a simple CLI or UI to list available save states and open them.