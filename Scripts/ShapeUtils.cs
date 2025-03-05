using CsXFL;
using System.Xml.Linq;

namespace Rendering
{

    // A path is made up of multiple segments, and every command, either explicit or implicit,
    // other than moveTo (M) or closepath (Z), defines one path segment.
    // For the sake of consistency, the moveTo command will be the first "segment" of the path
    // even though it is only used to set up the official first segment of a path

    internal class SvgPathSegment
    {
        public double Distance { get; set; } = 0;
        public string SegmentString { get; set; } = "";
        public string CommandType { get; set; } = "";
        public List<(double, double)> ControlPoints { get; set; } = new List<(double, double)>();

        public void AddControlPoint((double, double) controlPoint)
        {
            ControlPoints.Add(controlPoint);
        }

    }

    /// <summary>
    /// Utils for converting a XFL DOMShape element into its equivalent SVG path elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Base logic and documentation was largely taken from PluieElectrique's
    /// <see href="https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/shape.py">edge.py</see>
    /// and SynthBot-Anon's updated version of
    /// <see href="https://github.com/synthbot-anon/PluieElectrique-xfl2svg/blob/radial-gradient-take2/xfl2svg/shape/shape.py">edge.py.</see>
    /// </para>
    /// <para>
    /// To account for the original code's usage of the python dict's update() method which does not have a direct
    /// equivalent in .NET, I created my own method.
    /// </para>
    /// </remarks>
    internal class ShapeUtils
    {
        /// <summary>
        /// Converts a pointList into a SVG path string.
        /// </summary>
        /// <remarks>
        /// This method converts a pointList into the "d" attribute of a path element,
        /// NOT into an entire path element itself (with proper opening and closing path tags,
        /// d=, style= etc).
        /// </remarks>
        /// <param name="pointList">The pointList that is being converted.</param>
        /// <returns>The equivalent "d" string for the given pointList.</returns>
        public static string ConvertPointListToPathString(List<string> pointList)
        {
            // Using iterator to match previous method as well as Python implementation
            IEnumerator<string> pointEnumerator = pointList.GetEnumerator();
            // Start SVG path with M command and first point
            pointEnumerator.MoveNext();

            List<string> svgPath = new List<string> { "M", pointEnumerator.Current };
            string lastCommand = "M";

            while (pointEnumerator.MoveNext())
            {
                string currentPoint = pointEnumerator.Current;
                string currentCommand = currentPoint.Contains('[') && currentPoint.Contains(']') ? "Q" : "L";

                // SVG path element allows us to omit command letter if same command is used
                // multiple times in a row, so only add it to svgPath string if new command is found
                /*if (currentCommand != lastCommand)*/

                // TEST CODE- DELETE IF NEEDED- EXPLICITLY SHOW EVERY COMMAND FOR TEST PURPOSES
                /*{*/
                    svgPath.Add(currentCommand);
                    lastCommand = currentCommand;
                /*}*/

                if (currentCommand == "Q")
                {
                    // As this is a "quadTo" command, control point is formatted as "[x y]"- need to remove []
                    // add said point, and then add end point (next point)
                    currentPoint = currentPoint.Replace("[", "").Replace("]", "");

                    svgPath.Add(currentPoint);
                    pointEnumerator.MoveNext();
                    svgPath.Add(pointEnumerator.Current);
                }
                else
                {
                    svgPath.Add(currentPoint);
                }
            }

            // Animate adds a "closepath" (Z) command to every filled shape and
            // closed stroke. For shapes, it makes no difference, but for closed
            // strokes, it turns two overlapping line caps into a bevel, miter,
            // or round join, which does make a difference.

            // TODO: It is likely that stroked paths can be broken into multiple pointLists
            // and spread across multiple XFL Edge elements, which would require something
            // like ConvertPointListsToShapes() but for stroked paths.
            // It seems like closing a path seems good enough for now
            // Code was commented out in SynthBot-Anon's version, so I replicated it here.

            if (pointList[0] == pointList[pointList.Count - 1])
            {
                // If starting point == ending point i.e completes a closed shape/stroke,
                // Add Z command
                //svgPath.Add("Z");
            }

            // Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
        }

