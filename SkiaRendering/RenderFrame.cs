using CsXFL;
using SkiaSharp;
using System;
using System.Text.RegularExpressions;

// Logic largely taken from [https://github.com/PluieElectrique/xfl2svg]
namespace SkiaRendering
{
    class Renderer
    {
        public static float ParseNumber(string num)
        {
            // Check if the number is signed and 32-bit fixed-point number in hex
            if (num[0] == '#')
            {
                // Split the number into the integer and fractional parts
                string[] parts = num.Substring(1).Split('.');
                // Pad the integer part to 8 digits
                string hex_num = string.Format("{0:X8}{1:X2}", int.Parse(parts[0]), int.Parse(parts[1]));
                // Convert the hex number to a signed 32-bit integer
                int num_int = int.Parse(hex_num, System.Globalization.NumberStyles.HexNumber);
                // Convert the number to its decimal equivalent and scale it down by 256 and 20
                return (num_int / 256f) / 20f;
            }
            else
            {
                // The number is a decimal number. Scale it down by 20.
                return float.Parse(num) / 20f;
            }
        }

        static string pattern = @"
            [!|/\[\]]                |   # Move to, line to, quad to
            (?<!S)-?\d+(?:\.\d+)?   |   # Decimal number
            \#[A-Z0-9]+\.[A-Z0-9]+      # Hex number
        ";

        static Regex edgeTokenizer = new Regex(pattern, RegexOptions.IgnorePatternWhitespace);

        public static IEnumerable<List<string>> EdgeFormatToPointLists(string edges)
        {
            var tokens = edgeTokenizer.Matches(edges).Cast<Match>().Select(m => m.Value).GetEnumerator();

            // Assert that the first token is a moveto command
            if (!tokens.MoveNext() || tokens.Current != "!")
            {
                throw new ArgumentException("Edge format must start with moveto (!) command");
            }

            Func<string> nextPoint = () =>
            {
                tokens.MoveNext();
                string x = ParseNumber(tokens.Current).ToString();
                tokens.MoveNext();
                string y = ParseNumber(tokens.Current).ToString();
                return $"{x} {y}";
            };

            string prevPoint = nextPoint();
            List<string> pointList = new List<string> { prevPoint };

            while (tokens.MoveNext())
            {
                string command = tokens.Current;
                string currPoint = nextPoint();

                if (command == "!")
                {
                    // Move to
                    if (currPoint != prevPoint)
                    {
                        // If a move command doesn't change the current point, we
                        // ignore it. Otherwise, a new segment is starting, so we
                        // must yield the current point list and begin a new one.
                        yield return pointList;
                        pointList = new List<string> { currPoint };
                        prevPoint = currPoint;
                    }
                }
                else if (command == "|" || command == "/")
                {
                    // Line to
                    pointList.Add(currPoint);
                    prevPoint = currPoint;
                }
                else if (command == "[" || command == "]")
                {
                    // Quad to. Control point is currPoint, dest is prevPoint.
                    pointList.Add($"[{currPoint}");
                    prevPoint = nextPoint();
                    pointList.Add($"{prevPoint}]");
                }
            }

            yield return pointList;
        }

        public static string PointListToPathFormat(List<string> pointList)
        {
            var path = new List<string> { "M", pointList[0].ToString() }; // Start the path with an "M" command and the first point

            for (int i = 1; i < pointList.Count; i++)
            {
                var point = pointList[i];
                string command;

                if (point.Contains('['))
                {
                    command = "Q";
                    path.Add(command);

                    string[] pointParts = pointList[i + 1].Replace("]", "").Split(' ');
                    path.Add(point.Replace("[", "")); // ctrl points
                    path.Add($"{pointParts[0]} {pointParts[1]}"); // dest
                    i++;
                }
                else
                {
                    command = "L"; // If the point is non-quadratic, assume it represents a destination point
                    path.Add(command);
                    path.Add(point.ToString()); // Add the destination point to the path
                }
            }

            if (pointList[0].ToString() == pointList[pointList.Count - 1].ToString())
            {
                path.Add("Z"); // If the first and last points are the same, add a "Z" command to close the path
            }

            return string.Join(" ", path); // Join the path elements into a single string with spaces in between
        }

