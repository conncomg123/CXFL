using System.Xml.Linq;

namespace CsXFL;
public interface IEase
{
    internal const string EASES_NODEGROUP_IDENTIFIER = "tweens";
    public static HashSet<string> ACCEPTABLE_TARGETS = new() { "all", "position", "scale", "rotation", "color", "filters" };
    public static HashSet<string> ACCEPTABLE_METHODS = GenerateAcceptableMethods();

    private static HashSet<string> GenerateAcceptableMethods()
    {
        // cartesian product of methods and types
        var methods = new[] { "quad", "cubic", "quart", "quint", "sine", "bounce", "circ", "back", "elastic" };
        var types = new[] { "In", "Out", "InOut" };
        var acceptableMethods = new HashSet<string>();

        foreach (var method in methods)
        {
            foreach (var type in types)
            {
                acceptableMethods.Add($"{method}{type}");
            }
        }
        acceptableMethods.Add("none");
        acceptableMethods.Add(string.Empty);
        return acceptableMethods;
    }
    public static class DefaultValues
    {
        public const string Target = default;
        public const string Method = default;
    }
    public string Target { get; set; }
}
public class Ease : IEase
{
    internal const string EASE_NODE_IDENTIFIER = "Ease";
    private string target;
    private string method;
    private XElement? root;
    private XNamespace ns;
    
    public string Target
    {
        get => target; set
        {
            if (!IEase.ACCEPTABLE_TARGETS.Contains(value))
            {
                throw new ArgumentException($"Invalid target: {value}");
            }
            target = value;
            root?.SetOrRemoveAttribute("target", value, IEase.DefaultValues.Target);
        }
    }
    public string Method
    {
        get => method; set
        {
            if (!IEase.ACCEPTABLE_METHODS.Contains(value))
            {
                throw new ArgumentException($"Invalid method: {value}");
            }
            method = value;
            root?.SetOrRemoveAttribute("method", value, IEase.DefaultValues.Method);
        }
    }
    internal Ease(XElement easeNode)
    {
        root = easeNode;
        ns = easeNode.Name.Namespace;
        target = easeNode.Attribute("target")?.Value ?? "";
        method = easeNode.Attribute("method")?.Value ?? "";
        if (!IEase.ACCEPTABLE_TARGETS.Contains(target))
        {
            throw new ArgumentException($"Invalid target: {target}");
        }
        if (!IEase.ACCEPTABLE_METHODS.Contains(method))
        {
            throw new ArgumentException($"Invalid method: {method}");
        }
    }
#pragma warning disable CS8618
    public Ease(string? target, string? method, XNamespace ns)
    {
        root = new XElement(ns + Ease.EASE_NODE_IDENTIFIER);
        this.ns = ns;
        Target = target ?? IEase.DefaultValues.Target;
        Method = method ?? IEase.DefaultValues.Method;
    }
    public Ease(Ease other)
    {
        root = other.root == null ? null : new XElement(other.root);
        ns = other.ns;
        Target = other.Target;
        Method = other.Method;
    }
#pragma warning restore CS8618
}
public class CustomEase : IEase
{
    internal const string CUSTOM_EASE_NODE_IDENTIFIER = "CustomEase";
    private string target;
    private XElement? root;
    private XNamespace ns;
    public string Target
    {
        get => target; set
        {
            if (!IEase.ACCEPTABLE_TARGETS.Contains(value))
            {
                throw new ArgumentException($"Invalid target: {value}");
            }
            target = value;
        }
    }
    public List<Point> Points { get; }
    public CustomEase(XElement customEaseNode)
    {
        root = customEaseNode;
        ns = customEaseNode.Name.Namespace;
        target = customEaseNode.Attribute("target")?.Value ?? "";
        if (!IEase.ACCEPTABLE_TARGETS.Contains(target))
        {
            throw new ArgumentException($"Invalid target: {target}");
        }
        Points = customEaseNode.Elements(ns + Point.POINT_NODE_IDENTIFIER).Select(pointNode => new Point(pointNode)).ToList();
    }
    #pragma warning disable CS8618
    public CustomEase(string target, IEnumerable<Point> points, XNamespace ns)
    {
        root = new XElement(ns + CustomEase.CUSTOM_EASE_NODE_IDENTIFIER);
        this.ns = ns;
        Target = target;
        Points = new List<Point>(points);
    }
    public CustomEase(CustomEase other)
    {
        root = other.root == null ? null : new XElement(other.root);
        ns = other.ns;
        Target = other.Target;
        Points = new List<Point>(other.Points);
    }
    #pragma warning restore CS8618
}