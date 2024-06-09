using System.Xml.Linq;

namespace CsXFL;
public class Shape : Element
{
    public const string SHAPE_NODE_IDENTIFIER = "DOMShape";
    private readonly XElement root;
    private readonly XNamespace ns;
    private List<FillStyle> fills;
    private List<StrokeStyle> strokes;
    private List<Edge> edges;
    public List<FillStyle> Fills { get { return fills; } }
    public List<StrokeStyle> Strokes { get { return strokes; } }
    public List<Edge> Edges { get { return edges; } }

    public Shape(XElement root) : base(root, "shape")
    {
        this.root = root;
        ns = root.Name.Namespace;
        fills = root.Element(ns + FillStyle.FILL_STYLES_NODEGROUP_IDENTIFIER)?.Elements(ns + FillStyle.FILL_STYLE_NODE_IDENTIFIER)?.Select(x => new FillStyle(x)).ToList() ?? new List<FillStyle>();
        strokes = root.Element(ns + StrokeStyle.STROKE_STYLES_NODEGROUP_IDENTIFIER)?.Elements(ns + StrokeStyle.STROKE_STYLE_NODE_IDENTIFIER)?.Select(x => new StrokeStyle(x)).ToList() ?? new List<StrokeStyle>();
        edges = root.Element(ns + Edge.EDGES_NODEGROUP_IDENTIFIER)?.Elements(ns + Edge.EDGE_NODE_IDENTIFIER)?.Select(x => new Edge(x)).ToList() ?? new List<Edge>();
    }

}

