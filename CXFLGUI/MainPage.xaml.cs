namespace CXFLGUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        CsXFL.Document doc;
        public MainPage()
        {
            doc = new("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\test.fla");
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            CounterBtn.Text = doc.GetTimeline(0).GetFrameCount().ToString();

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}