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
    /// Utils for converting a XFL DOMShape element into its equivalent SVG path elements.
    /// </summary>
    internal class ShapeUtils
    {
        /// <summary>
        /// Converts XFL DOMShape element into its equivalent SVG path elements.
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
            // Dictionary that keeps track of style attributes associated with SVG equivalent of fillStyle with specific
            // fillStyle index
            Dictionary<int, Dictionary<string, string>> fillStylesAttributes = new Dictionary<int, Dictionary<string, string>>();
            
            // Dictionary that keeps track of style attributes associated with SVG equivalent of StrokeStyle with specific
            // strokeStyle index
            Dictionary<int, Dictionary<string, string>> strokeStylesAttributes = new Dictionary<int, Dictionary<string, string>>();
            
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();

            // For each FillStyle of DOMShape, create SVG attributes of its SVG equivalent
            foreach (FillStyle fillStyle in shapeElement.Fills)
            {
                int index = fillStyle.Index;
                if(mask)
                {
                    // Set the fill to white so that the mask is fully transparent
                    Dictionary<string, string> attributes = new Dictionary<string, string>()
                    {
                        {"fill", "#FFFFFF" },
                        {"stroke", "none" }
                    };
                    fillStylesAttributes[index] = attributes;
                }
                else
                {
                    (Dictionary<string, string>, Dictionary<string, XElement>)
                        svgAttributeElements = StyleUtils.ParseFillStyle(fillStyle);
                    fillStylesAttributes[index] = svgAttributeElements.Item1;
                    
                    // Add any elements that need to be added to SVG <defs>
                    foreach(KeyValuePair<string, XElement> defElementPair in svgAttributeElements.Item2)
                    {
                        extraDefElements[defElementPair.Key] = defElementPair.Value;
                    }
                }
            }

            // For each StrokeStyle of DOMShape, create SVG attributes of its SVG equivalent
            foreach (StrokeStyle strokeStyle in shapeElement.Strokes)
            {
                // TODO: Figure out how strokes are supposed to behave in masks
                if (mask)
                {
                    throw new Exception("Strokes in masks not supported yet!");
                }
                int index = strokeStyle.Index;
                //strokeStylesAttributes[index] = 
            }

            return (null, null, null);
        }
    }
}