        /// <summary>
        /// Converts XFL DOMShape element into its equivalent SVG path elements.
        /// </summary>
        /// <remarks>
        /// This method performs three actions:
        /// 1. Gathers fillStyle and strokeStyle indexes
        /// 2. Uses EdgeUtils to get pointLists and bounding boxes assoicated with these fill/strokeStyle indexes
        /// 3. Generates the SVG, including style information.
        /// </remarks>
        /// <param name="shapeElement">The XFL DOMShape being converted.</param>
        /// <param name="mask">If true, all fill colors will be set to #FFFFFF. This ensures
        /// that the resulting mask is fully transparent.</param>
        /// <returns>A 4-tuple consisting of: SVG g element containing filled paths elements,
        /// SVG g element containing stroked path elements, and
        /// dict of extra elements to put in SVG defs element (e.g.filters and gradients),
        /// bounding box of entire shape.</returns>
        public static (XElement?, XElement?, Dictionary<string, XElement>?, Rectangle?)
            ConvertShapeToSVG(Shape shapeElement, bool mask = false)
        {
            // Don't know if this is really necessary or not
            Dictionary<int, FillStyle> fillStyles = new Dictionary<int, FillStyle>();
            Dictionary<int, StrokeStyle> strokeStyles = new Dictionary<int, StrokeStyle>();

            foreach (FillStyle style in shapeElement.Fills)
            {
                fillStyles[style.Index] = style;
            }

            foreach (StrokeStyle style in shapeElement.Strokes)
            {
                strokeStyles[style.Index] = style;
            }

            List<XElement> filledPaths = new List<XElement>();
            List<XElement> strokedPaths = new List<XElement>();
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();
            Rectangle? boundingBox = null;

            (Dictionary<int, (List<List<string>>, Rectangle?)>,
            Dictionary<int, (List<List<string>>, Rectangle?)>) shapesAndStrokes =
                EdgeUtils.ConvertEdgesToPointListsAndBoxes(shapeElement.Edges, fillStyles, strokeStyles);

            foreach (KeyValuePair<int, (List<List<string>>, Rectangle?)> shapePair in shapesAndStrokes.Item1)
            {
                int fillStyleIndex = shapePair.Key;
                List<List<string>> pointLists = shapePair.Value.Item1;
                Rectangle? currentBoundingBox = shapePair.Value.Item2;
                FillStyle style = fillStyles[fillStyleIndex];
                Dictionary<string, string> styleSVGAttributes = new Dictionary<string, string>();

                if (mask)
                {
                    // Set the fill to white so that the mask is fully transparent
                    styleSVGAttributes["fill"] = "#FFFFFF";
                    styleSVGAttributes["stroke"] = "none";
                }
                else
                {
                    // Get this fillStyle's SVG attributes and any extra SVG elements
                    // Create the proper path element for this fillStyle
                    (Dictionary<string, string> properAttributes,
                        Dictionary<string, XElement> styleExtraElements) = StyleUtils.ParseFillStyle(style);

                    styleSVGAttributes = UpdateDictionary(styleSVGAttributes, properAttributes);
                    extraDefElements = UpdateDictionary(extraDefElements, styleExtraElements);
                }

                // Create XML path element with its proper attributes
                var pathElement = CreatePathElement(styleSVGAttributes);
                pathElement.SetAttributeValue("d", string.Join(" ", pointLists.Select(pl => ConvertPointListToPathString(pl))));
                filledPaths.Add(pathElement);

                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            foreach (KeyValuePair<int, (List<List<string>>, Rectangle?)> strokePair in shapesAndStrokes.Item2)
            {
                int strokeStyleIndex = strokePair.Key;
                List<List<string>> pointLists = strokePair.Value.Item1;
                Rectangle? currentBoundingBox = strokePair.Value.Item2;
                StrokeStyle style = strokeStyles[strokeStyleIndex];
                Dictionary<string, string> styleSVGAttributes = new Dictionary<string, string>();

                // TODO: Figure out how strokes are supposed to behave in masks
                if(mask)
                {
                    Console.WriteLine("Strokes in masks are not supported!");
                }

                // Get this strokeStyle's SVG attributes and any extra SVG elements
                // Create the proper path element for this strokeStyle
                (Dictionary<string, string> properAttributes,
                    Dictionary<string, XElement> styleExtraElements) = StyleUtils.ParseStrokeStyle(style);

                styleSVGAttributes = UpdateDictionary(styleSVGAttributes, properAttributes);
                extraDefElements = UpdateDictionary(extraDefElements, styleExtraElements);

                // Update bounding box for strokeStyle based on its width
                double strokeWidth = float.Parse(styleSVGAttributes["stroke-width"]);
                currentBoundingBox = BoxUtils.ExpandBoundingBox(currentBoundingBox!, strokeWidth);

                // pointLists already break a stroke by its moveTo commands (think of it like cursive)
                // So the entire SVG string is broken up by moveTo commands as well
                List<string> testing = pointLists.Select(ConvertPointListToPathString).ToList();

                // TEST CODE- DELETE IF NEEDED
                XNamespace xlink = "http://www.w3.org/1999/xlink";
                XNamespace svgNs = "http://www.w3.org/2000/svg";
                XElement svg = new XElement(svgNs + "svg",
                new XAttribute("version", "1.1"),
                new XAttribute("preserveAspectRatio", "none"),
                new XAttribute("x", "0px"),
                new XAttribute("y", "0px"),
                new XAttribute("width", $"{1920}px"),
                new XAttribute("height", $"{1080}px"),
                new XAttribute("viewBox", $"0 0 {1920} {1080}"),
                new XAttribute(XNamespace.Xmlns + "xlink", xlink.ToString())
                );
                Random random = new Random();
                SolidStroke solidStroke = (SolidStroke)style.Stroke;
                if(solidStroke != null && solidStroke.WidthMarkers != null && solidStroke.WidthMarkers.Count != 0)
                {
                    foreach(string pointListString in testing)
                    {
                        List<XElement> test = TestingOffsetSvgPath(pointListString, solidStroke);
                        foreach(XElement testElement in test)
                        {
                            svg.Add(testElement);
                        }
                    }

                    svg.Save(@"D:\Documents\EOJAssets\TestFolder\TestingCurve_"+ random.Next()+ ".svg");
                }
                // Create XML path element with its proper attributes
                var pathElement = CreatePathElement(styleSVGAttributes);
                pathElement.SetAttributeValue("d", string.Join(" ", pointLists.Select(pl => ConvertPointListToPathString(pl))));

                strokedPaths.Add(pathElement);
                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            XElement? fillsG = null;
            XElement? strokesG = null;

            if (filledPaths != null)
            {
                fillsG = new XElement("g");
                foreach (XElement fillPathElement in filledPaths)
                {
                    fillsG.Add(fillPathElement);
                }
            }

            if (strokedPaths != null)
            {
                strokesG = new XElement("g");
                foreach (XElement strokePathElement in strokedPaths)
                {
                    strokesG.Add(strokePathElement);
                }
            }

            return (fillsG, strokesG, extraDefElements, boundingBox);
        }

        /// <summary>
        /// Updates the contents of a dictionary based on the ones found in another.
        /// </summary>
        /// <remarks>
        /// This method both inserts new key value pairs that are not found in the original dictionary
        /// as well as updates ones that were already included.
        /// </remarks>
        /// <typeparam name="T1">The key's type.</typeparam>
        /// <typeparam name="T2">The value's type.</typeparam>
        /// <param name="dictionary">The original dictionary that will be updated.</param>
        /// <param name="dictionaryToAdd">The dictionary whose contents will be added to the original
        /// dictionary.</param>
        /// <returns>The original dictionary with both the new key value pairs as well as the updated
        /// ones. </returns>
        private static Dictionary<T1, T2> UpdateDictionary<T1,  T2>(Dictionary<T1, T2> dictionary,
            Dictionary<T1, T2> dictionaryToAdd) where T1 : notnull
        {
            foreach(KeyValuePair<T1, T2> keyValuePair in dictionaryToAdd)
            {
                dictionary[keyValuePair.Key] = keyValuePair.Value;
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a SVG path element.
        /// </summary>
        /// <param name="attributes">Attributes that will be included in the SVG path element.</param>
        /// <returns>The SVG path element with the associated set of attributes.</returns>
        private static XElement CreatePathElement(Dictionary<string, string> attributes)
        {
            XElement newPathElement = new XElement("path");
            foreach (var attribute in attributes)
            {
                newPathElement.SetAttributeValue(attribute.Key, attribute.Value);
            }

            return newPathElement;
        }

        private static List<SvgPathSegment> SplitSvgPathIntoSegments(string svgPathString)
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

                        double lineDistance = MathUtils.CalculateLineLength(prevPoint, currPoint);
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

                        double curveDistance = MathUtils.CalculateQuadBezierLength(point0, point1, point2, 1);
                        newSeg.Distance = curveDistance;
                        segments.Add(newSeg);

                        prevPoint = point2;
                        svgSegmentString = "";
                    }
                }
            }

            return segments;
        }

