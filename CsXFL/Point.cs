using System.Xml.Linq;

public class Point
{
    private const double Epsilon = 0.0001;
    public static class DefaultValues
    {
        public const double X = 0.0;
        public const double Y = 0.0;
    }
    private double x, y;
    private XElement? root;
        public double X { get { return x; } set { x = value; SetOrRemoveAttribute("x", value, DefaultValues.X); } }
    public double Y { get { return y; } set { y = value; SetOrRemoveAttribute("y", value, DefaultValues.Y); } }
    public ref XElement? Root { get { return ref root; } }

    private void SetOrRemoveAttribute(in string attributeName, double value, double defaultValue)
    {
        if (Math.Abs(value - defaultValue) < Epsilon)
        {
            root?.Attribute(attributeName)?.Remove();
        }
        else
        {
            root?.SetAttributeValue(attributeName, value);
        }
    }

    public Point()
    {
        root = null;
        x = DefaultValues.X;
        y = DefaultValues.Y;
    }
    public Point(double x, double y)
    {
        root = null;
        this.x = x;
        this.y = y;
    }
    public Point(XElement pointNode)
    {
        root = pointNode;
        x = (double?) pointNode.Attribute("x") ?? DefaultValues.X;
        y = (double?) pointNode.Attribute("y") ?? DefaultValues.Y;
    }
    public Point(in Point other)
    {
        root = other.Root is null ? null : new XElement(other.Root);
        x = other.X;
        y = other.Y;
    }
}