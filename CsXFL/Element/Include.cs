using System.Xml.Linq;

namespace CsXFL;
// class containing the Include node for SymbolItems
internal class Include 
{
    internal const string INCLUDE_NODE_IDENTIFIER = "Include";
    private readonly XElement? root;
    internal XElement? Root { get { return root; } }
    private string href;
    public string Href { get { return href; } set { href = value; root?.SetAttributeValue("href", value); } }
    public Include()
    {
        root = null;
        href = string.Empty;
    }
    public Include(XNamespace ns, string href)
    {
        root = new XElement(ns + Include.INCLUDE_NODE_IDENTIFIER);
        this.href = href;
        root.SetAttributeValue("href", href);
    }
    public Include(in XElement includeNode)
    {
        root = includeNode;
        href = (string)includeNode.Attribute("href")!;
    }
    public Include(in Include other)
    {
        root = other.root is null ? null : new XElement(other.root);
        href = other.href;
    }
}