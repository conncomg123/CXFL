namespace CXFLGUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            var mainViewModel = new MainViewModel();
            BindingContext = mainViewModel;

            var LibraryPanel1 = new LibraryPanel(mainViewModel);

            Content = new StackLayout
            {
                Children =
            {
                LibraryPanel1
            }
            };

        }
    }
}