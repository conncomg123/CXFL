namespace CsXFL;
public class LibraryEventMessenger
{
    private static LibraryEventMessenger? instance;
    private LibraryEventMessenger() { }

    public static LibraryEventMessenger Instance
    {
        get
        {
            instance ??= new LibraryEventMessenger();
            return instance;
        }
    }
    public class LibraryEventArgs : EventArgs
    {
        public string? OldName { get; set; }
        public string? NewName { get; set; }
        public string? Name { get; set; }
        public LibraryEvent EventType { get; set; }
    }

    public enum LibraryEvent
    {
        ItemRenamed,
        ItemRemoved
    }

    public delegate void LibraryEventHandler(object sender, LibraryEventArgs e);
    public event LibraryEventHandler? OnLibraryEvent;

    public void NotifyItemRenamed(string oldName, string newName)
    {
        OnLibraryEvent?.Invoke(this, new LibraryEventArgs { OldName = oldName, NewName = newName, EventType = LibraryEvent.ItemRenamed });
    }

    public void NotifyItemRemoved(string name)
    {
        OnLibraryEvent?.Invoke(this, new LibraryEventArgs { Name = name, EventType = LibraryEvent.ItemRemoved });
    }
}