        private static (List<(double, double)>, List<(double, double)>)
            GetWidthMarkerPoints(string svgPathString, SolidStroke solidStroke)
        {
            List<WidthMarker> widthMarkers = solidStroke.WidthMarkers!;
            List<SvgPathSegment> svgPathSegments = SplitSvgPathIntoSegments(svgPathString);
            List<(double, double)> topControlPoints = new List<(double, double)>();
            List<(double, double)> bottomControlPoints = new List<(double, double)>();

            int widthMarkerIndex = 0;
            int segmentIndex = 1;


            double distanceTraveled = 0;
            double totalDistance = svgPathSegments.Sum(s => s.Distance);

            // Start and end widthMarkers are based on the segment that they are part of
            // As the first "segment" is just a move command, when parsing the starting widthMarker
            // calculate the perpendicular of the tangent based on the segment after it
            while (widthMarkerIndex < widthMarkers.Count)
            {
                SvgPathSegment currentSeg = svgPathSegments[segmentIndex];
                WidthMarker currentMarker = widthMarkers[widthMarkerIndex];
                double distanceToMarker = totalDistance * currentMarker.Position;

                // Check if marker position is within the current segment (inclusive)
                if (distanceTraveled <= distanceToMarker
                    && distanceToMarker <= distanceTraveled + currentSeg.Distance)
                {
                    // Get WidthMarker's position ratio value (t value) relative to the segment itself
                    double relativeT = (distanceToMarker - distanceTraveled) / currentSeg.Distance;
                    (double, double) tangentVector = (0, 0);
                    (double, double) markerPoint = (0, 0);

                    if (currentSeg.CommandType == "L")
                    {
                        (double, double) point0 = currentSeg.ControlPoints[0];
                        (double, double) point1 = currentSeg.ControlPoints[1];

                        // Get the tangent vector of the segment
                        tangentVector = MathUtils.GetUnitTangentVectorOfLine(point0, point1);

                        // Get point on line where marker is using linear interpolation formula
                        markerPoint.Item1 = (1 - relativeT) * point0.Item1 + relativeT * point1.Item1;
                        markerPoint.Item2 = (1 - relativeT) * point0.Item2 + relativeT * point1.Item2;
                    }
                    else if (currentSeg.CommandType == "Q")
                    {
                        (double, double) point0 = currentSeg.ControlPoints[0];
                        (double, double) point1 = currentSeg.ControlPoints[1];
                        (double, double) point2 = currentSeg.ControlPoints[2];

                        // Get the tangent vector at point on curve
                        tangentVector = MathUtils.GetUnitTangentVectorOfQuadraticBezier(point0, point1, point2, relativeT);

                        markerPoint = MathUtils.GetPointOnQuadraticBezier(point0, point1, point2, relativeT);
                    }

                    // Get distance of how far left and right marker points are from center widthmarker
                    // point on center of SVG path
                    // "left" attribute = up (negative), "right" attribute = down (positive), just like in SVG

                    double leftDistance = currentMarker.Left * solidStroke.Weight;
                    double rightDistance = currentMarker.Right * solidStroke.Weight;

                    // Now use math to get second point for top and bottom that are left and right distance
                    // from center marker point
                    (double, double) leftPoint = (0, 0);
                    (double, double) rightPoint = (0, 0);

                    // Using markerPoint on segment, normal slope, and distance on each side, get marker points
                    // extending out on either side (the ends of the vertical line going through center marker point)

                    (double, double) rotatedCounter = (-tangentVector.Item2, tangentVector.Item1);
                    (double, double) rotatedClock = (tangentVector.Item2, -tangentVector.Item1);

                    leftPoint.Item1 = markerPoint.Item1 + rotatedClock.Item1 * leftDistance;
                    leftPoint.Item2 = markerPoint.Item2 + rotatedClock.Item2 * leftDistance;
                    rightPoint.Item1 = markerPoint.Item1 + rotatedCounter.Item1 * rightDistance;
                    rightPoint.Item2 = markerPoint.Item2 + rotatedCounter.Item2 * rightDistance;
                    topControlPoints.Add(leftPoint);
                    bottomControlPoints.Add(rightPoint);
                    widthMarkerIndex++;
                }
                else
                {
                    distanceTraveled += currentSeg.Distance;
                    segmentIndex++;
                }
            }

            return (topControlPoints, bottomControlPoints);
        }

