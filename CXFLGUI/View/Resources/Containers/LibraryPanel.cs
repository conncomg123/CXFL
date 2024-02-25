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
        private SearchBar Search_Library;

        int LoadedItemsCount = 0;

        Dictionary<string, CsXFL.Item> Tuple_LibraryItemDict = new Dictionary<string, CsXFL.Item>();
        ObservableCollection<LibraryItem> LibraryItems = new ObservableCollection<LibraryItem>();

        public class LibraryItem
        {
            public string Key { get; set; }
            public CsXFL.Item Value { get; set; }
        }

        public LibraryPanel(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.DocumentOpened += DocumentOpened;

            // Vertical divider
            var stackLayout = new StackLayout();
            stackLayout.Padding = new Thickness(0, 25, 0, 0);

            // Library count
            Label_LibraryCount = new Label();
            UpdateLibraryCount(0);

            // Search bar
            SearchBar SearchBar_Library = new SearchBar
            {
                Placeholder = "Search",
                BackgroundColor = Colors.LightGrey
            };

            SearchBar_Library.TextChanged += (sender, e) =>
            {
                string searchText = e.NewTextValue;
            };

            // Horizontal Divider for top section
            Grid HzDivider = new Grid();
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });

            Grid.SetColumn(Label_LibraryCount, 0);
            Grid.SetRow(Label_LibraryCount, 0);
            HzDivider.Children.Add(Label_LibraryCount);

            Grid.SetColumn(SearchBar_Library, 1);
            Grid.SetRow(SearchBar_Library, 0);
            HzDivider.Children.Add(SearchBar_Library);

            var listView = new ListView
            {
                ItemsSource = LibraryItems
            };

            listView.ItemTemplate = new DataTemplate(() =>
            {
                var cell = new ViewCell();

                var text = new Label();
                text.SetBinding(Label.TextProperty, "Key");

                var icon = new Label
                {
                    FontSize = 20,
                    TextColor = Colors.White
                };

                icon.SetBinding(Label.TextProperty, "Key");

                cell.View = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Padding = new Thickness(10, 5),
                    Spacing = 10,
                    Children = { icon, text }
                };

                cell.BindingContextChanged += (sender, e) =>
                {
                    if (cell.BindingContext is LibraryItem item)
                    {

                        // Offset folder children
                        int forwardslashCount = item.Key.Count(c => c == '/');
                        ((StackLayout)cell.View).Margin = new Thickness(forwardslashCount * 25, 0, 0, 0);

                        // Normalize the library name
                        int lastForwardsSlash = item.Key.LastIndexOf('/');
                        if (lastForwardsSlash >= 0 && lastForwardsSlash < item.Key.Length - 1)
                        {
                            text.Text = item.Key.Substring(lastForwardsSlash + 1);
                        }
                        else
                        {
                            text.Text = item.Key;
                        }

                        switch (item.Value.ItemType)
                        {
                            case "bitmap":
                                icon.Icon(MaterialIcons.Image);
                                break;
                            case "sound":
                                icon.Icon(MaterialIcons.VolumeUp);
                                break;
                            case "graphic":
                                icon.Icon(MaterialIcons.Category);
                                break;
                            case "movie clip":
                                icon.Icon(MaterialIcons.Movie);
                                break;
                            case "font":
                                icon.Icon(MaterialIcons.ABC);
                                break;
                            case "button":
                                icon.Icon(MaterialIcons.SmartButton);
                                break;
                            case "folder":
                                icon.Icon(MaterialIcons.Folder);
                                break;
                            default:
                                icon.Icon(MaterialIcons.QuestionMark);
                                break;
                        }
                    }
                };

                return cell;
            });

            listView.ItemSelected += MyListView_ItemSelected;

            stackLayout.Children.Add(HzDivider);
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
    }
}