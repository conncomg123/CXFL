// yeah a human wrote this
using System.Xml.Linq;

namespace CsXFL;
public abstract class Filter
{
    public const string FILTER_NODEGROUP_IDENTIFIER = "filters";
    protected XElement? root;
    protected XNamespace ns;
    public XElement? Root { get { return root; } }
    internal Filter(in XElement elementNode)
    {
        root = elementNode;
        ns = root.Name.Namespace;
    }
    internal Filter(XNamespace ns)
    {
        this.ns = ns;
    }
    internal Filter(Filter other)
    {
        root = other.root;
        ns = other.ns;
    }
    public abstract void SetAttr(string name, object value);
}
public class DropShadowFilter : Filter
{
    internal const string DROPSHADOWFILTER_NODE_IDENTIFIER = "DropShadowFilter";
    public static class DefaultValues
    {
        public const double Angle = 45.0;
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const string Color = "#000000";
        public const double Distance = 0.0;
        public const bool HideObject = false;
        public const bool Inner = false;
        public const bool Knockout = false;
        public const int Quality = 1;
        public const double Strength = 1.0;
    }
    private double angle, blurX, blurY, distance, strength;
    private string color;
    private bool hideObject, inner, knockout;
    private int quality;
    public double Angle { get { return angle; } set { angle = value; root?.SetOrRemoveAttribute("angle", value, DefaultValues.Angle); } }
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public string Color { get { return color; } set { color = value; root?.SetOrRemoveAttribute("color", value, DefaultValues.Color); } }
    public double Distance { get { return distance; } set { distance = value; root?.SetAttributeValue("distance", value); } }
    public bool HideObject { get { return hideObject; } set { hideObject = value; root?.SetOrRemoveAttribute("hideObject", value, DefaultValues.HideObject); } }
    public bool Inner { get { return inner; } set { inner = value; root?.SetOrRemoveAttribute("inner", value, DefaultValues.Inner); } }
    public bool Knockout { get { return knockout; } set { knockout = value; root?.SetOrRemoveAttribute("knockout", value, DefaultValues.Knockout); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    public double Strength { get { return strength; } set { strength = value; root?.SetOrRemoveAttribute("strength", value, DefaultValues.Strength); } }
    internal DropShadowFilter(in XElement elementNode) : base(elementNode)
    {
        angle = (double?)root?.Attribute("angle") ?? DefaultValues.Angle;
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        color = (string?)root?.Attribute("color") ?? DefaultValues.Color;
        distance = (double?)root?.Attribute("distance") ?? DefaultValues.Distance;
        hideObject = (bool?)root?.Attribute("hideObject") ?? DefaultValues.HideObject;
        inner = (bool?)root?.Attribute("inner") ?? DefaultValues.Inner;
        knockout = (bool?)root?.Attribute("knockout") ?? DefaultValues.Knockout;
        quality = (int?)root?.Attribute("quality") ?? DefaultValues.Quality;
        strength = (double?)root?.Attribute("strength") ?? DefaultValues.Strength;
    }
#pragma warning disable CS8618
    internal DropShadowFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + DROPSHADOWFILTER_NODE_IDENTIFIER);
        Angle = DefaultValues.Angle;
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Color = DefaultValues.Color;
        Distance = DefaultValues.Distance;
        HideObject = DefaultValues.HideObject;
        Inner = DefaultValues.Inner;
        Knockout = DefaultValues.Knockout;
        Quality = DefaultValues.Quality;
        Strength = DefaultValues.Strength;
    }
#pragma warning restore CS8618
    internal DropShadowFilter(DropShadowFilter other) : base(other)
    {
        angle = other.angle;
        blurX = other.blurX;
        blurY = other.blurY;
        color = other.color;
        distance = other.distance;
        hideObject = other.hideObject;
        inner = other.inner;
        knockout = other.knockout;
        quality = other.quality;
        strength = other.strength;
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "angle":
                Angle = Convert.ToDouble(value);
                break;
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "color":
                Color = (string)value;
                break;
            case "distance":
                Distance = Convert.ToDouble(value);
                break;
            case "hideObject":
                HideObject = (bool)value;
                break;
            case "inner":
                Inner = (bool)value;
                break;
            case "knockout":
                Knockout = (bool)value;
                break;
            case "quality":
                Quality = (int)value;
                break;
            case "strength":
                Strength = Convert.ToDouble(value);
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);

        }
    }
}
public class BlurFilter : Filter
{
    internal const string BLURFILTER_NODE_IDENTIFIER = "BlurFilter";
    public static class DefaultValues
    {
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const int Quality = 1;
    }
    private double blurX, blurY;
    private int quality;
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    internal BlurFilter(in XElement elementNode) : base(elementNode)
    {
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        quality = (int?)root?.Attribute("quality") ?? DefaultValues.Quality;
    }
    internal BlurFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + BLURFILTER_NODE_IDENTIFIER);
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Quality = DefaultValues.Quality;
    }
    internal BlurFilter(BlurFilter other) : base(other)
    {
        blurX = other.blurX;
        blurY = other.blurY;
        quality = other.quality;
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "quality":
                Quality = (int)value;
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}
public class GlowFilter : Filter
{
    internal const string GLOWFILTER_NODE_IDENTIFIER = "GlowFilter";
    public static class DefaultValues
    {
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const string Color = "#000000";
        public const bool Inner = false;
        public const bool Knockout = false;
        public const int Quality = 1;
        public const double Strength = 1.0;
    }
    private double blurX, blurY, strength;
    private string color;
    private bool inner, knockout;
    private int quality;
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public string Color { get { return color; } set { color = value; root?.SetOrRemoveAttribute("color", value, DefaultValues.Color); } }
    public bool Inner { get { return inner; } set { inner = value; root?.SetOrRemoveAttribute("inner", value, DefaultValues.Inner); } }
    public bool Knockout { get { return knockout; } set { knockout = value; root?.SetOrRemoveAttribute("knockout", value, DefaultValues.Knockout); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    public double Strength { get { return strength; } set { strength = value; root?.SetOrRemoveAttribute("strength", value, DefaultValues.Strength); } }
    internal GlowFilter(in XElement elementNode) : base(elementNode)
    {
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        color = (string?)root?.Attribute("color") ?? DefaultValues.Color;
        inner = (bool?)root?.Attribute("inner") ?? DefaultValues.Inner;
        knockout = (bool?)root?.Attribute("knockout") ?? DefaultValues.Knockout;
        quality = (int?)root?.Attribute("quality") ?? DefaultValues.Quality;
        strength = (double?)root?.Attribute("strength") ?? DefaultValues.Strength;
    }
#pragma warning disable CS8618
    internal GlowFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + GLOWFILTER_NODE_IDENTIFIER);
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Color = DefaultValues.Color;
        Inner = DefaultValues.Inner;
        Knockout = DefaultValues.Knockout;
        Quality = DefaultValues.Quality;
        Strength = DefaultValues.Strength;
    }
#pragma warning restore CS8618
    internal GlowFilter(GlowFilter other) : base(other)
    {
        blurX = other.blurX;
        blurY = other.blurY;
        color = other.color;
        inner = other.inner;
        knockout = other.knockout;
        quality = other.quality;
        strength = other.strength;
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "color":
                Color = (string)value;
                break;
            case "inner":
                Inner = (bool)value;
                break;
            case "knockout":
                Knockout = (bool)value;
                break;
            case "quality":
                Quality = (int)value;
                break;
            case "strength":
                Strength = Convert.ToDouble(value);
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}
public class BevelFilter : Filter
{
    internal const string BEVELFILTER_NODE_IDENTIFIER = "BevelFilter";
    public static class DefaultValues
    {
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const double Angle = 45.0;
        public const double Distance = 4.0;
        public const string HighlightColor = "#FFFFFF";
        public const bool Knockout = false;
        public const int Quality = 1;
        public const string ShadowColor = "#000000";
        public const double Strength = 1.0;
        public const string Type = "inner";
    }
    public static HashSet<string> ACCEPTABLE_TYPES = new() { "inner", "outer", "full" };
    private double blurX, blurY, angle, distance, strength;
    private string highlightColor, shadowColor, type;
    private bool knockout;
    private int quality;
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public double Angle { get { return angle; } set { angle = value; root?.SetOrRemoveAttribute("angle", value, DefaultValues.Angle); } }
    public double Distance { get { return distance; } set { distance = value; root?.SetAttributeValue("distance", value); } }
    public string HighlightColor { get { return highlightColor; } set { highlightColor = value; root?.SetOrRemoveAttribute("highlightColor", value, DefaultValues.HighlightColor); } }
    public bool Knockout { get { return knockout; } set { knockout = value; root?.SetOrRemoveAttribute("knockout", value, DefaultValues.Knockout); } }
    public string ShadowColor { get { return shadowColor; } set { shadowColor = value; root?.SetOrRemoveAttribute("shadowColor", value, DefaultValues.ShadowColor); } }
    public double Strength { get { return strength; } set { strength = value; root?.SetOrRemoveAttribute("strength", value, DefaultValues.Strength); } }
    public string Type { get { return type; } set { if (!ACCEPTABLE_TYPES.Contains(value)) throw new ArgumentException("Invalid type"); type = value; root?.SetOrRemoveAttribute("type", value, DefaultValues.Type); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    internal BevelFilter(in XElement elementNode) : base(elementNode)
    {
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        angle = (double?)root?.Attribute("angle") ?? DefaultValues.Angle;
        distance = (double?)root?.Attribute("distance") ?? DefaultValues.Distance;
        highlightColor = (string?)root?.Attribute("highlightColor") ?? DefaultValues.HighlightColor;
        knockout = (bool?)root?.Attribute("knockout") ?? DefaultValues.Knockout;
        shadowColor = (string?)root?.Attribute("shadowColor") ?? DefaultValues.ShadowColor;
        strength = (double?)root?.Attribute("strength") ?? DefaultValues.Strength;
        type = (string?)root?.Attribute("type") ?? DefaultValues.Type;
    }
#pragma warning disable CS8618
    internal BevelFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + BEVELFILTER_NODE_IDENTIFIER);
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Angle = DefaultValues.Angle;
        Distance = DefaultValues.Distance;
        HighlightColor = DefaultValues.HighlightColor;
        Knockout = DefaultValues.Knockout;
        ShadowColor = DefaultValues.ShadowColor;
        Strength = DefaultValues.Strength;
        Type = DefaultValues.Type;
    }
#pragma warning restore CS8618
    internal BevelFilter(BevelFilter other) : base(other)
    {
        blurX = other.blurX;
        blurY = other.blurY;
        angle = other.angle;
        distance = other.distance;
        highlightColor = other.highlightColor;
        knockout = other.knockout;
        shadowColor = other.shadowColor;
        strength = other.strength;
        type = other.type;
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "angle":
                Angle = Convert.ToDouble(value);
                break;
            case "distance":
                Distance = Convert.ToDouble(value);
                break;
            case "highlightColor":
                HighlightColor = (string)value;
                break;
            case "knockout":
                Knockout = (bool)value;
                break;
            case "shadowColor":
                ShadowColor = (string)value;
                break;
            case "strength":
                Strength = Convert.ToDouble(value);
                break;
            case "type":
                Type = (string)value;
                break;
            case "quality":
                Quality = (int)value;
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}

public class GradientEntry
{
    internal const string GRADIENTENTRY_NODE_IDENTIFIER = "GradientEntry";
    public static class DefaultValues
    {
        public const string Color = "#000000";
        public const double Alpha = 1.0;
        public const double Ratio = 0.0;
    }
    internal XElement? root;
    private string color;
    private double alpha, ratio;
    public string Color { get { return color; } set { color = value; root?.SetOrRemoveAttribute("color", value, DefaultValues.Color); } }
    public double Alpha { get { return alpha; } set { alpha = value; root?.SetOrRemoveAttribute("alpha", value, DefaultValues.Alpha); } }
    public double Ratio { get { return ratio; } set { ratio = value; root?.SetAttributeValue("ratio", value); } }
    public GradientEntry(in XElement elementNode)
    {
        root = elementNode;
        color = (string?)root?.Attribute("color") ?? DefaultValues.Color;
        alpha = (double?)root?.Attribute("alpha") ?? DefaultValues.Alpha;
        ratio = (double?)root?.Attribute("ratio") ?? DefaultValues.Ratio;
    }
#pragma warning disable CS8618
    public GradientEntry(XNamespace ns)
    {
        root = new XElement(ns + GRADIENTENTRY_NODE_IDENTIFIER);
        Color = DefaultValues.Color;
        Alpha = DefaultValues.Alpha;
        Ratio = DefaultValues.Ratio;
    }
#pragma warning restore CS8618
    public GradientEntry(GradientEntry other)
    {
        root = other.root == null ? null : new XElement(other.root);
        color = other.color;
        alpha = other.alpha;
        ratio = other.ratio;
    }
}

public class GradientGlowFilter : Filter
{
    internal const string GRADIENTGLOWFILTER_NODE_IDENTIFIER = "GradientGlowFilter";
    public static class DefaultValues
    {
        public const double Angle = 45.0;
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const int Quality = 1;
        public const double Distance = 4.0;
        public const bool Knockout = false;
        public const double Strength = 1.0;
        public const string Type = "inner";
    }
    public static HashSet<string> ACCEPTABLE_TYPES = new() { "inner", "outer", "full" };
    private double angle, blurX, blurY, distance, strength;
    private bool knockout;
    private int quality;
    private string type;
    private List<GradientEntry> gradientEntries;
    public double Angle { get { return angle; } set { angle = value; root?.SetOrRemoveAttribute("angle", value, DefaultValues.Angle); } }
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    public double Distance { get { return distance; } set { distance = value; root?.SetAttributeValue("distance", value); } }
    public bool Knockout { get { return knockout; } set { knockout = value; root?.SetOrRemoveAttribute("knockout", value, DefaultValues.Knockout); } }
    public double Strength { get { return strength; } set { strength = value; root?.SetOrRemoveAttribute("strength", value, DefaultValues.Strength); } }
    public string Type { get { return type; } set { if (!ACCEPTABLE_TYPES.Contains(value)) throw new ArgumentException("Invalid type"); type = value; root?.SetOrRemoveAttribute("type", value, DefaultValues.Type); } }
    public List<GradientEntry> GradientEntries { get { return gradientEntries; } }
    internal GradientGlowFilter(in XElement elementNode) : base(elementNode)
    {
        angle = (double?)root?.Attribute("angle") ?? DefaultValues.Angle;
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        quality = (int?)root?.Attribute("quality") ?? DefaultValues.Quality;
        distance = (double?)root?.Attribute("distance") ?? DefaultValues.Distance;
        knockout = (bool?)root?.Attribute("knockout") ?? DefaultValues.Knockout;
        strength = (double?)root?.Attribute("strength") ?? DefaultValues.Strength;
        type = (string?)root?.Attribute("type") ?? DefaultValues.Type;
        gradientEntries = root?.Elements(ns + GradientEntry.GRADIENTENTRY_NODE_IDENTIFIER).Select(e => new GradientEntry(e)).ToList() ?? new List<GradientEntry>();
    }
#pragma warning disable CS8618
    internal GradientGlowFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + GRADIENTGLOWFILTER_NODE_IDENTIFIER);
        Angle = DefaultValues.Angle;
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Quality = DefaultValues.Quality;
        Distance = DefaultValues.Distance;
        Knockout = DefaultValues.Knockout;
        Strength = DefaultValues.Strength;
        Type = DefaultValues.Type;
        gradientEntries = new List<GradientEntry>();
    }
#pragma warning restore CS8618
    internal GradientGlowFilter(GradientGlowFilter other) : base(other)
    {
        angle = other.angle;
        blurX = other.blurX;
        blurY = other.blurY;
        quality = other.quality;
        distance = other.distance;
        knockout = other.knockout;
        strength = other.strength;
        type = other.type;
        gradientEntries = other.gradientEntries.Select(e => new GradientEntry(e)).ToList();
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "angle":
                Angle = Convert.ToDouble(value);
                break;
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "quality":
                Quality = (int)value;
                break;
            case "distance":
                Distance = Convert.ToDouble(value);
                break;
            case "knockout":
                Knockout = (bool)value;
                break;
            case "strength":
                Strength = Convert.ToDouble(value);
                break;
            case "type":
                Type = (string)value;
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}
public class GradientBevelFilter : Filter
{
    internal const string GRADIENTBEVELFILTER_NODE_IDENTIFIER = "GradientBevelFilter";
    public static class DefaultValues
    {
        public const double Angle = 45.0;
        public const double BlurX = 4.0;
        public const double BlurY = 4.0;
        public const int Quality = 1;
        public const double Distance = 4.0;
        public const bool Knockout = false;
        public const double Strength = 1.0;
        public const string Type = "inner";
    }
    public static HashSet<string> ACCEPTABLE_TYPES = new() { "inner", "outer", "full" };
    private double angle, blurX, blurY, distance, strength;
    private bool knockout;
    private int quality;
    private string type;
    private List<GradientEntry> gradientEntries;
    public double Angle { get { return angle; } set { angle = value; root?.SetOrRemoveAttribute("angle", value, DefaultValues.Angle); } }
    public double BlurX { get { return blurX; } set { blurX = value; root?.SetAttributeValue("blurX", value); } }
    public double BlurY { get { return blurY; } set { blurY = value; root?.SetAttributeValue("blurY", value); } }
    public int Quality { get { return quality; } set { if (value < 1 || value > 3) throw new ArgumentException("Quality value must be 1, 2, or 3."); quality = value; root?.SetOrRemoveAttribute("quality", value, DefaultValues.Quality); } }
    public double Distance { get { return distance; } set { distance = value; root?.SetAttributeValue("distance", value); } }
    public bool Knockout { get { return knockout; } set { knockout = value; root?.SetOrRemoveAttribute("knockout", value, DefaultValues.Knockout); } }
    public double Strength { get { return strength; } set { strength = value; root?.SetOrRemoveAttribute("strength", value, DefaultValues.Strength); } }
    public string Type { get { return type; } set { if (!ACCEPTABLE_TYPES.Contains(value)) throw new ArgumentException("Invalid type"); type = value; root?.SetOrRemoveAttribute("type", value, DefaultValues.Type); } }
    public List<GradientEntry> GradientEntries { get { return gradientEntries; } }
    internal GradientBevelFilter(in XElement elementNode) : base(elementNode)
    {
        angle = (double?)root?.Attribute("angle") ?? DefaultValues.Angle;
        blurX = (double?)root?.Attribute("blurX") ?? DefaultValues.BlurX;
        blurY = (double?)root?.Attribute("blurY") ?? DefaultValues.BlurY;
        quality = (int?)root?.Attribute("quality") ?? DefaultValues.Quality;
        distance = (double?)root?.Attribute("distance") ?? DefaultValues.Distance;
        knockout = (bool?)root?.Attribute("knockout") ?? DefaultValues.Knockout;
        strength = (double?)root?.Attribute("strength") ?? DefaultValues.Strength;
        type = (string?)root?.Attribute("type") ?? DefaultValues.Type;
        gradientEntries = root?.Elements(ns + GradientEntry.GRADIENTENTRY_NODE_IDENTIFIER).Select(e => new GradientEntry(e)).ToList() ?? new List<GradientEntry>();
    }
#pragma warning disable CS8618
    internal GradientBevelFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + GRADIENTBEVELFILTER_NODE_IDENTIFIER);
        Angle = DefaultValues.Angle;
        BlurX = DefaultValues.BlurX;
        BlurY = DefaultValues.BlurY;
        Quality = DefaultValues.Quality;
        Distance = DefaultValues.Distance;
        Knockout = DefaultValues.Knockout;
        Strength = DefaultValues.Strength;
        Type = DefaultValues.Type;
        gradientEntries = new List<GradientEntry>();
    }
