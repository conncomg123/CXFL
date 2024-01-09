using System.Xml.Linq;

namespace CsXFL;
public class SymbolItem : Item
{
    private static readonly List<string> AcceptableSymbolTypes = new List<string> { "graphic", "button", "movie clip" };
    private readonly string symbolType;
    private readonly Timeline timeline;
    public string SymbolType { get { return symbolType; } }
    public Timeline Timeline { get { return timeline; } }
    public SymbolItem() : base()
    {
        ns = string.Empty;
        symbolType = string.Empty;
        timeline = new Timeline();
    }
    public SymbolItem(in XElement symbolItemNode) : base(symbolItemNode, (string)symbolItemNode.Attribute("symbolType")!)
    {
        if (!AcceptableSymbolTypes.Contains((string)symbolItemNode.Attribute("symbolType")!))
        {
            throw new ArgumentException("Invalid symbol type: " + (string)symbolItemNode.Attribute("symbolType")!);
        }
        symbolType = (string)symbolItemNode.Attribute("symbolType")!;
        timeline = new Timeline(symbolItemNode.Element(ns + "timeline")!.Element(ns + "DOMTimeline")!);
    }
    public SymbolItem(in SymbolItem other) : base(other)
    {
        symbolType = other.symbolType;
        timeline = new Timeline(other.timeline);
    }
}