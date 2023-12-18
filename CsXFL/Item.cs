using System.Xml.Linq;

namespace CsXFL;
public abstract class Item
{
    private static readonly List<string> AcceptableItemTypes = new List<string> {"undefined",
    "component", "movie clip", "graphic", "button", "folder", "font", "sound", "bitmap", "compiled clip",
    "screen", "video"};
    private readonly XElement? root;
    private string itemType, name;
    public string ItemType { get { return itemType; } }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public XElement? Root { get { return root; } }
    public Item()
    {
        root = null;
        itemType = string.Empty;
        name = string.Empty;
    }
    public Item(in XElement itemNode, string itemType)
    {
        if (!AcceptableItemTypes.Contains(itemType))
        {
            throw new ArgumentException("Invalid item type: " + itemType);
        }
        root = itemNode;
        this.itemType = itemType;
        name = (string)itemNode.Attribute("name")!; // all items have a name
    }
    public Item(in Item other)
    {
        root = other.root is null ? null : new XElement(other.root);
        itemType = other.itemType;
        name = other.name;
    }
}