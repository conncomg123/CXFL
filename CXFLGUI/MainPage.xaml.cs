namespace CXFLGUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        CsXFL.Document doc;
        public MainPage()
        {
            doc = new("C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\303_S1.fla");
            InitializeComponent();
        }
        //private void Panel1Button_Clicked(object sender, EventArgs e)
        //{
        //    Frame1.IsVisible = true;
        //    Frame2.IsVisible = false;
        //}

        //private void Panel2Button_Clicked(object sender, EventArgs e)
        //{
        //    Frame1.IsVisible = false;
        //    Frame2.IsVisible = true;
        //}

        //private void OnCounterClicked(object sender, EventArgs e)
        //{
        //    CounterBtn.Text = doc.GetTimeline(0).GetFrameCount().ToString();

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
    }
}