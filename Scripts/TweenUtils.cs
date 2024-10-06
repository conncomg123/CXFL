namespace Rendering;

using System.Numerics;
using CsXFL;
using KdTree.Math;

public static class TweenUtils
{
    public static Matrix4x4 CreateAffine(double a, double b, double c, double d, double tx, double ty)
    {
        return new Matrix4x4
        {
            M11 = (float)a,
            M12 = (float)b,
            M21 = (float)c,
            M22 = (float)d,
            M41 = (float)tx,
            M42 = (float)ty,
            M33 = 1,
            M44 = 1
        };
    }
    private static Matrix4x4 DeserializeMatrix(Matrix? matrix)
    {
        if (matrix is null)
        {
            return Matrix4x4.Identity;
        }

        return CreateAffine(matrix.A, matrix.B, matrix.C, matrix.D, matrix.Tx, matrix.Ty);
    }

    private static Matrix SerializeMatrix(Matrix4x4 matrix)
    {
        return new Matrix
        {
            A = matrix.M11,
            B = matrix.M12,
            C = matrix.M21,
            D = matrix.M22,
            Tx = matrix.M41,
            Ty = matrix.M42
        };
    }
    private static (double, double, double) AdjustAdobeMatrixParams(double rotation, double srot, double erot, double sshear, double eshear)
    {
        if (rotation > 0)
        {
            if (erot < srot)
            {
                erot += 2 * Math.PI;
            }
            erot += rotation * 2 * Math.PI;
        }
        else if (rotation < 0)
        {
            if (erot > srot)
            {
                erot -= 2 * Math.PI;
            }
            erot += rotation * 2 * Math.PI;
        }
        else if (Math.Abs(erot - srot) > Math.PI)
        {
            srot += Math.Sign(erot - srot) * 2 * Math.PI;
        }
        if (Math.Abs(eshear - sshear) > Math.PI)
        {
            sshear += Math.Sign(eshear - sshear) * 2 * Math.PI;
        }
        return (srot, erot, sshear);
    }
    private static (double, double, double, double) AdobeDecomposition(Matrix4x4 matrix)
    {
        double rotation = Math.Atan2(matrix[1, 0], matrix[0, 0]);
        double shear = Math.PI / 2 + rotation - Math.Atan2(matrix[1, 1], matrix[0, 1]);
        double scaleX = Math.Sqrt(matrix[0, 0] * matrix[0, 0] + matrix[1, 0] * matrix[1, 0]);
        double scaleY = Math.Sqrt(matrix[0, 1] * matrix[0, 1] + matrix[1, 1] * matrix[1, 1]);
        return (rotation, shear, scaleX, scaleY);
    }

