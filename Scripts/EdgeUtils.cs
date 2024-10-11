using CsXFL;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Rendering
{
    // Logic and Documentation largely taken from [https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/edge.py]
    // One aspect that had to be accounted for is the use of defaultdict, which assigns default
    // values to dictionary keys. To account for that, I used TryGetValue() and explicity set values
    internal class EdgeUtils
    {
        //Main Idea:
        // In XFL format, anything that is drawn can either be generally represented
        // as shapes (<DOMShape> elements) or symbols (<DOMSymbolInstance> elements) that in turn refers to shapes
        // A <DOMShape> is made of two elements- <fills> and <edges>
        // <fills> element- indicate the stroke style or fill style that will fill the shape
        // <edges> element- contain <Edge> elements-
        // "edges" attribute- string of commands and coordinates that indicate shape outline

        // outline is broken into pieces called segments- represented here as "point lists"
        // This outline will then be filled in using <fills> elements
        // Process: "edges" strings -> list of points -> SVG path elements -> render SVG as bitmap


        //"edges" string format:
        // First gives command type, then follows it with n coordinates
        // Commands- !- moveto, /- lineto, |- lineto, [- quadto, ]- quadto

        // Coordinates can either be decimal numbers or signed 32-number in Hex
        // Hex Coords are denoted by a # before them

        // "selects" in the format S[1-7] might be present as well in "moveto" commands- these are hints used
        // by Animate for noting selections (n=bitmask, 1:fillStyle0, 2:fillStyle1, 4:stroke)
        // When parsing Coords, they should be ignored (done with negative lookbehind)
        // Cubics are omitted as they only appear in "cubics" attribute and are only hints for Animate

        //@ notes regex string
        // Captures command, decimal number, or hex number
        // Whitespace is automatically ignored through matches
        // Negative lookbehind is used to ignore "select" (?<!S)
        private const string EDGE_REGEX = @"[!|/[\]]|(?<!S)-?\d+(?:\.\d+)?|\#[A-Z0-9]+\.[A-Z0-9]+";

        private static Regex edgeTokenizer = new Regex(EDGE_REGEX, RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Parses and converts a coordinate in the "edges" string.
        /// </summary>
        /// <param name="numberString">The coordinate in "edges" string being parsed.</param>
        /// <returns>The converted and scaled coordinate.</returns>
        public static float ParseNumber(string numberString)
        {
            // Check if the coordinate is signed and 32-bit fixed-point number in hex
            if (numberString[0] == '#')
            {
                // Split the coordinate into the integer and fractional parts
                string[] parts = numberString.Substring(1).Split('.');
                // Pad the integer part to 8 digits
                string hexNumberString = string.Format("{0:X8}{1:X2}", Convert.ToInt32(parts[0], 16), Convert.ToInt32(parts[1], 16));
                // Convert the hex coordinate to a signed 32-bit integer
                int numberInt = int.Parse(hexNumberString, System.Globalization.NumberStyles.HexNumber);
                // Convert the coordinate to its decimal equivalent and scale it down by 256 and 20
                return (numberInt / 256f) / 20f;
            }
            else
            {
                // The number is a decimal number. Scale it down by 20.
                return float.Parse(numberString) / 20f;
            }
        }

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
            //box[0] = minx, box[1] = miny, box[2] = maxx, box[3] = maxy
            // min x = left, max y = top, max x = right, min y = bottom

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

        //Point Format: "x y" string, "quadto" command control (start) point- "[x y]"

        //Point List Format:
        // Point List Example [A, B, [C], D, E] where letters are points
        // First point is always destination of "moveto" command. Subsequent points are "lineto" command destinations
        // "[x y]" point- control point of a quadratic Bézier curve and the following point is the destination of the curve
        // in the standard point format

        /// <summary>
        /// Converts an XML Edge element's "edges" string into a list of (point lists, bounding box) tuples.
        /// </summary>
        /// <remarks>
        /// Each segment of an "edge" is parsed into a point list and a bounding box.
        /// </remarks>
        /// <param name="edges">The "edges" attribute of an Edge XFL element.</param>
        /// <returns>An enumerable of (list of string points in "x y" format,
        /// bounding box of said point list) tuples.</returns>
        public static IEnumerable<(List<string>, Rectangle?)> ConvertEdgeFormatToPointLists(string edges)
        {
            // As MatchCollection was written before .NET 2, it uses IEnumerable for iteration rather
            // than IEnumerable<T>, meaning it defaults to an enumerable of objects.
            // To get enumerable of Matches, have to explicity type cast enumerable as Match
            
            IEnumerator<string> matchTokens = edgeTokenizer.Matches(edges).Cast<Match>().Select(currentMatch => currentMatch.Value).GetEnumerator();
            
            // Assert that the first token is a moveto command
            if(!matchTokens.MoveNext() || matchTokens.Current != "!")
            {
                throw new ArgumentException("Edge format must start with moveto (!) command");
            }

            // Using local delegate versus function for better performance
            Func<(double, double)> nextPoint = () =>
            {
                matchTokens.MoveNext();
                double x = ParseNumber(matchTokens.Current);
                matchTokens.MoveNext();
                double y = ParseNumber(matchTokens.Current);
                return (x, y);
            };
            List<string> pointList = new List<string>();

            (double, double) prevPoint = nextPoint();
            Rectangle? boundingBox = new Rectangle(prevPoint.Item1, prevPoint.Item2, prevPoint.Item1, prevPoint.Item2);

            while(matchTokens.MoveNext())
            {
                string command = matchTokens.Current;
                (double, double) currPoint = nextPoint();

                // "moveto" command
                if(command == "!")
                {
                    // If a move command doesn't change the current point, ignore it.
                    if(currPoint != prevPoint)
                    {
                        // Otherwise, a new segment is starting, so we must yield the current point list and begin a new one.
                        yield return (pointList, boundingBox);
                        pointList = new List<string>();
                        prevPoint = currPoint;
                        boundingBox = null;
                    }
                }
                // "lineto" command
                else if(command == "|" || command == "/")
                {
                    pointList.Add($"{prevPoint.Item1} {prevPoint.Item2}");
                    pointList.Add($"{currPoint.Item1} {currPoint.Item2}");
                    boundingBox = MergeBoundingBoxes(boundingBox, GetLineBoundingBox(prevPoint, currPoint));

                    prevPoint = currPoint;
                }
                // "quadto" command
                else if(command == "[" || command == "]")
                {
                    // prevPoint (the point before this in list) is the start of the quadratic Bézier curve
                    // currPoint is control point- this is denoted as a point string surrounded by []
                    // nextPoint() is destination point of curve
                    (double, double) endPoint = nextPoint();

                    pointList.Add($"{prevPoint.Item1} {prevPoint.Item2}");
                    pointList.Add($"[{currPoint.Item1} {currPoint.Item2}]");
                    pointList.Add($"{endPoint.Item1} {endPoint.Item2}");
                    boundingBox = MergeBoundingBoxes(boundingBox, GetQuadraticBoundingBox(prevPoint, currPoint, endPoint));
                    prevPoint = endPoint; // As we called nextPoint() prior- that's what the current Point atm is
                }
            }

            yield return (pointList, boundingBox);
            boundingBox = null;
        }

        private static List<string>? Walk(string currentPoint, HashSet<string> usedPoints, string originPoint,
                Dictionary<string, List<List<string>>> fillGraph)
        {
            // Recursively join point lists into shapes
            for (int i = 0; i < fillGraph[currentPoint].Count; i++)
            {
                List<string> nextPointList = fillGraph[currentPoint][i];
                string nextPoint = nextPointList[nextPointList.Count - 1];

                if (nextPoint.Equals(originPoint))
                {
                    // Found a cycle. This shape is now closed
                    fillGraph[currentPoint].RemoveAt(i);
                    return nextPointList;
                }
                else if (!usedPoints.Contains(nextPoint))
                {
                    // Try this point list
                    usedPoints.Add(nextPoint);
                    List<string>? shape = Walk(nextPoint, usedPoints, originPoint, fillGraph);
                    if (shape == null)
                    {
                        // Backtrack
                        usedPoints.Remove(nextPoint);
                    }
                    else
                    {
                        fillGraph[currentPoint].RemoveAt(i);
                        // Concat this point list, removing the redundant start move
                        List<string> result = new List<string>(nextPointList);
                        result.AddRange(shape.GetRange(1, shape.Count - 1));
                        return result;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Joins point lists into shapes and map various fillStyleIndexes to these shapes.
        /// </summary>
        /// <remarks>
        /// Algorithm for merging point lists into shapes:
        /// <br></br>
        /// Pick an unused segment. If it's already closed (start point equals
        /// end point), convert it to the SVG path format.<br></br>
        /// Otherwise, if it's open, randomly append segments (making sure to
        /// match start and end points) until:<br></br>
        /// 1. The segment is closed. Convert and start over with a new,
        /// unused segment.<br></br>
        /// 2. The segment intersects with itself (i.e. the current end point
        /// equals the end point of a previous segment). Backtrack.<br></br>
        /// 3. There are no more valid segments. Backtrack.
        /// </remarks>
        /// <param name="pointLists">A list of tuples consisting of point list associated with a
        /// respective fillStyleIndex.</param>
        /// <returns>A dictionary that maps fillStyleIndexes with shapes (list of point lists)</returns>
        /// <exception cref="Exception">Thrown when a shape could not be created.</exception>
        public static Dictionary<int, List<List<string>>> ConvertPointListsToShapes(List<(List<string>, int?)> pointLists)
        {
            // {fillStyleIndex: {origin point: [point list, ...], ...}, ...}
            // graph = defaultdict(lambda: defaultdict(list))
            // For any key, default value is dictionary whose default value is an empty list
            Dictionary<int, Dictionary<string, List<List<string>>>> graph = new Dictionary<int, Dictionary<string, List<List<string>>>>();

            // {fillStyleIndex: [shape point list, ...], ...}
            // shapes = defaultdict(list)
            // For any key, default value is empty list
            Dictionary<int, List<List<string>>> shapes = new Dictionary<int, List<List<string>>>();

            // Add open point lists into graph
            foreach ((List<string>, int?) tuple in pointLists)
            {
                List<string> pointList = tuple.Item1;
                int fillIndex = (int)tuple.Item2!;

                // Point list is already a closed shape, so just associate it with its
                // fillStyle index
                if (pointList[0] == pointList[pointList.Count - 1])
                {
                    // Either add to existing list of lists, or create new one
                    if(!shapes.TryGetValue(fillIndex, out var shapePointLists))
                    {
                        shapePointLists = new List<List<string>>();
                        shapes[fillIndex] = shapePointLists;
                    }
                    shapePointLists.Add(pointList);
                }
                else
                {
                    if(!graph.TryGetValue(fillIndex, out var fillGraph))
                    {
                        fillGraph = new Dictionary<string, List<List<string>>>();
                        graph[fillIndex] = fillGraph;
                    }

                    // At this point- key has empty Dictionary or existing Dictionary
                    string originPoint = pointList[0];
                    if(!fillGraph.TryGetValue(originPoint, out var originPointLists))
                    {
                        originPointLists = new List<List<string>>();
                        fillGraph[originPoint] = originPointLists;
                    }
                    originPointLists.Add(pointList);
                }
            }

            // For each fill style ID, pick a random origin and join point lists into
            // shapes with Walk() until we're done.
            foreach(var (fillIndex, fillGraph) in  graph)
            {
                foreach(string originPoint in fillGraph.Keys)
                {
                    // As we are popping off the top element, we have to check if list of lists
                    // is empty rather than null
                    while(fillGraph[originPoint].Count != 0)
                    {
                        // Pop off pointList from originPointLists
                        List<string> pointList = fillGraph[originPoint][0];
                        fillGraph[originPoint].RemoveAt(0);
                        string currentPoint = pointList[pointList.Count - 1];

                        HashSet<string> visited = new HashSet<string>() { originPoint, currentPoint};

                        List<string>? shape = Walk(currentPoint, visited, originPoint, fillGraph);
                        if(shape == null)
                        {
                            throw new Exception("Failed to build shape");
                        }

                        // Either add to existing list of shape point lists, or create new one
                        if (!shapes.TryGetValue(fillIndex, out var shapePointLists))
                        {
                            shapePointLists = new List<List<string>>();
                            shapes[fillIndex] = shapePointLists;
                        }

                        pointList.AddRange(shape.GetRange(1, shape.Count - 1));
                        shapePointLists.Add(pointList);
                    }
                }
            }

            return shapes;
        }

        // edges element = refers to group of Edge elements associated with DOMShape
        // "edges" attribute = refers to "edges" string of coordinates associated with Edge element

        /// <summary>
        /// Calculates and returns the point lists and bounding boxes of XFL edges element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Idea of this method is that pairs each fillStyle/strokeStyle with its respective point lists and
        /// bounding box.
        /// </para>
        /// <para>
        /// For point lists representing Edges with fills, they are merged into shapes first (list of point lists)
        /// and then their bounding box is calculated.
        /// This (list of point lists, bounding box) is associatd with a fillStyle index.
        /// For point lists representing Edges with strokes, their point lists are added to a list, calculating the proper
        /// bounding box after each addition.
        /// This (list of point lists, bounding box) is associatd with a strokeStyle index.
        /// </para>
        /// <para>
        /// For each fill/strokeStyle index, create the proper SVG path element (setting "d" attribute to
        /// SVG path string, setting "fill" attributes to the properly converted FillStyle/StrokeStyles
        /// </para>
        /// </remarks>
        /// <param name="edgesElement">The edges element of a DOMShape element.</param>
        /// <param name="strokeStyleAttributes">The SVG attributes of each strokeStyle element of a DOMShape.</param>
        /// <returns>A tuple consisting of Dictionary of fillStyle indexes mapped to their respective pointLists and bounding box,
        /// a Dictionary of strokeStyle indexes mapped to their respective pointLists and bounding box.</returns>
        public static (Dictionary<int, (List<List<string>>, Rectangle?)>,
            Dictionary<int, (List<List<string>>, Rectangle?)>) ConvertEdgesToShapes(List<Edge> edgesElement,
            Dictionary<string, Dictionary<string, string>> strokeStylesAttributes)
        {
            // When associating point lists to their fillstyle/strokestyle, using their
            // index" attribute- NOT their index in lists
            // fillStyle/strokeStyle index in CSXFL Shape list: XFL "index" attribute - 1

            // List of point lists with their associated fillStyle stored as pairs
            List<(List<string>, int?)> fillEdges = new List<(List<string>, int?)>();
            // Maps strokeStyle id to associated pointLists
            // For any key, default value is list
            Dictionary<int, List<List<string>>> strokePointLists = new Dictionary<int, List<List<string>>>();
            // For any key, default value is null
            Dictionary<int, Rectangle?> fillBoxes = new Dictionary<int, Rectangle?>();
            // For any key, default value is null
            Dictionary<int, Rectangle?> strokeBoxes = new Dictionary<int, Rectangle?>();

            foreach (Edge edgeElement in edgesElement)
            {
                // Get "edges" string, fill styles, and stroke styles of a specific Edge
                string? edgesString = edgeElement.Edges;
                int? fillStyleLeftIndex = edgeElement.FillStyle0;
                int? fillStyleRightIndex = edgeElement.FillStyle1;
                int? strokeStyleIndex = edgeElement.StrokeStyle;

                IEnumerable<(List<string>, Rectangle?)> edgesPointLists = (edgesString is null) 
                    ? new List<(List<string>, Rectangle?)>() : ConvertEdgeFormatToPointLists(edgesString);

                // Associate point lists with appropriate fillStyle/strokeStyle index in tuples
                // Map bounding boxes to proper fillStyle/strokeStyle indexes

                foreach ((List<string>, Rectangle?) pointListTuple in edgesPointLists)
                {
                    List<string> tuplePointList = pointListTuple.Item1;
                    Rectangle? tupleBoundingBox = pointListTuple.Item2;

                    if (fillStyleLeftIndex != null)
                    {

                        (List<string>, int?) tupleToAdd = new(pointListTuple.Item1, fillStyleLeftIndex);
                        fillEdges.Add(tupleToAdd);

                        if(!fillBoxes.TryGetValue((int)fillStyleLeftIndex, out Rectangle? existingBox))
                        {
                            existingBox = null;
                            fillBoxes[(int)fillStyleLeftIndex] = existingBox;
                        }

                        fillBoxes[(int)fillStyleLeftIndex] = MergeBoundingBoxes(existingBox, tupleBoundingBox);
                    }

                    if (fillStyleRightIndex != null)
                    {
                        // Reverse point lists so that fill is always to the left
                        // Python code does not change original pointList, so get reverse of Enumerable
                        // and convert that to a list
                        List<string> reversedList = tuplePointList.AsEnumerable().Reverse().ToList();
                        (List<string>, int?) tupleToAdd = new(reversedList, fillStyleRightIndex);
                        fillEdges.Add(tupleToAdd);

                        if (!fillBoxes.TryGetValue((int)fillStyleRightIndex, out Rectangle? existingBox))
                        {
                            existingBox = null;
                            fillBoxes[(int)fillStyleRightIndex] = existingBox;
                        }

                        fillBoxes[(int)fillStyleRightIndex] = MergeBoundingBoxes(existingBox, tupleBoundingBox);
                    }

                    // Do I need to check if strokeStyle exists? (Outside of checking for null)
                    // Is there a scenario where an Edge element references a strokeStyle that is not in the
                    // strokes element of the DOMShape that the said Edge is a part of?

                    // If strokeStyle exists for Edge, process immediately as no shape needs to be joined
                    if (strokeStyleIndex != null)
                    {
                        // Check if strokeStyle has associated SVG attributes created for it
                        // Do I really need to check this here? Seems a bit out of place
                        string? index = strokeStyleIndex.ToString();

                        if (index != null && strokeStylesAttributes.ContainsKey(index))
                        {
                            // defaultdict(list)- For any key, default value is empty list
                            // Is used to create a list of size 1 when first creating stroke path list
                            // Idea- ensuring that list exists for key (either existing one or an empty one)

                            if (!strokePointLists.TryGetValue((int)strokeStyleIndex, out var strokePathList))
                            {
                                // Setting this reference so item can be added to it afterwards
                                strokePathList = new List<List<string>>();
                                strokePointLists[(int)strokeStyleIndex] = strokePathList;
                            }
                            strokePathList.Add(tuplePointList);

                            if (!strokeBoxes.TryGetValue((int)strokeStyleIndex, out Rectangle? existingBox))
                            {
                                existingBox = null;
                                fillBoxes[(int)strokeStyleIndex] = existingBox;
                            }

                            fillBoxes[(int)strokeStyleIndex] = MergeBoundingBoxes(existingBox, tupleBoundingBox);
                        }
                    }
                }
            }

            Dictionary<int, List<List<string>>> shapes = ConvertPointListsToShapes(fillEdges);
            // At this point, we have fillStyle indexes associated with various shapes
            // (a list of point lists) and strokeStyle indexes associated with a list of point lists.
            // Now package the final point lists and bounding box in tuple for shape class to use to build proper SVG element

            Dictionary<int, (List<List<string>>, Rectangle?)> fillResults = new Dictionary<int, (List<List<string>>, Rectangle?)>();
            Dictionary<int, (List<List<string>>, Rectangle?)> strokeResults = new Dictionary<int, (List<List<string>>, Rectangle?)>();

            foreach(var fillShapeTuple in shapes)
            {
                fillResults[fillShapeTuple.Key] = (fillShapeTuple.Value, fillBoxes[fillShapeTuple.Key]);
            }

            foreach (var strokeTuple in shapes)
            {
                strokeResults[strokeTuple.Key] = (strokeTuple.Value, strokeBoxes[strokeTuple.Key]);
            }

            return (fillResults, strokeResults);
        }
    }
}
