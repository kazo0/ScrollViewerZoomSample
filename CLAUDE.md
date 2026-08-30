# ScrollViewerZoomSample — agent working notes

The plain-`ScrollViewer` counterpart to `../ZoomContentControlSample` (same layout, same demos,
no Uno Toolkit `ZoomContentControl`; zooming/panning via `ZoomMode` + `ChangeView`). The Toolkit
is still referenced **from source** (`ProjectReference` to
`..\..\uno.toolkit.ui\src\Uno.Toolkit.UI\Uno.Toolkit.WinUI.csproj`) for chrome (`Divider`,
`SafeArea`, `ToolkitResources`); that requires `src/crosstargeting_override.props` in the toolkit
repo pinning `TargetFrameworkOverride` to `desktop` (already in place there, untracked).

## Build & run (desktop)

```bash
cd ScrollViewerZoomSample
dotnet build -f net10.0-desktop -c Debug
./bin/Debug/net10.0-desktop/ScrollViewerZoomSample.exe
```

Stop the app before rebuilding — it locks its own bin. To attach to a specific DevServer:
`-p:UnoRemoteControlHost=localhost -p:UnoRemoteControlPort=<port>`. To keep the DevServer
status icons static for recordings, build against a dead port (`-p:UnoRemoteControlPort=1`).

## Scripted driving (no input injection needed)

Env-var hooks in `ScrollViewerZoomSample/SvzScript.cs`:

- `SVZ_SCRIPT` drives the **Image** tab, `SVZ_SCRIPT_XAML` the **XAML content** tab.
- `SVZ_TAB=xaml` opens the XAML tab at startup.
- Ops: `wait:<ms>` | `zoom:+d` | `zoom:-d` | `zoom:=v` | `pan:dx,dy` | `fit` | `center` | `reset`
  | `zoomto:<v>,<ms>` | `panby:<dx>,<dy>,<ms>` (eased, clock-driven — for smooth recordings)
  | `focus:<n>` (XAML tab ComboBox: 0 whole, 1 Shell, 2 Live controls, 3 Vector content).
- ScrollViewer offsets grow right/down and are clamped by `ChangeView`; there is no free panning,
  so `panby` deltas only move within the scrollable range at the current zoom.

## Agent environment constraints & capture

Same non-interactive desktop rules as `../ZoomContentControlSample/CLAUDE.md`: no SendInput /
cursor APIs; capture with ffmpeg gdigrab by window title (`title=ScrollViewerZoomSample`), binary
at `%APPDATA%\Python\Python312\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe`.
Recording flow: launch with a script whose first op is `wait:6000`, poll
`(Get-Process ScrollViewerZoomSample).MainWindowHandle` until non-zero, wait ~2 s, then gdigrab
(30 fps, H.264, yuv420p, `+faststart`, `-an`, crop to even dimensions).
