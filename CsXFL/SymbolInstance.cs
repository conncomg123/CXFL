using System;
using System.Xml.Linq;
namespace CsXFL;

public class SymbolInstance : Instance
{
    private const double Epsilon = 0.0001;
    new public static class DefaultValues
    {
        public const uint FirstFrame = 0;
        public const string SymbolType = "";
        public const string Loop = "";
    }
    private uint firstFrame;
    private uint? lastFrame;
    private string symbolType, loop;
    public uint FirstFrame { get { return firstFrame; } set { firstFrame = value; root?.SetOrRemoveAttribute("firstFrame", value, DefaultValues.FirstFrame); } }
    public uint? LastFrame { get { return lastFrame; } set { lastFrame = value; root?.SetOrRemoveAttribute("lastFrame", value, null); } }
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
    public SymbolInstance(in XElement elementNode) : base(elementNode)
    {
        firstFrame = (uint?)elementNode.Attribute("firstFrame") ?? DefaultValues.FirstFrame;
        lastFrame = (uint?)elementNode.Attribute("lastFrame");
        symbolType = (string?)elementNode.Attribute("symbolType") ?? DefaultValues.SymbolType;
        loop = (string?)elementNode.Attribute("loop") ?? DefaultValues.Loop;
    }
    public SymbolInstance(ref SymbolInstance other) : base(other)
    {
        firstFrame = other.firstFrame;
        lastFrame = other.lastFrame;
        symbolType = other.symbolType;
        loop = other.loop;
    }
}