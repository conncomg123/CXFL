using System.Security.Cryptography;
using System.Xml.Linq;

namespace CsXFL;
public class SymbolItem : Item
{
    public static class DefaultValues
    {
        public const string SymbolType = "movie clip";
    }
    private static readonly HashSet<string> AcceptableSymbolTypes = new HashSet<string> { "graphic", "button", "movie clip" };
    private Include include;
    private readonly string symbolType;
    private readonly Timeline timeline;
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
    public Timeline Timeline { get { return timeline; } }
    internal SymbolItem() : base()
    {
        ns = string.Empty;
        symbolType = string.Empty;
        timeline = new Timeline();
        include = new Include();
    }
    internal SymbolItem(in XElement symbolItemNode, in XElement include) : base(symbolItemNode, (string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType)
    {
        if (!AcceptableSymbolTypes.Contains((string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType))
        {
            throw new ArgumentException("Invalid symbol type: " + (string)symbolItemNode.Attribute("symbolType")!);
        }
        symbolType = (string?)symbolItemNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        timeline = new Timeline(symbolItemNode.Element(ns + "timeline")!.Element(ns + "DOMTimeline")!);
        this.include = new Include(include);
    }
    internal SymbolItem(in SymbolItem other) : base(other)
    {
        symbolType = other.symbolType;
        timeline = new Timeline(other.timeline);
        include = new Include(other.include);
    }
}