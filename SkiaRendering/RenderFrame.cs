using CsXFL;
using SkiaSharp;
using System;
using System.Text.RegularExpressions;

// Logic largely taken from [https://github.com/PluieElectrique/xfl2svg]
namespace SkiaRendering
{
    class Renderer
    {
        public class SymbolInstanceVisualRecord
        {
        public double MatrixA { get; set; }
        public double MatrixB { get; set; }
        public double MatrixC { get; set; }
        public double MatrixD { get; set; }
        public double MatrixTx { get; set; }
        public double MatrixTy { get; set; }

            public void AddTransformation(double a, double b, double c, double d, double tx, double ty)
            {
                // Add the transformation values to the current values in the record
                MatrixA += a;
                MatrixB += b;
                MatrixC += c;
                MatrixD += d;
                MatrixTx += tx;
                MatrixTy += ty;
            }
        }
        public static float ParseNumber(string num)
        {
            // Check if the number is signed and 32-bit fixed-point number in hex
            if (num[0] == '#')
            {
                // Split the number into the integer and fractional parts
                string[] parts = num.Substring(1).Split('.');
                // Pad the integer part to 8 digits
                string hex_num = string.Format("{0:X8}{1:X2}", Convert.ToInt32(parts[0], 16), Convert.ToInt32(parts[1], 16));
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
                        // If a move command doesn't change the current point,ignore it. Otherwise, a new segment is starting, so we must yield the current point list and begin a new one.
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

        static void DrawShape(SKCanvas canvas, List<string> pointList, Shape shapeIter, SymbolInstanceVisualRecord visRecord)
        {
            SymbolInstanceVisualRecord localVisRecord = visRecord ?? new SymbolInstanceVisualRecord() { };

            SKMatrix matrix = new SKMatrix()
            {
                Persp0 = 0,
                Persp1 = 0,
                Persp2 = 1,
                ScaleX = (float)localVisRecord.MatrixA,
                SkewY = (float)localVisRecord.MatrixB,
                SkewX = (float)localVisRecord.MatrixC,
                ScaleY = (float)localVisRecord.MatrixD,
                TransX = (float)localVisRecord.MatrixTx,
                TransY = (float)localVisRecord.MatrixTy
            };

            bool hasStrokes = false;
            bool hasFills = false;

            // SVG Path
            string TestPath = PointListToPathFormat(pointList);
            SKPath TestPath2 = SKPath.ParseSvgPathData(TestPath);

            // Apply positional offset
            TestPath2.Transform(matrix);

            // Strokes & Fills
            if (shapeIter.Strokes.Count > 0) { hasStrokes = true; }
            if (shapeIter.Fills.Count > 0) { hasFills = true; }

            if (hasStrokes) { 
            
                Stroke CurrentStroke = shapeIter.Strokes[0].Stroke; 

                SKStrokeCap StrokeCap = GetStrokeCap(CurrentStroke.Caps);

                // <!> This logic is assumed, test it later
                SKStrokeJoin StrokeJoin = GetStrokeJoin(CurrentStroke.Joints);

                var StrokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)CurrentStroke.Weight,
                    StrokeMiter = CurrentStroke.MiterLimit,
                    StrokeCap = StrokeCap,
                    StrokeJoin = StrokeJoin,
                    Color = SKColor.Parse(CurrentStroke.SolidColor.Color.ToString()),
                    IsAntialias = true
                    // <!> This doesn't do anything?
                    //FilterQuality = SKFilterQuality.High
                };

                canvas.DrawPath(TestPath2, StrokePaint);

            }

            if (hasFills)
            {

                FillStyle CurrentFill = shapeIter.Fills[0];

                var FillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColor.Parse(CurrentFill.SolidColor.Color.ToString())
                };

                canvas.DrawPath(TestPath2, FillPaint);

            }
        }

        static SKStrokeCap GetStrokeCap(string caps)
        {
            if (caps == "round")
            {
                return SKStrokeCap.Round;
            }
            else if (caps == "butt")
            {
                return SKStrokeCap.Square;
            }
            else
            {
                return SKStrokeCap.Round;
            }
        }

        static SKStrokeJoin GetStrokeJoin(string joints)
        {
            if (joints == "round")
            {
                return SKStrokeJoin.Round;
            }
            else if (joints == "butt")
            {
                return SKStrokeJoin.Bevel;
            }
            else
            {
                return SKStrokeJoin.Miter;
            }
        }

