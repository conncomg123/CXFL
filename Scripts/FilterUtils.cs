using CsXFL;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

// Missing a lot of Atomics
// FeMerge & MergeNode is unintuitive and bad
namespace Rendering
{
    // https://developer.mozilla.org/en-US/docs/Web/SVG/Element/filter

    public static class FilterUtils
    {
        public class AtomicFilter
        {
            public string FilterType { get; set; }
            public Dictionary<string, string> Attributes { get; set; }

            public AtomicFilter(string filterType, params (string key, string value)[] attributes)
            {
                FilterType = filterType;
                Attributes = new Dictionary<string, string>();

                if (attributes != null)
                {
                    foreach (var (key, value) in attributes)
                    {
                        Attributes[key] = value;
                    }
                }

                Attributes.Add("result", filterType);
            }

            public virtual XElement ToXElement()
            {
                var filterElement = new XElement(SVGRenderer.svgNs + this.FilterType);

                foreach (var attribute in Attributes)
                {
                    filterElement.Add(new XAttribute(attribute.Key, attribute.Value));
                }

                // Add child filters here, if any
                return filterElement;
            }
        }

        public class FeFlood : AtomicFilter
        {
            public FeFlood(string floodColor = "black", string floodOpacity = "1")
                : base("feFlood", ("flood-color", floodColor))
            {
            }
        }

        public class FeOffset : AtomicFilter
        {
            public FeOffset(double dx = 0, double dy = 0)
                : base("feOffset", ("dx", dx.ToString()), ("dy", dy.ToString()))
            {
            }
        }

        public class FeGaussianBlur : AtomicFilter
        {
            public FeGaussianBlur(double stdX = 0, double stdY = 0)
                : base("feGaussianBlur", ("stdDeviation", stdX.ToString() + " " + stdY.ToString()))
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
                : base("feBlend", ("in2", in2), ("mode", mode))
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