public class FillStyle
{
    public const string FILL_STYLE_NODE_IDENTIFIER = "FillStyle",
    FILL_STYLES_NODEGROUP_IDENTIFIER = "fills";
    private readonly XElement root;
    private readonly XNamespace ns;
    private readonly int index;
    private readonly SolidColor solidColor;
    public int Index { get { return index; } }
    public SolidColor SolidColor { get { return solidColor; } }
    internal FillStyle(XElement root)
    {
        this.root = root;
        ns = root.Name.Namespace;
        index = (int?)root.Attribute("index") ?? throw new ArgumentNullException(nameof(root));
        if (root.Element(ns + SolidColor.SOLID_COLOR_NODE_IDENTIFIER) != null)
        {
            solidColor = new SolidColor(root.Element(ns + SolidColor.SOLID_COLOR_NODE_IDENTIFIER)!);
        }
        else solidColor = new SolidColor(ns);
    }
}
public class SolidColor
{
    public const string SOLID_COLOR_NODE_IDENTIFIER = "SolidColor",
    SOLID_COLOR_NODEGROUP_IDENTIFIER = "fill";
    public static class DefaultValues
    {
        public const string Color = "#000000";
    }
    private readonly XElement root;
    private string color;
    public XElement Root { get { return root; } }
    public string Color { get { return color; } set { color = value; root?.SetOrRemoveAttribute("color", value, DefaultValues.Color); } }
    internal SolidColor(XElement root)
    {
        this.root = root;
        color = root.Attribute("color")?.Value ?? DefaultValues.Color;
    }
    internal SolidColor(XNamespace ns)
    {
        color = DefaultValues.Color;
        root = new XElement(ns + SOLID_COLOR_NODE_IDENTIFIER);
    }
}
public class Edge
{
    public const string EDGE_NODE_IDENTIFIER = "Edge",
    EDGES_NODEGROUP_IDENTIFIER = "edges";
    private readonly XElement root;
    private readonly XNamespace ns;
    private readonly int? fillStyle0, fillStyle1, strokeStyle;
    private readonly string? edges, cubics;
    public XElement Root { get { return root; } }
    public int? FillStyle0 { get { return fillStyle0; } }
    public int? FillStyle1 { get { return fillStyle1; } }
    public int? StrokeStyle { get { return strokeStyle; } }
    public string? Edges { get { return edges; } }
    public string? Cubics { get { return cubics; } }
    internal Edge(XElement root)
    {
        this.root = root;
        ns = root.Name.Namespace;
        fillStyle0 = (int?)root.Attribute("fillStyle0");
        fillStyle1 = (int?)root.Attribute("fillStyle1");
        strokeStyle = (int?)root.Attribute("strokeStyle");
        edges = root.Attribute("edges")?.Value;
        cubics = root.Attribute("cubics")?.Value;
    }
}
public class StrokeStyle
{
    public const string STROKE_STYLE_NODE_IDENTIFIER = "StrokeStyle",
    STROKE_STYLES_NODEGROUP_IDENTIFIER = "strokes";
    private readonly XElement root;
    private readonly XNamespace ns;
    private int index;
    private Stroke stroke;
    public int Index { get { return index; } }
    public Stroke Stroke { get { return stroke; } }
    public XElement Root { get { return root; } }
    public StrokeStyle(XElement root)
    {
        this.root = root;
        ns = root.Name.Namespace;
        index = (int?)root.Attribute("index") ?? throw new ArgumentNullException(nameof(root));
        string strokeType = root.Elements().First().Name.LocalName;
        switch (strokeType)
        {
            case SolidStroke.SOLID_STROKE_NODE_IDENTIFIER:
                stroke = new SolidStroke(root.Element(ns + SolidStroke.SOLID_STROKE_NODE_IDENTIFIER)!);
                break;
            case DottedStroke.DOTTED_STROKE_NODE_IDENTIFIER:
                stroke = new DottedStroke(root.Element(ns + DottedStroke.DOTTED_STROKE_NODE_IDENTIFIER)!);
                break;
            case RaggedStroke.RAGGED_STROKE_NODE_IDENTIFIER:
                stroke = new RaggedStroke(root.Element(ns + RaggedStroke.RAGGED_STROKE_NODE_IDENTIFIER)!);
                break;
            case StippleStroke.STIPPLE_STROKE_NODE_IDENTIFIER:
                stroke = new StippleStroke(root.Element(ns + StippleStroke.STIPPLE_STROKE_NODE_IDENTIFIER)!);
                break;
            case HatchedStroke.HATCHED_STROKE_NODE_IDENTIFIER:
                stroke = new HatchedStroke(root.Element(ns + HatchedStroke.HATCHED_STROKE_NODE_IDENTIFIER)!);
                break;
            default:
                throw new ArgumentException("Unknown stroke type: " + strokeType);

        }
    }
}
public abstract class Stroke
{
    public static class DefaultValues
    {
        public const string Caps = "round";
        public const bool PixelHinting = false;
        public const string Joints = "round";
        public const int MiterLimit = 0;
        public const double Weight = 1.0;

    }
    protected readonly XElement root;
    private readonly XNamespace ns;
    private bool pixelHinting;
    private string scaleMode, caps, joints;
    private int miterLimit;
    private double weight;
    private SolidColor solidColor;
    public XElement Root { get { return root; } }
    public string ScaleMode { get { return scaleMode; } set { scaleMode = value; root?.SetAttributeValue("scaleMode", value); } }
    public string Caps { get { return caps; } set { caps = value; root?.SetOrRemoveAttribute("caps", value, DefaultValues.Caps); } }
    public bool PixelHinting { get { return pixelHinting; } set { pixelHinting = value; root?.SetOrRemoveAttribute("pixelHinting", value, DefaultValues.PixelHinting); } }
    public string Joints { get { return joints; } set { joints = value; root?.SetOrRemoveAttribute("joints", value, DefaultValues.Joints); } }
    public double Weight { get { return weight; } set { weight = value; root?.SetOrRemoveAttribute("weight", value, DefaultValues.Weight); } }
    public int MiterLimit { get { return miterLimit; } set { miterLimit = value; root?.SetOrRemoveAttribute("miterLimit", value, DefaultValues.MiterLimit); } }
    public SolidColor SolidColor { get { return solidColor; } }
    internal Stroke(XElement root)
    {
        this.root = root;
        ns = root.Name.Namespace;
        scaleMode = root.Attribute("scaleMode")?.Value ?? throw new ArgumentNullException(nameof(root));
        caps = root.Attribute("caps")?.Value ?? DefaultValues.Caps;
        pixelHinting = (bool?)root.Attribute("pixelHinting") ?? DefaultValues.PixelHinting;
        joints = root.Attribute("joints")?.Value ?? DefaultValues.Joints;
        weight = (double?)root.Attribute("weight") ?? DefaultValues.Weight;
        miterLimit = (int?)root.Attribute("miterLimit") ?? DefaultValues.MiterLimit;
        if (root.Element(ns + SolidColor.SOLID_COLOR_NODEGROUP_IDENTIFIER)!.Element(ns + SolidColor.SOLID_COLOR_NODE_IDENTIFIER) != null)
            solidColor = new SolidColor(root.Element(ns + SolidColor.SOLID_COLOR_NODEGROUP_IDENTIFIER)!.Element(ns + SolidColor.SOLID_COLOR_NODE_IDENTIFIER)!);
        else solidColor = new SolidColor(ns);
    }
}
public class SolidStroke(XElement root) : Stroke(root)
{
    public const string SOLID_STROKE_NODE_IDENTIFIER = "SolidStroke";
}
public class DottedStroke : Stroke
{
    public const string DOTTED_STROKE_NODE_IDENTIFIER = "DottedStroke";
    private int dotSpace;
    public int DotSpace { get { return dotSpace; } set { dotSpace = value; root?.SetOrRemoveAttribute("dotSpace", value, 0); } }
    public DottedStroke(XElement root) : base(root)
    {
        dotSpace = (int?)root.Attribute("dotSpace") ?? throw new ArgumentNullException(nameof(root));
    }

}
public class RaggedStroke(XElement root) : Stroke(root)
{
    public const string RAGGED_STROKE_NODE_IDENTIFIER = "RaggedStroke";
}
public class StippleStroke(XElement root) : Stroke(root)
{
    public const string STIPPLE_STROKE_NODE_IDENTIFIER = "StippleStroke";
}
public class HatchedStroke(XElement root) : Stroke(root)
{
    public const string HATCHED_STROKE_NODE_IDENTIFIER = "HatchedStroke";
}
public class WidthMarker
{
    public const string WIDTH_MARKER_NODE_IDENTIFIER = "WidthMarker",
    WIDTH_MARKERS_NODEGROUP_IDENTIFIER = "VariblePointWidth";
    private XElement root;
    public XElement Root { get { return root; } }
    private double position, left, right;
    private string? type;
    public double Position { get { return position; } set { position = value; root?.SetAttributeValue("position", value); } }
    public double Left { get { return left; } set { left = value; root?.SetAttributeValue("left", value); } }
    public double Right { get { return right; } set { right = value; root?.SetAttributeValue("right", value); } }
    public string? Type { get { return type; } set { type = value; root?.SetAttributeValue("type", value); } }
    internal WidthMarker(XElement root)
    {
        this.root = root;
        position = (double?)root.Attribute("position") ?? throw new ArgumentNullException(nameof(root));
        left = (double?)root.Attribute("left") ?? throw new ArgumentNullException(nameof(root));
        right = (double?)root.Attribute("right") ?? throw new ArgumentNullException(nameof(root));
        type = (string?)root.Attribute("type");
    }
}