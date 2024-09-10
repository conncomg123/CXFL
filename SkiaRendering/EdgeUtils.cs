using CsXFL;
using SkiaSharp;
using System.Text.RegularExpressions;
using System.Linq;

namespace SkiaRendering
{
    // Logic and Documentation largely taken from [https://github.com/PluieElectrique/xfl2svg]
    // One aspect that had to be accounted for is the use of defaultdict, which gives default
    // values to keys. To account for that, I used TryGetValue() and explicity set values
    internal class EdgeUtils
    {
        //Main Idea:
        // In XFL format, anything that is drawn can either be generally represented
        // as shapes (<DOMShape> elements) or symbols (<DOMSymbolInstance> elements) that in turn refers to shapes
        // A <DOMShape> is made of two elements- <fills> and <edges>
        // <fills> element- indicate the color, stroke style, fill style that will fill the shape
        // <edges> element- contain <Edge> elements-
        // "edges" attribute- string of commands and coordinates that indicate shape outline
        
        // outline is broken into pieces called segments- represented here as "point lists"
        // This outline will then be filled in using fills' elements 
        // Process: string -> list of points -> SVG path elements -> render SVG as bitmap


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
            if(numberString[0] == '#')
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

        //Point Format: "x y" string, "quadto" command control (start) point- "[x y]"

        //Point List Format:
        // Point List Example [A, B, [C], D, E] where letters are points
        // First point is always destination of "moveto" command. Subsequent points are "lineto" command destinations
        // "[x y]" point- control point of a quadratic Bézier curve and the following point is the destination of the curve
        // in the standard point format

        /// <summary>
        /// Converts an XML Edge element's "edges" string into a list of points (segments).
        /// </summary>
        /// <param name="edges">The "edges" attribute of an Edge XFL element.</param>
        /// <returns>An enumerable of lists of string points in "x y" format- each list of points is a segment of the "edges" attribute.</returns>
        public static IEnumerable<List<string>> ConvertEdgeFormatToPointLists(string edges)
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
            Func<string> nextPoint = () =>
            {
                matchTokens.MoveNext();
                string x = ParseNumber(matchTokens.Current).ToString();
                matchTokens.MoveNext();
                string y = ParseNumber(matchTokens.Current).ToString();
                return $"{x} {y}";
            };

            string prevPoint = nextPoint();
            List<string> pointList = new List<string> { prevPoint };

            while(matchTokens.MoveNext())
            {
                string command = matchTokens.Current;
                string currPoint = nextPoint();

                // "moveto" command
                if(command == "!")
                {
                    // If a move command doesn't change the current point, ignore it.
                    if(currPoint != prevPoint)
                    {
                        // Otherwise, a new segment is starting, so we must yield the current point list and begin a new one.
                        yield return pointList;
                        pointList = new List<string> { currPoint };
                        prevPoint = currPoint;
                    }
                }
                // "lineto" command
                else if(command == "|" || command == "/")
                {
                    pointList.Add(currPoint);
                    prevPoint = currPoint;
                }
                // "quadto" command
                else if(command == "[" || command == "]")
                {
                    // Previous point (the point before this in list) is the start of the quadratic Bézier curve
                    // currPoint is control point- this is denoted as a point string surrounded by []
                    // nextPoint() is destination point of curve
                    pointList.Add($"[{currPoint}]");
                    prevPoint = nextPoint();
                    pointList.Add($"{prevPoint}");
                }
            }

            yield return pointList;
        }