        private static List<XElement> TestingOffsetSvgPath(string svgPathString, SolidStroke solidStroke)
        {
            List<XElement> pathElements = new List<XElement>();
            List<SvgPathSegment> strokeOutlineSegments = new List<SvgPathSegment>();
            (List<(double, double)> topMarkerPoints,
               List<(double, double)> bottomMarkerPoints) = GetWidthMarkerPoints(svgPathString, solidStroke);
            
            // Draw the width marker
            XNamespace svgNs = "http://www.w3.org/2000/svg";
            foreach (var point in topMarkerPoints)
            {
                XElement circle = new XElement(svgNs + "circle");
                //circle.SetAttributeValue("fill", $"#{red:X2}{green:X2}{blue:X2}");
                circle.SetAttributeValue("fill", "black");
                circle.SetAttributeValue("r", "1");
                circle.SetAttributeValue("cx", $"{point.Item1}");
                circle.SetAttributeValue("cy", $"{point.Item2}");
                pathElements.Add(circle);
            }

            foreach (var point in bottomMarkerPoints)
            {
                XElement circle = new XElement(svgNs + "circle");
                //circle.SetAttributeValue("fill", $"#{red:X2}{green:X2}{blue:X2}");
                circle.SetAttributeValue("fill", "black");
                circle.SetAttributeValue("r", "1");
                circle.SetAttributeValue("cx", $"{point.Item1}");
                circle.SetAttributeValue("cy", $"{point.Item2}");
                pathElements.Add(circle);
            }

            // We are drawing the fill shape that represents the greater VPW stroke clockwise

            if (topMarkerPoints.Count > 2)
            {
                string totalShape = "";

                (double, double)[] controlPoints = new (double, double)[4];

                for (int i = 0; i < topMarkerPoints.Count - 1; i++)
                {
                    if (i == 0)
                    {
                        controlPoints[0] = bottomMarkerPoints[^1];
                        controlPoints[1] = topMarkerPoints[i];
                        controlPoints[2] = topMarkerPoints[i + 1];
                        controlPoints[3] = topMarkerPoints[i + 2];
                    }
                    else if (i == topMarkerPoints.Count - 2)
                    {
                        controlPoints[0] = topMarkerPoints[i - 1];
                        controlPoints[1] = topMarkerPoints[i];
                        controlPoints[2] = topMarkerPoints[i + 1];
                        controlPoints[3] = bottomMarkerPoints[0];
                    }
                    else
                    {
                        controlPoints[0] = topMarkerPoints[i - 1];
                        controlPoints[1] = topMarkerPoints[i];
                        controlPoints[2] = topMarkerPoints[i + 1];
                        controlPoints[3] = topMarkerPoints[i + 2];
                    }

                    // In order to make the outline one entire path, only save the first move command
                    // and then use line and quad commands to draw the outline clockwise

                    string outlineString = $"M {controlPoints[1].Item1} {controlPoints[1].Item2} L";
                    CatmullRomCurve romCurve = new CatmullRomCurve(controlPoints[0], controlPoints[1],
                        controlPoints[2], controlPoints[3], 0.5);

                    double detail = 32;
                    for (int j = 0; j < detail; j++)
                    {
                        double t = (j / (detail - 1));
                        (double, double) topPoint = romCurve.GetPointOnCurve(t);
                        outlineString += $" {topPoint.Item1} {topPoint.Item2}";
                    }

                    totalShape += outlineString;
                }

                // As we want to draw the shape going clockwise, for the bottom part, we
                // are drawing it going from right to left
                // This is so the VPW stroke is an enclosed shape

                bottomMarkerPoints.Reverse();
                topMarkerPoints.Reverse();

                for (int i = 0; i < bottomMarkerPoints.Count - 1; i++)
                {
                    if (i == 0)
                    {
                        controlPoints[0] = topMarkerPoints[^1];
                        controlPoints[1] = bottomMarkerPoints[i];
                        controlPoints[2] = bottomMarkerPoints[i + 1];
                        controlPoints[3] = bottomMarkerPoints[i + 2];
                    }
                    else if (i == bottomMarkerPoints.Count - 2)
                    {
                        controlPoints[0] = bottomMarkerPoints[i - 1];
                        controlPoints[1] = bottomMarkerPoints[i];
                        controlPoints[2] = bottomMarkerPoints[i + 1];
                        controlPoints[3] = topMarkerPoints[0];
                    }
                    else
                    {
                        controlPoints[0] = bottomMarkerPoints[i - 1];
                        controlPoints[1] = bottomMarkerPoints[i];
                        controlPoints[2] = bottomMarkerPoints[i + 1];
                        controlPoints[3] = bottomMarkerPoints[i + 2];
                    }

                    string outlineString = $"M {controlPoints[1].Item1} {controlPoints[1].Item2} L";
                    CatmullRomCurve romCurve = new CatmullRomCurve(controlPoints[0], controlPoints[1],
                        controlPoints[2], controlPoints[3], 1);

                    double detail = 32;
                    for (int j = 0; j < detail; j++)
                    {
                        double t = (j / (detail - 1));
                        (double, double) topPoint = romCurve.GetPointOnCurve(t);
                        outlineString += $" {topPoint.Item1} {topPoint.Item2}";
                    }

                    totalShape += outlineString;
                }

                XElement dummyPath = new XElement(svgNs + "path");
                dummyPath.SetAttributeValue("fill", "none");
                dummyPath.SetAttributeValue("stroke", "black");
                dummyPath.SetAttributeValue("d", totalShape);
                pathElements.Add(dummyPath);
            }
            return pathElements;
        }

