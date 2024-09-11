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
    /// Parses a XFL DOMShape element into its equivalent SVG path elements.
    /// </summary>
    internal class ShapeUtils
    {
        /// <summary>
        /// Convert XFL DOMShape element into its equivalent SVG path elements.
        /// </summary>
        /// <param name="shapeElement">The XFL DOMShape being converted.</param>
        /// <param name="mask">If true, all fill colors will be set to #FFFFFF. This ensures
        /// that the resulting mask is fully transparent.</param>
        /// <returns>A 3-tuple consisting of: SVG g element containing filled paths elements,
        /// SVG g element containing stroked path elements, and
        /// dict of extra elements to put in SVG defs element (e.g.filters and gradients)</returns>
        public static (XElement?, XElement?, Dictionary<int, XElement>?) 
            ConvertShapeToSVG(Shape shapeElement, bool mask = false)
        {
            Dictionary<int, Dictionary<string, string>> fillStyleElements = new Dictionary<int, Dictionary<string, string>>();
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();

            foreach(FillStyle fillStyle in shapeElement.Fills)
            {
                int index = fillStyle.Index;
                if(mask)
                {
                    Dictionary<string, string> attributes = new Dictionary<string, string>()
                    {
                        {"fill", "#FFFFFF" },
                        {"stroke", "none" }
                    };

                }
            }

            return (null, null, null);
        }
    }
}
