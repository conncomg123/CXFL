using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SkiaRendering
{
    /// <summary>
    /// Utils for converting CSXFL gradient elements into their equivalent SVG elements.
    /// </summary>
    internal class GradientUtils
    {
        /// <summary>
        /// Creates a Unique ID for gradients.
        /// <remarks>
        /// This is used to access the gradient element in defs.
        /// </remarks>
        /// </summary>
        /// <returns>Unique ID to dedup SVG elements in defs element.</returns>
        public static string GenerateUniqueId()
        {
            int randomNumber = Random.Shared.Next();
            return $"Gradient_{(randomNumber.GetHashCode() & 0xFFFFFFFF):x8}";
        }

        /// <summary>
        /// Creates a SVG linearGradient element from a LinearGradient CSXFL object.
        /// </summary>
        /// <param name="gradient">The CSXFL LinearGradient object that is being converted to SVG.</param>
        /// <seealso cref="https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/gradient.py#L22"/>
        /// <returns>A SVG linearGradient element equivalent to LinearGradient CSXFL.</returns>
        public static XElement ConvertLinearGradientToSVG(LinearGradient gradient)
        {
            Matrix gradientMatrix = gradient.Matrix;
            double a = gradientMatrix.A;
            double b = gradientMatrix.B;
            double tx = gradientMatrix.Tx;
            double ty = gradientMatrix.Ty;

            // Account for magic number when getting start and finish positions of gradient
            (double, double) startPosition = (a * -16384 / 20 + tx, b * -16384 / 20 + ty);
            (double, double) endPosition = (a * 16384 / 20 + tx, b * 16384 / 20 + ty);

            // Create SVG <linearGradient> element from CSXFL LinearGradient object
            XElement linearGradientElement = new XElement("linearGradient");
            linearGradientElement.SetAttributeValue("id", GenerateUniqueId());
            linearGradientElement.SetAttributeValue("gradientUnits", "userSpaceOnUse");
            linearGradientElement.SetAttributeValue("x1", startPosition.ToString());
            linearGradientElement.SetAttributeValue("x2", endPosition.ToString());
            linearGradientElement.SetAttributeValue("spreadMethod", gradient.SpreadMethod);

            // Create stop child elements of gradient and add them to linearGradient SVG element.
            foreach(GradientEntry stopEntry in gradient.GradientEntries)
            {
                XElement stopSVGElement = new XElement("stop");
                double offset = stopEntry.Ratio * 100;
                stopSVGElement.SetAttributeValue("offset", $"{offset}%");
                stopSVGElement.SetAttributeValue("stop-color", stopEntry.Color);
                stopSVGElement.SetAttributeValue("stop-opacity", stopEntry.Alpha);
                linearGradientElement.Add(stopSVGElement);
            }

            return linearGradientElement;
        }
    }
}
