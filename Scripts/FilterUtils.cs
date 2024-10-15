using CsXFL;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

// Missing a lot of Atomics
// FeMerge & MergeNode is unintuitive and bad
namespace Rendering
{
    // https://developer.mozilla.org/en-US/docs/Web/SVG/Element/filter

    public class FilterUtils
    {
        public class AtomicFilter
        {
            public string FilterType { get; set; }
            public Dictionary<string, string> Attributes { get; set; }

            public AtomicFilter(string filterType, params (string key, string value)[] attributes)
            {
                FilterType = filterType;
                Attributes = new Dictionary<string, string>();

                Attributes.Add("result", filterType); // Add this line

                foreach (var (key, value) in attributes)
                {
                    Attributes[key] = value;
                }
            }

            public virtual XElement ToXElement()
            {
                var filterElement = new XElement(this.GetType().Name);

                // Add child filters here, if any
                return filterElement;
            }
        }

        public class FeFlood : AtomicFilter
        {
            public FeFlood(string floodColor = "black", string floodOpacity = "1")
                : base("FeFlood", ("flood-color", floodColor), ("flood-opacity", floodOpacity))
            {
            }
        }

        public class FeOffset : AtomicFilter
        {
            public FeOffset(double dx = 0, double dy = 0)
                : base("FeOffset", ("dx", dx.ToString()), ("dy", dy.ToString()))
            {
            }
        }

        public class FeGaussianBlur : AtomicFilter
        {
            public FeGaussianBlur(double stdX = 0, double stdY = 0)
                : base("FeGaussianBlur", ("stdDeviation", stdX.ToString() + " " + stdY.ToString()))
            {
            }
        }

        public class FeBlend : AtomicFilter
        {
            private static readonly string[] AllowedModes = new[]
            {
                "normal", "multiply", "screen", "overlay", "darken", "lighten",
                "color-dodge", "color-burn", "hard-light", "soft-light", "difference",
                "exclusion", "hue", "saturation", "color", "luminosity"
            };

            public FeBlend(string in2 = "SourceGraphic", string mode = "normal")
                : base("FeBlend", ("in2", in2), ("mode", mode))
            {
                if (!AllowedModes.Contains(mode))
                {
                    throw new ArgumentException($"Invalid mode: {mode}. Must be one of: {string.Join(", ", AllowedModes)}");
                }
            }
        }

        public class FeComponentTransfer : AtomicFilter
        {
            public List<FeFunc> Functions { get; set; }

            public FeComponentTransfer() : base("FeComponentTransfer")
            {
                Functions = new List<FeFunc>();
            }

            public void AddFeFunc(FeFunc func)
            {
                Functions.Add(func);
            }

            public override XElement ToXElement()
            {
                var componentTransfer = base.ToXElement();

                foreach (var func in Functions)
                {
                    componentTransfer.Add(func.ToXElement());
                }

                return componentTransfer;
            }
        }

        public abstract class FeFunc
        {
            public string Type { get; set; }
            public string Operation { get; set; }
            public string Amplitude { get; set; }
            public string Exponent { get; set; }
            public string Offset { get; set; }
            public string Slope { get; set; }
            public string Intercept { get; set; }
            public string TableValues { get; set; }

            protected void ValidateOperation()
            {
                if (Operation != null && Operation != "identity" && Operation != "table" && Operation != "discrete" && Operation != "linear" && Operation != "gamma")
                {
                    throw new ArgumentException("Invalid operation type. Must be one of: identity, table, discrete, linear, gamma");
                }

                switch (Operation)
                {
                    case "table":
                        if (string.IsNullOrEmpty(TableValues))
                        {
                            TableValues = string.Empty;
                        }
                        break;
                    case "linear":
                        if (string.IsNullOrEmpty(Slope))
                        {
                            Slope = "1";
                        }
                        if (string.IsNullOrEmpty(Intercept))
                        {
                            Intercept = "0";
                        }
                        break;
                    case "gamma":
                        if (string.IsNullOrEmpty(Amplitude))
                        {
                            Amplitude = "1";
                        }
                        if (string.IsNullOrEmpty(Exponent))
                        {
                            Exponent = "1";
                        }
                        if (string.IsNullOrEmpty(Offset))
                        {
                            Offset = "0";
                        }
                        break;
                }
            }