            public FeComponentTransfer() : base("feComponentTransfer")
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
                : base("feColorMatrix", ("values", type == "matrix" ? ValidateValues(value) : value), ("type", type))
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
                : base("feComposite", ("in2", input2), ("operator", ValidateOperator(operatorValue)))
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
                : base("feTurbulence",
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
                : base("feDisplacementMap",
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
                : base("feDropShadow",
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
            public string Name { get; set; }
            public List<AtomicFilter> Filters { get; set; }
            public int Width { get; set; } = 100;
            public int Height { get; set; } = 100;
            public string LastResult { get; set; } = "SourceGraphic";

            public XElement ToXElement()
            {
                var filterDef = new XElement(SVGRenderer.svgNs + "filter",
                    new XAttribute("id", Name),
                    new XAttribute("x", "-50%"),
                    new XAttribute("y", "-50%"),
                    new XAttribute("width", "200%"),
                    new XAttribute("height", "200%"));

                string lastResult = "SourceGraphic";

                foreach (var svgFilter in Filters)
                {
                    var filterElement = new XElement(SVGRenderer.svgNs + svgFilter.FilterType);

                    if (svgFilter is FeComponentTransfer feComponentTransfer)
                    {
                        foreach (var func in feComponentTransfer.Functions)
                        {
                            filterElement.Add(func.ToXElement());
                        }
                    }

                    if (svgFilter is FeMerge feMerge)
                    {
                        foreach (var node in feMerge.Nodes)
                        {
                            filterElement.Add(node.ToXElement());
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

        // Q: Hey what gives this doesn't look anything like Animate's AdjustColor
        // A: Go fuck yourself you limp dick monkey

        // Animate's AdjustColor is weird as shit and changing brightness/contrast will change the hue. Saturation works as expected
        // on vector content but there is a discrepancy on BITMAP content. Why? No fucking reason, probably unscrutinizable malicious matrix code.

        // This implementation is quite different but actually does what you ask it to do.
        public class AnAdjustColor : CompoundFilter
        {
            public AnAdjustColor(double brightness, double contrast, double saturation, double hue)
                : base()
                {
                    brightness/=100;
                    contrast/=100;
                    saturation/=100;
                    saturation++;

                    double cosHue = Math.Cos(hue * Math.PI / 180.0);
                    double sinHue = Math.Sin(hue * Math.PI / 180.0);

                    double[,] brightness_matrix = new double[5, 5] {
                        {1, 0, 0, 0, brightness},
                        {0, 1, 0, 0, brightness},
                        {0, 0, 1, 0, brightness},
                        {0, 0, 0, 1, 0},
                        {0, 0, 0, 0, 1}
                    };

                    double[,] contrast_matrix = new double[5, 5] {
                        {contrast, 0, 0, 0, 0},
                        {0, contrast, 0, 0, 0},
                        {0, 0, contrast, 0, 0},
                        {0, 0, 0, 1, 0},
                        {0, 0, 0, 0, 1}
                    };

                    double[,] saturation_matrix = new double[5, 5] {
                        {0.2127 + 0.7873 * saturation, 0.7152 - 0.7152 * saturation, 0.0722 - 0.0722 * saturation, 0, 0},
                        {0.2127 - 0.2127 * saturation, 0.7152 + 0.2848 * saturation, 0.0722 - 0.0722 * saturation, 0, 0},
                        {0.2127 - 0.2127 * saturation, 0.7152 - 0.7152 * saturation, 0.0722 + 0.9278 * saturation, 0, 0},
                        {0, 0, 0, 1, 0},
                        {0, 0, 0, 0, 1}
                    };

                    double[,] hueMatrix = new double[,] {
                        { +0.2127, +0.7152, +0.0722 },
                        { +0.2127, +0.7152, +0.0722 },
                        { +0.2127, +0.7152, +0.0722 }
                    };

                    double[,] cosMatrix = new double[,] {
                        { +0.7873, -0.7152, -0.0722 },
                        { -0.2127, +0.2848, -0.0722 },
                        { -0.2127, -0.7152, +0.9278 }
                    };

                    double[,] sinMatrix = new double[,] {
                        { -0.2127, -0.7152, +0.9278 },
                        { +0.143, +0.140, -0.283 },
                        { -0.7873, +0.7152, +0.0722 }
                    };

                    hueMatrix = Add(hueMatrix, Multiply(cosMatrix, cosHue));
                    hueMatrix = Add(hueMatrix, Multiply(sinMatrix, sinHue));

                    hueMatrix = new double[,] {
                        {hueMatrix[0, 0], hueMatrix[0, 1], hueMatrix[0, 2], 0, 0},
                        {hueMatrix[1, 0], hueMatrix[1, 1], hueMatrix[1, 2], 0, 0},
                        {hueMatrix[2, 0], hueMatrix[2, 1], hueMatrix[2, 2], 0, 0},
                        {0, 0, 0, 1, 0},
                        {0, 0, 0, 0, 1}
                    };

                    double[][] finalMatrix = ToJaggedArray(CalculateDotProduct(CalculateDotProduct(CalculateDotProduct(brightness_matrix, contrast_matrix), saturation_matrix), hueMatrix));
                    finalMatrix = finalMatrix.Where((x, i) => i != finalMatrix.Length - 1).ToArray();

                    var colorFilter = new FeColorMatrix(string.Join(" ", finalMatrix.SelectMany(x => x).Select(x => x.ToString())), "matrix");
                    
                    Filters = new List<AtomicFilter>();
                    Filters.Add(colorFilter);
                }

            public static double[,] CalculateDotProduct(double[,] matrixA, double[,] matrixB)
            {
                // Check if the matrices can be multiplied
                if (matrixA.GetLength(1) != matrixB.GetLength(0))
                {
                    throw new ArgumentException("Matrices cannot be multiplied");
                }

                // Create a new matrix to store the result
                double[,] result = new double[matrixA.GetLength(0), matrixB.GetLength(1)];

                // Calculate the dot product
                for (int i = 0; i < matrixA.GetLength(0); i++)
                {
                    for (int j = 0; j < matrixB.GetLength(1); j++)
                    {
                        for (int k = 0; k < matrixA.GetLength(1); k++)
                        {
                            result[i, j] += matrixA[i, k] * matrixB[k, j];
                        }
                    }
                }

                return result;
            }

            private static double[][] ToJaggedArray(double[,] array)
            {
                int rows = array.GetLength(0);
                int cols = array.GetLength(1);
                double[][] jaggedArray = new double[rows][];

                for (int i = 0; i < rows; i++)
                {
                    jaggedArray[i] = new double[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        jaggedArray[i][j] = array[i, j];
                    }
                }

                return jaggedArray;
            }

            public static double[][] MultiplyElementWise(params double[][][] matrices)
            {
                if (matrices.Length == 0)
                {
                    throw new ArgumentException("At least one matrix is required.");
                }

                int rows = matrices[0].Length;
                int cols = matrices[0][0].Length;

                for (int i = 1; i < matrices.Length; i++)
                {
                    if (matrices[i].Length != rows || matrices[i][0].Length != cols)
                    {
                        throw new ArgumentException("All matrices must have the same dimensions.");
                    }
                }

                double[][] result = new double[rows][];

                for (int i = 0; i < rows; i++)
                {
                    result[i] = new double[cols];

                    for (int j = 0; j < cols; j++)
                    {
                        double currentValue = matrices[0][i][j];

                        for (int k = 1; k < matrices.Length; k++)
                        {
                            if (currentValue == 0 || matrices[k][i][j] == 0)
                            {
                                break;
                            }

                            currentValue *= matrices[k][i][j];
                        }

                        result[i][j] = currentValue;
                    }
                }

                return result;
            }

            public static double[,] Add(double[,] matrix1, double[,] matrix2)
                {
                    int rows = matrix1.GetLength(0);
                    int cols = matrix1.GetLength(1);
                    double[,] result = new double[rows, cols];

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            result[i, j] = matrix1[i, j] + matrix2[i, j];
                        }
                    }

                    return result;
                }

            public static double[,] Multiply(double[,] matrix, double scalar)
                {
                    int rows = matrix.GetLength(0);
                    int cols = matrix.GetLength(1);
                    double[,] result = new double[rows, cols];

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            result[i, j] = matrix[i, j] * scalar;
                        }
                    }

                    return result;
                }
        }

        public class AnDropShadow : CompoundFilter
        {
            public AnDropShadow(double blurX, double blurY, double distance, double rotation, double opacity, string color, bool knockout = false, bool innerShadow = false, bool hideObject = false)
                : base()
            {
                this.BlurX = blurX/2;
                this.BlurY = blurY/2;
                this.Distance = distance * 2;
                this.Rotation = rotation;
                this.Opacity = opacity;
                this.Color = color;
                this.Knockout = knockout;
                this.InnerShadow = innerShadow;
                this.HideObject = hideObject;

                double dx = Math.Round(distance * Math.Cos(rotation * Math.PI / 180), 4);
                double dy = Math.Round(distance * Math.Sin(rotation * Math.PI / 180), 4);

                Filters = new List<AtomicFilter>();

                FeGaussianBlur gaussianBlur = null;
                FeComponentTransfer componentTransfer = null;
                FeComposite compositeFilter1, compositeFilter2, compositeFilter3, compositeFilter4, compositeFilter5, compositeFilter = null;

                var offsetFilter = new FeOffset(dx, dy);

                if (!innerShadow && !knockout) {
                    Filters.Add(offsetFilter);
                } else {
                    offsetFilter.Attributes["in"] = "SourceAlpha";
                    Filters.Add(offsetFilter);
                }

                gaussianBlur = new FeGaussianBlur(BlurX, BlurY);
                Filters.Add(gaussianBlur);

                if (InnerShadow) {
                    compositeFilter = new FeComposite("out", gaussianBlur.Attributes["result"]);
                    compositeFilter.Attributes["in"] = "SourceGraphic";
                    Filters.Add(compositeFilter);
                }

                var floodFilter = new FeFlood(this.HexToRgba(color.ToString(), opacity));
                Filters.Add(floodFilter);

                var mergeFilter = new FeMerge();

                if (!Knockout && !InnerShadow) {
                    compositeFilter1 = new FeComposite("in", gaussianBlur.Attributes["result"]);
                    compositeFilter1.Attributes["in"] = floodFilter.Attributes["result"];
                    compositeFilter1.Attributes["result"] = "compositeFilter1";
                    Filters.Add(compositeFilter1);
                    mergeFilter.AddNode(new FeMergeNode(compositeFilter1.Attributes["result"]));
                }

                if (InnerShadow) {
                    compositeFilter2 = new FeComposite("in", compositeFilter.Attributes["result"]);
                    compositeFilter2.Attributes["result"] = "compositeFilter2";
                    Filters.Add(compositeFilter2);
                    componentTransfer = new FeComponentTransfer 
                    { 
                        Functions = 
                        { 
                            new FeFuncImpl("A") 
                            { 
                                Operation = "linear"
                            }
                        }
                    };
                    Filters.Add(componentTransfer);
                }

                if (!InnerShadow && Knockout) {
                    compositeFilter3 = new FeComposite("out", "SourceAlpha");
                    compositeFilter3.Attributes["in"] = gaussianBlur.Attributes["result"];
                    compositeFilter3.Attributes["result"] = "compositeFilter3";
                    Filters.Add(compositeFilter3);
                    compositeFilter4 = new FeComposite("in", compositeFilter3.Attributes["result"]);
                    compositeFilter4.Attributes["in"] = floodFilter.Attributes["result"];
                    compositeFilter4.Attributes["result"] = "compositeFilter4";
                    Filters.Add(compositeFilter4);
                    mergeFilter.AddNode(new FeMergeNode(compositeFilter4.Attributes["result"]));
                }

                if (!HideObject && !Knockout) { mergeFilter.AddNode(new FeMergeNode("SourceGraphic")); };

                if (InnerShadow && !Knockout && !HideObject)     {
                    mergeFilter.AddNode(new FeMergeNode("SourceGraphic"));
                    mergeFilter.AddNode(new FeMergeNode(componentTransfer.Attributes["result"]));
                } else if (InnerShadow) {
                    mergeFilter.AddNode(new FeMergeNode(componentTransfer.Attributes["result"]));

                }

                Filters.Add(mergeFilter);

            }

            private string HexToRgba(string hexColor, double opacity)
            {
                hexColor = hexColor.TrimStart('#');
                var rgba = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    rgba[i] = Convert.ToInt32(hexColor.Substring(i * 2, 2), 16);
                }
                return $"rgba({rgba[0]}, {rgba[1]}, {rgba[2]}, {opacity / 1.0})";
            }

            public double BlurX { get; set; }
            public double BlurY { get; set; }
            public double Distance { get; set; }
            public double Rotation { get; set; }
            public double Opacity { get; set; }
            public string Color { get; set; }
            public bool Knockout { get; set; }
            public bool InnerShadow { get; set; }
            public bool HideObject { get; set; }
        }

        public static (XElement defs, XElement filteredGroup) ApplyFilter(XElement group, CompoundFilter filter)
        {
            // Clone the group element
            XElement groupClone = new XElement(group);

            // Add the filter definition to the <defs> element
            var filterElement = filter.ToXElement(); // Assuming Filter has a ToXElement() method

            // Create a new filter attribute with the filter name
            var filterAttr = new XAttribute("filter", $"url(#{filter.Name})");

            // Add the filter attribute to the cloned group element
            groupClone.Add(filterAttr);

            return (filterElement, groupClone);
        }
    }
}