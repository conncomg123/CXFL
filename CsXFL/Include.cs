using System.Xml.Linq;

namespace CsXFL;
// class containing the Include node for SymbolItems
public class Include 
{
    private XElement? root;
    public XElement? Root { get { return root; } }
    private string href;
    public string Href { get { return href; } set { href = value; root?.SetAttributeValue("href", value); } }
    public Include()
    {
        root = null;
        href = string.Empty;
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