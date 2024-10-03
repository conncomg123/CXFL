using System.Xml.Linq;

namespace CsXFL;
public class TextAttrs
{
    private const double LINEHEIGHT_MULTIPLIER = 1.279;
    internal const string TEXTATTRS_NODE_IDENTIFIER = "DOMTextAttrs",
    TEXTATTRS_NODEGROUP_IDENTIFIER = "textAttrs";
    private static readonly HashSet<string> ACCEPTABLE_ALIGNMENTS = new HashSet<string> { "left", "center", "right", "justify" },
        ACCEPTABLE_CHARACTERPOSITIONS = new HashSet<string> { "normal", "subscript", "superscript" };
    public static class DefaultValues
    {
        public const bool AutoKern = true, AliasText = true, Bold = false, Italic = false, Rotation = false;
        public const string Alignment = "left", CharacterPosition = "normal", FillColor = "#000000", Target = "", Url = "", Face = "Suburga2-SemicondensedRegular";
        public const int Indent = 0, LeftMargin = 0, LineSpacing = 0, RightMargin = 0, Size = 12;
        public const double LetterSpacing = 0;

        public const string StrokeColor = "#000000"; 
        public const int StrokeWidth = 0;  
    }
    private readonly XElement? root;
    private bool aliasText, autoKern, bold, italic, rotation;
    private string alignment, characterPosition, face, fillColor, target, url;
    private int indent, leftMargin, lineSpacing, rightMargin, size;
    private double letterSpacing;
    private readonly double lineHeight;
    private string strokeColor;
    private int strokeWidth;
    public bool AliasText { get { return aliasText; } set { aliasText = value; root?.SetOrRemoveAttribute("aliasText", value, DefaultValues.AliasText); } }
    public bool AutoKern { get { return autoKern; } set { autoKern = value; root?.SetOrRemoveAttribute("autoKern", value, DefaultValues.AutoKern); } }
    public bool Bold { get { return bold; } set { bold = value; root?.SetOrRemoveAttribute("bold", value, DefaultValues.Bold); } }
    public bool Italic { get { return italic; } set { italic = value; root?.SetOrRemoveAttribute("italic", value, DefaultValues.Italic); } }
    public bool Rotation { get { return rotation; } set { rotation = value; root?.SetOrRemoveAttribute("rotation", value, DefaultValues.Rotation); } }
    public string StrokeColor{ get { return strokeColor; } set { strokeColor = value; root?.SetOrRemoveAttribute("strokeColor", value, DefaultValues.StrokeColor); }}
    public int StrokeWidth{ get { return strokeWidth; } set { strokeWidth = value; root?.SetOrRemoveAttribute("strokeWidth", value, DefaultValues.StrokeWidth); }}
    public XElement Root { get { return root!; } }

    
    public string Alignment
    {
        get { return alignment; }
        set
        {
            if (!ACCEPTABLE_ALIGNMENTS.Contains(value)) throw new ArgumentException($"Invalid alignment: {value}");
            alignment = value; root?.SetOrRemoveAttribute("alignment", value, DefaultValues.Alignment);
        }
    }
    public string CharacterPosition
    {
        get { return characterPosition; }
        set
        {
            if (!ACCEPTABLE_CHARACTERPOSITIONS.Contains(value)) throw new ArgumentException($"Invalid character position: {value}");
            characterPosition = value; root?.SetOrRemoveAttribute("characterPosition", value, DefaultValues.CharacterPosition);
        }
    }
    public string Face { get { return face; } set { face = value; root?.SetAttributeValue("face", value); } }
    public string FillColor { get { return fillColor; } set { fillColor = value; root?.SetOrRemoveAttribute("fillColor", value, DefaultValues.FillColor); } }
    public string Target { get { return target; } set { target = value; root?.SetOrRemoveAttribute("target", value, DefaultValues.Target); } }
    public string Url { get { return url; } set { url = value; root?.SetOrRemoveAttribute("url", value, DefaultValues.Url); } }
    public int Indent { get { return indent; } set { indent = value; root?.SetOrRemoveAttribute("indent", value, DefaultValues.Indent); } }
    public int LeftMargin { get { return leftMargin; } set { leftMargin = value; root?.SetOrRemoveAttribute("leftMargin", value, DefaultValues.LeftMargin); } }
    public double LetterSpacing { get { return letterSpacing; } set { letterSpacing = value; root?.SetOrRemoveAttribute("letterSpacing", value, DefaultValues.LetterSpacing); } }
    public int LineSpacing { get { return lineSpacing; } set { lineSpacing = value; root?.SetOrRemoveAttribute("lineSpacing", value, DefaultValues.LineSpacing); } }
    public double LineHeight { get { return lineHeight; } } 
    public int RightMargin { get { return rightMargin; } set { rightMargin = value; root?.SetOrRemoveAttribute("rightMargin", value, DefaultValues.RightMargin); } }
    public int Size { get { return size; } set { size = value; root?.SetOrRemoveAttribute("size", value, DefaultValues.Size); } }
    #pragma warning disable CS8618
    internal TextAttrs(XNamespace ns)
    {
        root = new XElement(ns + TextAttrs.TEXTATTRS_NODE_IDENTIFIER);
        AliasText = DefaultValues.AliasText;
        AutoKern = DefaultValues.AutoKern;
        Bold = DefaultValues.Bold;
        Italic = DefaultValues.Italic;
        Rotation = DefaultValues.Rotation;
        Alignment = DefaultValues.Alignment;
        CharacterPosition = DefaultValues.CharacterPosition;
        Face = DefaultValues.Face;
        FillColor = DefaultValues.FillColor;
        Target = DefaultValues.Target;
        Url = DefaultValues.Url;
        Indent = DefaultValues.Indent;
        LeftMargin = DefaultValues.LeftMargin;
        LetterSpacing = DefaultValues.LetterSpacing;
        LineSpacing = DefaultValues.LineSpacing;
        RightMargin = DefaultValues.RightMargin;
        Size = DefaultValues.Size;
        lineHeight = Size * LINEHEIGHT_MULTIPLIER;
        StrokeColor = DefaultValues.StrokeColor;
        StrokeWidth = DefaultValues.StrokeWidth;
    }
    #pragma warning restore CS8618
    internal TextAttrs(XElement textAttrsNode)
    {
        root = textAttrsNode;
        aliasText = (bool?)textAttrsNode.Attribute("aliasText") ?? DefaultValues.AliasText;
        autoKern = (bool?)textAttrsNode.Attribute("autoKern") ?? DefaultValues.AutoKern;
        bold = (bool?)textAttrsNode.Attribute("bold") ?? DefaultValues.Bold;
        italic = (bool?)textAttrsNode.Attribute("italic") ?? DefaultValues.Italic;
        rotation = (bool?)textAttrsNode.Attribute("rotation") ?? DefaultValues.Rotation;
        alignment = (string?)textAttrsNode.Attribute("alignment") ?? DefaultValues.Alignment;
        characterPosition = (string?)textAttrsNode.Attribute("characterPosition") ?? DefaultValues.CharacterPosition;
        face = (string?)textAttrsNode.Attribute("face") ?? DefaultValues.Face;
        fillColor = (string?)textAttrsNode.Attribute("fillColor") ?? DefaultValues.FillColor;
        target = (string?)textAttrsNode.Attribute("target") ?? DefaultValues.Target;
        url = (string?)textAttrsNode.Attribute("url") ?? DefaultValues.Url;
        indent = (int?)textAttrsNode.Attribute("indent") ?? DefaultValues.Indent;
        leftMargin = (int?)textAttrsNode.Attribute("leftMargin") ?? DefaultValues.LeftMargin;
        letterSpacing = (double?)textAttrsNode.Attribute("letterSpacing") ?? DefaultValues.LetterSpacing;
        lineSpacing = (int?)textAttrsNode.Attribute("lineSpacing") ?? DefaultValues.LineSpacing;
        rightMargin = (int?)textAttrsNode.Attribute("rightMargin") ?? DefaultValues.RightMargin;
        size = (int?)textAttrsNode.Attribute("size") ?? DefaultValues.Size;
        lineHeight = (double?)textAttrsNode.Attribute("lineHeight") ?? size * LINEHEIGHT_MULTIPLIER;
        strokeColor = (string?)textAttrsNode.Attribute("strokeColor") ?? DefaultValues.StrokeColor;
        strokeWidth = (int?)textAttrsNode.Attribute("strokeWidth") ?? DefaultValues.StrokeWidth;
    }
    internal TextAttrs(TextAttrs other)
    {
        root = other.root == null ? null : new XElement(other.root);
        aliasText = other.aliasText;
        autoKern = other.autoKern;
        bold = other.bold;
        italic = other.italic;
        rotation = other.rotation;
        alignment = other.alignment;
        characterPosition = other.characterPosition;
        face = other.face;
        fillColor = other.fillColor;
        target = other.target;
        url = other.url;
        indent = other.indent;
        leftMargin = other.leftMargin;
        letterSpacing = other.letterSpacing;
        lineSpacing = other.lineSpacing;
        rightMargin = other.rightMargin;
        size = other.size;
        lineHeight = other.lineHeight;
        strokeColor = other.strokeColor;
        strokeWidth = other.strokeWidth;
    }
    internal void SetAttr(string name, object value)
    {
        switch(name)
        {
            case "aliasText":
                AliasText = (bool)value;
                break;
            case "autoKern":
                AutoKern = (bool)value;
                break;
            case "bold":
                Bold = (bool)value;
                break;
            case "italic":
                Italic = (bool)value;
                break;
            case "rotation":
                Rotation = (bool)value;
                break;
            case "alignment":
                Alignment = (string)value;
                break;
            case "characterPosition":
                CharacterPosition = (string)value;
                break;
            case "face":
                Face = (string)value;
                break;
            case "fillColor":
                FillColor = (string)value;
                break;
            case "target":
                Target = (string)value;
                break;
            case "url":
                Url = (string)value;
                break;
            case "indent":
                Indent = (int)value;
                break;
            case "leftMargin":
                LeftMargin = (int)value;
                break;
            case "letterSpacing":
                LetterSpacing = Convert.ToDouble(value);
                break;
            case "lineSpacing":
                LineSpacing = (int)value;
                break;
            case "rightMargin":
                RightMargin = (int)value;
                break;
            case "size":
                Size = (int)value;
                break;
            case "strokeColor":
                StrokeColor = (string)value;
                break;
            case "strokeWidth":
                StrokeWidth = (int)value;
                break;
            default:
                throw new ArgumentException($"Invalid attribute name: {name}");
        }
    }
}