    private static Matrix4x4 AdobeMatrix(double rotation, double shear, double scaleX, double scaleY)
    {
        var rotationMatrix = CreateAffine(Math.Cos(rotation), -Math.Sin(rotation), Math.Sin(rotation), Math.Cos(rotation), 0, 0);
        var skewMatrix = CreateAffine(1, Math.Tan(shear), 0, 1, 0, 0);
        var scaleMatrix = CreateAffine(scaleX, 0, 0, scaleY * Math.Cos(shear), 0, 0);
        return rotationMatrix * skewMatrix * scaleMatrix;
    }
    public static Matrix SimpleMatrixInterpolation(Matrix start, Matrix end, double t)
    {
        var startMatrix = DeserializeMatrix(start);
        var endMatrix = DeserializeMatrix(end);
        (var srot, var sshear, var sx, var sy) = AdobeDecomposition(startMatrix);
        (var erot, var eshear, var ex, var ey) = AdobeDecomposition(endMatrix);
        (srot, erot, sshear) = AdjustAdobeMatrixParams(0, srot, erot, sshear, eshear);
        var interpolatedLinear = AdobeMatrix(
            t * (erot) + (1 - t) * srot,
            t * eshear + (1 - t) * sshear,
            t * ex + (1 - t) * sx,
            t * ey + (1 - t) * sy
        );
        interpolatedLinear.M41 = (float)(startMatrix.M41 * (1 - t) + endMatrix.M41 * t);
        interpolatedLinear.M42 = (float)(startMatrix.M42 * (1 - t) + endMatrix.M42 * t);
        return SerializeMatrix(interpolatedLinear);
    }
    public static Matrix MatrixInterpolation(Matrix start, Matrix end, double rotation, Frame tweenFrame, int frameIndex, Point tp)
    {
        var startMatrix = DeserializeMatrix(start);
        var endMatrix = DeserializeMatrix(end);
        (var srot, var sshear, var sx, var sy) = AdobeDecomposition(startMatrix);
        (var erot, var eshear, var ex, var ey) = AdobeDecomposition(endMatrix);
        (srot, erot, sshear) = AdjustAdobeMatrixParams(rotation, srot, erot, sshear, eshear);
        var frot = tweenFrame.GetTweenMultiplier(frameIndex, "rotation");
        var fscale = tweenFrame.GetTweenMultiplier(frameIndex, "scale");
        var fpos = tweenFrame.GetTweenMultiplier(frameIndex, "position");
        var interpolatedLinear = AdobeMatrix(
            frot * erot + (1 - frot) * srot,
            frot * eshear + (1 - frot) * sshear,
            fscale * ex + (1 - fscale) * sx,
            fscale * ey + (1 - fscale) * sy
        );
        Matrix4x4 transformOrigin = Matrix4x4.Identity;
        transformOrigin.M41 = (float)-tp.X;
        transformOrigin.M42 = (float)-tp.Y;
        transformOrigin *= interpolatedLinear;
        transformOrigin.M41 += (float)tp.X;
        transformOrigin.M42 += (float)tp.Y;
        interpolatedLinear.M41 += (float)(startMatrix.M41 * (1 - fpos) + endMatrix.M41 * fpos + transformOrigin.M41);
        interpolatedLinear.M42 += (float)(startMatrix.M42 * (1 - fpos) + endMatrix.M42 * fpos + transformOrigin.M42);
        return SerializeMatrix(interpolatedLinear);
    }
    private static Color Scale(this Color color, double factor)
    {
        List<(double Red, double Green, double Blue, double Alpha)> multipliers = ColorEffectUtils.GetMultipliers(color);
        Color cpy = Color.DefaultColor();
        cpy.RedMultiplier = multipliers[0].Red * factor;
        cpy.GreenMultiplier = multipliers[0].Green * factor;
        cpy.BlueMultiplier = multipliers[0].Blue * factor;
        cpy.AlphaMultiplier = multipliers[0].Alpha * factor;
        cpy.RedOffset = (int)(multipliers[1].Red * factor);
        cpy.GreenOffset = (int)(multipliers[1].Green * factor);
        cpy.BlueOffset = (int)(multipliers[1].Blue * factor);
        cpy.AlphaOffset = (int)(multipliers[1].Alpha * factor);
        return cpy;
    }
    private static Color Add(this Color color, Color other)
    {
        List<(double Red, double Green, double Blue, double Alpha)> theseMultipliers = ColorEffectUtils.GetMultipliers(color);
        List<(double Red, double Green, double Blue, double Alpha)> otherMultipliers = ColorEffectUtils.GetMultipliers(other);
        Color cpy = Color.DefaultColor();
        cpy.RedMultiplier = theseMultipliers[0].Red + otherMultipliers[0].Red;
        cpy.GreenMultiplier = theseMultipliers[0].Green + otherMultipliers[0].Green;
        cpy.BlueMultiplier = theseMultipliers[0].Blue + otherMultipliers[0].Blue;
        cpy.AlphaMultiplier = theseMultipliers[0].Alpha + otherMultipliers[0].Alpha;
        cpy.RedOffset = (int)(theseMultipliers[1].Red + otherMultipliers[1].Red);
        cpy.GreenOffset = (int)(theseMultipliers[1].Green + otherMultipliers[1].Green);
        cpy.BlueOffset = (int)(theseMultipliers[1].Blue + otherMultipliers[1].Blue);
        cpy.AlphaOffset = (int)(theseMultipliers[1].Alpha + otherMultipliers[1].Alpha);
        return cpy;
    }
    public static Color ColorInterpolation(Color? start, Color? end, Frame tweenFrame, int frameIndex)
    {
        start ??= Color.DefaultColor();
        end ??= Color.DefaultColor();
        var frac = tweenFrame.GetTweenMultiplier(frameIndex, "color");
        return end.Scale(frac).Add(start.Scale(1 - frac));
    }
    public static Point InterpolatePoints((double X, double Y) start, (double X, double Y) end, int frameIndex, Frame tweenFrame)
    {
        double sx = start.X, sy = start.Y;
        double ex = end.X, ey = end.Y;
        var frac = tweenFrame.GetTweenMultiplier(frameIndex, "position");
        return new Point((ex - sx) * frac + sx, (ey - sy) * frac + sy, tweenFrame.Ns);
    }
    public static (int, int, int) SplitColors(string? color)
    {
        if (color == null) return (0, 0, 0);
        if (!color.StartsWith("#")) throw new ArgumentException($"Color {color} must start with #");
        if (color.Length != 7) throw new ArgumentException($"Color {color} must be 7 characters long");
        return (Convert.ToInt32(color.Substring(1, 2), 16), Convert.ToInt32(color.Substring(3, 2), 16), Convert.ToInt32(color.Substring(5, 2), 16));
    }
    public static double InterpolateValue(double x, double y, double frac)
    {
        return (1 - frac) * x + frac * y;
    }
    public static (string, double) InterpolateColor(string colx, double ax, string coly, double ay, double t)
    {
        (double rx, double gx, double bx) = SplitColors(colx);
        (double ry, double gy, double by) = SplitColors(coly);
        double ai = InterpolateValue(ax, ay, t);
        if (ai == 0) return (coly, 0);
        int ri = (int)(InterpolateValue(rx * ax, ry * ay, t) / ai);
        int gi = (int)(InterpolateValue(gx * ax, gy * ay, t) / ai);
        int bi = (int)(InterpolateValue(bx * ax, by * ay, t) / ai);
        return ($"#{ri:X2}{gi:X2}{bi:X2}", ai);
    }
    public static List<(GradientEntry, GradientEntry)> CalculateStopPaths(List<GradientEntry> init, List<GradientEntry> fin)
    {
        var availableStarts = new KdTree.KdTree<double, double?>(1, new DoubleMath());
        foreach (var entry in init)
        {
            availableStarts.Add([entry.Ratio], null);
        }
        var availableEnds = new KdTree.KdTree<double, double?>(1, new DoubleMath());
        foreach (var entry in fin)
        {
            availableEnds.Add([entry.Ratio], null);
        }
        Dictionary<double, GradientEntry> initMap = [];
        foreach (var entry in init)
        {
            initMap[entry.Ratio] = entry;
        }
        Dictionary<double, GradientEntry> finMap = [];
        foreach (var entry in fin)
        {
            finMap[entry.Ratio] = entry;
        }
        Dictionary<double, List<double>> forwardMap = [];
        Dictionary<double, int> coverCount = [];
        foreach (var stop in init)
        {
            var ratio = stop.Ratio;
            double match = availableEnds.GetNearestNeighbours([ratio], 1)[0].Point[0];
            forwardMap[ratio].Add(match);
            coverCount[match]++;
        }
        foreach (var stop in fin)
        {
            var ratio = stop.Ratio;
            if (coverCount[ratio] > 0) continue;
            double match = availableStarts.GetNearestNeighbours([ratio], 1)[0].Point[0];
            if (forwardMap.TryGetValue(match, out List<double>? matches))
            {
                double potentialRedundancy = forwardMap[match][0];
                if (coverCount[potentialRedundancy] > 1)
                {
                    forwardMap[match].Remove(potentialRedundancy);
                    coverCount[potentialRedundancy]--;
                }
            }
            forwardMap[match].Add(ratio);
        }
        // sort forwardMap by key
        forwardMap = forwardMap.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        List<(GradientEntry, GradientEntry)> retval = new();
        foreach (var start in forwardMap.Keys)
        {
            foreach (var end in forwardMap[start])
            {
                retval.Add((initMap[start], finMap[end]));
            }
        }
        return retval;
    }

