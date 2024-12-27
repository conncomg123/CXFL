using CsXFL;
using System.Text.RegularExpressions;

namespace Rendering
{

    /// <summary>
    /// Utils for converting XFL edges into its equivalent SVG elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Base logic and documentation was largely taken from PluieElectrique's
    /// <see href="https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/edge.py">edge.py</see>
    /// and SynthBot-Anon's updated version of
    /// <see href="https://github.com/synthbot-anon/PluieElectrique-xfl2svg/blob/radial-gradient-take2/xfl2svg/shape/edge.py">edge.py.</see>
    /// </para>
    /// <para>
    /// One aspect that had to be accounted in translating the code was its use of defaultdict, which assigns a default
    /// value to a key if it is not initially found in the dictionary. As .NET does not have a native data structure that
    /// behaves in this same manner, the extension method GetValueOrDefault() was used in its stead.
    /// </para>
    /// <para>
    /// In Animate, graphics that are drawn can be represented as either filled shapes (fills) or stroked paths (strokes),
    /// with fills representing the inside of a shape and strokes representing the shape's boundary (same definitions
    /// as in SVG). Both of these are defined by their outline which Animate breaks into various pieces or
    /// XFL Edge elements.
    /// </para>
    /// <para>
    /// An XFL Edge may be part up at most two shapes- one being on its left and one on its right. This is determined
    /// by the pressence of the "fillStyle0" (left) and "fillStyle1" (right) attributes, which specify the fill of the
    /// shape on that side. In comparison, a XFL Edge can only be part of up to one stroked path, determined by
    /// the pressence of a "strokeStyle" attribute.
    /// </para>
    /// <para>
    /// Given this, to extract the graphics from the XFL format in preparation to convert them into SVG, we first will
    /// convert the XFL Edge elements into smaller segments we will call pointLists (more about them later).
    /// Each XFL Edge element is broken into multiple segments, each of them inheriting the "fillStyle0",
    /// "fillStyle1", and "strokeStyle" attributes of the XFL Edge element they were taken from.
    /// </para>
    /// <para>
    /// Then, for filled shapes, we join segments of the same fill style and side (left or right) by their start
    /// and end points. For stroked paths, we just collect all segments of the same style.
    /// </para>
    /// <para>
    /// Finally, we convert segments to their equivalent SVG path d attribute strings, put this string as part of a
    /// SVG path element, and then assign the appropriate SVG fill/stroke attributes to said element.
    /// </para>
    /// <para>
    /// In summary: Filled Shape/Stroked Path -> XFL Edge elements -> segments (pointLists) -> SVG path elements
    /// </para>
    /// </remarks>
    /// <seealso href="https://github.com/SasQ/SavageFlask/blob/master/doc/FLA.txt"/>
    internal class EdgeUtilsNew
    {
        //XFL "edges" attribute format:
        // First gives command type, then follows it with n coordinates
        // Commands- !- moveto, /- lineto, |- lineto, [- quadto, ]- quadto

        // Coordinates can either be decimal numbers or signed 32-number in Hex
        // Hex Coords are denoted by a # before them

        // "selects" in the format S[1-7] might be present as well in "moveto" commands- these are hints used
        // by Animate for noting selections (n=bitmask, 1:fillStyle0, 2:fillStyle1, 4:stroke)
        // When parsing Coords, they should be ignored (done with negative lookbehind)
        // Cubic commands are omitted as they only appear in "cubics" attribute and are only hints for Animate

        // Captures command, decimal number, or hex number
        // Whitespace is automatically ignored through matches
        // Negative lookbehind is used to ignore "select" (?<!S)
        private const string EDGE_REGEX = @"[!|/[\]]|(?<!S)-?\d+(?:\.\d+)?|\#[A-Z0-9]+\.[A-Z0-9]+";

