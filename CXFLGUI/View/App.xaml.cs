using System.Collections.Generic;
using System.Diagnostics;

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

            // Contrastive Text
            Color primaryColor = (Color)Fixed_ResourceDictionary["Colors"]["Primary"];
            Color primaryTextColor = CalculateContrastColor(primaryColor);
            Fixed_ResourceDictionary["Colors"]["PrimaryText"] = primaryTextColor;
        }

        static Color CalculateContrastColor(Color color)
        {
            Trace.WriteLine(color);
            double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
            Trace.WriteLine(luminance > 0.5 ? Colors.Black : Colors.White);
            return luminance > 0.5 ? Colors.Black : Colors.White;
        }

    }
}