            public abstract XElement ToXElement();
        }

        public class FeFuncImpl : FeFunc
        {
            public FeFuncImpl(string type)
            {
                Type = type;
                Operation = "identity"; // default operation
            }

            public override XElement ToXElement()
            {
                ValidateOperation();

                var funcElement = new XElement("feFunc" + Type);

                if (!string.IsNullOrEmpty(Operation))
                {
                    funcElement.Add(new XAttribute("type", Operation));
                }

                if (!string.IsNullOrEmpty(Amplitude))
                {
                    funcElement.Add(new XAttribute("amplitude", Amplitude));
                }

                if (!string.IsNullOrEmpty(Exponent))
                {
                    funcElement.Add(new XAttribute("exponent", Exponent));
                }

                if (!string.IsNullOrEmpty(Offset))
                {
                    funcElement.Add(new XAttribute("offset", Offset));
                }

                if (!string.IsNullOrEmpty(Slope))
                {
                    funcElement.Add(new XAttribute("slope", Slope));
                }

                if (!string.IsNullOrEmpty(Intercept))
                {
                    funcElement.Add(new XAttribute("intercept", Intercept));
                }

                if (!string.IsNullOrEmpty(TableValues))
                {
                    funcElement.Add(new XAttribute("tableValues", TableValues));
                }

                return funcElement;
            }
        }

        public static class FeFuncFactory
        {
            public static FeFunc CreateFeFunc(string type)
            {
                return new FeFuncImpl(type);
            }
        }

        public class FeColorMatrix : AtomicFilter
        {
            private static readonly string[] AllowedTypes = new[]
            {
                "matrix", "saturate", "hueRotate", "luminanceToAlpha"
            };

            public FeColorMatrix(string value, string type = "matrix")
                : base("FeColorMatrix", ("values", type == "matrix" ? ValidateValues(value) : value), ("type", type))
            {
                if (!AllowedTypes.Contains(type))
                {
                    throw new ArgumentException($"Invalid type: {type}. Must be one of: {string.Join(", ", AllowedTypes)}", nameof(type));
                }
            }

            private static string ValidateValues(string value)
            {
                var values = value.Split(' ');
                if (values.Length != 20)
                {
                    throw new ArgumentException("Invalid FeColorMatrix. Must be a 4x5 matrix with 20 elements.", nameof(value));
                }
                return value;
            }
        }

        public class FeComposite : AtomicFilter
        {
            private static readonly string[] AllowedOperators = { "over", "in", "out", "atop", "xor", "lighter", "arithmetic" };

            public FeComposite(string operatorValue, string input2)
                : base("FeComposite", ("in2", input2), ("operator", ValidateOperator(operatorValue)))
            {
            }

            private static string ValidateOperator(string operatorValue)
            {
                if (!AllowedOperators.Contains(operatorValue))
                {
                    throw new ArgumentException($"Invalid Porter-Duff operator. Must be one of: {string.Join(", ", AllowedOperators)}", nameof(operatorValue));
                }
                return operatorValue;
            }
        }

        public class FeTurbulence : AtomicFilter
        {
            public FeTurbulence(string type, double baseFrequency, int numOctaves, int? seed = null, bool? stitchTiles = null)
                : base("FeTurbulence",
                    ("type", type),
                    ("baseFrequency", baseFrequency.ToString()),
                    ("numOctaves", numOctaves.ToString()),
                    ("seed", seed?.ToString()),
                    ("stitchTiles", stitchTiles?.ToString()))
            {
                if (type != "turbulence" && type != "noise")
                {
                    throw new ArgumentException("Invalid type. Must be 'turbulence' or 'noise'.", nameof(type));
                }
            }
        }

