using System.Xml.Linq;

namespace CsXFL;
public class SymbolItem : Item
{
    internal const string SYMBOLITEM_NODE_IDENTIFIER = "DOMSymbolItem",
    SYMBOLITEM_TIMELINE_NODE_IDENTIFIER = "timeline";
    public static class DefaultValues
    {
        public const string SymbolType = "movie clip";
    }
    private static readonly HashSet<string> AcceptableSymbolTypes = new HashSet<string> { "graphic", "button", "movie clip", "puppet", "puppetBase" };
    private readonly Include include;
    private readonly string symbolType;
    private readonly Lazy<Timeline> timeline;
    public string SymbolType
    {
        get { return symbolType; }
        set
        {
            if (!AcceptableSymbolTypes.Contains(value)) throw new ArgumentException("Invalid symbol type: " + value);
            Root?.SetOrRemoveAttribute("symbolType", value, DefaultValues.SymbolType);
        }
    }
    internal Include Include { get { return include; } }
    public Timeline Timeline { get { return timeline.Value; } }
    internal SymbolItem() : base()
    {
        ns = string.Empty;
        symbolType = DefaultValues.SymbolType;
        timeline = new Lazy<Timeline>(() => new Timeline());
        include = new Include();
    }
    internal SymbolItem(XNamespace ns, string name, Library library) : base(DefaultValues.SymbolType, name, ns)
    {
        symbolType = DefaultValues.SymbolType;
        timeline = new Lazy<Timeline>(() => new Timeline(ns, library, name));
        include = new Include(ns, name + ".xml");
        root = new XElement(ns + SymbolItem.SYMBOLITEM_NODE_IDENTIFIER);
        root.SetAttributeValue("name", name);
        root.SetAttributeValue("symbolType", symbolType);
    }
    internal SymbolItem(XElement symbolItemNode, in XElement include, Library? library, string? timelineName = null) : base(symbolItemNode, (string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType)
    {
        if (!AcceptableSymbolTypes.Contains((string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType))
        {
            throw new ArgumentException("Invalid symbol type: " + (string)symbolItemNode.Attribute("symbolType")!);
        }
        symbolType = (string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        timeline = new Lazy<Timeline>(() =>
        {
            var newTimeline = new Timeline(symbolItemNode.Element(ns + SymbolItem.SYMBOLITEM_TIMELINE_NODE_IDENTIFIER)!.Element(ns + Timeline.TIMELINE_NODE_IDENTIFIER)!, library)
            {
                Name = timelineName ?? Name.Substring(Name.LastIndexOf('/') + 1)
            };
            return newTimeline;
        });
        this.include = new Include(include);
    }
    internal SymbolItem(SymbolItem other) : base(other)
    {
        symbolType = other.symbolType;
        timeline = new Lazy<Timeline>(() => new Timeline(other.timeline.Value));
        include = new Include(other.include);
    }
    internal static SymbolItem FromFile(string path, Library? library = null, string? timelineName = null)
    {
        XDocument? xflTree = XDocument.Load(path);
        if (xflTree.Root is null)
        {
            throw new InvalidDataException("Invalid XFL file: " + path);
        }
        XNamespace ns = xflTree.Root.Name.Namespace;
        XElement includeNode = new(ns + Include.INCLUDE_NODE_IDENTIFIER);
        includeNode.SetAttributeValue("href", path);
        return new SymbolItem(xflTree.Root, includeNode, library, timelineName);
    }
}