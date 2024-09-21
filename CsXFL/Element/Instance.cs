using System.Xml.Linq;
namespace CsXFL;

public class Instance : Element, ILibraryEventReceiver, IDisposable
{
    private static readonly Dictionary<string, string> NodeNameToInstanceType = new Dictionary<string, string>
    {
        {SymbolInstance.SYMBOLINSTANCE_NODE_IDENTIFIER, "symbol"},
        {BitmapInstance.BITMAPINSTANCE_NODE_IDENTIFIER, "bitmap"},
        {"DOMVideoInstance", "video"},
        {"DOMCompiledClipInstance", "compiled clip"}
    };
    private static readonly HashSet<string> AcceptableInstanceTypes = new HashSet<string> { "symbol", "bitmap", "embedded video", "linked video", "video", "compiled clip" };
    private readonly string instanceType;
    private string libraryItemName;
    private Library? library;
    public string InstanceType { get { return instanceType; } }
    public string LibraryItemName
    {
        get { return libraryItemName; }
        set
        {
            libraryItemName = value;
            root?.SetAttributeValue("libraryItemName", value);
        }
    }
    public Item? CorrespondingItem
    {
        get
        {
            if(library is null) return null;
            return library.Items.TryGetValue(libraryItemName, out var item) ? item : null;
        }
    }

    internal Instance(in XElement elementNode, Library? library) : base(elementNode, "instance")
    {
        instanceType = NodeNameToInstanceType[elementNode.Name.LocalName];
        if (!AcceptableInstanceTypes.Contains(instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + instanceType);
        }
        libraryItemName = (string?)elementNode.Attribute("libraryItemName") ?? string.Empty;
        this.library = library;
        if (library is not null && CorrespondingItem is not null)
        {
            LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem, this);
            library.Items[libraryItemName].UseCount++;
        }
    }
    internal Instance(in Instance other) : base(other)
    {
        if (!AcceptableInstanceTypes.Contains(other.instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + other.instanceType);
        }
        instanceType = other.instanceType;
        libraryItemName = other.libraryItemName;
        library = other.library;
        if (library is not null && CorrespondingItem is not null)
        {
            LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem!, this);
            library.Items[libraryItemName].UseCount++;
        }
    }
    internal Instance(in Item item, string instanceType, string nodeName, Library? library) : base(item, "instance", nodeName)
    {
        if (!AcceptableInstanceTypes.Contains(instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + instanceType);
        }
        this.instanceType = instanceType;
        libraryItemName = item.Name;
        this.library = library;
        root!.SetAttributeValue("libraryItemName", libraryItemName);
        if (library is not null && CorrespondingItem is not null)
        {
            LibraryEventMessenger.Instance.RegisterReceiver(item, this);
            library.Items[libraryItemName].UseCount++;
        }
    }
    public void Dispose()
    {
        if (library is null || CorrespondingItem is null) return;
        LibraryEventMessenger.Instance.UnregisterReceiver(CorrespondingItem!, this);
        library.Items[libraryItemName].UseCount--;
    }
    void ILibraryEventReceiver.OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e)
    {
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRenamed && libraryItemName == e.OldName)
        {
            LibraryItemName = e.NewName!;
        }
    }
}