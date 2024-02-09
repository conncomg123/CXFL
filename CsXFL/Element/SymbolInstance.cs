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
    }
    private int firstFrame;
    private int? lastFrame;
    private string symbolType, loop;
    public int FirstFrame { get { return firstFrame; } set { firstFrame = value; root?.SetOrRemoveAttribute("firstFrame", value, DefaultValues.FirstFrame); } }
    public int? LastFrame { get { return lastFrame; } set { lastFrame = value; root?.SetOrRemoveAttribute("lastFrame", value, null); } }
    public string SymbolType { get { return symbolType; } set { symbolType = value; root?.SetOrRemoveAttribute("symbolType", value, DefaultValues.SymbolType); } }
    public string Loop { get { return loop; } set { loop = value; root?.SetOrRemoveAttribute("loop", value, DefaultValues.Loop); } }
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
    internal SymbolInstance(in XElement elementNode) : base(elementNode)
    {
        firstFrame = (int?)elementNode.Attribute("firstFrame") ?? DefaultValues.FirstFrame;
        lastFrame = (int?)elementNode.Attribute("lastFrame");
        symbolType = (string?)elementNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        loop = (string?)elementNode.Attribute("loop") ?? DefaultValues.Loop;
    }
    internal SymbolInstance(ref SymbolInstance other) : base(other)
    {
        firstFrame = other.firstFrame;
        lastFrame = other.lastFrame;
        symbolType = other.symbolType;
        loop = other.loop;
    }
    // cast from SymbolItem to SymbolInstance
    internal SymbolInstance(in SymbolItem item) : base(item, "symbol", "DOMSymbolInstance")
    {
        firstFrame = DefaultValues.FirstFrame;
        lastFrame = null;
        symbolType = item.SymbolType;
        loop = DefaultValues.Loop;
        root!.SetAttributeValue("symbolType", symbolType);
        root!.SetAttributeValue("loop", loop);
    }
}