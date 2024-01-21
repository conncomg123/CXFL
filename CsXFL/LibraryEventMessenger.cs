namespace CsXFL;
public interface ILibraryEventReceiver
{
    void OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e);
}
public class LibraryEventMessenger
{
    private static LibraryEventMessenger? instance;
    private LibraryEventMessenger() { }
    private Dictionary<string, List<WeakReference<ILibraryEventReceiver>>> itemToReceiversMap = new();
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
    public void RegisterReceiver(string itemName, ILibraryEventReceiver receiver)
    {
        if (!itemToReceiversMap.ContainsKey(itemName))
        {
            itemToReceiversMap[itemName] = new List<WeakReference<ILibraryEventReceiver>>();
        }
        itemToReceiversMap[itemName].Add(new WeakReference<ILibraryEventReceiver>(receiver));
    }
    public void UnregisterReceiver(string itemName, ILibraryEventReceiver receiver)
    {
        if (itemToReceiversMap.TryGetValue(itemName, out var receivers))
        {
            receivers.RemoveAll(receiverRef => receiverRef.TryGetTarget(out var target) && ReferenceEquals(target, receiver));
            if (receivers.Count == 0)
            {
                itemToReceiversMap.Remove(itemName);
            }
        }
    }
    public void NotifyItemRenamed(string oldName, string newName)
    {
        if (itemToReceiversMap.TryGetValue(oldName, out var receivers))
        {
            foreach (var receiverRef in receivers)
            {
                if (receiverRef.TryGetTarget(out var receiver))
                {
                    receiver.OnLibraryEvent(this, new LibraryEventArgs { OldName = oldName, NewName = newName, EventType = LibraryEvent.ItemRenamed });
                }
            }
            itemToReceiversMap.Remove(oldName);
            itemToReceiversMap[newName] = receivers;
        }
    }
    public void NotifyItemRemoved(string name)
    {
        if (itemToReceiversMap.TryGetValue(name, out var receivers))
        {
            for (int i = receivers.Count - 1; i >= 0; i--)
            {
                var receiverRef = receivers[i];
                if (receiverRef.TryGetTarget(out var receiver))
                {
                    receiver.OnLibraryEvent(this, new LibraryEventArgs { Name = name, EventType = LibraryEvent.ItemRemoved });
                }
            }
            itemToReceiversMap.Remove(name);
        }
    }
}