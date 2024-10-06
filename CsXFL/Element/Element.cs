using System.Xml.Linq;
namespace CsXFL;
public abstract class Element
{
    private static readonly HashSet<string> AcceptableElementTypes = new HashSet<string> { "shape", "text", "tflText", "instance", "shapeObj" };
    internal const string ELEMENTS_NODEGROUP_IDENTIFIER = "elements";
    public static class DefaultValues
    {
        public const double Width = double.NaN;
        public const double Height = double.NaN;
        public const bool Selected = false;
        public const string Name = "";

    }
    protected XElement? root;
    protected XNamespace ns;
    protected string elementType;
    protected string? name;
    protected double width, height;
    protected bool selected;
    protected Matrix matrix;
    protected Point transformationPoint;
    internal XElement? Root { get { return root; } }
    public string ElementType { get { return elementType; } protected set { elementType = value ?? ""; root?.SetAttributeValue("elementType", value); } }
    public string? Name { get { return name; } set { name = value; root?.SetOrRemoveAttribute("name", value, DefaultValues.Name); } }
    public virtual double Width { get { return width; } set { width = value; root?.SetAttributeValue("width", value); } }
    public virtual double Height { get { return height; } set { height = value; root?.SetAttributeValue("height", value); } }
    public bool Selected { get { return selected; } set { selected = value; root?.SetOrRemoveAttribute("isSelected", value, DefaultValues.Selected); } }
    public Matrix Matrix { get { return matrix; } set { SetMatrix(value); } }
    public double ScaleX { get { return matrix.A; } set { matrix.A = value; } }
    public double ScaleY { get { return matrix.D; } set { matrix.D = value; } }
    public Point TransformationPoint { get { return transformationPoint; } }
    internal Element(XNamespace ns)
    {
        root = null;
        this.ns = ns;
        elementType = string.Empty;
        width = double.NaN;
        height = double.NaN;
        selected = false;
        matrix = new Matrix(ns, root);
        transformationPoint = new Point(ns);
    }
    internal Element(in XElement elementNode, string elementType)
    {
        if (!AcceptableElementTypes.Contains(elementType))
        {
            throw new ArgumentException("Invalid element type: " + elementType);
        }
        root = elementNode;
        ns = root.Name.Namespace;
        this.elementType = elementType;
        width = double.NaN;
        height = double.NaN;
        selected = (bool?)elementNode.Attribute("isSelected") ?? DefaultValues.Selected;
        matrix = elementNode.Element(ns + Matrix.MATRIX_NODE_IDENTIFIER)?.Element(ns + Matrix.MATRIX_NODEGROUP_IDENTIFIER) is not null ? new Matrix(elementNode.Element(ns + Matrix.MATRIX_NODE_IDENTIFIER)!.Element(ns + Matrix.MATRIX_NODEGROUP_IDENTIFIER), root) : new Matrix(ns, root);
        transformationPoint = elementNode.Element(ns + Point.TRANSFORMATION_POINT_NODE_IDENTIFIER)?.Element(ns + Point.POINT_NODE_IDENTIFIER) is not null ? new Point(elementNode.Element(ns + Point.TRANSFORMATION_POINT_NODE_IDENTIFIER)!.Element(ns + Point.POINT_NODE_IDENTIFIER)!) : new Point(ns);
    }
    internal Element(in Element other)
    {
        root = other.Root is null ? null : new XElement(other.Root);
        ns = other.ns;
        elementType = other.elementType;
        width = other.width;
        height = other.height;
        selected = other.selected;
        matrix = new Matrix(other.matrix);
        transformationPoint = new Point(other.transformationPoint);
    }
    internal Element(Item item, string elementType, string nodeName) : this(item.Namespace)
    {
        if (!AcceptableElementTypes.Contains(elementType))
        {
            throw new ArgumentException("Invalid element type: " + elementType);
        }
        ns = item.Namespace;
        root = new XElement(ns + nodeName);
        root.Add(new XElement(ns + Point.TRANSFORMATION_POINT_NODE_IDENTIFIER));
        root.Element(ns + Point.TRANSFORMATION_POINT_NODE_IDENTIFIER)?.Add(transformationPoint.Root);
        this.elementType = elementType;
        matrix.SetParent(root);
    }
    private void SetMatrix(Matrix matrix)
    {
        // set values, not the matrix itself
        this.matrix.A = matrix.A;
        this.matrix.B = matrix.B;
        this.matrix.C = matrix.C;
        this.matrix.D = matrix.D;
        this.matrix.Tx = matrix.Tx;
        this.matrix.Ty = matrix.Ty;
    }
}