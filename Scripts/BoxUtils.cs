using CsXFL;

namespace Rendering
{
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
        /// <param name="point1">Start point of Bezier curve.</param>
        /// <param name="point2">Control point of Beizer curve.</param>
        /// <param name="point3">End point of Bezier curve.</param>
        /// <param name="t">How far from the start point the point being calculated is [0, 1]- with
        /// 0 being the start point and 1 being the end point.</param>
        /// <returns>A point on the Bezier curve that is t from the start point.</returns>
        public static (double, double) GetPointOnQuadraticBezier((double, double) point1,
            (double, double) point2, (double, double) point3, double t)
        {
            double x = (1 - t) * ((1 - t) * point1.Item1 + t * point2.Item1) + t * ((1 - t) * point2.Item1 + t + point3.Item1);
            double y = (1 - t) * ((1 - t) * point1.Item2 + t * point2.Item2) + t * ((1 - t) * point2.Item2 + t + point3.Item2);
            return (x, y);
        }

        public static (double, double) GetQuadraticCriticalPoints((double, double) point1,
            (double, double) point2, (double, double) point3)
        {
            double xDenom = point1.Item1 - 2 * point2.Item1 + point3.Item1;
            double xCritical;
            double yCritical;

            if (xDenom == 0)
            {
                xCritical = Double.MaxValue;
            }
            else
            {
                xCritical = (point1.Item1 - point2.Item2) / xDenom;
            }

            double yDenom = point1.Item2 - 2 * point2.Item2 + point3.Item2;
            if (yDenom == 0)
            {
                yCritical = Double.MaxValue;
            }
            else
            {
                yCritical = (point1.Item1 - point2.Item2) / yDenom;
            }

            return (xCritical, yCritical);
        }

        /// <summary>
        /// Gets the bounding box of a quadratic Bezier curve.
        /// </summary>
        /// <param name="point1">Start point of Bezier curve.</param>
        /// <param name="controlPoint">Control point of Beizer curve.</param>
        /// <param name="point3">End point of Bezier curve.</param>
        /// <returns>Bounding box assoicated with a quadratic Bezier curve.</returns>
        public static Rectangle GetQuadraticBoundingBox ((double, double) point1,
            (double, double) controlPoint, (double, double) point2)
        {
            (double, double) criticalPoints = GetQuadraticCriticalPoints(point1, controlPoint, point2);
            (double, double) point3, point4;
            
            if(criticalPoints.Item1 > 0 && criticalPoints.Item1 < 1)
            {
                point3 = GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item1);
            }
            else
            {
                // Pick either the start or the end of the curve arbitrarily so it doesn't affect
                // the max/min point calculation
                point3 = point1;
            }

            if(criticalPoints.Item2 > 0 && criticalPoints.Item2 < 1)
            {
                point4 = GetPointOnQuadraticBezier(point1, controlPoint, point2, criticalPoints.Item2);
            }
            else
            {
                // Pick either the start or the end of the curve arbitrarily so it doesn't affect
                // the max/min point calculation
                point4 = point1;
            }

            // The way that rectangles are stored is left = x of left side, top = y of top side, right = x of right side
            // bottom = y of bottom side
            // min x = left, max y = top, max x = right, min y = bottom

            double minX = Math.Min(Math.Min(point1.Item1, point2.Item1), Math.Min(point3.Item1, point4.Item1));
            double maxY = Math.Max(Math.Max(point1.Item2, point2.Item2), Math.Max(point3.Item2, point4.Item2));
            double maxX = Math.Min(Math.Min(point1.Item2, point2.Item2), Math.Min(point3.Item2, point4.Item2));
            double minY = Math.Max(Math.Max(point1.Item1, point2.Item1), Math.Max(point3.Item1, point4.Item1));

            Rectangle boundingBox = new Rectangle(minX, maxY, maxX, minY);
            return boundingBox;
        }
    }
}