        private static Regex edgeTokenizer = new Regex(EDGE_REGEX, RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Parses and converts a coordinate of the XFL "edges" attribute.
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

        // POINTLIST FORMAT INFO
        // Segments (pointLists) in reality are just a section of commands from the XFL Edge's "edges" attribute,
        // which dictates the XFL Edge's outline.

        // In order to join them into shapes, we need some way to reverse
        // segments. This is so we can normalize them so that the filled shape is always on the left.
        // In order to achieve this, we will store these segments as "pointLists".

        // The first command of these pointLists is always a moveTo command. Any command that follows
        // are lineTo commands. If a point is surrounded by [], that point is the control point of a
        // quadTo command (representing a Bezier Curve), and the following point is the curve's destination.

        // Given this format, we can just simply reverse the pointList to get the reverse of the segment.

        /// <summary>
        /// Converts an XFL Edge element into pointLists.
        /// </summary>
        /// <remarks>
        /// As each XFL Edge element can contain multiple segments, this function will yield multiple pointLists.
        /// </remarks>
        /// <param name="edges">The "edges" attribute of an Edge XFL element.</param>
        /// <returns>The XFL Edge broken into pointLists with their associated bounding boxes.</returns>
        /// <exception cref="ArgumentException">The XFL Edge element did not start with a moveto command.</exception>
        public static IEnumerable<(List<string>, Rectangle?)> ConvertEdgeFormatToPointListsNew(string edges)
        {
            // As MatchCollection was written before .NET 2, it uses IEnumerable for iteration rather
            // than IEnumerable<T>, meaning it defaults to an enumerable of objects.
            // To get enumerable of Matches, have to explicity type cast enumerable as Match
            IEnumerator<string> matchTokens = edgeTokenizer.Matches(edges).Cast<Match>().Select(currentMatch => currentMatch.Value).GetEnumerator();

            // Assert that the first token is a moveto command
            if (!matchTokens.MoveNext() || matchTokens.Current != "!")
            {
                throw new ArgumentException("Edge format must start with moveto (!) command");
            }

            // Using local delegate versus function for better performance
            Func<(float, float)> nextPoint = () =>
            {
                matchTokens.MoveNext();
                float x = ParseNumber(matchTokens.Current);
                matchTokens.MoveNext();
                float y = ParseNumber(matchTokens.Current);
                return (x, y);
            };

            (float, float) prevPoint = nextPoint();
            List<string> pointList = new List<string>();
            Rectangle? boundingBox = new Rectangle(prevPoint.Item1, prevPoint.Item2, prevPoint.Item1, prevPoint.Item2);

            while (matchTokens.MoveNext())
            {
                string command = matchTokens.Current;
                (float, float) currPoint = nextPoint();

                // "moveto" command
                if (command == "!")
                {
                    // If a move command doesn't change the current point, ignore it.
                    if (currPoint != prevPoint)
                    {
                        // Otherwise, a new pointList is starting, so we must yield the
                        // current (pointList, boundingBox) and begin a new one.
                        yield return (pointList, boundingBox);

                        pointList = new List<string>();
                        prevPoint = currPoint;
                        boundingBox = null;
                    }
                }
                // "lineto" command
                else if (command == "|" || command == "/")
                {
                    pointList.Add($"{prevPoint.Item1} {prevPoint.Item2}");
                    pointList.Add($"{currPoint.Item1} {currPoint.Item2}");
                    Rectangle? lineBoundingBox = BoxUtils.GetLineBoundingBox(prevPoint, currPoint);

                    boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, lineBoundingBox);
                    prevPoint = currPoint;
                }
                // "quadto" command
                else if (command == "[" || command == "]")
                {
                    // prevPoint is the start of the quadratic Bézier curve
                    // currPoint is control point- this is denoted as a point string surrounded by []
                    // nextPoint() is destination point of curve
                    (float, float) endPoint = nextPoint();
                    pointList.Add($"{prevPoint.Item1} {prevPoint.Item2}");
                    pointList.Add($"[{currPoint.Item1} {currPoint.Item2}]");
                    pointList.Add($"{endPoint.Item1} {endPoint.Item2}");
                    Rectangle? quadraticBoundingBox = BoxUtils.GetQuadraticBoundingBox(prevPoint, currPoint, endPoint);

                    boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, quadraticBoundingBox);
                    prevPoint = endPoint;
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

        public static Dictionary<int, List<List<string>>> ConvertPointListsToShapesNew(List<(List<string>, int?)> pointLists)
        {
            // {fillStyleIndex: {origin point: [pointlist, ...], ...}, ...}
            // graph = defaultdict(lambda: defaultdict(list))
            // For any key, default value is dictionary whose default value is an empty list
            Dictionary<int, Dictionary<string, List<List<string>>>> graph = new Dictionary<int, Dictionary<string, List<List<string>>>>();

            // {fillStyleIndex: [shapepointlist, ...], ...}
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
                    List<List<string>> shapePointLists = shapes.GetValueOrDefault(fillIndex, new List<List<string>>());
                    shapes[fillIndex] = shapePointLists;

                    shapePointLists.Add(pointList);
                }
                else
                {
                    // Either add to existing graph, or create a new one
                    Dictionary<string, List<List<string>>> fillGraph = graph.GetValueOrDefault(fillIndex, new Dictionary<string, List<List<string>>>());
                    graph[fillIndex] = fillGraph;

                    // At this point- key has empty Dictionary or existing Dictionary
                    string originPoint = pointList[0];

                    List<List<string>> originPointLists = fillGraph.GetValueOrDefault(originPoint, new List<List<string>>());
                    fillGraph[originPoint] = originPointLists;

                    originPointLists.Add(pointList);
                }
            }

            // For each fill style ID, pick a random origin and join point lists into
            // shapes with Walk() until we're done.
            foreach (var (fillIndex, fillGraph) in graph)
            {
                foreach (string originPoint in fillGraph.Keys)
                {
                    // As we are popping off the top element, we have to check if list of lists
                    // is empty rather than null
                    while (fillGraph[originPoint].Count != 0)
                    {
                        // Pop off pointList from originPointLists
                        List<string> pointList = fillGraph[originPoint][0];
                        fillGraph[originPoint].RemoveAt(0);
                        string currentPoint = pointList[pointList.Count - 1];

                        HashSet<string> visited = new HashSet<string>() { originPoint, currentPoint };

                        List<string>? shape = Walk(currentPoint, visited, originPoint, fillGraph);
                        if (shape == null)
                        {
                            throw new Exception("Failed to build shape");
                        }

                        // Either add to existing list of shape point lists, or create new one
                        List<List<string>> shapePointLists = shapes.GetValueOrDefault(fillIndex, new List<List<string>>());
                        shapes[fillIndex] = shapePointLists;

                        pointList.AddRange(shape.GetRange(1, shape.Count - 1));
                        shapePointLists.Add(pointList);
                    }
                }
            }

            return shapes;
        }

