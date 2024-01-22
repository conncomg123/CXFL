using System.Xml.Linq;
namespace CsXFL;

public class Instance : Element, ILibraryEventReceiver
{
    private static readonly Dictionary<string, string> NodeNameToInstanceType = new Dictionary<string, string>
    {
        {"DOMSymbolInstance", "symbol"},
        {"DOMBitmapInstance", "bitmap"},
        {"DOMVideoInstance", "video"},
        {"DOMCompiledClipInstance", "compiled clip"}
    };
    private static readonly HashSet<string> AcceptableInstanceTypes = new HashSet<string> {"symbol", "bitmap", "embedded video", "linked video", "video",  "compiled clip"};
    private readonly string instanceType;
    private string libraryItemName;
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

    internal Instance(in XElement elementNode) : base(elementNode, "instance")
    {
        instanceType = NodeNameToInstanceType[elementNode.Name.LocalName];
        if (!AcceptableInstanceTypes.Contains(instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + instanceType);
        }
        libraryItemName = (string?)elementNode.Attribute("libraryItemName") ?? string.Empty;
        LibraryEventMessenger.Instance.RegisterReceiver(libraryItemName, this);
    }
    internal Instance(in Instance other) : base(other)
    {
        if (!AcceptableInstanceTypes.Contains(other.instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + other.instanceType);
        }
        instanceType = other.instanceType;
        libraryItemName = other.libraryItemName;
        LibraryEventMessenger.Instance.RegisterReceiver(libraryItemName, this);
    }
    internal Instance(in Item item, string instanceType, string nodeName) : base(item, "instance", nodeName)
    {
        if (!AcceptableInstanceTypes.Contains(instanceType))
        {
            throw new ArgumentException("Invalid instance type: " + instanceType);
        }
        this.instanceType = instanceType;
        libraryItemName = item.Name;
        root!.SetAttributeValue("libraryItemName", libraryItemName);
        LibraryEventMessenger.Instance.RegisterReceiver(libraryItemName, this);
    }
    ~Instance()
    {
        LibraryEventMessenger.Instance.UnregisterReceiver(libraryItemName, this);
    }
    void ILibraryEventReceiver.OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e)
    {
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRenamed && libraryItemName == e.OldName)
        {
            LibraryItemName = e.NewName!;
        }
    }
}