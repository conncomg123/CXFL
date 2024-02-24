using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsXFL;
using static MainViewModel;

namespace CXFLGUI
{
    public class LibraryPanel : VanillaFrame
    {
        private MainViewModel viewModel;
        private Label Label_LibraryCount;

        int LoadedItemsCount = 0;

        Dictionary<string, CsXFL.Item> Tuple_LibraryItemDict = new Dictionary<string, CsXFL.Item>();
        ObservableCollection<string> String_LibraryItemList = new ObservableCollection<string>();

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
                ItemsSource = String_LibraryItemList
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
            String_LibraryItemList.Clear();

            // Initiating conversion sequence!
            foreach (var kvp in Tuple_LibraryItemDict)
            {
                String_LibraryItemList.Add(kvp.Key);
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