        private static List<T1> CreateListDeepCopy<T1> (List<T1> elements)
        {
            List<T1> deepCopy = new List<T1>();
            foreach(T1 element in elements)
            {
                deepCopy.Add(element);
            }

            return deepCopy;
        }

        public static (Dictionary<int, (List<List<string>>, Rectangle?)>,
            Dictionary<int, (List<List<string>>, Rectangle?)>) ConvertEdgesToSvgPathNew(List<Edge> edgesElement,
            Dictionary<int, FillStyle> fillStyles,
            Dictionary<int, StrokeStyle> strokeStyles)
        {
            // List of point lists with their associated fillStyle stored as pairs
            // Used syntax sugar version of new as variable type is very verbose
            List<(List<string>, int?)> fillEdges = new();

            Dictionary<int, List<List<string>>> strokePaths = new Dictionary<int, List<List<string>>>();

            // Default value for a key that does not exist is null
            Dictionary<int, Rectangle?> fillBoxes = new Dictionary<int, Rectangle?>();
            Dictionary<int, Rectangle?> strokeBoxes = new Dictionary<int, Rectangle?>();

            foreach(Edge edgeElement in edgesElement)
            {
                // Get "edges" string, fill styles, and stroke styles of a specific Edge
                string? edgesAttribute = edgeElement.Edges;
                int? fillStyleLeftIndex = edgeElement.FillStyle0;
                int? fillStyleRightIndex = edgeElement.FillStyle1;
                int? strokeStyleIndex = edgeElement.StrokeStyle;

                IEnumerable<(List<string>, Rectangle?)> pointListTuples = (edgesAttribute is null) ? new List<(List<string>, Rectangle?)>() : ConvertEdgeFormatToPointListsNew(edgesAttribute);
                foreach((List<string> pointList,  Rectangle? pointListBox) in pointListTuples)
                {

                    if (fillStyleLeftIndex != null)
                    {
                        (List<string>, int?) tupleToAdd = new(pointList, fillStyleLeftIndex);
                        fillEdges.Add(tupleToAdd);

                        // Update this fillStyle's bounding box to include this pointList's
                        // bounding box
                        Rectangle? existingBox = fillBoxes.GetValueOrDefault((int)fillStyleLeftIndex, null);
                        fillBoxes[(int)fillStyleLeftIndex] = BoxUtils.MergeBoundingBoxes(existingBox, pointListBox);

                    }

                    if (fillStyleRightIndex != null)
                    {
                        // First reverse point list in order to fill it from the left, then add it
                        // Python code does not change original pointList, so get reverse of Enumerable
                        // and covert that to a list
                        List<string> reversedList = pointList.AsEnumerable().Reverse().ToList();
                        (List<string>, int?) tupleToAdd = new(reversedList, fillStyleRightIndex);
                        fillEdges.Add(tupleToAdd);

                        // Update this fillStyle's bounding box to include this pointList's bounding box
                        Rectangle? existingBox = fillBoxes.GetValueOrDefault((int)fillStyleRightIndex, null);
                        fillBoxes[(int)fillStyleRightIndex] = BoxUtils.MergeBoundingBoxes(existingBox, pointListBox);
                    }

                    // For strokes, don't need to join any point lists into shapes
                    if (strokeStyleIndex != null && strokeStyles.ContainsKey((int)strokeStyleIndex))
                    {
                        List<string> newCopy = CreateListDeepCopy(pointList);

                        List<List<string>> strokePointLists = strokePaths.GetValueOrDefault((int)strokeStyleIndex, new List<List<string>>());
                        strokePointLists.Add(newCopy);
                        strokePaths[(int)strokeStyleIndex] = strokePointLists;

                        // Update this strokeStyle's bounding box to include this pointList's bounding box
                        Rectangle? existingBox = strokeBoxes.GetValueOrDefault((int)strokeStyleIndex, null);
                        strokeBoxes[(int)strokeStyleIndex] = BoxUtils.MergeBoundingBoxes(existingBox, pointListBox);
                    }
                }
            }

            Dictionary<int, (List<List<string>>, Rectangle?)> fillResult = new();
            Dictionary<int, (List<List<string>>, Rectangle?)> strokeResult = new();

            Dictionary<int, List<List<string>>> shapesAndBoundingBoxes = ConvertPointListsToShapesNew(fillEdges);

            foreach((int fillStyleIndex, List<List<string>> fillShape) in shapesAndBoundingBoxes)
            {
                (List<List<string>>, Rectangle?) tupleToAdd = new(fillShape, fillBoxes[fillStyleIndex]);
                fillResult[fillStyleIndex] = tupleToAdd;
            }

            foreach((int strokeStyleIndex, List<List<string>> strokePath) in strokePaths)
            {
                (List<List<string>>, Rectangle?) tupleToAdd = new(strokePath, strokeBoxes[strokeStyleIndex]);
                strokeResult[strokeStyleIndex] = tupleToAdd;
            }

            return (fillResult, strokeResult);
        }
    }
}