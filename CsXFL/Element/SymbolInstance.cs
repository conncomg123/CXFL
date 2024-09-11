using System.Collections.ObjectModel;
using System.Xml.Linq;
namespace CsXFL;

public class SymbolInstance : Instance
{
    internal const string SYMBOLINSTANCE_NODE_IDENTIFIER = "DOMSymbolInstance";
    private const double Epsilon = 0.0001;
    new public static class DefaultValues
    {
        public const int FirstFrame = 0;
        public const string SymbolType = "";
        public const string Loop = "loop";
        public const string BlendMode = "normal";
    }
    private static readonly HashSet<string> ACCEPTABLE_SYMBOL_TYPES = new HashSet<string> { "graphic", "button", "movie clip", "puppet", "puppetBase" },
    ACCEPTABLE_LOOP_VALUES = new HashSet<string> { "loop", "play once", "single frame", "loop reverse", "play once reverse" },
    ACCEPTABLE_BLENDMODE_VALUES = new HashSet<string> { "normal", "layer", "darken", "multiply", "lighten", "screen", "overlay", "hardlight", "add", "substract", "difference", "invert", "alpha", "erase" };
    private Color color;
    private int firstFrame;
    private int? lastFrame;
    private string symbolType, loop, blendMode;
    private List<Filter> filters;
    public Color Color { get { return color; } }
    public int FirstFrame { get { return firstFrame; } set { firstFrame = value; root?.SetOrRemoveAttribute("firstFrame", value, DefaultValues.FirstFrame); } }
    public int? LastFrame { get { return lastFrame; } set { lastFrame = value; root?.SetOrRemoveAttribute("lastFrame", value, null); } }
    public string SymbolType { get { return symbolType; } set { if (!ACCEPTABLE_SYMBOL_TYPES.Contains(value)) throw new ArgumentException("Invalid value for symbolType: " + value); symbolType = value; root?.SetOrRemoveAttribute("symbolType", value, DefaultValues.SymbolType); } }
    public string Loop { get { return loop; } set { if (!ACCEPTABLE_LOOP_VALUES.Contains(value)) throw new ArgumentException("Invalid value for loop: " + value); loop = value; root?.SetOrRemoveAttribute("loop", value, DefaultValues.Loop); } }
    public string BlendMode { get { return blendMode; } set { if (!ACCEPTABLE_BLENDMODE_VALUES.Contains(value)) throw new ArgumentException("Invalid value for blendMode: " + value); blendMode = value; root?.SetOrRemoveAttribute("blendMode", value, DefaultValues.BlendMode); } }
    public override double Width { get { return GetWidthHelper(); } set { width = value; root?.SetOrRemoveAttribute("width", value, double.NaN); } }
    public override double Height { get { return GetHeightHelper(); } set { height = value; root?.SetOrRemoveAttribute("height", value, double.NaN); } }
    public ReadOnlyCollection<Filter> Filters { get { return new ReadOnlyCollection<Filter>(filters); } }

