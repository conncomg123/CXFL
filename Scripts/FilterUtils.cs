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

                foreach (var (key, value) in attributes)
                {
                    Attributes[key] = value;
                }
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

        public class FeColorMatrix : AtomicFilter
        {
            public FeColorMatrix(string value, string type = "matrix")
                : base("FeColorMatrix", ("values", ValidateValues(value)), ("type", type))
            {
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

        public class FeDisplacementMap : AtomicFilter
        {
            public FeDisplacementMap(object inChannel, object in2Channel, double scale, string xChannelSelector = "R", string yChannelSelector = "G")
                : base("DisplacementMap",
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

        // FePointLight
        // FeDistantLight
        // FeDiffuseLighting

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

                    if (svgFilter.Attributes.ContainsKey("in"))
                    {
                        filterElement.Add(new XAttribute("in", svgFilter.Attributes["in"]));
                    }
                    else
                    {
                        filterElement.Add(new XAttribute("in", lastResult));
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