using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsXFL;
using MauiIcons.Core;
using MauiIcons.Material;
using Microsoft.Maui.Graphics.Win2D;
using Microsoft.UI.Xaml.Markup;
using static MainViewModel;

namespace CXFLGUI
{
    public class LibraryPanel : VanillaFrame
    {
        private MainViewModel viewModel;
        private Label Label_LibraryCount;

        int LoadedItemsCount = 0;

        Dictionary<string, CsXFL.Item> Tuple_LibraryItemDict = new Dictionary<string, CsXFL.Item>();
        ObservableCollection<LibraryItem> LibraryItems = new ObservableCollection<LibraryItem>();

        public LibraryPanel(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.DocumentOpened += DocumentOpened;

            var stackLayout = new StackLayout();
            stackLayout.Padding = new Thickness(0, 25, 0, 0);

            Label_LibraryCount = new Label();
            UpdateLibraryCount(0);

            var listView = new ListView
            {
                ItemsSource = LibraryItems,
                ItemTemplate = new DataTemplate(typeof(LibraryItemCell))
            };

            listView.ItemSelected += MyListView_ItemSelected;

            stackLayout.Children.Add(Label_LibraryCount);
            stackLayout.Children.Add(listView);

            Content = stackLayout;
        }

        private void DocumentOpened(object sender, DocumentEventArgs e)
        {
            Document Doc = e.Document;
            LoadedItemsCount = Doc.Library.Items.Count;
            UpdateLibraryCount(LoadedItemsCount);
            Tuple_LibraryItemDict = Doc.Library.Items;

            // Clear existing items
            LibraryItems.Clear();

            // Initiating conversion sequence!
            foreach (var kvp in Tuple_LibraryItemDict)
            {
                LibraryItems.Add(new LibraryItem { Key = kvp.Key, Value = kvp.Value });
            }
        }

        private void UpdateLibraryCount(int LoadedItemsCount)
        {
            Label_LibraryCount.Text = LoadedItemsCount + " Items";
        }

        private void MyListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            // Do something with the selected item (e.SelectedItem)
            Trace.WriteLine("Selected Item", e.SelectedItem.ToString());

            // Deselect the item
            ((ListView)sender).SelectedItem = null;
        }

        public class LibraryItem
        {
            public string Key { get; set; }
            public CsXFL.Item Value { get; set; }
        }

        [XamlCompilation(XamlCompilationOptions.Compile)]
        public class LibraryItemCell : ViewCell
        {
            public LibraryItemCell()
            {
                var icon = new Label
                {
                    FontSize = 20,
                    TextColor = Colors.White
                };

                var text = new Label();
                text.SetBinding(Label.TextProperty, new Binding("Key"));

                var stackLayout = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Padding = new Thickness(10, 5),
                    Spacing = 10,
                    Children = { icon, text }
                };

                var myBinding = new Binding("Value.ItemType");
                icon.Icon(IconExtension.GetIcon(myBinding))
                    .IconSize(20.0)
                    .IconColor(Colors.White);

                View = stackLayout;
            }

            public static class IconExtension
            {
                public static MaterialIcons GetIcon(Binding binding)
                {
                    var itemTypeConverter = new ItemTypeConverter();
                    // Accessing the object through Binding.Source
                    return (MaterialIcons)itemTypeConverter.Convert(binding.Source, typeof(MaterialIcons), null, CultureInfo.CurrentCulture);
                }
            }

            public class ItemTypeConverter : IValueConverter
            {
                Microsoft.Maui.Graphics.Color DefaultColor = Colors.Red;
                double DefaultSize = 20.0;
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    if (value is string itemType)
                    {
                        switch (itemType)
                        {
                            case "bitmap":
                                return MaterialIcons.Image;
                            case "sound":
                                return MaterialIcons.AudioFile;
                            case "graphic":
                                return MaterialIcons.Collections;
                            default:
                                return MaterialIcons.QuestionMark;
                        }
                    }
                    return MaterialIcons.QuestionMark;
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}