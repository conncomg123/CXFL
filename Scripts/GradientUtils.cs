using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Scripts
{
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
        /// Create an SVG linearGradient element from a LinearGradient CSXFL object.
        /// </summary>
        /// <param name="gradient">The CSXFL LinearGradient object that is being converted to SVG.</param>
        /// <seealso cref="https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/gradient.py#L22"/>
        /// <returns>A SVG linearGradient element equivalent to LinearGradient CSXFL.</returns>
        public static XElement ConvertLinearGradientToSVG(LinearGradient gradient)
        {
            Matrix gradientMatrix = gradient.Matrix;
            float a = (float) gradientMatrix.A;
            float b = (float) gradientMatrix.B;
            float tx = (float) gradientMatrix.Tx;
            float ty = (float)gradientMatrix.Ty;

            // Account for magic number when getting start and finish positions of gradient
            (float, float) startPosition = (a * -16384 / 20 + tx, b * -16384 / 20 + ty);
            (float, float) endPosition = (a * 16384 / 20 + tx, b * 16384 / 20 + ty);

            // Create SVG <linearGradient> element from CSXFL LinearGradient object
            XElement linearGradientElement = new XElement("linearGradient");
            linearGradientElement.SetAttributeValue("id", GenerateUniqueId());
            linearGradientElement.SetAttributeValue("gradientUnits", "userSpaceOnUse");
            linearGradientElement.SetAttributeValue("x1", startPosition.ToString());
            linearGradientElement.SetAttributeValue("x2", endPosition.ToString());
            linearGradientElement.SetAttributeValue("spreadMethod", gradient.SpreadMethod);

            foreach(GradientEntry stopEntry in gradient.GradientEntries)
            {
                XElement stopSVGElement = new XElement("stop");
                float offset = (float) stopEntry.Ratio * 100;
                stopSVGElement.SetAttributeValue("offset", $"{offset}%");
                stopSVGElement.SetAttributeValue("stop-color", stopEntry.Color);
                stopSVGElement.SetAttributeValue("stop-opacity", stopEntry.Alpha);
                linearGradientElement.Add(stopSVGElement);
            }

            return linearGradientElement;
        }
    }
}
