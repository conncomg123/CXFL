using CsXFL;
using SkiaRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SkiaRendering
{
    /// <summary>
    /// Utils for converting CSXFL Fill/StrokeStyle elements into their equivalent SVG elements.
    /// </summary>
    class StyleUtils
    {
        /// <summary>
        /// Parses CSXFL FillStyle element into SVG style attributes and any extra defs.
        /// </summary>
        /// <param name="fillStyle">CSXFL FillStyle element whose SVG attributes/defs elements
        /// will be parsed from.</param>
        /// <returns>A tuple: Dictionary of SVG style attributes, Dictionary of any SVG elements
        /// that should go into defs.</returns>
        /// <exception cref="Exception">When there is a fillStyle that a RadialGradient or one
        /// is not recognized.</exception>
        public static (Dictionary<string, string>, Dictionary<string, XElement>) ParseFillStyle(FillStyle fillStyle)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();

            if(fillStyle.SolidColor != null)
            {
                attributes["fill"] = fillStyle.SolidColor.Color;
                attributes["fill-opacity"] = fillStyle.SolidColor.Alpha.ToString();
            }
            else if(fillStyle.LinearGradient != null)
            {
                XElement gradientElement = GradientUtils.ConvertLinearGradientToSVG(fillStyle.LinearGradient);
                attributes["fill"] = $"url(#{gradientElement.Attribute("id")!.Value}";
                extraDefElements[gradientElement.Attribute("id")!.Value] = gradientElement;
            }
            else if(fillStyle.RadialGradient != null)
            {
                // TODO: Support RadialGradient
                throw new Exception("RadialGradient is not supported yet!");
            }
            else
            {
                // Should print warning rather than throw error?- ask Soundman
                throw new Exception($"Unknown fill style: {fillStyle.ToString()}");
            }

            return (attributes, extraDefElements);
        }

        /// <summary>
        /// Parses CSXFL FillStyle element into SVG style attributes.
        /// </summary>
        /// <param name="strokeStyle">CSXFL StrokeStyle element whose SVG attributes
        /// will be parsed from.</param>
        /// <returns>A Dictionary of SVG style attributes.</returns>
        public static Dictionary<string, string> ParseStrokeStyle(StrokeStyle strokeStyle)
        {
            if(strokeStyle.Stroke == null)
            {
                throw new Exception($"Unknown stroke style: {strokeStyle.ToString()}");
            }

            if(strokeStyle.Stroke.ScaleMode != "normal")
            {
                throw new Exception($"Unknown 'scaleMode' value: {strokeStyle.ToString()}");
            }

            // Need to check if this should be the default value that should be set for SVG stroke-linecap
            // attribute
            string capsProperty = strokeStyle.Stroke.Caps;

            if(capsProperty == "none")
            {
                capsProperty = "butt";
            }

            Dictionary<string, string> attributes = new Dictionary<string, string>()
            {
                {"stroke-linecap", capsProperty },
                {"stroke-width", strokeStyle.Stroke.Weight.ToString() },
                {"stroke-linejoin", strokeStyle.Stroke.Joints },
                {"fill", "none" }
            };

            // Are we always going to try to get solidStyle even if Stroke isn't
            // SolidStroke?
            SolidStroke solidStroke = (SolidStroke) strokeStyle.Stroke;

            if(solidStroke.SolidStyle == "hairline")
            {
                // A hairline solidStyle overrides the "weight" XFL attribute.
                attributes["stroke-width"] = "0.05";
            }
            else if(solidStroke.SolidStyle != string.Empty)
            {
                throw new Exception($"Unknown 'solidStyle' value: {solidStroke.ToString()}");
            }

            if(strokeStyle.Stroke.RadialGradient != null)
            {
                // TODO: add support for RadialGradient
                throw new Exception("RadialGradient is not supported yet!");
            }
            else if(strokeStyle.Stroke.SolidColor == null)
            {
                throw new Exception($"Unknown stroke fill: {strokeStyle.ToString()}");
            }

            attributes["stroke"] = strokeStyle.Stroke.SolidColor.Color;
            attributes["fill-opacity"] = strokeStyle.Stroke.SolidColor.Alpha.ToString();

            if (attributes["stroke-linejoin"] == "miter")
            {
                attributes["stroke-miterlimit"] = strokeStyle.Stroke.MiterLimit.ToString();
            }

            return attributes;
        }
    }
}
