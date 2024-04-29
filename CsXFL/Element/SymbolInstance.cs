using System.Xml.Linq;
namespace CsXFL;

public class SymbolInstance : Instance
{
    private const double Epsilon = 0.0001;
    new public static class DefaultValues
    {
        public const int FirstFrame = 0;
        public const string SymbolType = "";
        public const string Loop = "loop";
        public const string BlendMode = "normal";
    }
    private static readonly HashSet<string> ACCEPTABLE_LOOP_VALUES = new HashSet<string> { "loop", "play once", "single frame", "loop reverse", "play once reverse" },
    ACCEPTABLE_BLENDMODE_VALUES = new HashSet<string> { "normal", "layer", "darken", "multiply", "lighten", "screen", "overlay", "hardlight", "add", "substract", "difference", "invert", "alpha", "erase" };
    private int firstFrame;
    private int? lastFrame;
    private string symbolType, loop, blendMode;
    public int FirstFrame { get { return firstFrame; } set { firstFrame = value; root?.SetOrRemoveAttribute("firstFrame", value, DefaultValues.FirstFrame); } }
    public int? LastFrame { get { return lastFrame; } set { lastFrame = value; root?.SetOrRemoveAttribute("lastFrame", value, null); } }
    public string SymbolType { get { return symbolType; } set { symbolType = value; root?.SetOrRemoveAttribute("symbolType", value, DefaultValues.SymbolType); } }
    public string Loop { get { return loop; } set { if (!ACCEPTABLE_LOOP_VALUES.Contains(value)) throw new ArgumentException("Invalid value for loop"); loop = value; root?.SetOrRemoveAttribute("loop", value, DefaultValues.Loop); } }
    public string BlendMode { get { return blendMode; } set { if (!ACCEPTABLE_BLENDMODE_VALUES.Contains(value)) throw new ArgumentException("Invalid value for blendMode"); blendMode = value; root?.SetOrRemoveAttribute("blendMode", value, DefaultValues.BlendMode); } }
    public override double Width { get { return GetWidthHelper(); } set { width = value; root?.SetOrRemoveAttribute("width", value, double.NaN); } }
    public override double Height { get { return GetHeightHelper(); } set { height = value; root?.SetOrRemoveAttribute("height", value, double.NaN); } }

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
    internal SymbolInstance(in XElement elementNode, Library? library) : base(elementNode, library)
    {
        firstFrame = (int?)elementNode.Attribute("firstFrame") ?? DefaultValues.FirstFrame;
        lastFrame = (int?)elementNode.Attribute("lastFrame");
        symbolType = (string?)elementNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        loop = (string?)elementNode.Attribute("loop") ?? DefaultValues.Loop;
        blendMode = (string?)elementNode.Attribute("blendMode") ?? DefaultValues.BlendMode;
    }
    internal SymbolInstance(ref SymbolInstance other) : base(other)
    {
        firstFrame = other.firstFrame;
        lastFrame = other.lastFrame;
        symbolType = other.symbolType;
        loop = other.loop;
        blendMode = other.blendMode;
    }
    // cast from SymbolItem to SymbolInstance
    internal SymbolInstance(in SymbolItem item, Library? library) : base(item, "symbol", "DOMSymbolInstance", library)
    {
        firstFrame = DefaultValues.FirstFrame;
        lastFrame = null;
        symbolType = item.SymbolType;
        loop = DefaultValues.Loop;
        blendMode = DefaultValues.BlendMode;
        root!.SetAttributeValue("symbolType", symbolType);
        root!.SetAttributeValue("loop", loop);
    }
}