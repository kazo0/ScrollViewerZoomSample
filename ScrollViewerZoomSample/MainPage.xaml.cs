namespace ScrollViewerZoomSample;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        // Test hook: SVZ_TAB=xaml opens the XAML-content tab at startup (see SvzScript).
        // Done on Loaded because SelectorBar re-asserts its XAML-declared selection during load.
        if (Environment.GetEnvironmentVariable("SVZ_TAB") == "xaml")
        {
            Loaded += (_, _) => XamlTab.IsSelected = true;
        }
    }

    private void OnTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var showImage = sender.SelectedItem == ImageTab;

        ImageView.Visibility = showImage ? Visibility.Visible : Visibility.Collapsed;
        XamlView.Visibility = showImage ? Visibility.Collapsed : Visibility.Visible;
    }
}
