using CsXFL;
using System.Numerics;
using System;

namespace Rendering
{
    internal class MathUtils
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

        public static double CalculateLineLength((double, double) point0, (double, double) point1)
        {
            double xPart = Math.Pow(point1.Item1 - point0.Item1, 2);
            double yPart = Math.Pow(point1.Item2 - point0.Item2, 2);
            return Math.Sqrt(xPart + yPart);
        }
        public static (double, double) GetUnitTangentVectorOfQuadraticBezier((double, double) point0,
            (double, double) point1, (double, double) point2, double t)
        {
            // First use derivative to get tangent vector at specific point on curve
            double xTangent = 2 * (1 - t) * (point1.Item1 - point0.Item1) + 2 * t * (point2.Item1 - point0.Item1);
            double yTangent = 2 * (1 - t) * (point1.Item2 - point0.Item2) + 2 * t * (point2.Item2 - point0.Item2);

            (double, double) tangentVector = (xTangent, yTangent);

            // Then calculate the magnitude of the vector so we can use that to get the unit tangent vector
            double vectorMagnitude = Math.Sqrt(tangentVector.Item1 * tangentVector.Item1 +
                tangentVector.Item2 * tangentVector.Item2);

            return (tangentVector.Item1 / vectorMagnitude, tangentVector.Item2 / vectorMagnitude);
        }

        public static (double, double) GetUnitTangentVectorOfLine((double, double) point0, (double, double) point1)
        {
            (double, double) tangentVector = (point1.Item1 - point0.Item1, point1.Item2 - point0.Item2);

            // Then calculate the magnitude of the vector so we can use that to get the unit tangent vector
            double vectorMagnitude = Math.Sqrt(tangentVector.Item1 * tangentVector.Item1 +
                tangentVector.Item2 * tangentVector.Item2);

            return (tangentVector.Item1/ vectorMagnitude, tangentVector.Item2 / vectorMagnitude);
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
            for (int i = 0; i < legendreGaussWeights.Length; i++)
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

        public static double GetNormalOfQuadBezierCurve((double, double) point0,
            (double, double) point1, (double, double) point2, double t)
        {
            // Use derivative to get slope at point (t from the start of the curve)
            double xBezierCurveDerivative = 2 * (1 - t) * (point1.Item1 - point0.Item1) +
                    2 * t * (point2.Item1 - point1.Item1);
            double yBezierCurveDerivative = 2 * (1 - t) * (point1.Item2 - point0.Item2) +
                2 * t * (point2.Item2 - point1.Item2);

            return -yBezierCurveDerivative / xBezierCurveDerivative;
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

        /// <summary>
        /// Merges a list of bounding boxes together.
        /// </summary>
        /// <param name="rectangles">The list of rectangles to merge.</param>
        /// <returns>A Rectangle representing the combined bounding box, or null if the list is empty or contains only null values.</returns>
        /// 
        /// 
        public static Rectangle? MergeBoundingBoxes(Rectangle? rect1, Rectangle? rect2)
        {
            return MergeBoundingBoxes(new List<Rectangle?> { rect1, rect2 });
        }

        public static Rectangle? MergeBoundingBoxes(List<Rectangle?> rectangles)
        {
            if (rectangles == null || rectangles.Count == 0)
            {
                return null;
            }

            Rectangle? result = null;

            foreach (Rectangle? rectangle in rectangles)
            {
                if (rectangle != null)
                {
                    if (result == null)
                    {
                        result = rectangle;
                    }
                    else
                    {
                        // The way that rectangles are stored is left = x of left side, top = y of top side, right = x of right side
                        // bottom = y of bottom side
                        // min x = left, min y = top, max x = right, max y = bottom
                        double minX = Math.Min(result.Left, rectangle.Left);
                        double minY = Math.Min(result.Top, rectangle.Top);      // Changed: top should be minimum Y
                        double maxX = Math.Max(result.Right, rectangle.Right);
                        double maxY = Math.Max(result.Bottom, rectangle.Bottom); // Changed: bottom should be maximum Y

                        result = new Rectangle(minX, minY, maxX, maxY);         // Changed: (left, top, right, bottom)
                    }
                }
            }

            return result;
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

            (double, double) criticalPoints = MathUtils.GetQuadraticCriticalPoints(point1, controlPoint, point2);
            (double, double) xExtremePoint, yExtremePoint;
            
            if(criticalPoints.Item1 > 0 && criticalPoints.Item1 < 1)
            {
                xExtremePoint = MathUtils.GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item1);
            }
            else
            {
                // Pick either the start or the end of the curve arbitrarily so it doesn't affect
                // the max/min point calculation
                xExtremePoint = point1;
            }

            if(criticalPoints.Item2 > 0 && criticalPoints.Item2 < 1)
            {
                yExtremePoint = MathUtils.GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item2);
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

    internal class CatmullRomCurve
    {
        public double Alpha { get; set; }
        public (double, double) Point0 { get; set; }
        public (double, double) Point1 { get; set; }
        public (double, double) Point2 { get; set; }
        public (double, double) Point3 { get; set; }

        public CatmullRomCurve((double, double) point0, (double, double) point1,
            (double, double) point2, (double, double) point3, double alpha)
        {
            Alpha = alpha;
            Point0 = point0;
            Point1 = point1;
            Point2 = point2;
            Point3 = point3;
        }

        // Evaluates a point at the given t-value from 0 to 1
        public (double, double) GetPointOnCurve(double t)
        {
            // calculate knots
            const float k0 = 0;
            double k1 = GetKnotInterval(Point0, Point1);
            double k2 = GetKnotInterval(Point1, Point2) + k1;
            double k3 = GetKnotInterval(Point2, Point3) + k2;

            // evaluate the point
            double u = LerpUnclamped(k1, k2, t);
            (double, double) A1 = Remap(k0, k1, Point0, Point1, u);
            (double, double) A2 = Remap(k1, k2, Point1, Point2, u);
            (double, double) A3 = Remap(k2, k3, Point2, Point3, u);
            (double, double) B1 = Remap(k0, k2, A1, A2, u);
            (double, double) B2 = Remap(k1, k3, A2, A3, u);
            return Remap(k1, k2, B1, B2, u);
        }

        private double LerpUnclamped(double a, double b, double t)
        {
            return a + t * (b - a);
        }

        private (double, double) LerpUnclamped(
            (double, double) a, (double, double) b, double t)
        {
            return (a.Item1 + t * (b.Item1 - a.Item1), a.Item2 + t * (b.Item2 - a.Item2));
        }

        private double SquareMagnitude(double a, double b)
        {
            return a * a + b * b;
        }

        private double GetKnotInterval((double, double) a, (double, double) b)
        {
            return Math.Pow(SquareMagnitude(a.Item1 - b.Item1, a.Item2 - b.Item2), 0.5f * Alpha);
        }

        private (double, double) Remap(double a, double b, (double, double) c, (double, double) d, double u)
        {
            return LerpUnclamped(c, d, (u - a) / (b - a));
        }
    }
}