        // This one is special
        public class FeDisplacementMap : AtomicFilter
        {
            public FeDisplacementMap(object inChannel, object in2Channel, double scale, string xChannelSelector = "R", string yChannelSelector = "G")
                : base("FeDisplacementMap",
                    ("in", inChannel.ToString()),
                    ("in2", in2Channel.ToString()),
                    ("scale", scale.ToString()),
                    ("xChannelSelector", xChannelSelector),
                    ("yChannelSelector", yChannelSelector))
            {
                if (xChannelSelector != "R" && xChannelSelector != "G" && xChannelSelector != "B" && xChannelSelector != "A")
                {
                    throw new ArgumentException("Invalid xChannelSelector. Must be 'R', 'G', 'B', or 'A'.", nameof(xChannelSelector));
                }
                if (yChannelSelector != "R" && yChannelSelector != "G" && yChannelSelector != "B" && yChannelSelector != "A")
                {
                    throw new ArgumentException("Invalid yChannelSelector. Must be 'R', 'G', 'B', or 'A'.", nameof(yChannelSelector));
                }
            }
        }

        public class FeDropShadow : AtomicFilter
        {
            public FeDropShadow(double dx, double dy, double stdDeviation, string floodColor, double floodOpacity)
                : base("DropShadow",
                    ("dx", dx.ToString()),
                    ("dy", dy.ToString()),
                    ("stdDeviation", stdDeviation.ToString()),
                    ("flood-color", floodColor),
                    ("flood-opacity", floodOpacity.ToString()))
            {
            }
        }

        public class FeMerge : AtomicFilter
        {
            public FeMerge()
                : base("feMerge", null)
            {
                Nodes = new List<FeMergeNode>();
            }

            public List<FeMergeNode> Nodes { get; }

            public void AddNode(FeMergeNode node)
            {
                Nodes.Add(node);
            }
        }

        public class FeMergeNode : AtomicFilter
        {
            public FeMergeNode(string input1 = "SourceGraphic")
                : base("feMergeNode", ("in", input1))
            {
            }
        }

        public class CompoundFilter
        {
            public required string Name { get; set; }
            public required List<AtomicFilter> Filters { get; set; }
            public int Width { get; set; } = 100;
            public int Height { get; set; } = 100;
            public string LastResult { get; set; } = "SourceGraphic";

            public XElement ToXElement()
            {
                var filterDef = new XElement("filter",
                    new XAttribute("id", Name),
                    new XAttribute("x", "-50%"),
                    new XAttribute("y", "-50%"),
                    new XAttribute("width", "200%"),
                    new XAttribute("height", "200%"));

                string lastResult = "SourceAlpha";

                foreach (var svgFilter in Filters)
                {
                    var filterElement = new XElement(svgFilter.FilterType);

                    if (svgFilter is FeComponentTransfer feComponentTransfer)
                    {
                        // Special handling for FeComponentTransfer
                        // You can access feComponentTransfer's properties and methods here
                        foreach (var func in feComponentTransfer.Functions)
                        {
                            filterElement.Add(func.ToXElement());
                        }
                    }
                    
                    // Your existing code here
                    if (svgFilter.Attributes.ContainsKey("in"))
                    {
                        filterElement.Add(new XAttribute("in", svgFilter.Attributes["in"]));
                    }
                    else
                    {
                        filterElement.Add(new XAttribute("in", lastResult));
                        lastResult = svgFilter.FilterType;
                    }

                    if (svgFilter.Attributes.ContainsKey("result"))
                    {
                        filterElement.Add(new XAttribute("result", svgFilter.Attributes["result"]));
                        lastResult = svgFilter.Attributes["result"];
                    }

                    foreach (var attribute in svgFilter.Attributes)
                    {
                        if (attribute.Key != "in" && attribute.Key != "result")
                        {
                            filterElement.Add(new XAttribute(attribute.Key, attribute.Value));
                        }
                    }

                    filterDef.Add(filterElement);
                }

                return filterDef;
            }

        }

        public (XElement defs, XElement filteredGroup) ApplyFilter(XElement group, CompoundFilter filter)
        {
            // Add the filter definition to the <defs> element
            var filterElement = filter.ToXElement(); // Assuming Filter has a ToXElement() method

            // Create a new filter attribute with the filter name
            var filterAttr = new XAttribute("filter", $"url(#{filter.Name})");

            // Add the filter attribute to the cloned group element
            group.Add(filterAttr);

            return (filterElement, group);
        }
    }
}