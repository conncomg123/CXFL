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
using Microsoft.Maui.Graphics.Text;
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

            // Top / bottom of pane
            var StackLayout_Pane = new StackLayout();
            StackLayout_Pane.Padding = new Thickness(0, 25, 0, 0);

            // Library count
            Label_LibraryCount = new Label();
            Label_LibraryCount.TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"];
            UpdateLibraryCount(0);

            // Searchbar
            SearchBar SearchBar_Library = new SearchBar
            {
                WidthRequest = 300,
                HeightRequest = 40,
                //BackgroundColor = (Color)App.Fixed_ResourceDictionary["DefaultSearchBar"]["BackgroundColor"],
                //CancelButtonColor = (Color)App.Fixed_ResourceDictionary["DefaultSearchBar"]["CancelButtonColor"],
                Placeholder = "Search...",
                //PlaceholderColor = (Color)App.Fixed_ResourceDictionary["DefaultSearchBar"]["Textcolor"],
                //TextColor = (Color)App.Fixed_ResourceDictionary["DefaultSearchBar"]["TextColor"]
            };

            SearchBar_Library.TextChanged += (sender, e) =>
            {
                string searchText = e.NewTextValue;
            };

            // Horizontal Divider between label + search and listView
            Grid HzDivider = new Grid();
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });

            Grid.SetColumn(Label_LibraryCount, 0);
            Grid.SetRow(Label_LibraryCount, 0);
            HzDivider.Children.Add(Label_LibraryCount);

            Grid.SetColumn(SearchBar_Library, 1);
            Grid.SetRow(SearchBar_Library, 0);
            HzDivider.Children.Add(SearchBar_Library);

            // Sort it
            var SortedLibraryItems = SortLibraryItems(LibraryItems.ToList());

            var ListView_LibraryDisplay = new ListView
            {
                ItemsSource = LibraryItems,
                RowHeight = 35
            };

            ListView_LibraryDisplay.ItemTemplate = new DataTemplate(() =>
            {
                var ViewCell_LibraryEntry = new ViewCell();

                var LibraryEntryText = new Label();
                LibraryEntryText.TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"];
                LibraryEntryText.SetBinding(Label.TextProperty, "Key");

                var LibraryEntryIcon = new Label
                {
                    FontSize = 20,
                    TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"]
                };

                LibraryEntryIcon.SetBinding(Label.TextProperty, "Key");

                ViewCell_LibraryEntry.View = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Padding = new Thickness(10, 5),
                    Spacing = 10,
                    Children = { LibraryEntryIcon, LibraryEntryText }
                };

                ViewCell_LibraryEntry.BindingContextChanged += (sender, e) =>
                {
                    if (ViewCell_LibraryEntry.BindingContext is LibraryItem item)
                    {

                        // Offset folder children
                        int forwardslashCount = item.Key.Count(c => c == '/');
                        ((StackLayout)ViewCell_LibraryEntry.View).Margin = new Thickness(forwardslashCount * 25, 0, 0, 0);

                        // Normalize the library name
                        int lastForwardsSlash = item.Key.LastIndexOf('/');
                        if (lastForwardsSlash >= 0 && lastForwardsSlash < item.Key.Length - 1)
                        {
                            LibraryEntryText.Text = item.Key.Substring(lastForwardsSlash + 1);
                        }
                        else
                        {
                            LibraryEntryText.Text = item.Key;
                        }

                        switch (item.Value.ItemType)
                        {
                            case "bitmap":
                                LibraryEntryIcon.Icon(MaterialIcons.Image);
                                break;
                            case "sound":
                                LibraryEntryIcon.Icon(MaterialIcons.VolumeUp);
                                break;
                            case "graphic":
                                LibraryEntryIcon.Icon(MaterialIcons.Category);
                                break;
                            case "movie clip":
                                LibraryEntryIcon.Icon(MaterialIcons.Movie);
                                break;
                            case "font":
                                LibraryEntryIcon.Icon(MaterialIcons.ABC);
                                break;
                            case "button":
                                LibraryEntryIcon.Icon(MaterialIcons.SmartButton);
                                break;
                            case "folder":
                                LibraryEntryIcon.Icon(MaterialIcons.Folder);
                                break;
                            default:
                                LibraryEntryIcon.Icon(MaterialIcons.QuestionMark);
                                break;
                        }
                    }
                };

                return ViewCell_LibraryEntry;
            });

            ListView_LibraryDisplay.ItemSelected += ListView_Library_ItemSelected;

            StackLayout_Pane.Children.Add(HzDivider);
            StackLayout_Pane.Children.Add(ListView_LibraryDisplay);

            Content = StackLayout_Pane;
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

        private List<LibraryItem> SortLibraryItems(List<LibraryItem> items)
        {
            // Use LINQ OrderBy to sort items alphabetically by Key
            return items.OrderBy(item => item.Key).ToList();
        }

        private void ListView_Library_ItemSelected(object sender, SelectedItemChangedEventArgs e)
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