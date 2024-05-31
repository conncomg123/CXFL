using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using CsXFL;
using MauiIcons.Core;
using MauiIcons.Material;
using Microsoft.Maui.Controls.Shapes;

namespace CXFLGUI
{
    public class LibraryItem
    {
        public string Key { get; set; }
        public CsXFL.Item Value { get; set; }
    }

    public class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double nameWidth)
            {
                // You can adjust the multiplier as needed to fit your layout
                return nameWidth * 2;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabWidths
    {
        public double NameWidth { get; set; }
        public double UseCountWidth { get; set; }
        public double DateModifiedWidth { get; set; }

        public TabWidths(double nameWidth, double useCountWidth, double dateModifiedWidth)
        {
            NameWidth = nameWidth;
            UseCountWidth = useCountWidth;
            DateModifiedWidth = dateModifiedWidth;
        }

        public void UpdateWidth(Label element, double newWidth)
        {
            // I'm basically honorary nasty
            if (element.Text == "Name")
            {
                NameWidth = newWidth;
            }
            else if (element.Text == "Use Count")
            {
                UseCountWidth = newWidth;
            }
            else if (element.Text == "Date Modified")
            {
                DateModifiedWidth = newWidth;
            }
        }

        public double GetNameWidth()
        {
            return NameWidth;
        }

        public double GetUseCountWidth()
        {
            return UseCountWidth;
        }

        public double GetDateModifiedWidth()
        {
            return DateModifiedWidth;
        }
    }

    public class DraggableSeparator : Grid
    {
        private readonly BoxView visualIndicator;
        private readonly BoxView hitArea;
        private double initialX;
        private bool isDragging;
        private Label leftElement;
        private Label rightElement;
        private TabWidths tabWidths;
        double MIN_WIDTH = 50;

        // <!> Clicking on the separator will not drag, this is a layering issue
        // <!> Pan event drops when cursor leaves parent container

        public DraggableSeparator(Label leftElement, Label rightElement, TabWidths tabWidths)
        {
            this.leftElement = leftElement;
            this.rightElement = rightElement;
            this.tabWidths = tabWidths;

            visualIndicator = new BoxView
            {
                BackgroundColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.FillAndExpand,
                WidthRequest = 0.5,
            };

            hitArea = new BoxView
            {
                Color = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                WidthRequest = 30,
            };

            var gestureRecognizer = new PanGestureRecognizer();
            gestureRecognizer.PanUpdated += OnPanUpdated;
            hitArea.GestureRecognizers.Add(gestureRecognizer);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(hitArea, 0);
            Grid.SetRow(hitArea, 0);
            grid.Children.Add(hitArea);

            // Create a container for the visual indicator to allow centering
            var visualIndicatorContainer = new ContentView
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.FillAndExpand,
            };
            visualIndicatorContainer.Content = visualIndicator;

            Grid.SetColumn(visualIndicatorContainer, 0);
            Grid.SetRow(visualIndicatorContainer, 0);
            grid.Children.Add(visualIndicatorContainer);

            Children.Add(grid);
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    initialX = e.TotalX;
                    isDragging = true;
                    break;
                case GestureStatus.Running when isDragging:
                    double offset = e.TotalX - initialX;

                    if (leftElement != null)
                    {
                        var WidthRequest = Math.Max(leftElement.WidthRequest + offset, MIN_WIDTH);
                        leftElement.WidthRequest = WidthRequest;
                        tabWidths.UpdateWidth(leftElement, WidthRequest);
                    }

                    if (rightElement != null)
                    {
                        var WidthRequest = Math.Max(rightElement.WidthRequest - offset, MIN_WIDTH);
                        rightElement.WidthRequest = WidthRequest;
                        tabWidths.UpdateWidth(rightElement, WidthRequest);
                    }

                    initialX = e.TotalX;
                    break;
                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    isDragging = false;
                    break;
            }
        }
    }

    public class LibraryPanel : VanillaFrame
    {
        private Label LoadedLibraryItemCount;

        int LoadedItemsCount = 0;
        int LibraryRowHeight = 35;
        int MainHorizontalPadding = 15;

        // Library Tabs
        TabWidths tabWidths = new TabWidths(300, 100, 100);

        Dictionary<string, CsXFL.Item> Tuple_LibraryItemDict = new Dictionary<string, CsXFL.Item>();
        ObservableCollection<LibraryItem> LibraryItems = new ObservableCollection<LibraryItem>();

        public static ImageButton CreateIconButton(MaterialIcons icon, int size, string tooltip = null, Style buttonStyle = null)
        {
            var button = new ImageButton().Icon(icon);
            button.HeightRequest = size;
            button.WidthRequest = size;

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

        private void UpdateLibraryCount(int LoadedItemsCount)
        {
            LoadedLibraryItemCount.Text = LoadedItemsCount + " Items";
        }

        private List<LibraryItem> SortLibraryItems(List<LibraryItem> items)
        {
            // Naive alphabetical sort, doesn't accomodate file structure
            return items.OrderBy(item => item.Key).ToList();
        }

        Label CreateLabel(string text, double width)
        {
            return new Label
            {
                Text = text,
                TextColor = Colors.White,
                WidthRequest = width,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Padding = new Thickness(5, 0, 0, 0),
                LineBreakMode = LineBreakMode.NoWrap
            };
        }

        private void Library_CellSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            // Do something with the selected item (e.SelectedItem)
            Trace.WriteLine("Selected Item", e.SelectedItem.ToString());

            // Deselect the item
            ((ListView)sender).SelectedItem = null;
        }

        public LibraryPanel(LibraryPanelViewModel viewModel)
        {
            // Library Count
            LoadedLibraryItemCount = new Label()
            {
                TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"],
                Padding = new Thickness(10, 5, 20, 5),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
            };

            LoadedItemsCount = viewModel.Library.Items.Count;
            UpdateLibraryCount(LoadedItemsCount);
            Tuple_LibraryItemDict = viewModel.Library.Items;

            // Clear existing items
            LibraryItems.Clear();

            // Initiating conversion sequence!
            foreach (var kvp in Tuple_LibraryItemDict)
            {
                LibraryItems.Add(new LibraryItem { Key = kvp.Key, Value = kvp.Value });
            }

            // Top / Bottom of Library Pane
            var MainPane = new StackLayout()
            {
                Padding = new Thickness(0, 25, 0, 0),
            };

            // SearchBar
            SearchBar SearchLibraryItems = new SearchBar
            {
                Style = (Style)App.Fixed_ResourceDictionary["DefaultSearchBar"]["Style"],
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Placeholder = "Search...",
            };

            var SearchPadding = new Microsoft.Maui.Controls.Frame
            {
                Padding = new Thickness(10, 5, 20, 5),
                HasShadow = true
            };

            // Horizontal Divider between Library Count and SearchBar
            Grid OverheadLibraryDisplay = new Grid
            {
                Padding = new Thickness(MainHorizontalPadding, 5, MainHorizontalPadding, 5),
                ColumnDefinitions =
                {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            Grid.SetColumn(LoadedLibraryItemCount, 0);
            Grid.SetRow(LoadedLibraryItemCount, 0);
            OverheadLibraryDisplay.Children.Add(LoadedLibraryItemCount);

            Grid.SetColumn(SearchLibraryItems, 1);
            Grid.SetRow(SearchLibraryItems, 0);
            OverheadLibraryDisplay.Children.Add(SearchLibraryItems);

            // Library Display
            Grid LibraryTable = new Grid
            {
                Padding = new Thickness(MainHorizontalPadding, 5, MainHorizontalPadding, 5),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            var SortedLibraryItems = SortLibraryItems(LibraryItems.ToList());

            var nameLabel = CreateLabel("Name", tabWidths.GetNameWidth());
            var useCountLabel = CreateLabel("Use Count", tabWidths.GetUseCountWidth());
            var dateModifiedLabel = CreateLabel("Date Modified", tabWidths.GetDateModifiedWidth());

            var LibraryTableTabs = new Border
            {
                Background = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryDark"],
                Stroke = (Color)App.Fixed_ResourceDictionary["Colors"]["White"],
                StrokeThickness = 0.5,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(25, 25, 0, 0)
                },
                Padding = new Thickness(MainHorizontalPadding, 0, MainHorizontalPadding, 0),
                HeightRequest = 25,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Content = new StackLayout
                {
                    Children =
                    {
                        nameLabel,
                        new DraggableSeparator(nameLabel, useCountLabel, tabWidths),
                        useCountLabel,
                        new DraggableSeparator(useCountLabel, dateModifiedLabel, tabWidths),
                        dateModifiedLabel,
                    },
                    Orientation = StackOrientation.Horizontal
                }
            };

            var EmptyView = new Grid
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children =
                        {
                            new Label
                            {
                                Text = "No library items.",
                                FontSize = 14,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                Padding = new Thickness(0, 0, 30, 0),
                            }
                        }
            };

            var LibraryItemList = new CollectionView
            {
                EmptyView = EmptyView,
                ItemsSource = LibraryItems,
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                ItemTemplate = new DataTemplate(() =>
                {
                    var LibraryCell_Entry = new Entry()
                    {
                        Style = (Style)App.Fixed_ResourceDictionary["DefaultEntry"]["Style"],
                        TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"],
                        IsSpellCheckEnabled = false
                    };

                    LibraryCell_Entry.SetBinding(Entry.TextProperty, "Key");

                    var LibraryCell_Icon = new ImageButton
                    {
                        Style = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Style"],
                    };

                    LibraryCell_Icon.Icon(MaterialIcons.QuestionMark);

                    //itemType dependent icons and context options
                    //switch (.Value.ItemType)
                    //{
                    //    case "bitmap":
                    //        LibraryCell_Icon.Icon(MaterialIcons.Image);
                    //        break;
                    //    case "sound":
                    //        LibraryCell_Icon.Icon(MaterialIcons.VolumeUp);
                    //        break;
                    //    case "graphic":
                    //        LibraryCell_Icon.Icon(MaterialIcons.Category);
                    //        break;
                    //    case "movie clip":
                    //        LibraryCell_Icon.Icon(MaterialIcons.Movie);
                    //        break;
                    //    case "font":
                    //        LibraryCell_Icon.Icon(MaterialIcons.ABC);
                    //        break;
                    //    case "button":
                    //        LibraryCell_Icon.Icon(MaterialIcons.SmartButton);
                    //        break;
                    //    case "folder":
                    //        LibraryCell_Icon.Icon(MaterialIcons.Folder);
                    //        break;
                    //    default:
                    //        LibraryCell_Icon.Icon(MaterialIcons.QuestionMark);
                    //        break;
                    //};

                    //Visually unfocus when enter button is pressed
                    LibraryCell_Entry.Completed += (sender, e) =>
                    {
                        LibraryCell_Entry.Unfocus();
                    };

                    var Library_StackCell = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Padding = new Thickness(0, 0),
                        Spacing = 10,
                        Children = { LibraryCell_Icon, LibraryCell_Entry }
                    };

                    return Library_StackCell;

                })
            };

            var scrollView = new ScrollView
            {
                Content = LibraryItemList,
                VerticalOptions = LayoutOptions.FillAndExpand,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            var scrollViewBorder = new Border
            {
                Padding = new Thickness(MainHorizontalPadding, 0, MainHorizontalPadding, 0),
                Content = scrollView,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Background = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryDark"],
                Stroke = (Color)App.Fixed_ResourceDictionary["Colors"]["White"],
            };

            Grid.SetColumn(LibraryTableTabs, 0);
            Grid.SetRow(LibraryTableTabs, 0);
            LibraryTable.Children.Add(LibraryTableTabs);

            Grid.SetColumn(scrollViewBorder, 0);
            Grid.SetRow(scrollViewBorder, 1);
            LibraryTable.Children.Add(scrollViewBorder);

            // SearchBar logic
            SearchLibraryItems.TextChanged += (sender, e) =>
            {
                string searchText = e.NewTextValue;
            };

            // Footer
            var footerGrid = new Grid
            {
                Padding = new Thickness(10, 5),
                HorizontalOptions = LayoutOptions.Start,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            int ButtonSize = 25;
            Style IconButtonStyle = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Style"];

            var NewSymbol = CreateIconButton(MaterialIcons.Add, ButtonSize, "New Symbol", IconButtonStyle);
            var NewFolder = CreateIconButton(MaterialIcons.Folder, ButtonSize, "New Folder", IconButtonStyle);
            var EditProperties = CreateIconButton(MaterialIcons.Help, ButtonSize, "Properties", IconButtonStyle);
            var Delete = CreateIconButton(MaterialIcons.Delete, ButtonSize, "Delete", IconButtonStyle);

            Grid.SetColumn(NewSymbol, 1);
            Grid.SetColumn(NewFolder, 2);
            Grid.SetColumn(EditProperties, 3);
            Grid.SetColumn(Delete, 4);

            footerGrid.Children.Add(NewSymbol);
            footerGrid.Children.Add(NewFolder);
            footerGrid.Children.Add(EditProperties);
            footerGrid.Children.Add(Delete);

            MainPane.Children.Add(OverheadLibraryDisplay);
            MainPane.Children.Add(LibraryTable);
            MainPane.Children.Add(footerGrid);

            Content = MainPane;
        }
    }
}