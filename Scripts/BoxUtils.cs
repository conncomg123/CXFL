using CsXFL;
using System.Xml.Linq;

namespace Rendering
{
    internal class SvgPathSegment
    {
        public double Distance { get; set; } = 0;
        public string SegmentString { get; set; } = "";
        public string CommandType { get; set; } = "";
        public List<(double, double)> ControlPoints { get; set; } = new List<(double, double)> ();

        public void AddControlPoint((double, double) controlPoint)
        {
            ControlPoints.Add(controlPoint);
        }

    }

    // The way that CSXFL Rectangles are stored is left = x of left side, top = y of top side, right = x of right side
    // bottom = y of bottom side
    // In xfl2svg code, box[0] = minx, box[1] = miny, box[2] = maxx, box[3] = maxy
    // min x = left,  max y = top, max x = right, min y = bottom of Rectangle

    /// <summary>
    /// Utils for handling bounding boxes when converting XFL elements to SVG.
    /// </summary>
    internal class BoxUtils
    {
        // Note that a higher number of Gauss-Legendre numbers can be used for better accuracy
        private static double[] legendreGaussWeights =
        [
            0.1279381953467522, 0.1279381953467522, 0.1258374563468283, 0.1258374563468283, 0.1216704729278034,
            0.1216704729278034, 0.1155056680537256, 0.1155056680537256, 0.1074442701159656, 0.1074442701159656,
            0.0976186521041139, 0.0976186521041139, 0.0861901615319533, 0.0861901615319533, 0.0733464814110803,
            0.0733464814110803, 0.0592985849154368, 0.0592985849154368, 0.0442774388174198, 0.0442774388174198,
            0.0285313886289337, 0.0285313886289337, 0.0123412297999872, 0.0123412297999872
        ];

        private static double[] legendreGaussAbscissa =
        [
            -0.0640568928626056, 0.0640568928626056, -0.1911188674736163 ,0.1911188674736163, -0.3150426796961634,
            0.3150426796961634, -0.4337935076260451, 0.4337935076260451, -0.5454214713888396, 0.5454214713888396,
            -0.6480936519369755, 0.6480936519369755, -0.7401241915785544, 0.7401241915785544, -0.8200019859739029,
            0.8200019859739029, -0.8864155270044011, 0.8864155270044011, -0.9382745520027328, 0.9382745520027328,
            -0.9747285559713095, 0.9747285559713095, -0.9951872199970213, 0.9951872199970213
        ];

        /// <summary>
        /// Merges two bounding boxes together.
        /// </summary>
        /// <param name="original">The first bounding box being merged.</param>
        /// <param name="addition">The second bounding box being merged.</param>
        /// <returns>A Rectangle representing the new combined bounding box.</returns>
        public static Rectangle? MergeBoundingBoxes(Rectangle? original, Rectangle? addition)
        {
            if(addition == null)
            {
                return original;
            }
            else if(original == null)
            {
                return addition;
            }

            // The way that rectangles are stored is left = x of left side, top = y of top side, right = x of right side
            // bottom = y of bottom side
            // min x = left, max y = top, max x = right, min y = bottom
            double minX = Math.Min(original.Left, addition.Left);
            double maxY = Math.Max(original.Top, addition.Top);
            double maxX = Math.Max(original.Right, addition.Right);
            double minY = Math.Min(original.Bottom, addition.Bottom);

            return new Rectangle(minX, maxY, maxX, minY);
        }

        /// <summary>
        /// Expands a bounding box on all four sides by width.
        /// </summary>
        /// <param name="rectangle">The Rectangle that is being expanded.</param>
        /// <param name="width">The amount that this Rectangle will be expanded by on all four sides.</param>
        /// <returns>An new expanded Rectangle.</returns>
        public static Rectangle ExpandBoundingBox(Rectangle rectangle, double width)
        {
            // min x = left, max y = top, max x = right, min y = bottom of rectangle

            // Create new object to separate Rectangle instances
            Rectangle newRectangle = new Rectangle(rectangle.Left - width / 2,
                rectangle.Top + width / 2, rectangle.Right + width / 2, rectangle.Bottom - width / 2);
            return newRectangle;
        }