        /// <summary>
        /// Converts a point list into the SVG path format.
        /// </summary>
        /// <remarks>
        /// This method converts a point list into the "d" attribute of a path element,
        /// NOT into an entire path element itself (with proper opening and closing path tags,
        /// d=, style= etc).
        /// </remarks>
        /// <param name="pointList">The point list that is being converted.</param>
        /// <returns>The equivalent "d" string for the given point list.</returns>
        public static string ConvertPointListToPathFormat(List<string> pointList)
        {
            // Using iterator to match previous method as well as Python implementation
            IEnumerator<string> pointEnumerator = pointList.GetEnumerator();
            // Start SVG path with M command and first point
            pointEnumerator.MoveNext();

            List<string> svgPath = new List<string> { "M", pointEnumerator.Current};
            string lastCommand = "M";

            while(pointEnumerator.MoveNext())
            {
                string currentPoint = pointEnumerator.Current;
                string currentCommand = currentPoint.Contains('[') && currentPoint.Contains(']') ? "Q" : "L";
                
                // SVG path element allows us to omit command letter if same command is used
                // multiple times in a row, so only add it to svgPath string if new command is found
                if(currentCommand != lastCommand)
                {
                    svgPath.Add(currentCommand);
                    lastCommand = currentCommand;
                }

                if(currentCommand == "Q")
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
            if (pointList[0] == pointList[pointList.Count - 1])
            {
                // If starting point == ending point i.e completes a closed shape/stroke,
                // Add Z command
                svgPath.Add("Z");
            }

            // Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
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
        /// Converts XFL edges element into SVG path elements.
        /// </summary>
        /// <param name="edgesElement">The edges element of a DOMShape element.</param>
        /// <param name="fillStyles">The fills element of the DOMShape element.</param>
        /// <param name="strokeStyles">The strokes element of the DOMShape element.</param>
        public static (List<SKPath>, List<SKPath>) ConvertEdgesToSvgPath(List<Edge> edgesElement,
            List<FillStyle> fillStyles, List<StrokeStyle> strokeStyles)
        {
            // List of point lists with their associated fillStyle stored as pairs
            // Used syntax sugar version of new as variable type is very verbose
            List<(List<string>, int?)> fillEdges = new();

            // Should StrokeStyle object be used as key or just its index?
            // StrokePaths = refers to all converted SVG path strings associated with a strokeStyle
            Dictionary<int, List<string>> strokePaths = new Dictionary<int, List<string>>();

            foreach(Edge edgeElement in edgesElement)
            {
                // Get "edges" string, fill styles, and stroke styles of a specific Edge
                string edgesString = edgeElement.Edges!;
                int? fillStyleLeftIndex = edgeElement.FillStyle0;
                int? fillStyleRightIndex = edgeElement.FillStyle1;
                int? strokeStyleIndex = edgeElement.StrokeStyle;

                IEnumerable<List<string>> edgesPointLists = ConvertEdgeFormatToPointLists(edgesString);
                foreach(List<string> pointList in edgesPointLists)
                {
                    if(fillStyleLeftIndex != null)
                    {
                        (List<string>, int?) tupleToAdd = new(pointList,  fillStyleLeftIndex);
                        fillEdges.Add(tupleToAdd);
                    }

                    if(fillStyleRightIndex != null)
                    {
                        // First reverse point list in order to fill it from the left, then add it
                        // Python code does not change original pointList, so get reverse of Enumerable
                        // and covert that to a list
                        List<string> reversedList = pointList.AsEnumerable().Reverse().ToList();
                        (List<string>, int?) tupleToAdd = new(reversedList, fillStyleRightIndex);
                        fillEdges.Add(tupleToAdd);
                    }

                    // strokeStyle index in CSXFL Shape Stroke list (DOMShape): XFL "index" - 1
                    // Do I need to check if strokeStyle exists? (Outside of checking for null)
                    // Is there a scenario where an Edge element references a strokeStyle that is not in the
                    // strokes element of the DOMShape that the said Edge is a part of?
                    // As a result, couldn't I just get the StrokeStyle from strokeStyles using "index" - 1?

                    // If strokeStyle exists for Edge, convert immediately as no shape needs to be joined
                    if (strokeStyleIndex != null)
                    {
                        // Should list be replaced with dictionary- figured it would make more sense
                        StrokeStyle? foundStroke = strokeStyles.FirstOrDefault((strokeObject) =>
                        {
                            return strokeObject.Index == strokeStyleIndex;
                        });

                        if(foundStroke != null)
                        {
                            // First get converted path format for this Edge, then add it to
                            // associated strokeStyle
                            string svgPathString = ConvertPointListToPathFormat(pointList);

                            // defaultdict(list)- For any key, default value is empty list
                            // Is used to create a list of size 1 when first creating stroke path list

                            // Idea- ensuring that list exists for key (either existing one or an empty one)
                            if (!strokePaths.TryGetValue(foundStroke.Index, out var strokePathList))
                            {
                                // Setting this reference so item can be added to it afterwards
                                strokePathList = new List<string>();
                                strokePaths[foundStroke.Index] = strokePathList;
                            }

                            strokePathList.Add(svgPathString);
                        }
                    }
                }
            }

            List<SKPath> filledPaths = new List<SKPath>();
            List<SKPath> strokedPaths = new List<SKPath>();

            Dictionary<int, List<List<string>>> shapes = ConvertPointListsToShapes(fillEdges);
            foreach(var (fillIndex, pointLists) in shapes)
            {
                string svgPathString = string.Join(" ", pointLists.ConvertAll(ConvertPointListToPathFormat));
                SKPath newSKPath = SKPath.ParseSvgPathData(svgPathString);
                filledPaths.Add(newSKPath);
            }

            foreach(var (strokeIndex, pathData) in strokePaths)
            {
                string svgPathString = string.Join(" ", pathData);
                SKPath newSKPath = SKPath.ParseSvgPathData(svgPathString);
                strokedPaths.Add(newSKPath);
            }

            return (filledPaths, strokedPaths);
        }
    }
}
