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
        public const string Method = Frame.DefaultValues.EaseMethodName;
    }
    public string Target { get; set; }
    internal double GetMultiplier(int frameIndex, int frameCount);
}
public class Ease : IEase
{
    public static class DefaultValues
    {
        public const int Intensity = 0;
    }
    internal const string EASE_NODE_IDENTIFIER = "Ease";
    private string target;
    private string method;
    private int intensity;
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
    public int Intensity { get => intensity; set { intensity = value; root?.SetOrRemoveAttribute("intensity", value, DefaultValues.Intensity); } }
    internal Ease(XElement easeNode)
    {
        root = easeNode;
        ns = easeNode.Name.Namespace;
        target = easeNode.Attribute("target")?.Value ?? "";
        method = easeNode.Attribute("method")?.Value ?? "";
        intensity = (int?)easeNode.Attribute("intensity") ?? DefaultValues.Intensity;
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
    public Ease(string? target, string? method, XNamespace ns, int intensity = 0)
    {
        root = new XElement(ns + Ease.EASE_NODE_IDENTIFIER);
        this.ns = ns;
        Target = target ?? IEase.DefaultValues.Target;
        Method = method ?? IEase.DefaultValues.Method;
        Intensity = intensity;
    }
    public Ease(Ease other)
    {
        root = other.root == null ? null : new XElement(other.root);
        ns = other.ns;
        Target = other.Target;
        Method = other.Method;
        Intensity = other.Intensity;
    }
#pragma warning restore CS8618
    public Ease(XNamespace ns)
    {
        this.ns = ns;
        method = IEase.DefaultValues.Method;
        target = IEase.DefaultValues.Target;
    }

    double IEase.GetMultiplier(int frameIndex, int frameCount)
    {
        double progress = frameIndex / (double)frameCount;
        double oneMinusProgress = 1 - progress;
        const double backConst1 = 1.70158, backConst2 = backConst1 + 1, backConst3 = backConst1 * 1.525;
        const double elasticConst1 = 2 * Math.PI / 3.0, elasticConst2 = 2 * Math.PI / 4.5;
        return method switch
        {
            "none" => progress,
            "classic" => EvaluateClassicEase(progress),
            "quadIn" => progress * progress,
            "quadOut" => 1 - oneMinusProgress * oneMinusProgress,
            "quadInOut" => progress < 0.5 ? 2 * progress * progress : 1 - Math.Pow(-2 * progress + 2, 2) / 2,
            "cubicIn" => progress * progress * progress,
            "cubicOut" => 1 - oneMinusProgress * oneMinusProgress * oneMinusProgress,
            "cubicInOut" => progress < 0.5 ? 4 * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 3) / 2,
            "quartIn" => progress * progress * progress * progress,
            "quartOut" => 1 - oneMinusProgress * oneMinusProgress * oneMinusProgress * oneMinusProgress,
            "quartInOut" => progress < 0.5 ? 8 * progress * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 4) / 2,
            "quintIn" => progress * progress * progress * progress * progress,
            "quintOut" => 1 - oneMinusProgress * oneMinusProgress * oneMinusProgress * oneMinusProgress * oneMinusProgress,
            "quintInOut" => progress < 0.5 ? 16 * progress * progress * progress * progress * progress : 1 - Math.Pow(-2 * progress + 2, 5) / 2,
            "sineIn" => 1 - Math.Cos(progress * Math.PI / 2),
            "sineOut" => Math.Sin(progress * Math.PI / 2),
            "sineInOut" => -(Math.Cos(Math.PI * progress) - 1) / 2,
            "bounceIn" => 1 - EaseOutBounce(1 - progress),
            "bounceOut" => EaseOutBounce(progress),
            "bounceInOut" => progress < 0.5 ? (1 - EaseOutBounce(1 - 2 * progress)) / 2 : (1 + EaseOutBounce(2 * progress - 1)) / 2,
            "backIn" => backConst2 * progress * progress * progress - backConst1 * progress * progress,
            "backOut" => 1 + backConst2 * Math.Pow(progress - 1, 3) + backConst1 * Math.Pow(progress - 1, 2),
            "backInOut" => progress < 0.5 ? Math.Pow(2 * progress, 2) * ((backConst3 + 1) * 2 * progress - backConst3) / 2 : (Math.Pow(2 * progress - 2, 2) * ((backConst3 + 1) * (progress * 2 - 2) + backConst3) + 2) / 2,
            "elasticIn" => (progress == 0) ? 0 : progress == 1 ? 1 : -Math.Pow(2, 10 * progress - 10) * Math.Sin((progress * 10 - 10.75) * elasticConst1),
            "elasticOut" => (progress == 0) ? 0 : progress == 1 ? 1 : Math.Pow(2, -10 * progress) * Math.Sin((progress * 10 - 0.75) * elasticConst1) + 1,
            "elasticInOut" => (progress == 0) ? 0 : progress == 1 ? 1 : progress < 0.5 ? -(Math.Pow(2, 20 * progress - 10) * Math.Sin((20 * progress - 11.125) * elasticConst2)) / 2 : Math.Pow(2, -20 * progress + 10) * Math.Sin((20 * progress - 11.125) * elasticConst2) / 2 + 1,
            _ => throw new ArgumentException($"Invalid method: {method}"),
        };
    }
    private double EvaluateClassicEase(double progress)
    {
        double delta = (100.0 - intensity) / 300.0;
        Point p0 = new(0, 0, ""), p1 = new(1.0 / 3.0, delta, ""), p2 = new(2.0 / 3.0, 1.0 / 3.0 + delta, ""), p3 = new(1, 1, "");
        return CustomEase.EvaluateBezierPoint(p0, p1, p2, p3, progress);
    }
    private static double EaseOutBounce(double x)
    {
        const double n1 = 7.5625;
        const double d1 = 2.75;

        if (x < 1 / d1)
        {
            return n1 * x * x;
        }
        else if (x < 2 / d1)
        {
            return n1 * (x -= 1.5 / d1) * x + 0.75;
        }
        else if (x < 2.5 / d1)
        {
            return n1 * (x -= 2.25 / d1) * x + 0.9375;
        }
        else
        {
            return n1 * (x -= 2.625 / d1) * x + 0.984375;
        }
    }
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

    double IEase.GetMultiplier(int frameIndex, int frameCount)
    {
        // cubic bezier evaluation
        double t = frameIndex / (double)frameCount;
        if (t == 1) return 1;
        if (t == 0) return 0;
        List<double> xVals = [Points[0].X];
        for (int i = 3; i < Points.Count - 1; i += 3)
        {
            xVals.Add(Points[i].X);
        }
        xVals.Add(Points[^1].X);
        // find index that t is in between-- guaranteed because xVals is sorted (could use binary search)
        int tIndex = xVals.FindIndex(x => t <= x) - 1;
        Point p0 = Points[3 * tIndex];
        Point p1 = Points[3 * tIndex + 1];
        Point p2 = Points[3 * tIndex + 2];
        Point p3 = Points[3 * tIndex + 3];
        // update t to fit the new frame of reference
        t = (t - p0.X) / (p3.X - p0.X);
        return EvaluateBezierPoint(p0, p1, p2, p3, t);
    }
    public static double EvaluateBezierPoint(Point p0, Point p1, Point p2, Point p3, double t)
    {
        double t2 = t * t;
        double t3 = t * t2;
        double mt = 1 - t;
        double mt2 = mt * mt;
        double mt3 = mt2 * mt;
        double y = mt3 * p0.Y + 3 * mt2 * t * p1.Y + 3 * mt * t2 * p2.Y + t3 * p3.Y;
        return y;
    }
}