        /// <summary>
        /// Gets the bounding box of a line segment.
        /// </summary>
        /// <param name="point1">First point of line segment.</param>
        /// <param name="point2">Second point of line segment.</param>
        /// <returns></returns>
        public static Rectangle GetLineBoundingBox((double, double) point1, (double, double) point2)
        {
            // The way that rectangles are stored is left = x of left side, top = y of top side, right = x of right side
            // bottom = y of bottom side
            // min x = left, max y = top, max x = right, min y = bottom

            Rectangle boundingBox = new Rectangle(Math.Min(point1.Item1, point2.Item1), Math.Max(point1.Item2, point2.Item2),
                Math.Max(point1.Item1, point2.Item1), Math.Min(point1.Item2, point2.Item2));

            return boundingBox;
        }

        /// <summary>
        /// Gets a point on a quadratic Bezier curve.
        /// </summary>
        /// <param name="point0">Start point of Bezier curve.</param>
        /// <param name="point1">Control point of Beizer curve.</param>
        /// <param name="point2">End point of Bezier curve.</param>
        /// <param name="t">How far from the start point the point being calculated is [0, 1]- with
        /// 0 being the start point and 1 being the end point.</param>
        /// <returns>A point on the Bezier curve that is t from the start point.</returns>
        public static (double, double) GetPointOnQuadraticBezier((double, double) point0,
            (double, double) point1, (double, double) point2, double t)
        {
            // Using first version of formula (no simplification using linear interpolation)
            double x = (1 - t) * ((1 - t) * point0.Item1 + t * point1.Item1) + t * ((1 - t) * point1.Item1 + t * point2.Item1);
            double y = (1 - t) * ((1 - t) * point0.Item2 + t * point1.Item2) + t * ((1 - t) * point1.Item2 + t * point2.Item2);
            return (x, y);
        }

        /// <summary>
        /// Gets the critical points of the Bezier Curve.
        /// </summary>
        /// <param name="point0">Start point of Bezier curve.</param>
        /// <param name="point1">Control point of Beizer curve.</param>
        /// <param name="point2">End point of Bezier curve.</param>
        /// <returns>The critical points of the Bezier Curve for both the x and y axis (the t values
        /// of which these extreme points are found, if any).</returns>
        public static (double, double) GetQuadraticCriticalPoints((double, double) point0,
            (double, double) point1, (double, double) point2)
        {
            //Get the critical point by taking the derivative of the Bezier Curve and solving for 0
            double xDenom = point0.Item1 - (2 * point1.Item1) + point2.Item1;
            double xCritical;
            double yCritical;

            // If denominator is 0, that means that there is no critical point
            // As such, just set t = -1, this will be ignored as the t value for Bezier Curve's can only
            // be [0 - 1]

            if (xDenom == 0)
            {
                xCritical = -1;
            }
            else
            {
                xCritical = (point0.Item1 - point1.Item1) / xDenom;
            }

            double yDenom = point0.Item2 - (2 * point1.Item2) + point2.Item2;
            if (yDenom == 0)
            {
                yCritical = -1;
            }
            else
            {
                yCritical = (point0.Item2 - point1.Item2) / yDenom;
            }

            return (xCritical, yCritical);
        }

