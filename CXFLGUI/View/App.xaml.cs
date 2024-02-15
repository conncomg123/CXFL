namespace CXFLGUI
{
    public partial class App : Application
    {
        public static Dictionary<string, ResourceDictionary> Fixed_ResourceDictionary { get; private set; }

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();

            Fixed_ResourceDictionary = new Dictionary<string, ResourceDictionary>();
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                string key = dictionary.Source.OriginalString.Split(';').First().Split('/').Last().Split('.').First();
                Fixed_ResourceDictionary.Add(key, dictionary);
            }
        }
    }
}