    public static GradientEntry InterpolateStops(GradientEntry start, GradientEntry end, double t)
    {
        GradientEntry retval = new(start);
        double ratio = t * end.Ratio + (1 - t) * start.Ratio;
        var (color, alpha) = InterpolateColor(start.Color, start.Alpha, end.Color, end.Alpha, t);
        retval.Ratio = ratio;
        retval.Color = color;
        retval.Alpha = alpha;
        return retval;
    }

    public static RadialGradient InterpolateRadialGradients(RadialGradient x, RadialGradient y, double t)
    {
        Matrix newMatrix = SimpleMatrixInterpolation(x.Matrix, y.Matrix, t);
        double newFocalPoint = (1 - t) * x.FocalPointRatio + t * y.FocalPointRatio;
        List<GradientEntry> newStops = new();
        var interpedPaths = CalculateStopPaths(x.GradientEntries, y.GradientEntries);
        foreach (var interpedStop in interpedPaths)
        {
            newStops.Add(InterpolateStops(interpedStop.Item1, interpedStop.Item2, t));
        }
        RadialGradient cpy = new(x)
        {
            Matrix = newMatrix,
            FocalPointRatio = newFocalPoint,
            GradientEntries = newStops
        };
        return cpy;
    }
    public static LinearGradient InterpolateLinearGradients(LinearGradient x, LinearGradient y, double t)
    {
        Matrix newMatrix = SimpleMatrixInterpolation(x.Matrix, y.Matrix, t);
        List<GradientEntry> newStops = new();
        var interpedPaths = CalculateStopPaths(x.GradientEntries, y.GradientEntries);
        foreach (var interpedStop in interpedPaths)
        {
            newStops.Add(InterpolateStops(interpedStop.Item1, interpedStop.Item2, t));
        }
        LinearGradient cpy = new(x)
        {
            Matrix = newMatrix,
            GradientEntries = newStops
        };
        return cpy;
    }
    public static SolidColor InterpolateSolidColors(SolidColor x, SolidColor y, double t)
    {
        var (color, alpha) = InterpolateColor(x.Color, x.Alpha, y.Color, y.Alpha, t);
        SolidColor cpy = new(x)
        {
            Color = color,
            Alpha = alpha
        };
        return cpy;
    }
    public static Gradient InterpolateSolidWithGradient(SolidColor solid, Gradient gradient, double t)
    {
        List<GradientEntry> newStops = new();
        foreach (var entry in gradient.GradientEntries)
        {
            var (newColor, newAlpha) = InterpolateColor(solid.Color, solid.Alpha, entry.Color, entry.Alpha, t);
            GradientEntry gradCpy = new(entry)
            {
                Color = newColor,
                Alpha = newAlpha
            };
            newStops.Add(gradCpy);
        }
        Gradient cpy;
        if (gradient is LinearGradient lg) cpy = new LinearGradient(lg);
        else if (gradient is RadialGradient rg) cpy = new RadialGradient(rg);
        else throw new Exception("Unknown gradient type");
        cpy.GradientEntries = newStops;
        return cpy;
    }
    public static object? InterpolateFillStyles(object startFill, object endFill, double t)
    {
        if (endFill is null) return startFill;