        /// <summary>
        /// Calculates the approximate arc length of part of a quadratic Bezier curve.
        /// </summary>
        /// <remarks>
        /// The mathematics and logic behind this method were based on the section "Arc Length"
        /// from Pomax's A Primer on Bezier Curves.
        /// </remarks>
        /// <param name="point0">Start point of Bezier curve.</param>
        /// <param name="point1">Control point of Beizer curve.</param>
        /// <param name="point2">End point of Bezier curve.</param>
        /// <param name="z">The section of the curve whose length is being calculated- from [0, 1] with
        /// 0 being the start point and 1 being the end point.</param>
        /// <returns>An approximation of the arc length for a section of the quadratic Bezier curve.</returns>
        /// <seealso href="https://pomax.github.io/bezierinfo/#arclength"/>
        public static double CalculateQuadBezierLength((double, double) point0,
            (double, double) point1, (double, double) point2, double z)
        {
            double arcLength = 0;

            // (z/2) part of the arc length equation
            double zConstant = z / 2;

            // Note that a higher number of Gauss-Legendre numbers can be used for better accuracy
            for(int i = 0; i < legendreGaussWeights.Length; i++)
            {
                //Get Ci and ti for each rectangle strip being used to approximate arc length
                double stripThicknessCi = legendreGaussWeights[i];
                double stripLocationTi = legendreGaussAbscissa[i];

                // f(t) is from the parametric curve length equation
                // sqrt((dx/dt)^2 + (dy/dt)^2), where dx/dt is derivative of Bezier Curve equation

                double tValue = (zConstant * stripLocationTi) + zConstant;

                // Alternate form of Bezier Curve derivative
                // double xBezierCurveDerivative = 2 * (point1.Item1 - point0.Item1) + 2 * tValue
                //    *(point2.Item1 - (2 * point1.Item1) + point0.Item1);
                //double yBezierCurveDerivative = 2 * (point1.Item2 - point0.Item2) + 2 * tValue
                //    * (point2.Item2 - (2 * point1.Item2) + point0.Item2);

                double xBezierCurveDerivative = 2 * (1 - tValue) * (point1.Item1 - point0.Item1) +
                    2 * tValue * (point2.Item1 - point1.Item1);
                double yBezierCurveDerivative = 2 * (1 - tValue) * (point1.Item2 - point0.Item2) +
                    2 * tValue * (point2.Item2 - point1.Item2);

                double functionValue = Math.Sqrt(Math.Pow(xBezierCurveDerivative, 2)
                    + Math.Pow(yBezierCurveDerivative, 2));

                // Ci * f(z/2 * ti + z/2)
                arcLength += stripThicknessCi * functionValue;
            }
            // z/2 * summation
            arcLength = zConstant * arcLength;

            return arcLength;
        }

        public static double CalculateLineLength((double, double) point0, (double, double) point1)
        {
            double xPart = Math.Pow(point1.Item1 - point0.Item1, 2);
            double yPart = Math.Pow(point1.Item2 - point0.Item2, 2);
            return Math.Sqrt(xPart + yPart);
        }

        public static double GetNormalSlopeOfLine((double, double) point0, (double, double) point1)
        {
            // Get slope at point by using slope formula - this is the slope of the tangent line
            double slope = (point1.Item2 - point1.Item2) / (point1.Item1 - point0.Item1);

            // Negative reciprocal = normal slope
            return (-1 / slope);
        }

        public static List<SvgPathSegment> SplitSvgPathIntoSegments(string svgPathString)
        {
            List<SvgPathSegment> segments = new List<SvgPathSegment>();

            IEnumerator<string> pathStringIterator = svgPathString.Split(" ").ToList().GetEnumerator();
            string prevCommand = "M";

            Func<(double, double)> nextPoint = () =>
            {
                pathStringIterator.MoveNext();
                double x = double.Parse(pathStringIterator.Current);
                pathStringIterator.MoveNext();
                double y = double.Parse(pathStringIterator.Current);
                return (x, y);
            };

            // Process moveTo command at start of SVG path (required by format)
            // by skipping command and getting starting point of SVG path
            pathStringIterator.MoveNext();
            (double, double) prevPoint = nextPoint();
            (double, double) currPoint = prevPoint;

            SvgPathSegment moveSegment = new SvgPathSegment();
            moveSegment.CommandType = prevCommand;
            moveSegment.SegmentString = $"M {currPoint.Item1} {currPoint.Item2}";

            moveSegment.AddControlPoint(currPoint);
            segments.Add(moveSegment);

            // As commands are processed separately from coordinates, to ensure that each segment has the
            // proper part of the larger SVG path string, manually reset string depending on section of it
            // processed
            string svgSegmentString = "";
            while (pathStringIterator.MoveNext())
            {
                // The next token is either a new command or a x coordinate of the first point of a command
                string nextToken = pathStringIterator.Current;

                if (nextToken == "L" || nextToken == "Q")
                {
                    prevCommand = nextToken;
                    svgSegmentString += $"{prevCommand} ";
                }
                else
                {
                    // If next token is x coord, get the associated y coord to get entire next point
                    double x = double.Parse(pathStringIterator.Current);
                    pathStringIterator.MoveNext();
                    double y = double.Parse(pathStringIterator.Current);
                    currPoint = (x, y);

                    if (prevCommand == "L")
                    {
                        //Set values of segment
                        SvgPathSegment newSeg = new SvgPathSegment();
                        newSeg.CommandType = prevCommand;
                        newSeg.SegmentString = svgSegmentString + $"{currPoint.Item1} {currPoint.Item2}";
                        newSeg.AddControlPoint(prevPoint);
                        newSeg.AddControlPoint(currPoint);

                        double lineDistance = CalculateLineLength(prevPoint, currPoint);
                        newSeg.Distance = lineDistance;
                        segments.Add(newSeg);

                        prevPoint = currPoint;
                        svgSegmentString = "";
                    }
                    else if (prevCommand == "Q")
                    {
                        // The point that was before this one (either directly given via a command or
                        // calculated) is the start of this curve
                        // The control point is the coordinates immediately after the command
                        // The end point is the set after that
                        (double, double) point0 = prevPoint;
                        (double, double) point1 = currPoint;
                        (double, double) point2 = nextPoint();

                        // Set values of segment
                        SvgPathSegment newSeg = new SvgPathSegment();
                        newSeg.CommandType = prevCommand;
                        newSeg.SegmentString = svgSegmentString + $"{point1.Item1} {point1.Item2}"
                            + $" {point2.Item1} {point2.Item2}";
                        newSeg.AddControlPoint(point0);
                        newSeg.AddControlPoint(point1);
                        newSeg.AddControlPoint(point2);

                        double curveDistance = CalculateQuadBezierLength(point0, point1, point2, 1);
                        newSeg.Distance = curveDistance;
                        segments.Add(newSeg);

                        prevPoint = point2;
                        svgSegmentString = "";
                    }
                }
            }

            return segments;
        }

