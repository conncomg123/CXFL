using System.Xml.Linq;

public class Instance : Element
{
    private readonly string instanceType;
    private string libraryItem;
    public string InstanceType { get { return instanceType; } }
    public string LibraryItem
    {
        get { return libraryItem; }
        set
        {
            libraryItem = value;
            root?.SetAttributeValue("libraryItem", value);
        }
    }

    public Instance(in XElement elementNode) : base(elementNode, "instance")
    {
        instanceType = (string?)elementNode.Attribute("instanceType") ?? string.Empty;
        libraryItem = (string?)elementNode.Attribute("libraryItem") ?? string.Empty;
    }
    public Instance(in Instance other) : base(other)
    {
        instanceType = other.instanceType;
        libraryItem = other.libraryItem;
    }

}