    private double GetWidthHelper()
    {
        // todo: implement
        throw new NotImplementedException("pls be patient");
    }
    private double GetHeightHelper()
    {
        // todo: implement
        throw new NotImplementedException("pls be patient");
    }
    private void LoadFilters(in XElement symbolInstanceNode)
    {
        List<XElement>? filterNodes = symbolInstanceNode.Element(ns + Filter.FILTER_NODEGROUP_IDENTIFIER)?.Elements().ToList();
        if (filterNodes is null) return;
        foreach (XElement filterNode in filterNodes)
        {
            switch (filterNode.Name.LocalName)
            {
                case DropShadowFilter.DROPSHADOWFILTER_NODE_IDENTIFIER:
                    filters.Add(new DropShadowFilter(filterNode));
                    break;
                case BlurFilter.BLURFILTER_NODE_IDENTIFIER:
                    filters.Add(new BlurFilter(filterNode));
                    break;
                case GlowFilter.GLOWFILTER_NODE_IDENTIFIER:
                    filters.Add(new GlowFilter(filterNode));
                    break;
                case BevelFilter.BEVELFILTER_NODE_IDENTIFIER:
                    filters.Add(new BevelFilter(filterNode));
                    break;
                case GradientBevelFilter.GRADIENTBEVELFILTER_NODE_IDENTIFIER:
                    filters.Add(new GradientBevelFilter(filterNode));
                    break;
                case GradientGlowFilter.GRADIENTGLOWFILTER_NODE_IDENTIFIER:
                    filters.Add(new GradientGlowFilter(filterNode));
                    break;
                case AdjustColorFilter.ADJUSTCOLORFILTER_NODE_IDENTIFIER:
                    filters.Add(new AdjustColorFilter(filterNode));
                    break;
                default:
                    throw new ArgumentException("Invalid filter type: " + filterNode.Name.LocalName);
            }
        }
    }
    internal SymbolInstance(in XElement elementNode, Library? library) : base(elementNode, library)
    {
        XElement? colorNode = elementNode.Element(ns + Color.COLOR_NODEGROUP_IDENTIFIER)?.Element(ns + Color.COLOR_NODE_IDENTIFIER);
        if (colorNode is null) color = new Color(ns);
        else color = new Color(colorNode);
        firstFrame = (int?)elementNode.Attribute("firstFrame") ?? DefaultValues.FirstFrame;
        lastFrame = (int?)elementNode.Attribute("lastFrame");
        symbolType = (string?)elementNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        loop = (string?)elementNode.Attribute("loop") ?? DefaultValues.Loop;
        blendMode = (string?)elementNode.Attribute("blendMode") ?? DefaultValues.BlendMode;
        filters = new List<Filter>();
        LoadFilters(root!);
    }
    internal SymbolInstance(ref SymbolInstance other) : base(other)
    {
        color = new Color(other.color);
        firstFrame = other.firstFrame;
        lastFrame = other.lastFrame;
        symbolType = other.symbolType;
        loop = other.loop;
        blendMode = other.blendMode;
        filters = new List<Filter>(other.filters);
    }
    // cast from SymbolItem to SymbolInstance
    internal SymbolInstance(in SymbolItem item, Library? library) : base(item, "symbol", SymbolInstance.SYMBOLINSTANCE_NODE_IDENTIFIER, library)
    {
        color = new Color(ns);
        firstFrame = DefaultValues.FirstFrame;
        lastFrame = null;
        symbolType = item.SymbolType;
        loop = DefaultValues.Loop;
        blendMode = DefaultValues.BlendMode;
        root!.SetAttributeValue("symbolType", symbolType);
        root!.SetAttributeValue("loop", loop);
        root!.SetAttributeValue("blendMode", blendMode);
        filters = new List<Filter>();
    }
    public Filter AddFilter(string filterName)
    {
        if (symbolType != "movie clip") throw new InvalidOperationException("Filters can only be added to movie clips.");
        Filter filter = filterName switch
        {
            DropShadowFilter.DROPSHADOWFILTER_NODE_IDENTIFIER => new DropShadowFilter(ns),
            BlurFilter.BLURFILTER_NODE_IDENTIFIER => new BlurFilter(ns),
            GlowFilter.GLOWFILTER_NODE_IDENTIFIER => new GlowFilter(ns),
            BevelFilter.BEVELFILTER_NODE_IDENTIFIER => new BevelFilter(ns),
            GradientBevelFilter.GRADIENTBEVELFILTER_NODE_IDENTIFIER => new GradientBevelFilter(ns),
            GradientGlowFilter.GRADIENTGLOWFILTER_NODE_IDENTIFIER => new GradientGlowFilter(ns),
            AdjustColorFilter.ADJUSTCOLORFILTER_NODE_IDENTIFIER => new AdjustColorFilter(ns),
            _ => throw new ArgumentException("Invalid filter type: " + filterName)
        };
        filters.Add(filter);
        if (root!.Element(ns + Filter.FILTER_NODEGROUP_IDENTIFIER) is null)
        {
            root!.Add(new XElement(ns + Filter.FILTER_NODEGROUP_IDENTIFIER));
        }
        root!.Element(ns + Filter.FILTER_NODEGROUP_IDENTIFIER)!.Add(filter.Root);
        return filter;
    }
    public void RemoveFilter(Filter filter)
    {
        if (!filters.Remove(filter)) throw new ArgumentException("Filter not found");
        filter.Root!.Remove();
    }
    public void RemoveFilter(int index)
    {
        if (index < 0 || index >= filters.Count) throw new ArgumentOutOfRangeException("index");
        filters[index].Root!.Remove();
        filters.RemoveAt(index);
    }
    public void SetFilterAttr(int filterIndex, string attrName, object value)
    {
        filters[filterIndex].SetAttr(attrName, value);
    }
}