        public static List<List<(double, double)>> SplitQuadBezierCurve((double, double) point0,
            (double, double) point1, (double, double) point2, double t)
        {
            // Utilize the De Casteljau Algorithm of drawing curves to split curve
            // In interpolating various lines when obtaining a point on the curve,
            // we can use the points created to actually the split the curve as well

            // Calculate intermediate points in between the main points of curve using
            // linear interpolation formula

            // First Level linear interpolation
            double xfirstSkeletonPoint = (1 - t) * point0.Item1 + t * point1.Item1;
            double yfirstSkeletonPoint = (1 - t) * point0.Item2 + t * point1.Item2;
            double xsecondSkeletonPoint = (1 - t) * point1.Item1 + t * point2.Item1;
            double ysecondSkeletonPoint = (1 - t) * point1.Item2 + t * point2.Item2;

            // Second Level linear interpolation
            double xthirdSkeletonPoint = (1 - t) * xfirstSkeletonPoint + t * xsecondSkeletonPoint;
            double ythirdSkeletonPoint = (1 - t) * yfirstSkeletonPoint + t * ysecondSkeletonPoint;

            // First subcurve is defined by point0, first skeleton point, and third skeleton point
            // Second subcurve is defined by third skeleton point, second skeleton point, and point2
            List<List<(double, double)>> subCurves = new()
            {
                new List<(double, double)>()
                {
                    point0,
                    (xfirstSkeletonPoint, yfirstSkeletonPoint),
                    (xthirdSkeletonPoint, ythirdSkeletonPoint)
                },
                new List<(double, double)>()
                {
                    (xthirdSkeletonPoint, ythirdSkeletonPoint),
                    (xsecondSkeletonPoint, ysecondSkeletonPoint),
                    point2
                }
            };

            return subCurves;
        }

        public static void OffsetQuadBezierCurve((double, double) point0,
            (double, double) point1, (double, double) point2)
        {
            // Note that we will be splitting the subcurves going from from t = 0 to t = 1
            // A subcurve from t = 0 to t = 0.6 would be "before" or earlier along in the original
            // curve than a subcurve from t = 0.6 to t = 1

            // Stores all subcurves of the original curve going from t = 0 to t = 1
            List<List<(double, double)>> subCurveList = new List<List<(double, double)>>()
            {
                new List<(double, double)>()
                {
                    point0, point1, point2
                }
            };

            // Stores ratio range that a subcurve covers relative to the original curve
            List<(double, double)> startEndValues = new List<(double, double)>() { (0, 1) };

            (double, double) criticalValues = GetQuadraticCriticalPoints(point0, point1, point2);

            if(criticalValues.Item1 > 0 && criticalValues.Item1 < 1)
            {
                List<(double, double)> curve = subCurveList[0];
                subCurveList.RemoveAt(0);
                startEndValues.RemoveAt(0);

                List<List<(double, double)>> splitCurve =
                    SplitQuadBezierCurve(curve[0], curve[1], curve[2], criticalValues.Item1);

                subCurveList.Add(splitCurve[0]); //left subcurve
                subCurveList.Add(splitCurve[1]); //right subcurve
                startEndValues.Add((0, criticalValues.Item1));
                startEndValues.Add((criticalValues.Item1, 1));
            }

            if(criticalValues.Item2 > 0 && criticalValues.Item2 < 1)
            {
                // First check to see if curve was already split using x extreme's t value
                // If so, see which subcurve y extreme's t value falls under, adjust it for said subcurve
                // and then split it
                if(subCurveList.Count > 0)
                {
                    // We have to get the t value from the original curve relative to the subcurve
                    
                    int subCurveToSplit = 0;
                    if(criticalValues.Item1 > criticalValues.Item2)
                    {
                        subCurveToSplit = 0;
                    }
                }
            }

            //IsQuadBezierCurveSafe(point0, point1, point2);
        }