        public static (double, double) GetDirectionAtStartOfSegment(List<SvgPathSegment> entirePath, int segmentIndex)
        {
            double totalLength = entirePath.Sum(s => s.Distance);
            SvgPathSegment currentSegment = entirePath[segmentIndex];

            // If the segment is part of path of zero length, just default to positive x axis direction
            if (totalLength == 0)
            {
                return (1, 0);
            }
            else if (currentSegment.Distance == 0)
            {
                // Go backwards in path until we reach first non-zero non-moveTo segment. If we find one,
                // use the direction at the end of that segment
                int startingPosition = segmentIndex - 1;
                while (startingPosition > 0)
                {
                    SvgPathSegment previous = entirePath[startingPosition];
                    if (previous.Distance != 0)
                    {
                        return GetDirectionAtEndOfSegment(entirePath, startingPosition);
                    }
                    startingPosition--;
                }

                // If there is no previous segment that has a non-zero length, use end direction of
                // segment as the start direction
                return GetDirectionAtEndOfSegment(entirePath, segmentIndex);
            }
            else
            {
                // Segment is non-zero in length, direction is just one coming out of start point
                if (currentSegment.CommandType == "L")
                {
                    (double, double) point0 = currentSegment.ControlPoints[0];
                    (double, double) point1 = currentSegment.ControlPoints[1];

                    // Get the tangent vector of the segment
                    (double, double) tangentVector = MathUtils.GetUnitTangentVectorOfLine(point0, point1);
                    return tangentVector;
                }
            }
            return (1, 0);
        }

