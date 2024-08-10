using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkiaRendering
{
    internal class ShapeUtils
    {
        //"edge" Attribute String Format Info
        //First give command type, then follow it with proper number of points
        //Commands- !- moveTo, /- lineTo, |- lineTo, [- curveTo(quadratic), ]- curveTo(quadratic)
        //Points can either be decimal numbers or signed 32-number in Hex
        //Hex numbers are denoted by a # before them

        //"select" in the format S[1-7] might be present as well- these are hints used
        //by Animate for noting selections ((n=bitmask, 1:fillStyle0, 2:fillStyle1, 4:stroke)
        //When parsing points, they should be ignored (done with negative lookbehind)
        //Cubics are omitted as they only appear in "cubics" attribute and are only hints for Animate

        //@ notes regex string
        //Whitespace is automatically ignored through matches
        //Negative lookbehind is used to ignore "select" (?<!S)
        private const string EDGE_REGEX = @"[!|/[\]]|(?<!S)-?\d+(?:\.\d+)?|\#[A-Z0-9]+\.[A-Z0-9]+";

        private static Regex edgeTokenizer = new Regex(EDGE_REGEX, RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Parses and converts a number in the "edges" attribute string.
        /// </summary>
        /// <param name="numberString">The number in "edges" string being parsed.</param>
        /// <returns>The converted and scaled number.</returns>
        public static float ParseNumber(string numberString)
        {
            // Check if the number is signed and 32-bit fixed-point number in hex
            if (numberString[0] == '#')
            {
                // Split the number into the integer and fractional parts
                string[] parts = numberString.Substring(1).Split('.');
                // Pad the integer part to 8 digits
                string hexNumberString = string.Format("{0:X8}{1:X2}", Convert.ToInt32(parts[0], 16), Convert.ToInt32(parts[1], 16));
                // Convert the hex number to a signed 32-bit integer
                int numberInt = int.Parse(hexNumberString, System.Globalization.NumberStyles.HexNumber);
                // Convert the number to its decimal equivalent and scale it down by 256 and 20
                return (numberInt / 256f) / 20f;
            }
            else
            {
                // The number is a decimal number. Scale it down by 20.
                return float.Parse(numberString) / 20f;
            }
        }

        /// <summary>
        /// Converts the XFL "edges" attribute string format into a list of points.
        /// </summary>
        /// <param name="edges">The "edges" attribute of an Edge XFL element.</param>
        /// <returns>A list of points (string numbers) for each segement of "edges" attribute.</returns>
        public static IEnumerable<List<string>> EdgeFormatToPointLists(string edges)
        {
            //As MatchCollection was written before .NET 2, it uses IEnumerable for iteration rather
            //than IEnumerable<T>, meaning it defaults to an enumerable of objects.
            //To get enumerable of Matches, have to explicity type cast enumerable as Match
            
            var testing = edgeTokenizer.Matches(edges).Cast<Match>().Select(currentMatch => currentMatch.Value);
            
            return null;
        }
    }
}
