using System.Xml.Linq;
namespace CsXFL;
public abstract class Element
{
    private static readonly HashSet<string> AcceptableElementTypes = new HashSet<string> { "shape", "text", "tflText", "instance", "shapeObj" };
    internal static class DefaultValues
    {
        public const double Width = double.NaN;
        public const double Height = double.NaN;
        public const bool Selected = false;

    }
    protected XElement? root;
    protected XNamespace ns;
    protected string elementType;
    protected double width, height;
    protected bool selected;
    protected Matrix matrix;
    protected Point transformationPoint;
    internal XElement? Root { get { return root; } }
    public string? ElementType { get { return elementType; } }
    public virtual double Width { get { return width; } set { width = value; root?.SetAttributeValue("width", value); } }
    public virtual double Height { get { return height; } set { height = value; root?.SetAttributeValue("height", value); } }
    public bool Selected { get { return selected; } set { selected = value; root?.SetOrRemoveAttribute("isSelected", value, DefaultValues.Selected); } }
    public Matrix Matrix { get { return matrix; } set { SetMatrix(value); } }
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
        matrix = elementNode.Element(ns + "matrix")?.Element(ns + "Matrix") is not null ? new Matrix(elementNode.Element(ns + "matrix")!.Element(ns + "Matrix"), root) : new Matrix(ns, root);
        transformationPoint = elementNode.Element(ns + "transformationPoint")?.Element(ns + "Point") is not null ? new Point(elementNode.Element(ns + "transformationPoint")!.Element(ns + "Point")!) : new Point(ns);
    }
    internal Element(in Element other)
    {
        root = other.Root is null ? null : new XElement(other.Root);
        ns = other.ns;
        elementType = other.elementType;
        width = other.width;
        height = other.height;
        selected = other.selected;
        matrix = new Matrix(root?.Element(ns + "matrix")?.Element(ns + "Matrix")!, root);
        transformationPoint = new Point(root?.Element(ns + "transformationPoint")!.Element(ns + "Point")!);
    }
    internal Element(Item item, string elementType, string nodeName) : this(item.Namespace)
    {
        if (!AcceptableElementTypes.Contains(elementType))
        {
            throw new ArgumentException("Invalid element type: " + elementType);
        }
        ns = item.Namespace;
        root = new XElement(ns + nodeName);
        root.Add(new XElement(ns + "transformationPoint"));
        root.Element(ns + "transformationPoint")?.Add(transformationPoint.Root);
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