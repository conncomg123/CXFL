using System.Xml.Linq;

namespace CsXFL;
public abstract class Item
{
    private static readonly HashSet<string> AcceptableItemTypes = new HashSet<string> {"undefined",
    "component", "movie clip", "graphic", "button", "puppet", "puppetBase", "folder", "font", "sound", "bitmap", "compiled clip",
    "screen", "video"};
    protected XElement? root;
    protected XNamespace ns;
    private readonly string itemType;
    private string name;
    private int useCount;
    public string ItemType { get { return itemType; } }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public int UseCount { get { return useCount; } set { useCount = value; } }
    public XNamespace Namespace { get { return ns; } }
    public XElement? Root { get { return root; } }
    internal Item()
    {
        root = null;
        itemType = string.Empty;
        name = string.Empty;
        ns = string.Empty;
    }
    internal Item(string itemType, string name, XNamespace ns)
    {
        if (!AcceptableItemTypes.Contains(itemType))
        {
            throw new ArgumentException("Invalid item type: " + itemType);
        }
        this.itemType = itemType;
        this.name = name;
        this.ns = ns;
    }
    internal Item(in XElement itemNode, string itemType)
    {
        if (!AcceptableItemTypes.Contains(itemType))
        {
            throw new ArgumentException("Invalid item type: " + itemType);
        }
        root = itemNode;
        this.itemType = itemType;
        name = (string)itemNode.Attribute("name")!; // all items have a name
        ns = root.Name.NamespaceName;
    }
    internal Item(in Item other)
    {
        root = other.root is null ? null : new XElement(other.root);
        itemType = other.itemType;
        name = other.name;
        ns = other.ns;
    }
}