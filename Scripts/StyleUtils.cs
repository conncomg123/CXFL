using CsXFL;
using Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SkiaRendering
{
    class StyleUtils
    {
        /// <summary>
        /// Parse CSXFL FillStyle element of its SVG style attributes and any extra-defs.
        /// </summary>
        /// <param name="fillStyle">CSXFL FillStyle element whose SVG elements will be parsed from.</param>
        /// <returns>A tuple: attributes which is a Dictionary of SVG style attributes</returns>
        /// <exception cref="Exception">When there is a fillStyle that a RadialGradient or one
        /// is not recognized</exception>
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
                throw new Exception($"Unknown fill style: {fillStyle.ToString()}");
            }

            return (attributes, extraDefElements);
        }
    }
}
