# ScrollViewerZoomSample

The counterpart to [ZoomContentControlSample](../ZoomContentControlSample): the same two demos,
built **without** `ZoomContentControl`, using only the stock WinUI `ScrollViewer` APIs that Uno
implements — `ZoomMode`, `MinZoomFactor`/`MaxZoomFactor`, the read-only `ZoomFactor`, and
`ChangeView(horizontalOffset, verticalOffset, zoomFactor)`.

Reference: [Scroll viewer controls](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/scroll-controls?tabs=scrollviewer)
(the `ScrollViewer` tabs), plus the linked
[Optical zoom and resizing](https://learn.microsoft.com/en-us/windows/apps/design/input/guidelines-for-optical-zoom)
guidelines.

## Running it

```bash
dotnet run --project ScrollViewerZoomSample/ScrollViewerZoomSample.csproj -f net10.0-desktop
```

Other heads: `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`.

## What's in here

| File | Purpose |
| --- | --- |
| `MainPage.xaml` | A two-tab `SelectorBar` that hosts the demos. |
| `ImageZoomView.xaml` | Demo 1 — a big bitmap you can zoom and pan. |
| `XamlContentZoomView.xaml` | Demo 2 — an ordinary XAML tree in the same control. |
| `SvzScript.cs` | The `ChangeView` plumbing both views share, plus the scripting test hook. |
| `Assets/profile.png` | The 2048 x 1817 image used by demo 1. |

## Interacting with it

What `ScrollViewer` gives you for free once `ZoomMode="Enabled"`:

- **Ctrl + Mouse Wheel** zooms in and out, centered on the cursor
- **Mouse Wheel** scrolls vertically, **Shift + Mouse Wheel** scrolls horizontally
- **Touch pinch/stretch** zooms — built in here, unlike `ZoomContentControl`

What it doesn't give you: middle-click-drag panning, and everything in the next section.

## The plumbing you write yourself

This sample exists to make the comparison concrete. `ZoomFactor` and the offsets are
**read-only**, so every programmatic view change goes through `ChangeView`, and each of these
`ZoomContentControl` one-liners became a hand-written helper (see `SvzScript`):

| ZoomContentControl | Here |
| --- | --- |
| `ZoomLevel = x` (settable, bindable) | `ChangeView(null, null, x)`; slider synced back manually from `ViewChanged` |
| `FitToCanvas()` | compute `min(viewport/content)` from `ActualWidth/Height`, clamp, `ChangeView` |
| `CenterContent()` | `ChangeView(ScrollableWidth / 2, ScrollableHeight / 2, null)` |
| `ResetViewport()` | `ChangeView(0, 0, 1f)` |
| `AutoFitToCanvas="True"` | refit from `ImageOpened` + `SizeChanged` in code-behind |
| `AutoCenterContent="True"` | `HorizontalAlignment/VerticalAlignment="Center"` on the content (presenter honors it when the extent fits) |
| `ElementOnFocus` / `SetLocalFocus(...)` | `TransformToVisual` + rect math + `ChangeView` (`SvzScript.FitTo`) |
| `AdditionalMargin`, `AllowFreePanning`, wheel ratios | no equivalent |

Two `ScrollViewer` details that are load-bearing:

- **`Stretch="None"`** on the `Image`, for the same reason as the other sample: a stretching
  image sizes itself to the viewport and there's nothing left to zoom into. `Stretch="None"`
  makes the extent the image's natural 2048 x 1817.
- **`ChangeView` is asynchronous and clamps.** It animates by default (pass
  `disableAnimation: true` to snap), silently clamps offsets/zoom to the valid range, and
  no-ops before the control is loaded — which is why the initial fit hangs off `ImageOpened`
  rather than the constructor.

## Demo 2 — ordinary XAML content

Same fixed 900 x 560 `Grid` of `Border`s, shapes, and live controls as the other sample, and the
same two proofs: zoomed content stays interactive (the counter button still clicks at 400%), and
vector content stays crisp (the 6pt `TextBlock` is legible zoomed in, unlike a bitmap).

One deliberate difference you'll feel immediately: it opens at 100% **scrolled to the top-left
corner**, because that's a `ScrollViewer`'s natural resting state — there is no
`AutoCenterContent` to hand it to you.

## Scripted driving (no input injection needed)

Env-var test hooks, same op language as the other sample (see `SvzScript.cs`):

- `SVZ_SCRIPT` drives the **Image** tab's ScrollViewer, `SVZ_SCRIPT_XAML` the **XAML content** tab's.
- `SVZ_TAB=xaml` opens the XAML tab at startup.
- Script = `;`-separated ops: `wait:<ms>` | `zoom:+d` | `zoom:-d` | `zoom:=v` | `pan:dx,dy` |
  `fit` | `center` | `reset` | `zoomto:<v>,<ms>` | `panby:<dx>,<dy>,<ms>` | `focus:<n>` (XAML tab).