#pragma warning restore CS8618
    internal GradientBevelFilter(GradientBevelFilter other) : base(other)
    {
        angle = other.angle;
        blurX = other.blurX;
        blurY = other.blurY;
        quality = other.quality;
        distance = other.distance;
        knockout = other.knockout;
        strength = other.strength;
        type = other.type;
        gradientEntries = other.gradientEntries.Select(e => new GradientEntry(e)).ToList();
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "angle":
                Angle = Convert.ToDouble(value);
                break;
            case "blurX":
                BlurX = Convert.ToDouble(value);
                break;
            case "blurY":
                BlurY = Convert.ToDouble(value);
                break;
            case "quality":
                Quality = (int)value;
                break;
            case "distance":
                Distance = Convert.ToDouble(value);
                break;
            case "knockout":
                Knockout = (bool)value;
                break;
            case "strength":
                Strength = Convert.ToDouble(value);
                break;
            case "type":
                Type = (string)value;
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}
public class AdjustColorFilter : Filter
{
    internal const string ADJUSTCOLORFILTER_NODE_IDENTIFIER = "AdjustColorFilter";
    public static class DefaultValues
    {
        public const double Brightness = 0.0;
        public const double Contrast = 0.0;
        public const double Saturation = 0.0;
        public const double Hue = 0.0;
    }
    private double brightness, contrast, saturation, hue;
    public double Brightness { get { return brightness; } set { brightness = value; root?.SetOrRemoveAttribute("brightness", value, DefaultValues.Brightness); } }
    public double Contrast { get { return contrast; } set { contrast = value; root?.SetOrRemoveAttribute("contrast", value, DefaultValues.Contrast); } }
    public double Saturation { get { return saturation; } set { saturation = value; root?.SetOrRemoveAttribute("saturation", value, DefaultValues.Saturation); } }
    public double Hue { get { return hue; } set { hue = value; root?.SetOrRemoveAttribute("hue", value, DefaultValues.Hue); } }
    internal AdjustColorFilter(in XElement elementNode) : base(elementNode)
    {
        brightness = (double?)root?.Attribute("brightness") ?? DefaultValues.Brightness;
        contrast = (double?)root?.Attribute("contrast") ?? DefaultValues.Contrast;
        saturation = (double?)root?.Attribute("saturation") ?? DefaultValues.Saturation;
        hue = (double?)root?.Attribute("hue") ?? DefaultValues.Hue;
    }
    internal AdjustColorFilter(XNamespace ns) : base(ns)
    {
        root = new XElement(ns + ADJUSTCOLORFILTER_NODE_IDENTIFIER);
        Brightness = DefaultValues.Brightness;
        Contrast = DefaultValues.Contrast;
        Saturation = DefaultValues.Saturation;
        Hue = DefaultValues.Hue;
    }
    internal AdjustColorFilter(AdjustColorFilter other) : base(other)
    {
        brightness = other.brightness;
        contrast = other.contrast;
        saturation = other.saturation;
        hue = other.hue;
    }
    public override void SetAttr(string name, object value)
    {
        switch (name)
        {
            case "brightness":
                Brightness = Convert.ToDouble(value);
                break;
            case "contrast":
                Contrast = Convert.ToDouble(value);
                break;
            case "saturation":
                Saturation = Convert.ToDouble(value);
                break;
            case "hue":
                Hue = Convert.ToDouble(value);
                break;
            default:
                throw new ArgumentException("Invalid attribute name: " + name);
        }
    }
}