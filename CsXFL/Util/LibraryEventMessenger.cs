namespace CsXFL;
internal interface ILibraryEventReceiver
{
    internal void OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e);
}
public class LibraryEventMessenger
{
    private static LibraryEventMessenger? instance;
    private LibraryEventMessenger() { }
    private readonly Dictionary<Item, List<WeakReference<ILibraryEventReceiver>>> itemToReceiversMap = new();
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
        public Item? Item { get; set; }
        public LibraryEvent EventType { get; set; }
    }

    public enum LibraryEvent
    {
        ItemRenamed,
        ItemRemoved
    }
internal void RegisterReceiver(Item item, ILibraryEventReceiver receiver)
{
    if (!itemToReceiversMap.TryGetValue(item, out var receivers))
    {
        receivers = new List<WeakReference<ILibraryEventReceiver>>();
        itemToReceiversMap[item] = receivers;
    }
    receivers.Add(new WeakReference<ILibraryEventReceiver>(receiver));
}
    internal void UnregisterReceiver(Item item, ILibraryEventReceiver receiver)
    {
        if (itemToReceiversMap.TryGetValue(item, out var receivers))
        {
            receivers.RemoveAll(receiverRef => receiverRef.TryGetTarget(out var target) && ReferenceEquals(target, receiver));
            if (receivers.Count == 0)
            {
                itemToReceiversMap.Remove(item);
            }
        }
    }
    internal void NotifyItemRenamed(string oldName, string newName, Item item)
    {
        if (itemToReceiversMap.TryGetValue(item, out var receivers))
        {
            for (int i = receivers.Count - 1; i >= 0; i--)
            {
                var receiverRef = receivers[i];
                if (receiverRef.TryGetTarget(out var receiver))
                {
                    receiver.OnLibraryEvent(this, new LibraryEventArgs { OldName = oldName, NewName = newName, EventType = LibraryEvent.ItemRenamed });
                }
            }
        }
    }
    internal void NotifyItemRemoved(Item item)
    {
        if (itemToReceiversMap.TryGetValue(item, out var receivers))
        {
            for (int i = receivers.Count - 1; i >= 0; i--)
            {
                var receiverRef = receivers[i];
                if (receiverRef.TryGetTarget(out var receiver))
                {
                    receiver.OnLibraryEvent(this, new LibraryEventArgs { Item = item, EventType = LibraryEvent.ItemRemoved });
                }
            }
            itemToReceiversMap.Remove(item);
        }
    }
}