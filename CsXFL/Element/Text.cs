using System.Xml.Linq;

namespace CsXFL;
// encompasses all types of text: static, dynamic, and input
public class Text : Element
{
    new private static class DefaultValues
    {
        public const double AntiAliasSharpness = 0.0, AntiAliasThickness = 0.0;
        public const string LineType = "single line";
        public const string FontRenderingMode = "advanced";
        public const string Orientation = "horizontal";
    }
    private static readonly Dictionary<string, string> DOMTEXTTYPE_TO_TEXTTYPE = new Dictionary<string, string>
    {
        {"DOMStaticText", "static"},
        {"DOMDynamicText", "dynamic"},
        {"DOMInputText", "input"}
    }, TEXTTYPE_TO_DOMTEXTTYPE = DOMTEXTTYPE_TO_TEXTTYPE.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    private static HashSet<string> ACCEPTABLE_LINETYPES = new HashSet<string> { "single line", "multiline", "multiline no wrap", "password" },
        ACCEPTABLE_TEXTTYPES = new HashSet<string> { "static", "dynamic", "input" },
        ACCEPTABLE_FONTRENDERINGMODES = new HashSet<string> { "device", "bitmap", "advanced", "standard", "customThicknessSharpness" },
        ACCEPTABLE_ORIENTATIONS = new HashSet<string> { "horizontal", "vertical left to right", "vertical right to left" };
    new private double width, height;
    private double antiAliasSharpness, antiAliasThickness;
    private string lineType, textType, fontRenderingMode, orientation;
    private List<TextRun> textRuns;
    public double AntiAliasSharpness { get { return antiAliasSharpness; } set { antiAliasSharpness = value; root?.SetOrRemoveAttribute("antiAliasSharpness", value, DefaultValues.AntiAliasSharpness); } }
    public double AntiAliasThickness { get { return antiAliasThickness; } set { antiAliasThickness = value; root?.SetOrRemoveAttribute("antiAliasThickness", value, DefaultValues.AntiAliasThickness); } }
    public string LineType
    {
        get { return lineType; }
        set
        {
            if (!ACCEPTABLE_LINETYPES.Contains(value)) throw new ArgumentException($"Invalid line type: {value}");
            lineType = value; root?.SetOrRemoveAttribute("lineType", value, DefaultValues.LineType);
        }
    }
    public string TextType
    {
        get { return textType; }
        set
        {
            if (!ACCEPTABLE_TEXTTYPES.Contains(value)) throw new ArgumentException($"Invalid text type: {value}");
            if (root != null) root.Name = ns + TEXTTYPE_TO_DOMTEXTTYPE[value];
        }
    }
    public string FontRenderingMode