        public static bool IsQuadBezierCurveSafe((double, double) point0,
            (double, double) point1, (double, double) point2)
        {
            // Check if subcurve is safe
            // 1. Baseline check- are control points (start, control(s), end points) of the subcurve all on same side 
            // of line from its start and end points?

            // To do this, we can perform multiple leftness tests on control points against the subcurve's
            // baseline vector
            // Lef(A, B, C) - stand at A, look toward B, is C left or right relative to that line?
            // As a Quad Bezier Curve only has one control point, we don't need to check for this
            // For cubic or more, we do

            // 2. Midpoint check- is the midpoint of the subcurve (where t = 0.5) close to the geometric 
            // center of the curve's control points?
            // Note that the start and the end points are control points as well

            (double, double) midpoint = GetPointOnQuadraticBezier(point0, point1, point2, 0.5);

            // Calculate geometric center
            double xGeometricCenter = (point0.Item1 + point1.Item1 + point2.Item1) / 3;
            double yGeometricCenter = (point0.Item2 + point1.Item2 + point2.Item2) / 3;
            (double, double) difference = (midpoint.Item1 - xGeometricCenter, midpoint.Item2 - yGeometricCenter);
            if(difference.Item1 > 0.5 || difference.Item2 > 0.5)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the bounding box of a quadratic Bezier curve.
        /// </summary>
        /// <param name="point1">Start point of Bezier curve.</param>
        /// <param name="controlPoint">Control point of Beizer curve.</param>
        /// <param name="point2">End point of Bezier curve.</param>
        /// <returns>Bounding box assoicated with a quadratic Bezier curve.</returns>
        public static Rectangle GetQuadraticBoundingBox ((double, double) point1,
            (double, double) controlPoint, (double, double) point2)
        {
            // t values of where derivative is = 0, which indicates a potential min or max
            // Use those values to get the local extreme x and y points
            // Compare those local extremes with start and end points to get absolute x and y extreme

            (double, double) criticalPoints = GetQuadraticCriticalPoints(point1, controlPoint, point2);
            (double, double) xExtremePoint, yExtremePoint;
            
            if(criticalPoints.Item1 > 0 && criticalPoints.Item1 < 1)
            {
                xExtremePoint = GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item1);
            }
            else
            {
                // Pick either the start or the end of the curve arbitrarily so it doesn't affect
                // the max/min point calculation
                xExtremePoint = point1;
            }

            if(criticalPoints.Item2 > 0 && criticalPoints.Item2 < 1)
            {
                yExtremePoint = GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item2);
            }
            else
            {
                // Pick either the start or the end of the curve arbitrarily so it doesn't affect
                // the max/min point calculation
                yExtremePoint = point1;
            }

            // The way that rectangles are stored is left = x of left side, top = y of top side, right = x of right side
            // bottom = y of bottom side
            // min x = left, max y = top, max x = right, min y = bottom

            double minX = Math.Min(Math.Min(point1.Item1, point2.Item1), Math.Min(xExtremePoint.Item1, yExtremePoint.Item1));
            double maxY = Math.Max(Math.Max(point1.Item2, point2.Item2), Math.Max(xExtremePoint.Item2, yExtremePoint.Item2));
            double maxX = Math.Max(Math.Max(point1.Item1, point2.Item1), Math.Max(xExtremePoint.Item1, yExtremePoint.Item1));
            double minY = Math.Min(Math.Min(point1.Item2, point2.Item2), Math.Min(xExtremePoint.Item2, yExtremePoint.Item2));

            Rectangle boundingBox = new Rectangle(minX, maxY, maxX, minY);
            return boundingBox;
        }
    }
}