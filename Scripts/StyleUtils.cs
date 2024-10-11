using CsXFL;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rendering
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
        /// <param name="boundingBox">A bounding box that is associated with a CSXFL FillStyle element.
        /// <remarks>
        /// Note that a FillStyle element just describes a type of fill- it can have multiple bounding boxes associated
        /// with it as those boxes would just be filled with that type of fill.
        /// </remarks>
        /// </param>
        /// <returns>A tuple: Dictionary of SVG style attributes, Dictionary of any SVG elements
        /// that should go into defs.</returns>
        /// <exception cref="Exception">When there is a fillStyle that is not recognized.</exception>
        public static (Dictionary<string, string>, Dictionary<string, XElement>) ParseFillStyle(FillStyle fillStyle, Rectangle boundingBox)
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
                attributes["fill"] = $"url(#{gradientElement.Attribute("id")!.Value})";
                extraDefElements[gradientElement.Attribute("id")!.Value] = gradientElement;
            }
            else if(fillStyle.RadialGradient != null)
            {
                XElement gradientElement = GradientUtils.ConvertRadialGradientToSVG(fillStyle.RadialGradient);
                attributes["fill"] = $"url(#{gradientElement.Attribute("id")!.Value})";
                extraDefElements[gradientElement.Attribute("id")!.Value] = gradientElement;
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
        /// <param name="boundingBox">A bounding box that is associated with a CSXFL StrokeStyle element.
        /// /// <remarks>
        /// Note that a StrokeStyle element just describes a type of stroke- it can have multiple bounding boxes associated
        /// with it as those boxes would just be filled with that type of stroke.
        /// </remarks>
        /// </param>
        /// <returns>A tuple: Dictionary of SVG style attributes, Dictionary of any SVG elements
        /// that should go into defs.</returns>
        /// /// <exception cref="Exception">When there is an invalid style value that is found.</exception>
        public static (Dictionary<string, string>, Dictionary<string, XElement>) ParseStrokeStyle(StrokeStyle strokeStyle, Rectangle boundingBox)
        {
            if(strokeStyle.Stroke == null)
            {
                throw new Exception($"Unknown stroke style: {strokeStyle.ToString()}");
            }

            if(strokeStyle.Stroke.ScaleMode != "normal")
            {
                Console.Error.WriteLine($"'stroke-scaleMode' value not supported: {strokeStyle.Stroke.ScaleMode}");
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
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();

            // Are we always going to try to get solidStyle even if Stroke isn't
            // SolidStroke?
            SolidStroke solidStroke = (SolidStroke) strokeStyle.Stroke;

            if(solidStroke != null)
            {
                if(solidStroke.SolidStyle == "hairline")
                {
                    // A hairline solidStyle overrides the "weight" XFL attribute.
                    attributes["stroke-width"] = "0.05";
                }
                else if(solidStroke.SolidStyle != string.Empty)
                {
                    throw new Exception($"Unknown 'solidStyle' value: {solidStroke.ToString()}");
                }
            }

            if (attributes["stroke-linejoin"] == "miter")
            {
                attributes["stroke-miterlimit"] = strokeStyle.Stroke.MiterLimit.ToString();
            }

            if(strokeStyle.Stroke.RadialGradient != null)
            {
                XElement gradientElement = GradientUtils.ConvertRadialGradientToSVG(strokeStyle.Stroke.RadialGradient);
                attributes["stroke"] = $"url(#{gradientElement.Attribute("id")!.Value})";
                extraDefElements[gradientElement.Attribute("id")!.Value] = gradientElement;
            }
            else if(strokeStyle.Stroke.SolidColor != null)
            {
                attributes["stroke"] = strokeStyle.Stroke.SolidColor.Color;
                attributes["fill-opacity"] = strokeStyle.Stroke.SolidColor.Alpha.ToString();
            }
            else
            {
                throw new Exception($"Unknown stroke fill: {strokeStyle.ToString()}");
            }

            return (attributes, extraDefElements);
        }
    }
}
