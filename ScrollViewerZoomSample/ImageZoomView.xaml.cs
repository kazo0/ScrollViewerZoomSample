using Microsoft.UI.Xaml.Controls.Primitives;

namespace ScrollViewerZoomSample;

public sealed partial class ImageZoomView : UserControl
{
    private const float ZoomStep = 0.2f;

    private bool _syncingSlider;

    public ImageZoomView()
    {
        this.InitializeComponent();
        UpdateZoomText(Scroller.ZoomFactor);
        SvzScript.Run(Scroller, "SVZ_SCRIPT");
    }

    // ScrollViewer has no AutoFitToCanvas: fit when the image gets its natural layout size
    // (zooming is a render transform, so this doesn't refire on zoom), and again on viewport
    // resizes while the toggle is on.
    private void OnContentSizeChanged(object sender, SizeChangedEventArgs e) => OnViewportSizeChanged(sender, e);

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (AutoFitToggle.IsOn)
        {
            SvzScript.Fit(Scroller, disableAnimation: true);
        }
    }

    // ZoomFactor is read-only; every write goes through ChangeView. Passing null for the
    // offsets keeps the current scroll position (ChangeView re-clamps it after the zoom).
    private void OnZoomInClick(object sender, RoutedEventArgs e)
        => Scroller.ChangeView(null, null, Math.Min(Scroller.ZoomFactor + ZoomStep, Scroller.MaxZoomFactor));

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
        => Scroller.ChangeView(null, null, Math.Max(Scroller.ZoomFactor - ZoomStep, Scroller.MinZoomFactor));

    private void OnFitClick(object sender, RoutedEventArgs e) => SvzScript.Fit(Scroller);

    private void OnCenterClick(object sender, RoutedEventArgs e) => SvzScript.Center(Scroller);

    private void OnResetClick(object sender, RoutedEventArgs e) => SvzScript.Reset(Scroller);

    private void OnZoomSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_syncingSlider)
        {
            Scroller.ChangeView(null, null, (float)e.NewValue);
        }
    }

    private void OnZoomAllowedToggled(object sender, RoutedEventArgs e)
        => Scroller.ZoomMode = ((ToggleSwitch)sender).IsOn ? ZoomMode.Enabled : ZoomMode.Disabled;

    private void OnPanAllowedToggled(object sender, RoutedEventArgs e)
    {
        var mode = ((ToggleSwitch)sender).IsOn ? ScrollMode.Enabled : ScrollMode.Disabled;
        Scroller.HorizontalScrollMode = mode;
        Scroller.VerticalScrollMode = mode;
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        UpdateZoomText(Scroller.ZoomFactor);

        // reflect interaction-driven zoom (Ctrl+wheel, pinch) back into the slider without
        // triggering another ChangeView from its ValueChanged.
        _syncingSlider = true;
        ZoomSlider.Value = Scroller.ZoomFactor;
        _syncingSlider = false;
    }

    private void UpdateZoomText(float zoomFactor)
        => ZoomText.Text = $"{zoomFactor * 100:0}%";
}
