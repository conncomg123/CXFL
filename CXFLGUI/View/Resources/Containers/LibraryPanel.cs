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

            // Top / Bottom of Library Pane
            var StackLayout_Pane = new StackLayout();
            StackLayout_Pane.Padding = new Thickness(0, 25, 0, 0);

            // Library Count
            Label_LibraryCount = new Label();
            Label_LibraryCount.TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"];
            UpdateLibraryCount(0);

            // SearchBar
            SearchBar SearchBar_Library = new SearchBar
            {
                WidthRequest = 300,
                HeightRequest = 40,
                Placeholder = "Search...",
                Style = (Style)App.Fixed_ResourceDictionary["DefaultSearchBar"]["SearchBar"]
            };

            SearchBar_Library.TextChanged += (sender, e) =>
            {
                string searchText = e.NewTextValue;
            };

            // Horizontal Divider between Library Count and SearchBar
            Grid HzDivider = new Grid();
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });
            HzDivider.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });

            Grid.SetColumn(Label_LibraryCount, 0);
            Grid.SetRow(Label_LibraryCount, 0);
            HzDivider.Children.Add(Label_LibraryCount);

            Grid.SetColumn(SearchBar_Library, 1);
            Grid.SetRow(SearchBar_Library, 0);
            HzDivider.Children.Add(SearchBar_Library);

            var SortedLibraryItems = SortLibraryItems(LibraryItems.ToList());

            var ListView_LibraryDisplay = new ListView
            {
                ItemsSource = LibraryItems,
                RowHeight = 50
            };

            // Library Display
            ListView_LibraryDisplay.ItemTemplate = new DataTemplate(() =>
            {
                var ViewCell_LibraryEntry = new ViewCell();

                var LibraryEntryText = new Entry();
                LibraryEntryText.TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"];
                //LibraryEntryText.SetBinding(Label.TextProperty, "Key");

                var LibraryEntryIcon = new ImageButton
                {
                    Style = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Button"]
                };

                //LibraryEntryIcon.SetBinding(Label.TextProperty, "Key");

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

            // Footer
            Grid footerGrid = new Grid();
            footerGrid.Padding = new Thickness(10, 5);
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            footerGrid.HorizontalOptions = LayoutOptions.Start;

            ImageButton button1 = CreateIconButton(MaterialIcons.Add, "New Symbol", (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Button"]);
            ImageButton button2 = CreateIconButton(MaterialIcons.Folder, "New Folder", (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Button"]);
            ImageButton button3 = CreateIconButton(MaterialIcons.Help, "Properties", (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Button"]);
            ImageButton button4 = CreateIconButton(MaterialIcons.Delete, "Delete", (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Button"]);

            int ButtonSize = 35;

            button1.WidthRequest = ButtonSize;
            button2.WidthRequest = ButtonSize;
            button3.WidthRequest = ButtonSize;
            button4.WidthRequest = ButtonSize;

            button1.HeightRequest = ButtonSize;
            button2.HeightRequest = ButtonSize;
            button3.HeightRequest = ButtonSize;
            button4.HeightRequest = ButtonSize;

            Grid.SetColumn(button1, 1);
            Grid.SetColumn(button2, 2);
            Grid.SetColumn(button3, 3);
            Grid.SetColumn(button4, 4);

            footerGrid.Children.Add(button1);
            footerGrid.Children.Add(button2);
            footerGrid.Children.Add(button3);
            footerGrid.Children.Add(button4);

            ListView_LibraryDisplay.ItemSelected += ListView_Library_ItemSelected;

            StackLayout_Pane.Children.Add(HzDivider);
            StackLayout_Pane.Children.Add(ListView_LibraryDisplay);
            StackLayout_Pane.Children.Add(footerGrid);

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

        public static ImageButton CreateIconButton(MaterialIcons icon, string tooltip = null, Style buttonStyle = null)
        {
            var button = new ImageButton().Icon(icon);

            if (!string.IsNullOrEmpty(tooltip))
            {
                ToolTipProperties.SetText(button, tooltip);
            }

            if (buttonStyle != null)
            {
                button.Style = buttonStyle;
            }

            return button;
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