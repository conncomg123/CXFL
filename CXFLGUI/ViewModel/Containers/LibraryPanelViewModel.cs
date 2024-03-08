using System.Collections.ObjectModel;
using CsXFL;

public class LibraryPanelViewModel
{
    private readonly Library library;
    private ObservableCollection<(string Name, Item Item)> libraryItems = new();
    public LibraryPanelViewModel(Library library)
    {
        this.library = library;
        foreach (var item in library.Items)
        {
            libraryItems.Add((item.Key, item.Value));
        }
    }
    public Library Library
    {
        get { return library; }
    }
    public ObservableCollection<(string Name, Item Item)> LibraryItems
    {
        get { return libraryItems; }
    }
}