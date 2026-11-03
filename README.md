# FileAction

🇬🇧 **English** · 🇨🇿 [Čeština](README.cs.md) · 🌐 [fileaction.apexpiksel.cz](https://fileaction.apexpiksel.cz)

**Click-free file conversion for Windows (and Linux).** Grab a file, tap a key while dragging, and drop it on a format in a radial wheel. Everything runs locally; your files never leave your PC.

Inspired by the wonderful [Tangerine](https://tangerineformac.com/) for macOS. FileAction is an independent Windows take on the idea and is not affiliated with Tangerine.

<p align="center">
  <img src="docs/screenshots/en/wheel-video-convert.png" width="32%" alt="Convert wheel for a video" />
  <img src="docs/screenshots/en/wheel-image-tools.png" width="32%" alt="Tools wheel for an image" />
  <img src="docs/screenshots/en/wheel-actions.png" width="32%" alt="Actions wheel" />
</p>
<p align="center">
  <img src="docs/screenshots/en/wheel-light.png" width="32%" alt="Light mode" />
  <img src="docs/screenshots/en/settings.png" width="32%" alt="Settings" />
  <img src="docs/screenshots/en/jobs.png" width="32%" alt="Progress" />
</p>

## How it works

1. Grab a file: in File Explorer, on the desktop, an image or link in your browser, an e-mail attachment, a file inside a ZIP folder…
2. While dragging, **tap** a key:

   | Key | Wheel | What it offers |
   |---|---|---|
   | **Shift** | Convert | JPG, PNG, WebP, MP4, MP3, DOCX, PDF, XLSX… depending on the file type |
   | **Ctrl** | Tools | compress, shrink, rotate, trim, merge, split, archive… |
   | **Alt** | Actions | copy to…, move to…, copy path, terminal, floating shelf, share |

3. Drop the file on a slice. The result is saved next to the original (the original is never changed). Drop it in the middle of the wheel to cancel.

The wheel stays open until you drop, so you can release the key and tap another one to switch wheels. Keys can be reassigned in Settings.

**Repeat the last action:** tap the same key again while dragging (or press **R** on an open wheel) and the wheel turns into a single slice that repeats what you did last, with the same settings (e.g. the same trim times).

**Your own wheel:** in **Settings → Wheels** arrange each wheel by dragging: put slices where your hand expects them, drop the ones you never use off the wheel, bring them back from the library, or drag a slice onto another wheel's tab to move it there (e.g. Compress onto the Convert wheel).

**Presets:** in **Settings → Presets** put together slices of your own, like "JPG 80 %, max 1920 px, no metadata" or "MP4 720p, small file". They appear on the wheel next to the built-in ones.

**Watched folders:** in **Settings → Watched folders** pick a folder and what to do with new files, e.g. screenshots → WebP or phone photos HEIC → JPG. Every new file is processed once it has finished downloading or copying; the originals stay untouched.

**No mouse:** select files in File Explorer, or copy a file or a screenshot (**Win + Shift + S**), and press **Ctrl + Alt + Space**. Arrows pick a slice, Enter runs it, Tab switches wheels, Esc closes, 1–9 run a slice directly.
You can also open the wheel with **right-click → FileAction…** (on Windows 11 under “Show more options”).

Progress shows up in the corner of the screen, where you can open the result, show it in its folder or cancel.

## What it converts

| Type | From | To |
|---|---|---|
| Images | JPG, PNG, WebP, HEIC, TIFF, AVIF, BMP, GIF, ICO, SVG | JPG, PNG, WebP, AVIF, TIFF, ICO, PDF, SVG (real vector shapes, best for logos and drawings) (GIF → MP4/WebM) |
| Audio | MP3, M4A, AAC, WAV, FLAC, OGG, Opus, AIFF, WMA | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF |
| Video | MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPG, 3GP, TS | MP4, MOV, MKV, WebM, GIF, MP3 |
| PDF | PDF | DOCX, XLSX (tables), JPG, PNG (all pages, 300 DPI), PPTX, TXT, MD, HTML, EPUB |
| Documents | DOCX, DOC, ODT, RTF, MD, TXT, HTML, EPUB | DOCX, PDF, ODT, MD, HTML, TXT, EPUB, RTF |
| Slides | PPTX, PPT, ODP | PDF, PNG, JPG (all slides), PPTX, ODP |
| Spreadsheets | XLSX, XLSM, XLS, ODS, CSV, TSV | XLSX, CSV, TSV, ODS, PDF |
| Subtitles | SRT, VTT | SRT, VTT, TXT |
| Archives | ZIP, TAR, TAR.GZ, GZ, BZ2, XZ, RAR, 7z | extract, ZIP, TAR, TAR.GZ |

**Tools (Ctrl):**
- images: compress, shrink to 50 %, max 1920 px, rotate, mirror, crop to square, grayscale, strip metadata (GPS, EXIF), merge into PDF,
- video: compress (H.264), 720p, mute, trim by time, rotate, 2× speed, thumbnail frame, Sony S-Log3 to normal video, apply a .cube LUT (drop it together with the videos; the recording date is kept),
- audio: compress, normalize loudness, trim, join,
- PDF: merge, split into pages, rotate, optimize,
- images and scanned PDFs: **extract text (OCR)** with the recognizer built into Windows; the text is saved as .txt and copied to the clipboard,
- anything, folders included: archive to ZIP / TAR.GZ.

Things that aren't files yet (images dragged from a browser, Outlook attachments, screenshots on the clipboard) are saved to **Downloads\FileAction** first, and the results go next to them.

With several files selected, each one is converted separately; tools like “merge” produce a single result.

### Helper programs

FileAction handles images, PDFs, spreadsheets, subtitles and archives on its own. Some formats need extra programs:

- **FFmpeg** (audio and video, 34 MB) and **Pandoc** (documents, 35 MB) are downloaded the first time you need them: the wheel shows their slices dimmed with “get FFmpeg”, and dropping a file there asks once, downloads the tool (checked against its published SHA-256) and runs the conversion. If you already have them installed (e.g. `winget install Gyan.FFmpeg`), FileAction uses those,
- **LibreOffice** (DOC, PPTX, ODS, Word documents and spreadsheets to PDF) has to be installed separately: `winget install TheDocumentFoundation.LibreOffice`.
- Markdown and web pages become PDFs through **Microsoft Edge** (part of Windows) or Chrome, no LibreOffice needed.

**Settings → Helper programs** shows what is missing and can download or install it for you. Keeping the tools out of the installer keeps it at about 50 MB.

## Install

Download `FileAction-Setup-x.y.z.exe` from [Releases](../../releases), or from the artifacts of the latest [GitHub Actions](../../actions) run. The installer (about 50 MB) doesn't need admin rights. A portable `.zip` is available too.

The app speaks English and Czech and comes in light and dark mode; both follow your Windows settings, and the language can be switched in Settings.

On first launch FileAction adds itself to startup and to the File Explorer context menu (both can be turned off in Settings). It then lives in the notification area next to the clock.

**Updates** install themselves: FileAction asks GitHub for the latest release now and then (nothing about your files is sent), offers the update in a notification and the tray menu, verifies the installer's SHA-256 and installs it silently. You can turn the check off in Settings. The portable version opens the download page instead.

Requirements: Windows 10 (1809+) or Windows 11, x64.

## Linux

**Arch Linux:** every release has a `PKGBUILD` attached (the `fileaction-bin` package). Download it into an empty folder and run `makepkg -si`. The package is coming to the AUR (`yay -S fileaction-bin`) as soon as AUR account registration reopens.

**Other distributions:** download `FileAction-x.y.z-linux-x64.tar.xz` (about 32 MB) from [Releases](../../releases), unpack it and run `fileaction/app/fileaction` (no .NET needed).

Linux doesn't let apps watch the keyboard and mouse of other programs (Wayland forbids it on purpose), so the "drag and tap a key" gesture is Windows-only. On Linux you use the same wheel like this:

- **Drop zone:** run `fileaction` (or *FileAction* from the app menu) and drag files onto the wheel. It stays on screen; move it by its middle.
- **Right-click in the file manager:** `fileaction integrate` adds *FileAction…* to Nautilus (*Scripts* submenu), Dolphin and Nemo (the AUR package installs Dolphin and Nemo for everyone).
- **Command line** and keyboard shortcuts of your desktop:

  ```sh
  fileaction photo.heic                       # the wheel for this file
  fileaction list photo.heic                  # what can be done with it
  fileaction run image.to.webp *.heic         # do it, no window
  fileaction run video.trim clip.mov --param start=0:10 --param end=0:25
  ```

- **Settings:** `fileaction --settings` (also *Settings* in the app menu entry's right-click menu, or the button at the corner of the drop zone) for presets, watched folders, feedback and support. Presets show up on the wheels and in `fileaction list/run`. Watched folders are handled by a background `fileaction --watch`, started automatically when you log in while at least one folder is on.

Optional packages: `ffmpeg` (audio, video), `pandoc-cli` (documents), `libreoffice-fresh` (Office files), `tesseract` + `tesseract-data-eng` (extract text). Settings live in `~/.config/fileaction/settings.json`.

## Build from source

```powershell
# .NET 10 SDK
./scripts/fetch-tools.ps1                      # downloads ffmpeg.exe and pandoc.exe into .\tools (for the tests)
$env:PATH = "$PWD\tools;$env:PATH"; dotnet test tests/FileAction.Tests
dotnet publish src/FileAction.App -c Release -r win-x64 --self-contained -o publish   # trimmed, ~165 MB unpacked
publish\FileAction.exe
```

`FileAction.exe --self-test <folder>` builds every window offscreen and saves PNG previews in both languages (used by CI).

### Releasing

Push a commit to `main` whose message starts with `Release v1.2.3` (or run the **Build** workflow manually with a version). CI builds the installer and zip and publishes them as a GitHub release. Add a repository secret `RELEASE_TOKEN` (a fine-grained personal access token with *Contents: read and write* on this repository) to publish releases under your own account instead of github-actions.

The same run builds the Linux tarball, installs the AUR package on Arch Linux to test it and, when the `AUR_SSH_PRIVATE_KEY` secret holds the key of the AUR account that maintains `fileaction-bin`, pushes the updated PKGBUILD to the AUR.

## Project layout

```
src/FileAction.Core     conversions and tools (cross-platform library, also tested on Linux)
  Backends/             ImageMagick, FFmpeg, Pandoc, LibreOffice, PDF, spreadsheets, subtitles, archives
  Operations/Catalog.cs every slice on the Convert and Tools wheels
  Loc.cs                English / Czech strings
src/FileAction.Linux    the Linux app (Avalonia): drop zone, wheel, CLI, file manager integration
src/FileAction.App      the Windows app (WPF)
  Input/GlobalInput.cs  global mouse and keyboard hooks (the “drag + tap” gesture)
  Wheel/                the radial menu and when it opens
  Actions/              the Actions wheel, floating shelf, sharing
  Jobs/                 background jobs and progress cards
  Shell/                tray icon, hotkey, context menu, single instance
tests/FileAction.Tests  unit tests plus integration tests that run every operation on sample files
installer/              Inno Setup
packaging/              Linux tarball and the AUR PKGBUILD template
website/                the project page (static)
```

## Feedback and support

Found a bug or have an idea? Write it in the app (**Settings → About → Send feedback**, also in the tray icon menu), use the [form on the website](https://fileaction.apexpiksel.cz/#feedback), email [support@apexpiksel.cz](mailto:support@apexpiksel.cz) or open an [issue](https://github.com/Metamorfolog/file_action/issues).

FileAction is free and will stay free. If it saves you time, you can support its development:

- **Solana** (SOL, USDC): `J7vmQqiFe6t3RSX9m8qCTFkxe6xDRzbPy5nMaeGNV7bL`  
  QR code and an "Open in wallet" button on the [website](https://fileaction.apexpiksel.cz/#support).
- **Card or PayPal:** [ko-fi.com/metamorfolog](https://ko-fi.com/metamorfolog)

## License

[MIT](LICENSE). FileAction uses other open-source components under their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Differences from Tangerine

- Shift / Ctrl / Alt instead of Shift / Command / Control.
- Apple formats (Pages, Keynote, RTFD) and writing HEIC are not supported; HEIC can be opened.
- PDF → DOCX/EPUB/HTML converts the text, not the exact page layout (use PDF → PPTX or images for that).
- Tangerine's “AirDrop” action is **Share** on Windows (Nearby Sharing, Teams, Mail…).
