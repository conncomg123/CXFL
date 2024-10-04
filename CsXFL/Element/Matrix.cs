using System.Xml.Linq;
namespace CsXFL;
public class Matrix
{
    internal const string MATRIX_NODE_IDENTIFIER = "matrix",
    MATRIX_NODEGROUP_IDENTIFIER = "Matrix";
    private const double Epsilon = 0.0001;
    public static class DefaultValues
    {
        public const double A = 1.0;
        public const double B = 0.0;
        public const double C = 0.0;
        public const double D = 1.0;
        public const double Tx = 0.0;
        public const double Ty = 0.0;
    }
    private double a, b, c, d, tx, ty;
    private XElement? root;
    private readonly XNamespace ns;
    private XElement? parent;
    private void SetOrRemoveAttribute(in string attributeName, double value, double defaultValue)
    {
        bool IsDefaultValue = Math.Abs(value - defaultValue) < Epsilon;
        if (root is null)
        {
            if (!IsDefaultValue)
            {
                parent?.Add(new XElement(ns + Matrix.MATRIX_NODE_IDENTIFIER));
                parent?.Element(ns + Matrix.MATRIX_NODE_IDENTIFIER)?.Add(new XElement(ns + Matrix.MATRIX_NODEGROUP_IDENTIFIER));
                Root = parent?.Element(ns + Matrix.MATRIX_NODE_IDENTIFIER)?.Element(ns + Matrix.MATRIX_NODEGROUP_IDENTIFIER);
            }
            else return;
        }
        if (IsDefaultValue)
        {
            Root?.Attribute(attributeName)?.Remove();
        }
        else
        {
            Root?.SetAttributeValue(attributeName, value);
        }
    }
    public static bool IsDefaultMatrix(in Matrix matrix)
    {
        return Math.Abs(matrix.A - DefaultValues.A) < Epsilon &&
            Math.Abs(matrix.B - DefaultValues.B) < Epsilon &&
            Math.Abs(matrix.C - DefaultValues.C) < Epsilon &&
            Math.Abs(matrix.D - DefaultValues.D) < Epsilon &&
            Math.Abs(matrix.Tx - DefaultValues.Tx) < Epsilon &&
            Math.Abs(matrix.Ty - DefaultValues.Ty) < Epsilon;
    }
    private void RemoveDefaultMatrix()
    {
        if (IsDefaultMatrix(this))
        {
            parent?.Element(ns + Matrix.MATRIX_NODE_IDENTIFIER)?.Remove();
            Root = null;
        }
    }
    public double A
    {
        get { return a; }
        set
        {
            a = value;
            SetOrRemoveAttribute("a", value, DefaultValues.A);
            RemoveDefaultMatrix();
        }
    }
    public double B
    {
        get { return b; }
        set
        {
            b = value;
            SetOrRemoveAttribute("b", value, DefaultValues.B);
            RemoveDefaultMatrix();
        }
    }
    public double C
    {
        get { return c; }
        set
        {
            c = value;
            SetOrRemoveAttribute("c", value, DefaultValues.C);
            RemoveDefaultMatrix();
        }
    }
    public double D
    {
        get { return d; }
        set
        {
            d = value;
            SetOrRemoveAttribute("d", value, DefaultValues.D);
            RemoveDefaultMatrix();
        }
    }
    public double Tx
    {
        get { return tx; }
        set
        {
            tx = value;
            SetOrRemoveAttribute("tx", value, DefaultValues.Tx);
            RemoveDefaultMatrix();
        }
    }
    public double Ty
    {
        get { return ty; }
        set
        {
            ty = value;
            SetOrRemoveAttribute("ty", value, DefaultValues.Ty);
            RemoveDefaultMatrix();
        }
    }
    internal ref XElement? Root { get { return ref root; } }
    internal void SetParent(in XElement? parent)
    {
        this.parent = parent;
    }
    internal Matrix(XNamespace ns, XElement? parent)
    {
        this.ns = ns;
        root = null;
        this.parent = parent;
        A = DefaultValues.A;
        B = DefaultValues.B;
        C = DefaultValues.C;
        D = DefaultValues.D;
        Tx = DefaultValues.Tx;
        Ty = DefaultValues.Ty;
    }
    internal Matrix(double a, double b, double c, double d, double tx, double ty)
    {
        ns = string.Empty;
        A = a;
        B = b;
        C = c;
        D = d;
        Tx = tx;
        Ty = ty;
    }
    internal Matrix(in XElement? matrixNode, in XElement? parent)
    {
        root = matrixNode;
        ns = (root is null) ? (parent is null) ? string.Empty : parent.Name.Namespace : root.Name.Namespace;
        this.parent = parent;
        a = (double?)matrixNode?.Attribute("a") ?? DefaultValues.A;
        b = (double?)matrixNode?.Attribute("b") ?? DefaultValues.B;
        c = (double?)matrixNode?.Attribute("c") ?? DefaultValues.C;
        d = (double?)matrixNode?.Attribute("d") ?? DefaultValues.D;
        tx = (double?)matrixNode?.Attribute("tx") ?? DefaultValues.Tx;
        ty = (double?)matrixNode?.Attribute("ty") ?? DefaultValues.Ty;
    }
    internal Matrix(in Matrix other)
    {
        root = other.Root is null ? null : new XElement(other.Root);
        parent = other.parent;
        ns = other.ns;
        a = other.A;
        b = other.B;
        c = other.C;
        d = other.D;
        tx = other.Tx;
        ty = other.Ty;
    }
    public Matrix()
    {
        ns = string.Empty;
    }
}