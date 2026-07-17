# Track 002 Plan: Terminal State Capture

## Objective
Implement state capture for PowerShell/CMD environments.

## Tasks
- [x] Research and select the best method for terminal capture (PSReadLine history hooking vs. ConPTY wrapping) as outlined in the architecture document.
- [x] Implement the chosen method to ensure executed commands and outputs (where possible) are saved periodically.
- [x] Integrate terminal backup logic into the main 10-minute service loop alongside the Notepad backups.