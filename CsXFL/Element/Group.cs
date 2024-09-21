using System.Xml.Linq;

namespace CsXFL;

public class Group : Element, ILibraryEventReceiver
{
    public const string GROUP_NODE_IDENTIFIER = "DOMGroup", MEMBERS_NODEGROUP_IDENTIFIER = "members";
    private Library? library;
    private readonly List<Element> members;
    public List<Element> Members { get { return members; } }

    private void LoadMembers(XElement groupNode)
    {
        List<XElement>? memberNodes = groupNode.Element(ns + MEMBERS_NODEGROUP_IDENTIFIER)?.Elements().ToList();
        if (memberNodes is null) return;
        foreach (XElement memberNode in memberNodes)
        {
            string memberName = memberNode.Name.LocalName.ToString();
            switch (memberName)
            {
                case BitmapInstance.BITMAPINSTANCE_NODE_IDENTIFIER:
                    members.Add(new BitmapInstance(memberNode, library));
                    var CorrespondingItem = (members.Last() as BitmapInstance)!.CorrespondingItem;
                    if (CorrespondingItem is not null)
                        LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem, this);
                    break;
                case SymbolInstance.SYMBOLINSTANCE_NODE_IDENTIFIER:
                    members.Add(new SymbolInstance(memberNode, library));
                    CorrespondingItem = (members.Last() as SymbolInstance)!.CorrespondingItem;
                    if (CorrespondingItem is not null)
                        LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem, this);
                    break;
                case Text.STATIC_TEXT_NODE_IDENTIFIER:
                case Text.DYNAMIC_TEXT_NODE_IDENTIFIER:
                case Text.INPUT_TEXT_NODE_IDENTIFIER:
                    members.Add(new Text(memberNode));
                    break;
                case Shape.SHAPE_NODE_IDENTIFIER:
                    members.Add(new Shape(memberNode));
                    break;
                case GROUP_NODE_IDENTIFIER: // yes, groups can be nested 
                    members.Add(new Group(memberNode, library));
                    break;
            }
        }
    }
    public Group(XNamespace ns, Library? library) : base(ns)
    {
        members = new List<Element>();
        this.library = library;
        elementType = "group";
    }
    public Group(XElement groupNode, Library? library) : base(groupNode, "group")
    {
        members = new List<Element>();
        this.library = library;
        LoadMembers(groupNode);
    }

    public Group(Group other) : base(other)
    {
        members = new List<Element>();
        if(root is not null)
        {
            LoadMembers(root);
        }
    }
    void ILibraryEventReceiver.OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e)
    {
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRemoved)
        {
            for (int i = members.Count - 1; i >= 0; i--)
            {
                Element element = members[i];
                if (element is Instance instance && instance.CorrespondingItem == e.Item)
                {
                    members.RemoveAt(i);
                    instance.Root?.Remove();
                }
            }
        }
    }
}