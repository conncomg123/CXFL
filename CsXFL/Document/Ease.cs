using System.Xml.Linq;

namespace CsXFL;
public class Ease
{
    private static HashSet<string> ACCEPTABLE_TARGETS = new() { "all", "position", "scale", "rotation", "color", "filters" };
    private static HashSet<string> ACCEPTABLE_METHODS = GenerateAcceptableMethods();

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
        return acceptableMethods;
    }
    private static class DefaultValues
    {
        public const string Target = default;
        public const string Method = default;
    }
    private string target, method;
    private XElement? root;
    public string Target
    {
        get => target; set
        {
            if (!ACCEPTABLE_TARGETS.Contains(value))
            {
                throw new ArgumentException($"Invalid target: {value}");
            }
            target = value;
            root?.SetOrRemoveAttribute("target", value, DefaultValues.Target);
        }
    }
    public string Method
    {
        get => method; set
        {
            if (!ACCEPTABLE_METHODS.Contains(value))
            {
                throw new ArgumentException($"Invalid method: {value}");
            }
            method = value;
            root?.SetOrRemoveAttribute("method", value, DefaultValues.Method);
        }
    }
    internal Ease(XElement easeNode)
    {
        root = easeNode;
        target = easeNode.Attribute("target")?.Value ?? "";
        method = easeNode.Attribute("method")?.Value ?? "";
        if(!ACCEPTABLE_TARGETS.Contains(target))
        {
            throw new ArgumentException($"Invalid target: {target}");
        }
        if(!ACCEPTABLE_METHODS.Contains(method))
        {
            throw new ArgumentException($"Invalid method: {method}");
        }
    }
#pragma warning disable CS8618
    public Ease(string? target, string? method)
    {
        root = new XElement("Ease");
        Target = target ?? DefaultValues.Target;
        Method = method ?? DefaultValues.Method;
    }
    public Ease(Ease other)
    {
        root = new XElement("Ease");
        Target = other.Target;
        Method = other.Method;
    }
#pragma warning restore CS8618
}