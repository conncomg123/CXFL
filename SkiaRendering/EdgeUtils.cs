using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkiaRendering
{
    // Logic and Documentation largely taken from [https://github.com/PluieElectrique/xfl2svg]
    internal class EdgeUtils
    {
        //Main Idea:
        // In XFL format, anything that is drawn can either be generally represented
        // as shapes (DOMShape elements) or symbols (DOMSymbolInstance elements) that in turn refers to shapes
        // A DOMShape is made of two elements- fills and edges
        // fills element- indicate the color, stroke style, fill style that will fill the shape
        // edges element- contain Edge elements-
        // "edges" attribute- string of commands and coordinates that indicate shape outline
        // outline is broken into pieces- here will be called "segments"
        // This outline will then be filled in using fills' elements
        
        // Process: string -> list of points -> SVG path -> render SVG image


        //"edges" attribute string format:
        // First gives command type, then follows it with n coordinates
        // Commands- !- moveto, /- lineto, |- lineto, [- quadto, ]- quadto

        // Coordinates can either be decimal numbers or signed 32-number in Hex
        // Hex Coords are denoted by a # before them

        // "selects" in the format S[1-7] might be present as well in "moveto" commands- these are hints used
        // by Animate for noting selections (n=bitmask, 1:fillStyle0, 2:fillStyle1, 4:stroke)
        // When parsing Coords, they should be ignored (done with negative lookbehind)
        // Cubics are omitted as they only appear in "cubics" attribute and are only hints for Animate

        //@ notes regex string
        // Whitespace is automatically ignored through matches
        // Negative lookbehind is used to ignore "select" (?<!S)
        private const string EDGE_REGEX = @"[!|/[\]]|(?<!S)-?\d+(?:\.\d+)?|\#[A-Z0-9]+\.[A-Z0-9]+";

        private static Regex edgeTokenizer = new Regex(EDGE_REGEX, RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Parses and converts a coordinate in the "edges" attribute string.
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
        /// Converts the XFL "edges" attribute string into a list of points.
        /// </summary>
        /// <param name="edges">The "edges" attribute of an Edge XFL element.</param>
        /// <returns>A list of string points in "x y" format for each segement of "edges" attribute.</returns>
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
                    // Control point is currPoint, dest is prevPoint.
                    pointList.Add($"[{currPoint}]");
                    prevPoint = nextPoint();
                    pointList.Add($"{prevPoint}]");
                }
            }

            yield return pointList;
        }

        /// <summary>
        /// Converts a point list into the SVG path element format.
        /// </summary>
        /// <param name="pointList">The point list that is being converted.</param>
        /// <returns>A SVG path element as a string.</returns>
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
                // multiple times in a row
                if(currentCommand != lastCommand)
                {
                    svgPath.Add(currentPoint);
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
                //If starting point == ending point i.e completes a closed shape/stroke,
                //Add Z command
                svgPath.Add("Z");
            }

            //Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
        }
    }
}