    {
        get { return fontRenderingMode; }
        set
        {
            if (!ACCEPTABLE_FONTRENDERINGMODES.Contains(value)) throw new ArgumentException($"Invalid font rendering mode: {value}");
            fontRenderingMode = value; root?.SetOrRemoveAttribute("fontRenderingMode", value, DefaultValues.FontRenderingMode);
        }
    }
    public string Orientation
    {
        get { return orientation; }
        set
        {
            if (!ACCEPTABLE_ORIENTATIONS.Contains(value)) throw new ArgumentException($"Invalid orientation: {value}");
            orientation = value; root?.SetOrRemoveAttribute("orientation", value, DefaultValues.Orientation);
        }
    }
    public List<TextRun> TextRuns { get { return textRuns; } }
    public override double Width { get { return width; } set { width = value; root?.SetOrRemoveAttribute("width", value, double.NaN); } }
    public override double Height { get { return height; } set { height = value; root?.SetOrRemoveAttribute("height", value, double.NaN); } }
    internal Text(XElement textNode) : base(textNode, "text")
    {
        width = (double?)root!.Attribute("width") ?? Element.DefaultValues.Width;
        height = (double?)root.Attribute("height") ?? Element.DefaultValues.Height;
        antiAliasSharpness = (double?)root.Attribute("antiAliasSharpness") ?? DefaultValues.AntiAliasSharpness;
        antiAliasThickness = (double?)root.Attribute("antiAliasThickness") ?? DefaultValues.AntiAliasThickness;
        lineType = (string?)root.Attribute("lineType") ?? DefaultValues.LineType;
        textType = DOMTEXTTYPE_TO_TEXTTYPE[root.Name.LocalName];
        fontRenderingMode = (string?)root.Attribute("fontRenderingMode") ?? DefaultValues.FontRenderingMode;
        orientation = (string?)root.Attribute("orientation") ?? DefaultValues.Orientation;
        textRuns = new List<TextRun>();
        foreach (XElement textRunNode in root.Elements(ns + "textRuns").Elements(ns + "DOMTextRun"))
        {
            textRuns.Add(new TextRun(textRunNode));
        }
    }
#pragma warning disable CS8618
    internal Text(Rectangle boundingRect, string characters, XNamespace ns) : base(ns)
    {
        root = new XElement(ns + "DOMStaticText");
        root.Add(new XElement(ns + "transformationPoint"));
        root.Element(ns + "transformationPoint")!.Add(transformationPoint.Root);
        matrix.SetParent(root);
        elementType = "text";
        this.ns = ns;
        Width = boundingRect.Right - boundingRect.Left;
        Height = boundingRect.Bottom - boundingRect.Top;
        Matrix.Tx = boundingRect.Left;
        Matrix.Ty = boundingRect.Top;
        AntiAliasSharpness = DefaultValues.AntiAliasSharpness;
        AntiAliasThickness = DefaultValues.AntiAliasThickness;
        LineType = DefaultValues.LineType;
        TextType = "static";
        FontRenderingMode = DefaultValues.FontRenderingMode;
        Orientation = DefaultValues.Orientation;
        TextRun added = new TextRun(characters, ns);
        textRuns = new List<TextRun> { added };
        root.Add(new XElement(ns + "textRuns", added.Root));
    }
#pragma warning restore CS8618
    internal Text(in Text other) : base(other)
    {
        width = other.width;
        height = other.height;
        antiAliasSharpness = other.antiAliasSharpness;
        antiAliasThickness = other.antiAliasThickness;
        lineType = other.lineType;
        textType = other.textType;
        fontRenderingMode = other.fontRenderingMode;
        orientation = other.orientation;
        textRuns = new List<TextRun>();
        foreach (XElement textRunNode in root!.Elements(ns + "textRuns").Elements(ns + "DOMTextRun"))
        {
            textRuns.Add(new TextRun(textRunNode));
        }
    }
    public void SetTextString(string characters)
    {
        if (textRuns.Count == 1)
        {
            textRuns[0].Characters = characters;
            return;
        }
        root?.Element(ns + "textRuns")?.RemoveNodes();
        TextRun added = new TextRun(characters, ns);
        textRuns = new List<TextRun> { added };
        root?.Element(ns + "textRuns")?.Add(added.Root);
    }
    public string GetTextString()
    {
        string result = "";
        foreach (TextRun run in textRuns)
        {
            result += run.Characters;
        }
        return result;
    }
    public void SetTextAttr(string attrName, object value)
    {
        // set for all text runs
        foreach (TextRun run in textRuns)
        {
            run.TextAttrs.SetAttr(attrName, value);
        }
    }
}

public class Rectangle
{
    private double left, right, top, bottom;
    public double Left { get { return left; } set { left = value; } }
    public double Right { get { return right; } set { right = value; } }
    public double Top { get { return top; } set { top = value; } }
    public double Bottom { get { return bottom; } set { bottom = value; } }
    public Rectangle(double left, double top, double right, double bottom)
    {
        this.left = left;
        this.right = right;
        this.top = top;
        this.bottom = bottom;
    }
    public static implicit operator Rectangle(SixLabors.Fonts.FontRectangle rect)
    {
        return new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }
}
