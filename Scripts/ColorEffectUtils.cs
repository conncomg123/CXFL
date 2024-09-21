using CsXFL;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Scripts
{
    internal class ColorEffectUtils
    {
        private const string EDGE_REGEX = @"#[A-Za-z0-9]{6}";

        /// <summary>
        /// Creates a Unique ID for ColorEffect.
        /// <remarks>
        /// This is used to access the ColorEffects SVG element(s) in defs.
        /// </remarks>
        /// </summary>
        /// <returns>Unique ID to dedup SVG elements in defs element.</returns>
        public static string GenerateUniqueId()
        {
            int randomNumber = Random.Shared.Next();
            return $"Gradient_{(randomNumber.GetHashCode() & 0xFFFFFFFF):x8}";
        }

        /// <summary>
        /// Create an SVG filter element from a ColorEffect.
        /// </summary>
        /// <param name="gradient"></param>
        /// <returns></returns>
        public static XElement ConvertColorEffectToSVG(Color colorEffect)
        {
            List<(double, double, double, double)> multiplierList = GetMultipliers(colorEffect);

            (double, double, double, double) multiplier = multiplierList[0];
            (double, double, double, double) offset = multiplierList[1];

            string matrix = string.Format(
                "{0} 0 0 0 {4} " +
                "0 {1} 0 0 {5} " +
                "0 0 {2} 0 {6} " +
                "0 0 0 {3} {7}",
                multiplier.Item1, multiplier.Item2, multiplier.Item3, multiplier.Item4,
                offset.Item1, offset.Item2, offset.Item3, offset.Item4
            );

            XElement element = new XElement(
                "filter",
                new XAttribute("id", GenerateUniqueId()),
                new XAttribute("x", "-20%"),
                new XAttribute("y", "-20%"),
                new XAttribute("width", "140%"),
                new XAttribute("height", "140%"),
                new XAttribute("color-interpolation-filters", "sRGB")
            );

            XElement feColorMatrix = new XElement(
                "feColorMatrix",
                new XAttribute("in", "SourceGraphic"),
                new XAttribute("type", "matrix"),
                new XAttribute("values", matrix)
            );

            element.Add(feColorMatrix);

            return element;
        }

        /// <summary>
        /// Checks if ColorEffect does nothing.
        /// </summary>
        /// <param name="colorElement"></param>
        /// <returns></returns>
        public static bool IsIdentify(Color colorEffect)
        {
            List<(double, double, double, double)> multiplierList = GetMultipliers(colorEffect);

            (double, double, double, double) multiplier = multiplierList[0];
            (double, double, double, double) offset = multiplierList[1];

            return multiplier == (1, 1, 1, 1) && offset == (0, 0, 0, 0);
        }

        public static List<(double, double, double, double)> GetMultipliers(Color colorEffect)
        {
            // Create the proper multipler and offset tuples based on multiplier values
            (double, double, double, double) multiplier;
            (double, double, double, double) offset;

            if (colorEffect.AlphaMultiplier != 0)
            {
                multiplier = (1, 1, 1, colorEffect.AlphaMultiplier);
                offset = (0, 0, 0, 0);
            }
            // Brightness: linearly interpolate towards black or white
            else if (colorEffect.Brightness != 0)
            {
                double brightness = colorEffect.Brightness;
                if (brightness < 0)
                {
                    // Linearly interpolate towards black
                    multiplier = (1 + brightness, 1 + brightness, 1 + brightness, 1);
                    offset = (0, 0, 0, 0);
                }
                else
                {
                    // Linearly interpolate towards white
                    multiplier = (1 - brightness, 1 - brightness, 1 - brightness, 1);
                    offset = (brightness, brightness, brightness, 0);
                }
            }
            // Tint: linearly interpolate between the original color and a tint color
            else if (colorEffect.TintMultiplier != 0 || colorEffect.TintColor != null)
            {
                // color * (1 - tint_multiplier) + tint_color * tint_multiplier
                double tintMultiplier = colorEffect.TintMultiplier;
                multiplier = (
                    1 - tintMultiplier,
                    1 - tintMultiplier,
                    1 - tintMultiplier,
                    1);

                string tintColor = colorEffect.TintColor;
                if (Regex.IsMatch(tintColor, EDGE_REGEX))
                {
                    new Exception($"Color isn't in hex format: {tintColor}");
                }

                offset = (
                    tintMultiplier * int.Parse(tintColor.Substring(1, 3), NumberStyles.HexNumber) / 255f,
                    tintMultiplier * int.Parse(tintColor.Substring(3, 5), NumberStyles.HexNumber) / 255f,
                    tintMultiplier * int.Parse(tintColor.Substring(5, 7), NumberStyles.HexNumber) / 255f,
                    0);
            }
            // Advanced: multiply and offset each channel
            // I think in &ing against the attribute dictionary for its existance, 
            else
            {
                // Multipliers are in [-1, 1]
                multiplier = (
                    colorEffect.RedMultiplier,
                    colorEffect.GreenMultiplier,
                    colorEffect.BlueMultiplier,
                    colorEffect.AlphaMultiplier);

                // Offsets are in [-255, 255]
                offset = (
                    colorEffect.RedOffset / 255,
                    colorEffect.GreenOffset / 255,
                    colorEffect.BlueOffset / 255,
                    colorEffect.AlphaOffset / 255);
            }

            List<(double, double, double, double)> multipliersList = new List<(double, double, double, double)>
            {
                multiplier, offset
            };
            return multipliersList;
        }
    }
}