        static void TimelineCascadeRender(Document OperatingDoc, Timeline RenderingTimeline, int startFrame, int endFrame, int invokationLevel, SKCanvas? Canvas = null, SymbolInstanceVisualRecord? visRecord = null)
        {
            for (int currentFrame = startFrame; currentFrame <= endFrame; currentFrame++)
            {
                SKBitmap Bitmap = new SKBitmap(OperatingDoc.Width, OperatingDoc.Height);
                SKCanvas localCanvas = Canvas ?? new SKCanvas(Bitmap);
                SymbolInstanceVisualRecord localVisRecord = visRecord ?? new SymbolInstanceVisualRecord() { };

                // <!> TODO: Needs transparent option. How do we signal this?
                if (invokationLevel == 0)
                {
                    string bgColorStr = OperatingDoc.BackgroundColor;
                    int bgColor = int.Parse(bgColorStr.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier);
                    uint red = (uint)(bgColor >> 16) & 0xFF;
                    uint green = (uint)(bgColor >> 8) & 0xFF;
                    uint blue = (uint)bgColor & 0xFF;
                    SKColor bgSKColor = new SKColor((byte)red, (byte)green, (byte)blue);
                    localCanvas.Clear(bgSKColor);
                }

                for (int i = RenderingTimeline.Layers.Count - 1; i >= 0; i--)
                {
                    Layer layerIter = RenderingTimeline.Layers[i];

                    // Skip these layer types
                    if (layerIter.LayerType == "guide" || layerIter.LayerType == "guided" || layerIter.LayerType == "folder") { continue; }

                    // If current frame is empty, skip
                    if (layerIter.GetFrame(currentFrame).IsEmpty()) { continue; }

                    var ElementArray = layerIter.GetFrame(currentFrame).Elements;

                    for (int elementIter = 0; elementIter < ElementArray.Count; elementIter++)
                    {
                        // Drawing Shapes
                        if (ElementArray[elementIter] is Shape)
                        {
                            Shape shapeIter = ElementArray[elementIter] as Shape;

                            foreach (Edge edgeIter in shapeIter.Edges)
                            {
                                if (edgeIter.Edges == null) { continue; }

                                var pointLists = EdgeFormatToPointLists(edgeIter.Edges);

                                foreach (var pointList in pointLists)
                                {
                                    // Why do we do this?
                                    // We do this because of the very ugly way I have handled visRecords, which track transformation
                                    // changes and accumulate them throughout recursive executions. visRecord is initialized with all
                                    // zeros, which includes a ScaleX and ScaleY of zero, which causes your shape to collapse. However,
                                    // this needs to be initialized with zero, or else symbols with scaling will double up. Doubly
                                    // however, shapes with no corresponding symbol get the default visRecord, which will cause them
                                    // to collapse. This is a temporary fix, and if you want to elegantly fix it, look at visRecord.

                                    if (localVisRecord.MatrixA == 0 && localVisRecord.MatrixD == 0)
                                    {
                                        localVisRecord.MatrixA = 1;
                                        localVisRecord.MatrixD = 1;
                                    }

                                    DrawShape(localCanvas, pointList, shapeIter, localVisRecord);
                                }
                            }
                        }

                        // Drawing Symbols
                        if (ElementArray[elementIter] is SymbolInstance)
                        {
                            SymbolInstance symbolIter = ElementArray[elementIter] as SymbolInstance;
                            SymbolItem SymbolLibraryItem = symbolIter.CorrespondingItem as SymbolItem;
                            Timeline ReferenceTimeline = SymbolLibraryItem.Timeline;

                            // Track visual record
                            localVisRecord.AddTransformation(symbolIter.Matrix.A, symbolIter.Matrix.B, symbolIter.Matrix.C, symbolIter.Matrix.D, symbolIter.Matrix.Tx, symbolIter.Matrix.Ty);

                            // TODO: Last frame logic is incorrect
                            TimelineCascadeRender(OperatingDoc, ReferenceTimeline, symbolIter.FirstFrame, symbolIter.FirstFrame, invokationLevel + 1, localCanvas, localVisRecord);
                            // I shall call it... De-initializing... [Dr. Evil sting]
                            localVisRecord = new SymbolInstanceVisualRecord() { };
                        }
                    }
                }

                // Write image
                if (invokationLevel == 0) {
                    var Image = SKImage.FromBitmap(Bitmap);
                    var Data = Image.Encode(SKEncodedImageFormat.Png, 100);
                    using (var Stream = System.IO.File.OpenWrite("output_" + currentFrame.ToString("D6") + ".png"))
                    {
                        Data.SaveTo(Stream);
                    }
                }
            }
        }

        /*static void Main(string[] args)
        {
            Document Doc = new("C:\\Users\\Administrator\\Desktop\\INDEV_NULL_VECTOR_TRUCY.fla");
            // Safety check for out of bounds frames
            TimelineCascadeRender(Doc, Doc.Timelines[0], 0, 0, 0);
        }*/
    }
}