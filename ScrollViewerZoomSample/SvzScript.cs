using System.Diagnostics;
using System.Globalization;
using Windows.Foundation;

namespace ScrollViewerZoomSample;

// Test hook: drives a ScrollViewer from an env var, for shells that cannot inject real
// pointer input. e.g. SVZ_SCRIPT="wait:2000;zoom:+0.2;wait:500;center"
// ops: wait:<ms> | zoom:+d | zoom:-d | zoom:=v | pan:dx,dy | fit | center | reset
//    | zoomto:<v>,<ms> | panby:<dx>,<dy>,<ms>  (eased over wall-clock time, for recordings)
//    | focus:<n>  (only where the view passes a focus callback, e.g. the XAML tab's ComboBox)
//
// Also holds the ChangeView plumbing the views share: unlike ZoomContentControl, ScrollViewer
// has no FitToCanvas/CenterContent/ResetViewport — they all have to be derived from
// ZoomFactor + offsets and issued through ChangeView.
internal static class SvzScript
{
    /// <summary>Zoom so the content fits the viewport, scrolled back to origin.</summary>
    public static void Fit(ScrollViewer sv, bool disableAnimation = false)
    {
        if (sv.Content is FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } content &&
            sv is { ViewportWidth: > 0, ViewportHeight: > 0 })
        {
            var zoom = Math.Min(sv.ViewportWidth / content.ActualWidth, sv.ViewportHeight / content.ActualHeight);
            zoom = Math.Clamp(zoom, sv.MinZoomFactor, sv.MaxZoomFactor);

            // at the fit zoom the content is no larger than the viewport on either axis,
            // so (0,0) is also the centered offset; alignment does the visual centering.
            sv.ChangeView(0, 0, (float)zoom, disableAnimation);
        }
    }

    /// <summary>Scroll so the content is centered, keeping the current zoom.</summary>
    public static void Center(ScrollViewer sv, bool disableAnimation = false)
        => sv.ChangeView(sv.ScrollableWidth / 2, sv.ScrollableHeight / 2, null, disableAnimation);

    /// <summary>Back to 100% at the origin.</summary>
    public static void Reset(ScrollViewer sv, bool disableAnimation = false)
        => sv.ChangeView(0, 0, 1f, disableAnimation);

    /// <summary>Zoom and scroll so <paramref name="element"/> fills the viewport, centered.</summary>
    public static void FitTo(ScrollViewer sv, FrameworkElement element, bool disableAnimation = false)
    {
        if (sv.Content is not UIElement content ||
            element is not { ActualWidth: > 0, ActualHeight: > 0 } ||
            sv is not { ViewportWidth: > 0, ViewportHeight: > 0 })
        {
            return;
        }

        var rect = element.TransformToVisual(content)
            .TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

        var zoom = Math.Clamp(
            Math.Min(sv.ViewportWidth / rect.Width, sv.ViewportHeight / rect.Height),
            sv.MinZoomFactor, sv.MaxZoomFactor);

        // ChangeView offsets are in post-zoom coordinates; aim the element's center at
        // the viewport's center and let ChangeView clamp to the scrollable range.
        var x = (rect.X + rect.Width / 2) * zoom - sv.ViewportWidth / 2;
        var y = (rect.Y + rect.Height / 2) * zoom - sv.ViewportHeight / 2;
        sv.ChangeView(Math.Max(0, x), Math.Max(0, y), (float)zoom, disableAnimation);
    }

    public static async void Run(ScrollViewer sv, string envVar, Action<int>? focus = null)
    {
        try
        {
            if (Environment.GetEnvironmentVariable(envVar) is not { Length: > 0 } script) return;

            foreach (var step in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = step.Split(':', 2);
                var arg = parts.Length > 1 ? parts[1] : "";
                switch (parts[0])
                {
                    case "wait": await Task.Delay(int.Parse(arg, CultureInfo.InvariantCulture)); break;
                    case "zoom" when arg.StartsWith('='): sv.ChangeView(null, null, float.Parse(arg[1..], CultureInfo.InvariantCulture), disableAnimation: true); break;
                    case "zoom": sv.ChangeView(null, null, sv.ZoomFactor + float.Parse(arg, CultureInfo.InvariantCulture), disableAnimation: true); break;
                    case "pan" when arg.Split(',') is [var dx, var dy]:
                        sv.ChangeView(
                            sv.HorizontalOffset + double.Parse(dx, CultureInfo.InvariantCulture),
                            sv.VerticalOffset + double.Parse(dy, CultureInfo.InvariantCulture),
                            null, disableAnimation: true);
                        break;
                    case "zoomto" when arg.Split(',') is [var v, var ms]:
                    {
                        var from = sv.ZoomFactor;
                        var to = float.Parse(v, CultureInfo.InvariantCulture);
                        await Animate(int.Parse(ms, CultureInfo.InvariantCulture), e => sv.ChangeView(null, null, from + (to - from) * (float)e, disableAnimation: true));
                        break;
                    }
                    // like zoomto, but keeps the content centered while zooming — ChangeView with a
                    // zoom alone anchors the top-left, so the centered offsets are recomputed per tick.
                    case "zoomctr" when arg.Split(',') is [var v, var ms] && sv.Content is FrameworkElement c:
                    {
                        var from = sv.ZoomFactor;
                        var to = float.Parse(v, CultureInfo.InvariantCulture);
                        await Animate(int.Parse(ms, CultureInfo.InvariantCulture), e =>
                        {
                            var z = from + (to - from) * (float)e;
                            sv.ChangeView(
                                Math.Max(0, (c.ActualWidth * z - sv.ViewportWidth) / 2),
                                Math.Max(0, (c.ActualHeight * z - sv.ViewportHeight) / 2),
                                z, disableAnimation: true);
                        });
                        break;
                    }
                    case "panby" when arg.Split(',') is [var dx, var dy, var ms]:
                    {
                        var (x0, y0) = (sv.HorizontalOffset, sv.VerticalOffset);
                        var (tx, ty) = (double.Parse(dx, CultureInfo.InvariantCulture), double.Parse(dy, CultureInfo.InvariantCulture));
                        await Animate(int.Parse(ms, CultureInfo.InvariantCulture), e => sv.ChangeView(x0 + tx * e, y0 + ty * e, null, disableAnimation: true));
                        break;
                    }
                    case "focus" when focus is not null: focus(int.Parse(arg, CultureInfo.InvariantCulture)); break;
                    case "fit": Fit(sv, disableAnimation: true); break;
                    case "center": Center(sv, disableAnimation: true); break;
                    case "reset": Reset(sv, disableAnimation: true); break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{envVar} failed: {ex}");
        }
    }

    // Applies an ease-in-out progress [0..1] derived from elapsed wall-clock time, so
    // Task.Delay jitter shifts the sampling instants but never the motion curve.
    private static async Task Animate(int durationMs, Action<double> apply)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            var t = Math.Min(1.0, sw.ElapsedMilliseconds / (double)durationMs);
            apply(t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2);
            if (t >= 1) break;
            await Task.Delay(8);
        }
    }
}
