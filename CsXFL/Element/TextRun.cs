using System.Xml.Linq;

namespace CsXFL;
public class TextRun
{
    private XElement? root;
    private XNamespace ns;
    private string characters;
    private TextAttrs textAttrs;
    public string Characters { get { return characters; } set { characters = value; root?.SetAttributeValue("characters", value); } }
    public TextAttrs TextAttrs { get { return textAttrs; } }
    public XElement Root { get { return root!; } }
    public TextRun(in XElement elementNode)
    {
        root = elementNode;
        ns = root.Name.Namespace;
        characters = (string?)elementNode.Attribute("characters") ?? string.Empty;
        textAttrs = new TextAttrs(elementNode.Element(ns + "textAttrs")!.Element(ns + "DOMTextAttrs")!);
    }
    public TextRun(string characters, XNamespace ns)
    {
        root = new XElement(ns + "DOMTextRun");
        this.ns = ns;
        this.characters = characters;
        textAttrs = new TextAttrs(ns);
        root.Add(new XElement(ns + "characters", characters));
        root.Add(new XElement(ns + "textAttrs", textAttrs.Root));
    }
    public TextRun(in TextRun other)
    {
        root = other.root == null ? null : new XElement(other.root);
        ns = other.ns;
        characters = other.characters;
        textAttrs = new TextAttrs(root!.Element(ns + "textAttrs")!.Element(ns + "DOMTextAttrs")!);
    }
}