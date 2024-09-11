using System.Xml.Linq;

namespace CsXFL;

public class Color
{
    public const string COLOR_NODEGROUP_IDENTIFIER = "color", COLOR_NODE_IDENTIFIER = "Color";

    private readonly XNamespace ns;
    private readonly XElement? root;
    public static class DefaultValues
    {
        public const double Brightness = 0;
        public const double TintMultiplier = 0;
        public const double AlphaMultiplier = 1;
        public const double RedMultiplier = 1;
        public const double GreenMultiplier = 1;
        public const double BlueMultiplier = 1;
        public const string TintColor = "#000000";
        public const int AlphaOffset = 0;
        public const int RedOffset = 0;
        public const int GreenOffset = 0;
        public const int BlueOffset = 0;

    }
    private double brightness, tintMultiplier, alphaMultiplier, redMultiplier, greenMultiplier, blueMultiplier;
    private string tintColor;
    private int alphaOffset, redOffset, greenOffset, blueOffset;
    public XElement? Root { get { return root; } }
    public double Brightness { get { return brightness; } set { brightness = value; root?.SetOrRemoveAttribute("brightness", value, DefaultValues.Brightness); } }
    public double TintMultiplier { get { return tintMultiplier; } set { tintMultiplier = value; root?.SetOrRemoveAttribute("tintMultiplier", value, DefaultValues.TintMultiplier); } }
    public double AlphaMultiplier { get { return alphaMultiplier; } set { alphaMultiplier = value; root?.SetOrRemoveAttribute("alphaMultiplier", value, DefaultValues.AlphaMultiplier); } }
    public double RedMultiplier { get { return redMultiplier; } set { redMultiplier = value; root?.SetOrRemoveAttribute("redMultiplier", value, DefaultValues.RedMultiplier); } }
    public double GreenMultiplier { get { return greenMultiplier; } set { greenMultiplier = value; root?.SetOrRemoveAttribute("greenMultiplier", value, DefaultValues.GreenMultiplier); } }
    public double BlueMultiplier { get { return blueMultiplier; } set { blueMultiplier = value; root?.SetOrRemoveAttribute("blueMultiplier", value, DefaultValues.BlueMultiplier); } }
    public string TintColor { get { return tintColor; } set { tintColor = value; root?.SetOrRemoveAttribute("tintColor", value, DefaultValues.TintColor); } }
    public int AlphaOffset { get { return alphaOffset; } set { alphaOffset = value; root?.SetOrRemoveAttribute("alphaOffset", value, DefaultValues.AlphaOffset); } }
    public int RedOffset { get { return redOffset; } set { redOffset = value; root?.SetOrRemoveAttribute("redOffset", value, DefaultValues.RedOffset); } }
    public int GreenOffset { get { return greenOffset; } set { greenOffset = value; root?.SetOrRemoveAttribute("greenOffset", value, DefaultValues.GreenOffset); } }
    public int BlueOffset { get { return blueOffset; } set { blueOffset = value; root?.SetOrRemoveAttribute("blueOffset", value, DefaultValues.BlueOffset); } }
    internal Color(XNamespace ns)
    {
        this.ns = ns;
        root = null;
        brightness = DefaultValues.Brightness;
        tintMultiplier = DefaultValues.TintMultiplier;
        alphaMultiplier = DefaultValues.AlphaMultiplier;
        redMultiplier = DefaultValues.RedMultiplier;
        greenMultiplier = DefaultValues.GreenMultiplier;
        blueMultiplier = DefaultValues.BlueMultiplier;
        tintColor = DefaultValues.TintColor;
        alphaOffset = DefaultValues.AlphaOffset;
        redOffset = DefaultValues.RedOffset;
        greenOffset = DefaultValues.GreenOffset;
        blueOffset = DefaultValues.BlueOffset;
    }
    internal Color(XElement colorNode)
    {
        root = colorNode;
        ns = root.Name.Namespace;
        brightness = (double?)colorNode.Attribute("brightness") ?? DefaultValues.Brightness;
        tintMultiplier = (double?)colorNode.Attribute("tintMultiplier") ?? DefaultValues.TintMultiplier;
        alphaMultiplier = (double?)colorNode.Attribute("alphaMultiplier") ?? DefaultValues.AlphaMultiplier;
        redMultiplier = (double?)colorNode.Attribute("redMultiplier") ?? DefaultValues.RedMultiplier;
        greenMultiplier = (double?)colorNode.Attribute("greenMultiplier") ?? DefaultValues.GreenMultiplier;
        blueMultiplier = (double?)colorNode.Attribute("blueMultiplier") ?? DefaultValues.BlueMultiplier;
        tintColor = (string?)colorNode.Attribute("tintColor") ?? DefaultValues.TintColor;
        alphaOffset = (int?)colorNode.Attribute("alphaOffset") ?? DefaultValues.AlphaOffset;
        redOffset = (int?)colorNode.Attribute("redOffset") ?? DefaultValues.RedOffset;
        greenOffset = (int?)colorNode.Attribute("greenOffset") ?? DefaultValues.GreenOffset;
        blueOffset = (int?)colorNode.Attribute("blueOffset") ?? DefaultValues.BlueOffset;
    }
    internal Color(in Color other)
    {
        root = other.Root is null ? null : new XElement(other.Root);
        ns = other.ns;
        brightness = other.Brightness;
        tintMultiplier = other.TintMultiplier;
        alphaMultiplier = other.AlphaMultiplier;
        redMultiplier = other.RedMultiplier;
        greenMultiplier = other.GreenMultiplier;
        blueMultiplier = other.BlueMultiplier;
        tintColor = other.TintColor;
        alphaOffset = other.AlphaOffset;
        redOffset = other.RedOffset;
        greenOffset = other.GreenOffset;
        blueOffset = other.BlueOffset;
    }
    
}