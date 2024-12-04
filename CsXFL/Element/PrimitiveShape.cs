using System.Xml.Linq;

namespace CsXFL;

public abstract class PrimitiveShape : Element
{
    public const string PRIMITIVE_FILL_NODE_IDENTIFIER = "fill",
PRIMITIVE_STROKE_NODE_IDENTIFIER = "stroke";
    private double objectWidth, objectHeight, x, y;
    private FillStyle? fill;
    private StrokeStyle? stroke;
    public double ObjectWidth { get { return objectWidth; } set { objectWidth = value; root?.SetAttributeValue("objectWidth", value); } }
    public double ObjectHeight { get { return objectHeight; } set { objectHeight = value; root?.SetAttributeValue("objectHeight", value); } }
    public double X { get { return x; } set { x = value; root?.SetAttributeValue("x", value); } }
    public double Y { get { return y; } set { y = value; root?.SetAttributeValue("y", value); } }
    public FillStyle? Fill { get { return fill; } set { SetFill(value); } }
    public StrokeStyle? Stroke { get { return stroke; } set { SetStroke(value); } }
    private void SetFill(FillStyle? fill)
    {
        this.fill = fill;
        if (root is null) return;
        root.Element(ns + PRIMITIVE_FILL_NODE_IDENTIFIER)?.Remove();
        if (fill is null) return;
        XElement fillNode = new XElement(ns + PRIMITIVE_FILL_NODE_IDENTIFIER);
        fillNode.Add(fill.Root);
        root.Add(fillNode);
    }
    private void SetStroke(StrokeStyle? stroke)
    {
        this.stroke = stroke;
        if (root is null) return;
        root.Element(ns + PRIMITIVE_STROKE_NODE_IDENTIFIER)?.Remove();
        if(stroke is null) return;
        XElement strokeNode = new XElement(ns + PRIMITIVE_STROKE_NODE_IDENTIFIER);
        strokeNode.Add(stroke.Root);
        root.Add(strokeNode);
    }
    public PrimitiveShape(XElement primitiveNode) : base(primitiveNode, "shape")
    {
        objectWidth = (double?)primitiveNode.Attribute("objectWidth") ?? double.NaN;
        objectHeight = (double?)primitiveNode.Attribute("objectHeight") ?? double.NaN;
        x = (double?)primitiveNode.Attribute("x") ?? double.NaN;
        y = (double?)primitiveNode.Attribute("y") ?? double.NaN;
        fill = primitiveNode.Element(ns + PRIMITIVE_FILL_NODE_IDENTIFIER) is null ? null : new FillStyle(primitiveNode.Element(ns + PRIMITIVE_FILL_NODE_IDENTIFIER)!);
        stroke = primitiveNode.Element(ns + PRIMITIVE_STROKE_NODE_IDENTIFIER) is null ? null : new StrokeStyle(primitiveNode.Element(ns + PRIMITIVE_STROKE_NODE_IDENTIFIER)!);
    }
    public PrimitiveShape(PrimitiveShape other) : base(other)
    {
        objectWidth = other.objectWidth;
        objectHeight = other.objectHeight;
        x = other.x;
        y = other.y;
        fill = other.fill is null ? null : new FillStyle(other.fill);
        stroke = other.stroke is null ? null : new StrokeStyle(other.stroke);
    }
}
public class PrimitiveOval : PrimitiveShape
{
    public const string PRIMITIVE_OVAL_NODE_IDENTIFIER = "DOMOvalObject";
    public new static class DefaultValues
    {
        public const double StartAngle = 0;
        public const double EndAngle = 360;
        public const double InnerRadius = 0;
        public const bool ClosePath = true;
    }
    private double startAngle, endAngle, innerRadius;
    private bool closePath;
    public double StartAngle { get { return startAngle; } set { startAngle = value; root?.SetOrRemoveAttribute("startAngle", value, DefaultValues.StartAngle); } }
    public double EndAngle { get { return endAngle; } set { endAngle = value; root?.SetOrRemoveAttribute("endAngle", value, DefaultValues.EndAngle); } }
    public double InnerRadius { get { return innerRadius; } set { innerRadius = value; root?.SetOrRemoveAttribute("innerRadius", value, DefaultValues.InnerRadius); } }
    public bool ClosePath { get { return closePath; } set { closePath = value; root?.SetOrRemoveAttribute("closePath", value, DefaultValues.ClosePath); } }
    public PrimitiveOval(XElement ovalNode) : base(ovalNode)
    {
        startAngle = (double?)ovalNode.Attribute("startAngle") ?? DefaultValues.StartAngle;
        endAngle = (double?)ovalNode.Attribute("endAngle") ?? DefaultValues.EndAngle;
        innerRadius = (double?)ovalNode.Attribute("innerRadius") ?? DefaultValues.InnerRadius;
        closePath = (bool?)ovalNode.Attribute("closePath") ?? DefaultValues.ClosePath;
    }
    public PrimitiveOval(PrimitiveOval other) : base(other)
    {
        startAngle = other.startAngle;
        endAngle = other.endAngle;
        innerRadius = other.innerRadius;
        closePath = other.closePath;
    }
}
public class PrimitiveRectangle : PrimitiveShape
{
    public const string PRIMITIVE_RECTANGLE_NODE_IDENTIFIER = "DOMRectangleObject";
    public new static class DefaultValues
    {
        public const double TopLeftRadius = 0;
        public const double TopRightRadius = 0;
        public const double BottomLeftRadius = 0;
        public const double BottomRightRadius = 0;
        public const bool LockFlag = false;
    }
    private double topLeftRadius, topRightRadius, bottomLeftRadius, bottomRightRadius;
    private bool lockFlag;
    public double TopLeftRadius { get { return topLeftRadius; } set { topLeftRadius = value; root?.SetOrRemoveAttribute("topLeftRadius", value, DefaultValues.TopLeftRadius); } }
    public double TopRightRadius { get { return topRightRadius; } set { topRightRadius = value; root?.SetOrRemoveAttribute("topRightRadius", value, DefaultValues.TopRightRadius); } }
    public double BottomLeftRadius { get { return bottomLeftRadius; } set { bottomLeftRadius = value; root?.SetOrRemoveAttribute("bottomLeftRadius", value, DefaultValues.BottomLeftRadius); } }
    public double BottomRightRadius { get { return bottomRightRadius; } set { bottomRightRadius = value; root?.SetOrRemoveAttribute("bottomRightRadius", value, DefaultValues.BottomRightRadius); } }
    public bool LockFlag { get { return lockFlag; } set { lockFlag = value; root?.SetOrRemoveAttribute("lockFlag", value, DefaultValues.LockFlag); } }
    public PrimitiveRectangle(XElement rectangleNode) : base(rectangleNode)
    {
        topLeftRadius = (double?)rectangleNode.Attribute("topLeftRadius") ?? DefaultValues.TopLeftRadius;
        topRightRadius = (double?)rectangleNode.Attribute("topRightRadius") ?? DefaultValues.TopRightRadius;
        bottomLeftRadius = (double?)rectangleNode.Attribute("bottomLeftRadius") ?? DefaultValues.BottomLeftRadius;
        bottomRightRadius = (double?)rectangleNode.Attribute("bottomRightRadius") ?? DefaultValues.BottomRightRadius;
        lockFlag = (bool?)rectangleNode.Attribute("lockFlag") ?? DefaultValues.LockFlag;
    }
    public PrimitiveRectangle(PrimitiveRectangle other) : base(other)
    {
        topLeftRadius = other.topLeftRadius;
        topRightRadius = other.topRightRadius;
        bottomLeftRadius = other.bottomLeftRadius;
        bottomRightRadius = other.bottomRightRadius;
        lockFlag = other.lockFlag;
    }
}