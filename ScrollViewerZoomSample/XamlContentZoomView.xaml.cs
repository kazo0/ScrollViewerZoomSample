using Microsoft.UI.Xaml.Controls.Primitives;

namespace ScrollViewerZoomSample;

public sealed partial class XamlContentZoomView : UserControl
{
    private int _clicks;
    private bool _syncingSlider;

    public XamlContentZoomView()
    {
        this.InitializeComponent();
        UpdateZoomText(Scroller.ZoomFactor);
        SvzScript.Run(Scroller, "SVZ_SCRIPT_XAML", i => FocusSelector.SelectedIndex = i);
    }

    private void OnFitClick(object sender, RoutedEventArgs e) => SvzScript.Fit(Scroller);

    private void OnResetClick(object sender, RoutedEventArgs e) => SvzScript.Reset(Scroller);

    // Proof that the content stays live: this is a real Button inside the zoomed tree.
    private void OnCounterClick(object sender, RoutedEventArgs e)
    {
        _clicks++;
        CounterButton.Content = $"Clicked {_clicks} times";
    }

    // ZoomContentControl's ElementOnFocus, reconstructed by hand: fit-and-center on one
    // node via TransformToVisual + ChangeView, or back out to the whole diagram.
    private void OnFocusSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (FocusSelector.SelectedIndex)
        {
            case 1: SvzScript.FitTo(Scroller, ShellNode); break;
            case 2: SvzScript.FitTo(Scroller, CounterNode); break;
            case 3: SvzScript.FitTo(Scroller, ShapesNode); break;
            default: SvzScript.Fit(Scroller); break;
        }
    }

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

        _syncingSlider = true;
        ZoomSlider.Value = Scroller.ZoomFactor;
        _syncingSlider = false;
    }

    private void UpdateZoomText(float zoomFactor)
        => ZoomText.Text = $"{zoomFactor * 100:0}%";
}
