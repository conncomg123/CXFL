namespace CXFLGUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            var mainViewModel = new MainViewModel();
            BindingContext = mainViewModel;

            // Create VanillaFrame instances
            //var vanillaFrame1 = new VanillaFrame();
            //var vanillaFrame2 = new VanillaFrame();
            var LibraryPanel1 = new LibraryPanel(mainViewModel);

            // Add VanillaFrame instances to the layout
            Content = new StackLayout
            {
                Children =
            {
                LibraryPanel1
                //vanillaFrame1,
                //vanillaFrame2
            }
            };

            // Add content to the VanillaFrames
            //var label1 = new Label { Text = "Content for VanillaFrame 1" };
            //var label2 = new Label { Text = "Content for VanillaFrame 2" };

            //vanillaFrame1.AddContent(label1);
            //vanillaFrame2.AddContent(label2);

            // Add tabs to the VanillaFrames
            //vanillaFrame1.AddTabContentPair("Tab 1", new Label { Text = "Content for VanillaFrame 1, Tab 1" });
            //vanillaFrame1.AddTabContentPair("Tab 2", new Label { Text = "Content for VanillaFrame 1, Tab 2" });
            //vanillaFrame1.AddTabContentPair("Tab 3", new Label { Text = "Content for VanillaFrame 1, Tab 3" });
            //vanillaFrame1.AddTabContentPair("Tab 4", new Label { Text = "Content for VanillaFrame 1, Tab 4" });

            //vanillaFrame2.AddTabContentPair("Tab A", new Label { Text = "Content for VanillaFrame 2, Tab A" });
            //vanillaFrame2.AddTabContentPair("Tab B", new Label { Text = "Content for VanillaFrame 2, Tab B" });
            //vanillaFrame2.AddTabContentPair("Tab C", new Label { Text = "Content for VanillaFrame 2, Tab C" });
        }
    }
}