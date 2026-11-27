# eKim 2026

**eKim 2026** (Keep In Mind) is a Windows desktop utility written in Visual Basic .NET. It runs in the system tray and lets you quickly copy selected text by pressing **Ctrl+Caps Lock**, then saves it to a text file and an HTML log.

## Features

- Captures selected text with a global `Ctrl+Caps Lock` shortcut
- Saves captures into files named after the source application's window title
- Stores text files on the Windows Desktop, inside a `KeepMyText` folder
- Keeps a UTF-8 HTML journal in the user's Documents folder
- Creates a daily capture file with optional timestamps
- Runs in the system tray with active and paused states
- Supports Romanian (`ro-RO`) and English (`en-US`) interfaces
- Can optionally start with Windows
- Can show balloon notifications
- Automatically moves journal files older than 30 days into dated backup files

## Requirements

- Windows
- .NET Framework 4.7.2
- Visual Studio 2017 or later with **.NET desktop development** support
- Microsoft Windows Script Host Object Model COM reference (`IWshRuntimeLibrary`) for the optional startup shortcut feature

## Build

1. Clone the repository:

   ```bash
   git clone https://github.com/userkbmsr-png/eKIm2026.git
   cd eKIm2026
   ```

2. Open `eKIm2026.sln` in Visual Studio.

3. Restore any project dependencies and COM references if prompted.

4. Build the solution in **Debug** or **Release** mode.

5. Run `KeepMyText.exe` from:

   ```text
   bin/Debug/KeepMyText.exe
   ```

   or

   ```text
   bin/Release/KeepMyText.exe
   ```

## Usage

1. Launch **eKim / KeepMyText**.
2. Select text in an application.
3. Press **Ctrl+Caps Lock**.
4. The application captures the selected text and saves it as a `.txt` file.
5. Use the tray icon menu to:
   - pause or resume monitoring,
   - open the settings window,
   - open today's HTML journal, or
   - exit the application.

Left-clicking the tray icon opens the settings window.

## Captured files

Captured text is saved under:

```text
Desktop\KeepMyText\<source-window-name>.txt
```

The filename is derived from the active window title. Invalid filename characters are replaced or removed.

The HTML log is stored in:

```text
Documents\KeepMyText Log.htm
```

The journal is written using UTF-8 with BOM so Romanian diacritics display correctly.

## Settings

The settings window allows you to configure:

- interface language,
- balloon notifications,
- startup with Windows,
- and inclusion of timestamps in captured text.

Saving settings restarts the application so all options are applied consistently.

## Project structure

```text
eKim2026.sln                 Visual Studio solution
eKim.vbproj                  Windows Forms project
My Project/eKim.vb           Main application logic
My Project/Form1.designer.vb Windows Forms designer code
My Project/Settings.settings Application settings
Resources/                   Icons and image resources
```

## Notes

- `Ctrl+Caps Lock` is a Windows global hotkey; it may be unavailable if another application has already registered the same shortcut.
- The application captures text by copying the current selection to the Windows clipboard.
- If no text is selected or the clipboard cannot be read, an error notification is shown.
- This project is specific to Windows and does not support Linux or macOS.

## License

No license has been declared for this repository. Copyright and redistribution terms should be confirmed before reuse.

## Repository

- GitHub: <https://github.com/userkbmsr-png/eKIm2026>