        static void Main(string[] args)
        {
            int currentFrame = 0;
            Document Doc = new("C:\\Users\\Administrator\\Desktop\\RenderingTest.fla");
            SKBitmap Bitmap = new SKBitmap(Doc.Width, Doc.Height);
            SKCanvas Canvas = new SKCanvas(Bitmap);

            // TODO: Should be Doc background color, or transparent if specified
            Canvas.Clear(SKColors.White);

            // TODO: Should iterate over all layers
            foreach (Layer layerIter in Doc.Timelines[0].Layers)
            {
                if (layerIter.GetFrame(currentFrame).IsEmpty()) { continue; } // Skip blank frames

                var ElementArray = layerIter.GetFrame(currentFrame).Elements;

                for (int elementIter = 0; elementIter < ElementArray.Count; elementIter++)
                {
                    // Symbol Logic
                    if (ElementArray[elementIter] is SymbolInstance) { continue; }

                    // Shape Logic
                    if (ElementArray[elementIter] is Shape)
                    {
                        Shape shapeIter = ElementArray[elementIter] as Shape;

                        foreach (Edge edgeIter in shapeIter.Edges)
                        {
                            if (edgeIter.Edges == null) { continue; }

                            var pointLists = EdgeFormatToPointLists(edgeIter.Edges);

                            // <!> Do things
                            foreach (var pointList in pointLists)
                            {
                                // SVG Path
                                string TestPath = PointListToPathFormat(pointList);
                                SKPath TestPath2 = SKPath.ParseSvgPathData(TestPath);

                                // Strokes
                                Stroke CurrentStroke = shapeIter.Strokes[0].Stroke;
                                FillStyle CurrentFill = shapeIter.Fills[0];

                                SKStrokeCap StrokeCap;

                                if (CurrentStroke.Caps == "round")
                                {
                                    StrokeCap = SKStrokeCap.Round;
                                } else if (CurrentStroke.Caps == "butt")
                                {
                                    StrokeCap = SKStrokeCap.Square;
                                } else
                                {
                                    StrokeCap = SKStrokeCap.Round;
                                }

                                SKStrokeJoin StrokeJoin;

                                // <!> This logic is assumed, test it later
                                if (CurrentStroke.Joints == "round")
                                {
                                    StrokeJoin = SKStrokeJoin.Round;
                                } else if (CurrentStroke.Joints == "butt")
                                {
                                    StrokeJoin = SKStrokeJoin.Bevel;
                                } else
                                {
                                    StrokeJoin = SKStrokeJoin.Miter;
                                }

                                var StrokePaint = new SKPaint
                                {
                                    Style = SKPaintStyle.Stroke,
                                    StrokeWidth = (float)CurrentStroke.Weight,
                                    StrokeMiter = CurrentStroke.MiterLimit,
                                    StrokeCap = StrokeCap,
                                    StrokeJoin = StrokeJoin,
                                    Color = SKColor.Parse(CurrentStroke.SolidColor.Color.ToString()),
                                    IsAntialias = true
                                };

                                var FillPaint = new SKPaint
                                {
                                    Style = SKPaintStyle.Fill,
                                    Color = SKColor.Parse(CurrentFill.SolidColor.Color.ToString())
                                };

                                Canvas.DrawPath(TestPath2, FillPaint);
                                Canvas.DrawPath(TestPath2, StrokePaint);
                            }
                        }
                    }
                }

                var Image = SKImage.FromBitmap(Bitmap);
                var Data = Image.Encode(SKEncodedImageFormat.Png, 100);
                using (var Stream = System.IO.File.OpenWrite("output.png"))
                {
                    Data.SaveTo(Stream);
                }

            }
        }
    }
}