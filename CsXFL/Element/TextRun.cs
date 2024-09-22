using System.Xml.Linq;

namespace CsXFL;
public class TextRun
{
    internal const string TEXT_RUN_NODE_IDENTIFIER = "DOMTextRun",
    TEXTRUNS_NODEGROUP_IDENTIFIER = "textRuns",
    CHARACTERS_NODE_IDENTIFIER = "characters";
    private readonly XElement? root;
    private readonly XNamespace ns;
    private string characters;
    private readonly TextAttrs textAttrs;
    public string Characters { get { return characters; } set { characters = value; root?.SetAttributeValue(TextRun.CHARACTERS_NODE_IDENTIFIER, value); } }
    public TextAttrs TextAttrs { get { return textAttrs; } }
    public XElement Root { get { return root!; } }
    internal TextRun(in XElement elementNode)
    {
        root = elementNode;
        ns = root.Name.Namespace;
        characters = (string?)elementNode.Element(ns + TextRun.CHARACTERS_NODE_IDENTIFIER) ?? string.Empty;
        textAttrs = new TextAttrs(elementNode.Element(ns + TextAttrs.TEXTATTRS_NODEGROUP_IDENTIFIER)!.Element(ns + TextAttrs.TEXTATTRS_NODE_IDENTIFIER)!);
    }
    internal TextRun(string characters, XNamespace ns)
    {
        root = new XElement(ns + TextRun.TEXT_RUN_NODE_IDENTIFIER);
        this.ns = ns;
        this.characters = characters;
        textAttrs = new TextAttrs(ns);
        root.Add(new XElement(ns + TextRun.CHARACTERS_NODE_IDENTIFIER, characters));
        root.Add(new XElement(ns + TextAttrs.TEXTATTRS_NODEGROUP_IDENTIFIER, textAttrs.Root));
    }
    internal TextRun(in TextRun other)
    {
        root = other.root == null ? null : new XElement(other.root);
        ns = other.ns;
        characters = other.characters;
        textAttrs = new TextAttrs(root!.Element(ns + TextAttrs.TEXTATTRS_NODEGROUP_IDENTIFIER)!.Element(ns + TextAttrs.TEXTATTRS_NODE_IDENTIFIER)!);
    }
}