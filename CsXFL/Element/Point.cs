using System.Xml.Linq;
namespace CsXFL;

public class Point
{
    internal const string POINT_NODE_IDENTIFIER = "Point",
    TRANSFORMATION_POINT_NODE_IDENTIFIER = "transformationPoint";
    private const double Epsilon = 0.0001;
    internal static class DefaultValues
    {
        public const double X = 0.0;
        public const double Y = 0.0;
    }
    private double x, y;
    private readonly XNamespace ns;
    private XElement root;
    public double X { get { return x; } set { x = value; root?.SetOrRemoveAttribute("x", value, DefaultValues.X); } }
    public double Y { get { return y; } set { y = value; root?.SetOrRemoveAttribute("y", value, DefaultValues.Y); } }
    internal ref XElement Root { get { return ref root; } }
    public Point(XNamespace ns)
    {
        this.ns = ns;
        root = new XElement(ns + Point.POINT_NODE_IDENTIFIER);
        x = DefaultValues.X;
        y = DefaultValues.Y;
    }
    public Point(double x, double y, XNamespace ns)
    {
        this.ns = ns;
        root = new XElement(ns + Point.POINT_NODE_IDENTIFIER);
        this.x = x;
        this.y = y;
    }
    public Point(XElement pointNode)
    {
        ns = pointNode.Name.Namespace;
        root = pointNode;
        x = (double?) pointNode.Attribute("x") ?? DefaultValues.X;
        y = (double?) pointNode.Attribute("y") ?? DefaultValues.Y;
    }
    public Point(in Point other)
    {
        ns = other.ns;
        root = new XElement(other.root);
        x = other.X;
        y = other.Y;
    }
}