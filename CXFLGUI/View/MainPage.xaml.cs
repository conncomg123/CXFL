namespace CXFLGUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel viewModel;
        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            BindingContext = viewModel;
            viewModel.DocumentOpened += OnDocumentOpened;
        }
        private void OnDocumentOpened(object sender, MainViewModel.DocumentEventArgs args)
        {
            var libraryPanelViewModel = new LibraryPanelViewModel(args.Document.Library);
            var libraryPanel = new LibraryPanel(libraryPanelViewModel);

            Content = new StackLayout
            {
                Children =
            {
                libraryPanel
            }
            };

        }
    }
}