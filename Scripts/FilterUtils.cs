using CsXFL;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

// Missing a lot of Atomics
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
                    new XAttribute("height", "200%"),
                    new XAttribute("color-interpolation-filters", "sRGB"));

                string lastResult = "SourceGraphic";

                foreach (var svgFilter in Filters)
                {
                    var filterElement = new XElement(SVGRenderer.svgNs + svgFilter.FilterType);

                    if (svgFilter is FeComponentTransfer feComponentTransfer)
                    {
                        foreach (var func in feComponentTransfer.Functions)
                        {
                            // svgNs Shenanigans
                            var funcElement = func.ToXElement();
                            var newFuncElement = new XElement(SVGRenderer.svgNs + funcElement.Name.LocalName, funcElement.Attributes(), funcElement.Elements());
                            filterElement.Add(newFuncElement);
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

        public class AnColorEffects : CompoundFilter
        {
            public AnColorEffects(double brightness, double contrast, double saturation, double hue)
                : base()
            {
                

                
            }
        }

        // Fix Contrast
        public class AnAdjustColor : CompoundFilter
        {
            public AnAdjustColor(double brightness, double contrast, double saturation, double hue)
                : base()
                {
                    // Brightness starts at 1.0 / 3
                    brightness = 1.1666;

                    // -1 should yield rgb 64^3
                    contrast = 0;

                    // Saturation starts at 0 to 1 * 2
                    saturation = 2;

                    // Hue
                    hue = 130;

                    double s = saturation;
                    double cosHue = Math.Cos(hue * Math.PI / 180.0);
                    double sinHue = Math.Sin(hue * Math.PI / 180.0);

                    // Linear gamma coefficients
                    double r_w = 0.3086;
                    double g_w = 0.6094;
                    double b_w = 0.0820;

                    double[,] matrix_brightness = new double[,] {
                        {brightness, 0, 0, 0},
                        {0, brightness, 0, 0},
                        {0, 0, brightness, 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_contrast = new double[,] {
                        {1, 0, 0, 0},
                        {0, 1, 0, 0},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_translation = new double[,] {
                        {1, 0, 0, 0.5},
                        {0, 1, 0, 0.5},
                        {0, 0, 1, 0.5},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_negative_translation = new double[,] {
                        {-1, 0, 0, -0.5},
                        {0, -1, 0, -0.5},
                        {0, 0, -1, -0.5},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix1 = new double[3, 3] {
                        { 0.2127, 0.7152, 0.0722 },
                        { 0.2127, 0.7152, 0.0722 },
                        { 0.2127, 0.7152, 0.0722 }
                    };

                    double[,] matrix2 = new double[3, 3] {
                        { 0.7873, -0.7152, -0.0722 },
                        { -0.2127, 0.2848, -0.0722 },
                        { -0.2127, -0.7152, 0.9278 }
                    };

                    double[,] matrix3 = new double[3, 3] {
                        { -0.2127, -0.7152, 0.9278 },
                        { 0.143, 0.140, -0.283 },
                        { -0.7873, 0.7152, 0.0722 }
                    };

                    double[,] scaledMatrix2 = MultiplyMatrixByScalar(matrix2, cosHue);
                    double[,] scaledMatrix3 = MultiplyMatrixByScalar(matrix3, sinHue);

                    double[,] hueMatrix = AddMatrices(matrix1, scaledMatrix2, scaledMatrix3);

                    var matrix_hue = new double[,] {
                        {hueMatrix[0,0], hueMatrix[0,1], hueMatrix[0,2], 0},
                        {hueMatrix[1,0], hueMatrix[1,1], hueMatrix[1,2], 0}, 
                        {hueMatrix[2,0], hueMatrix[2,1], hueMatrix[2,2], 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_saturation = new double[,] {
                        {0.2127 + 0.7873 * saturation, 0.7152 - 0.7152 * saturation, 0.0722 - 0.0722 * saturation, 0},
                        {0.2127 - 0.2127 * saturation, 0.7152 + 0.2848 * saturation, 0.0722 - 0.0722 * saturation, 0},
                        {0.2127 - 0.2127 * saturation, 0.7152 - 0.7152 * saturation, 0.0722 + 0.9278 * saturation, 0},
                        {0, 0, 0, 1},
                    };

                    double[,] matrix_rx = new double[,] {
                        {1, 0, 0, 0},
                        {0, 1/Math.Sqrt(2), -(1/Math.Sqrt(2)), 0},
                        {0, 1/Math.Sqrt(2), 1/Math.Sqrt(2), 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_ry = new double[,] {
                        {Math.Sqrt(2)/Math.Sqrt(3), 0, -(1/Math.Sqrt(3)), 0},
                        {0, 1, 0, 0},
                        {1/Math.Sqrt(3), 0, Math.Sqrt(2)/Math.Sqrt(3), 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_omega_values = { {r_w, g_w, b_w, 0} };
                    double[,] matrix_tmp_prime_values = Multiply(Multiply(matrix_omega_values, matrix_rx), matrix_ry);
                
                    double r_p_w = matrix_tmp_prime_values[0,0];
                    double g_p_w = matrix_tmp_prime_values[0,1];
                    double b_p_w = matrix_tmp_prime_values[0,2];

                    double[,] matrix_hue_shear = new double[,] {
                        {1, 0, -(r_p_w/b_p_w), 0},
                        {0, 1, -(g_p_w/b_p_w), 0},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    };

                    // Theta expected to go from 0 to 2pi
                    double[,] matrix_hue_rotation = new double[,] {
                        {cosHue, -sinHue, 0, 0},
                        {sinHue, cosHue, 0, 0},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    };
                    
                    double[,] matrix_inv_hue_shear = new double[,] {
                        {1, 0, (r_p_w/b_p_w), 0},
                        {0, 1, (g_p_w/b_p_w), 0},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    };
                    
                    double[,] matrix_inv_ry = new double[,] {
                        {Math.Sqrt(2)/Math.Sqrt(3), 0, 1/Math.Sqrt(3), 0},
                        {0, 1, 0, 0},
                        {-(1/Math.Sqrt(3)), 0, Math.Sqrt(2)/Math.Sqrt(3), 0},
                        {0, 0, 0, 1}
                    };

                    double[,] matrix_inv_rx = new double[,] {
                        {1, 0, 0, 0},
                        {0, 1/Math.Sqrt(2), 1/Math.Sqrt(2), 0},
                        {0, -(1/Math.Sqrt(2)), 1/Math.Sqrt(2), 0},
                        {0, 0, 0, 1}
                    };

                    //var matrix_hue = Multiply(matrix_rx, Multiply(matrix_ry, Multiply(matrix_hue_shear, Multiply(matrix_hue_rotation, Multiply(matrix_inv_hue_shear, Multiply(matrix_inv_rx, matrix_inv_ry))))));

                    double MultiplyIfNonZero(double a, double b) => a != 0 && b != 0 ? a * b : 0;

                    var matrix_master = new double[,] {
                        {matrix_hue[0,0] * matrix_saturation[0,0], matrix_hue[0,1] * matrix_saturation[0,1], matrix_hue[0,2] * matrix_saturation[0,2], matrix_hue[0,3] * matrix_saturation[0,3], 0},
                        {matrix_hue[1,0] * matrix_saturation[1,0], matrix_hue[1,1] * matrix_saturation[1,1], matrix_hue[1,2] * matrix_saturation[1,2], matrix_hue[1,3] * matrix_saturation[1,3], 0}, 
                        {matrix_hue[2,0] * matrix_saturation[2,0], matrix_hue[2,1] * matrix_saturation[2,1], matrix_hue[2,2] * matrix_saturation[2,2], matrix_hue[2,3] * matrix_saturation[2,3], 0},
                        {matrix_hue[3,0] * matrix_saturation[3,0], matrix_hue[3,1] * matrix_saturation[3,1], matrix_hue[3,2] * matrix_saturation[3,2], matrix_hue[3,3] * matrix_saturation[3,3], 0}
                    };

                    // Treating luminance matrix as contrast
                    // M = B * S * C * H
                    //var matrix_master = Multiply(matrix_brightness, Multiply(matrix_saturation, Multiply(matrix_contrast, matrix_hue)));
                    matrix_master = matrix_hue;

                    var matrix_master_fixed = new double[,] {
                        {matrix_master[0,0], matrix_master[0,1], matrix_master[0,2], matrix_master[0,3], 0},
                        {matrix_master[1,0], matrix_master[1,1], matrix_master[1,2], matrix_master[1,3], 0}, 
                        {matrix_master[2,0], matrix_master[2,1], matrix_master[2,2], matrix_master[2,3], 0},
                        {matrix_master[3,0], matrix_master[3,1], matrix_master[3,2], matrix_master[3,3], 0}
                    };

                    var master_matrix_string = string.Join(" ", matrix_master_fixed.Cast<double>());

                    Console.WriteLine(matrix_master_fixed);
                    Console.WriteLine(master_matrix_string);

                    Filters = new List<AtomicFilter>();

                    var colorFilter = new FeColorMatrix(master_matrix_string, "matrix");
                    Filters.Add(colorFilter);
                }

            public static double[,] AddMatrices(double[,] matrix1, double[,] matrix2, double[,] matrix3)
            {
                int rows = matrix1.GetLength(0);
                int cols = matrix1.GetLength(1);
                double[,] result = new double[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        result[i, j] = matrix1[i, j] + matrix2[i, j] + matrix3[i, j];
                    }
                }

                return result;
            }

            public static double[,] PadMatrix(double[,] matrix)
            {
                double[,] paddedMatrix = new double[5, 5];

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        paddedMatrix[i, j] = matrix[i, j];
                    }
                }

                paddedMatrix[3, 3] = 1;
                paddedMatrix[4, 4] = 1;

                return paddedMatrix;
            }

            public static double[,] MultiplyMatrixByScalar(double[,] matrix, double scalar)
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
            static double[,] Multiply(double[,] matrix1, double[,] matrix2)
                {
                    // Check if the matrices can be multiplied
                    if (matrix1.GetLength(1) != matrix2.GetLength(0))
                    {
                        throw new ArgumentException("Matrices cannot be multiplied");
                    }

                    // Get the dimensions of the matrices
                    int rows = matrix1.GetLength(0);
                    int cols = matrix2.GetLength(1);
                    int innerDim = matrix1.GetLength(1);

                    // Perform matrix multiplication
                    double[,] result = new double[rows, cols];

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            for (int k = 0; k < innerDim; k++)
                            {
                                result[i, j] += matrix1[i, k] * matrix2[k, j];
                            }
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