        public static (double, double) GetDirectionAtEndOfSegment(List<SvgPathSegment> entirePath, int segmentIndex)
        {
            double totalLength = entirePath.Sum(s => s.Distance);
            SvgPathSegment currentSegment = entirePath[segmentIndex];

            // If the segment is part of path of zero length, just default to positive x axis direction
            if (totalLength == 0)
            {
                return (1, 0);
            }
            else if (currentSegment.Distance == 0)
            {
                // Go forwards in path until we reach first non-zero non-moveTo segment. If we find one,
                // use the direction at the start of that segment
                int startingPosition = segmentIndex + 1;
                while (startingPosition < entirePath.Count)
                {
                    SvgPathSegment previous = entirePath[startingPosition];
                    if (previous.Distance != 0)
                    {
                        return GetDirectionAtStartOfSegment(entirePath, startingPosition);
                    }
                    startingPosition++;
                }

                // If there is no suceeding segment that has a non-zero length, use start direction of
                // segment as the end direction
                return GetDirectionAtEndOfSegment(entirePath, segmentIndex);
            }
            else
            {
                // Segment is non-zero in length, direction is just one coming out of end point
                if (currentSegment.CommandType == "L")
                {
                    (double, double) point0 = currentSegment.ControlPoints[0];
                    (double, double) point1 = currentSegment.ControlPoints[1];

                    // Get the tangent vector of the segment
                    (double, double) tangentVector = MathUtils.GetUnitTangentVectorOfLine(point0, point1);
                    return tangentVector;
                }
            }
            return (1, 0);
        }
    }
}