        if (startFill is SolidColor sc)
        {
            if (endFill is SolidColor ec)
            {
                return InterpolateSolidColors(sc, ec, t);
            }
            else if (endFill is Gradient eg)
            {
                return InterpolateSolidWithGradient(sc, eg, t);
            }
            else throw new Exception($"Unknown fill style: {endFill}");
        }
        else if (startFill is LinearGradient lg)
        {
            if (endFill is LinearGradient eg)
            {
                return InterpolateLinearGradients(lg, eg, t);
            }
            else if (endFill is SolidColor ec)
            {
                return InterpolateSolidWithGradient(ec, lg, 1 - t);
            }
            else throw new Exception("Cannot interpolate between LinearGradient and RadialGradient");
        }
        else if (startFill is RadialGradient rg)
        {
            if (endFill is RadialGradient eg)
            {
                return InterpolateRadialGradients(rg, eg, t);
            }
            else if (endFill is SolidColor ec)
            {
                return InterpolateSolidWithGradient(ec, rg, 1 - t);
            }
            else throw new Exception("Cannot interpolate between RadialGradient and LinearGradient");
        }
        else
        {
            throw new Exception($"Unknown fill style: {startFill}");
        }
    }
    public static void ReplaceFill(StrokeStyle element, object start, object? repalcement)
    {
        object newFill;
        if (repalcement is SolidColor rsc)
        {
            newFill = rsc;
        }
        else if (repalcement is LinearGradient rlg)
        {
            newFill = rlg;
        }
        else if (repalcement is RadialGradient rrg)
        {
            newFill = rrg;
        }
        else
        {
            throw new Exception($"Unknown fill style: {repalcement}");
        }
        if (start is SolidColor sc)
        {
            element.Stroke.SolidColor = newFill as SolidColor;
        }
        else if (start is LinearGradient lg)
        {
            element.Stroke.LinearGradient = newFill as LinearGradient;
        }
        else if (start is RadialGradient rg)
        {
            element.Stroke.RadialGradient = newFill as RadialGradient;
        }
        else
        {
            throw new Exception($"Unknown fill style: {start}");
        }
    }
    public static void ReplaceFill(FillStyle element, object start, object? repalcement)
    {
        object newFill;
        if (repalcement is SolidColor rsc)
        {
            newFill = rsc;
        }
        else if (repalcement is LinearGradient rlg)
        {
            newFill = rlg;
        }
        else if (repalcement is RadialGradient rrg)
        {
            newFill = rrg;
        }
        else
        {
            throw new Exception($"Unknown fill style: {repalcement}");
        }
        if (start is SolidColor sc)
        {
            element.SolidColor = newFill as SolidColor;
        }
        else if (start is LinearGradient lg)
        {
            element.LinearGradient = newFill as LinearGradient;
        }
        else if (start is RadialGradient rg)
        {
            element.RadialGradient = newFill as RadialGradient;
        }
        else
        {
            throw new Exception($"Unknown fill style: {start}");
        }
    }
    public static (List<StrokeStyle>, List<FillStyle>) InterpolateColorMaps(Shape startShape, Shape endShape, int frameIndex, Frame tweenFrame)
    {
        double t = tweenFrame.GetTweenMultiplier(frameIndex, "color");
        List<StrokeStyle> newStrokes = new List<StrokeStyle>(startShape.Strokes);
        for (int i = 0; i < newStrokes.Count; i++)
        {
            newStrokes[i] = new StrokeStyle(newStrokes[i]);
        }
        List<FillStyle> newFills = new List<FillStyle>(startShape.Fills);
        for (int i = 0; i < newFills.Count; i++)
        {
            newFills[i] = new FillStyle(newFills[i]);
        }
        if (startShape.Strokes.Count != 0 && endShape.Strokes.Count != 0)
        {
            Dictionary<int, object> endFills = new Dictionary<int, object>();
            foreach (var stroke in endShape.Strokes)
            {
                object? fill = stroke.Stroke.SolidColor;
                fill ??= stroke.Stroke.LinearGradient;
                fill ??= stroke.Stroke.RadialGradient;
                if (fill is null) throw new Exception($"Unknown fill style: {fill}");
                endFills[stroke.Index] = fill;
            }
            foreach (var stroke in newStrokes)
            {
                object? startFill = stroke.Stroke.SolidColor;
                startFill ??= stroke.Stroke.LinearGradient;
                startFill ??= stroke.Stroke.RadialGradient;
                if (startFill is null) throw new Exception($"Unknown fill style: {startFill}");
                var interpolated = InterpolateFillStyles(startFill, endFills[stroke.Index], t);
                ReplaceFill(stroke, startFill, interpolated);
            }
        }
        if (startShape.Fills.Count != 0 && endShape.Fills.Count != 0)
        {
            Dictionary<int, object> endFills = new Dictionary<int, object>();
            foreach (var fillStyle in endShape.Fills)
            {
                object? fill = fillStyle.SolidColor;
                fill ??= fillStyle.LinearGradient;
                fill ??= fillStyle.RadialGradient;
                if (fill is null) throw new Exception($"Unknown fill style: {fill}");
                endFills[fillStyle.Index] = fill;
            }
            foreach (var fillStyle in newFills)
            {
                object? startFill = fillStyle.SolidColor;
                startFill ??= fillStyle.LinearGradient;
                startFill ??= fillStyle.RadialGradient;
                if (startFill is null) throw new Exception($"Unknown fill style: {startFill}");
                var interpolated = InterpolateFillStyles(startFill, endFills[fillStyle.Index], t);
                ReplaceFill(fillStyle, startFill, interpolated);
            }
        }
        return (newStrokes, newFills);
    }
    public static int? SegmentIndex(int? index)
    {
        if (index is null) return null;
        return index.Value + 1;
    }
    public static string XflPoint(Point point)
    {
        return $"{Math.Round(point.X, 6)} {Math.Round(point.Y, 6)}";
    }
    public static (double, double) GetStartPoint(Shape shape)
    {
        string edges = shape.Edges[0].Edges!;
        var points = EdgeUtils.ConvertEdgeFormatToPointLists(edges);
        return (double.Parse(points.First()[0].Split(' ')[0]), double.Parse(points.First()[0].Split(' ')[1]));
    }
    public class KDMap<T>
    {
        KdTree.KdTree<double, double?> points;
        Dictionary<(double, double), List<T>> items;
        public KDMap()
        {
            points = new KdTree.KdTree<double, double?>(2, new DoubleMath());
            items = new Dictionary<(double, double), List<T>>();
        }
        public void Add((double X, double Y) point, T item)
        {
            points.Add([point.X, point.Y], null);
            if (items.TryGetValue((point.X, point.Y), out var list))
            {
                list.Add(item);
            }
            else
            {
                items[(point.X, point.Y)] = [item];
            }
        }
        public List<T> Get((double X, double Y) point)
        {
            var pt = points.GetNearestNeighbours([point.X, point.Y], 1)[0];
            return items[(pt.Point[0], pt.Point[1])];
        }
    }
    public static KDMap<string> GetEdgesByStartpoint(Shape shape)
    {
        KDMap<string> result = new KDMap<string>();
        foreach (var edge in shape.Edges)
        {
            var edges = edge.Edges;
            if (edges is null) continue;
            var points = EdgeUtils.ConvertEdgeFormatToPointLists(edges);
            foreach (var pl in points)
            {
                foreach (var pt in pl)
                {
                    double x = 20.0 * double.Parse(pt.Split(' ')[0]);
                    double y = 20.0 * double.Parse(pt.Split(' ')[1]);
                    result.Add((x, y), pt);
                }
            }
        }
        return result;
    }
    public static double ParseNumber(string numberString)
    {
        // Check if the coordinate is signed and 32-bit fixed-point number in hex
        if (numberString[0] == '#')
        {
            // Split the coordinate into the integer and fractional parts
            string[] parts = numberString.Substring(1).Split('.');
            // Pad the integer part to 8 digits
            string hexNumberString = string.Format("{0:X6}", Convert.ToInt32(parts[0], 16));
            // Convert the hex coordinate to a signed 32-bit integer
            int numberInt = int.Parse(hexNumberString, System.Globalization.NumberStyles.HexNumber);
            // since only 6 hex digits are part of the integral part, if the first bit of the hex number is 1, the number is negative
            if ((numberInt & 0x800000) != 0)
            {
                numberInt -= 0x1000000;
            }
            double fraction = 0.0;
            for (int i = 0; i < parts[1].Length; i++)
            {
                fraction += Convert.ToInt32(parts[1][i].ToString(), 16) / Math.Pow(16, i + 1);
            }
            return numberInt + fraction;
        }
        else
        {
            return float.Parse(numberString);
        }
    }
    public static (double, double) ParseCoord(string? coord)
    {
        if (coord is null) return (0, 0);
        var parts = coord.Split(", ");
        return (ParseNumber(parts[0]) * 256f, ParseNumber(parts[1]) * 256f);
    }
    public static Shape ShapeInterpolation(Shape start, Shape end, Frame tweenFrame, int frameIndex)
    {
        MorphShape? morphShape = tweenFrame.MorphShape;
        if (morphShape is null) return start;
        if (morphShape.MorphSegments.Count == 0) return start;
        if (frameIndex == 0) return start;
        Shape result = new Shape(start);
        Edge dummyEdge = start.Edges[0];
        var (strokes, fills) = InterpolateColorMaps(start, end, frameIndex, tweenFrame);
        var edgesByStartpoint = GetEdgesByStartpoint(start);
        List<Edge> edges = new List<Edge>();
        foreach (MorphSegment morphSegment in morphShape.MorphSegments)
        {
            List<string> points = new List<string>();
            (double, double) startA;
            if (morphSegment.StartPointA != MorphSegment.DefaultValues.StartPointA) startA = ParseCoord(morphSegment.StartPointA);
            else startA = GetStartPoint(start);
            (double, double) startB;
            if (morphSegment.StartPointB != MorphSegment.DefaultValues.StartPointB) startB = ParseCoord(morphSegment.StartPointB);
            else startB = GetStartPoint(end);
            var prevPoint = InterpolatePoints(startA, startB, frameIndex, tweenFrame);
            points.Add($"!{XflPoint(prevPoint)}");
            foreach (var curve in morphSegment.MorphCurves)
            {
                var anchA = ParseCoord(curve.AnchorPointA);
                var anchB = ParseCoord(curve.AnchorPointB);
                if (curve.IsLine)
                {
                    var lineTo = InterpolatePoints(anchA, anchB, frameIndex, tweenFrame);
                    points.Add($"|{XflPoint(lineTo)}");
                }
                else
                {
                    var ctrlA = ParseCoord(curve.ControlPointA);
                    var ctrlB = ParseCoord(curve.ControlPointB);
                    var ctrl = InterpolatePoints(ctrlA, ctrlB, frameIndex, tweenFrame);
                    var quadTo = InterpolatePoints(anchA, anchB, frameIndex, tweenFrame);
                    points.Add($"[{XflPoint(ctrl)} {XflPoint(quadTo)}");
                }
            }
            var pointsStr = string.Join("", points);
            var edgeStr = edgesByStartpoint.Get(startA)[0];
            Edge edge = new Edge(dummyEdge); // too lazy to do this properly
            edge.Edges = pointsStr;
            edge.Cubics = "";
            edges.Add(edge);
        }
        result.Edges = edges;
        result.Fills = fills;
        result.Strokes = strokes;
        return result;
    }
}