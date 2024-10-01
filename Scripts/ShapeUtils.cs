using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
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
        public static (XElement?, XElement?, Dictionary<string, XElement>?) 
            ConvertShapeToSVG(Shape shapeElement, bool mask = false, string? maskId = null)
        {
            // Dictionary that keeps track of style attributes associated with SVG equivalent of fillStyle with specific
            // fillStyle index
            Dictionary<string, Dictionary<string, string>> fillStylesAttributes = new();
            
            // Dictionary that keeps track of style attributes associated with SVG equivalent of StrokeStyle with specific
            // strokeStyle index
            Dictionary<string, Dictionary<string, string>> strokeStylesAttributes = new();

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
                    fillStylesAttributes[index.ToString()] = attributes;
                }
                else
                {
                    (Dictionary<string, string>, Dictionary<string, XElement>)
                        svgAttributeElements = StyleUtils.ParseFillStyle(fillStyle);
                    fillStylesAttributes[index.ToString()] = svgAttributeElements.Item1;
                    
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
                int index = strokeStyle.Index;
                strokeStylesAttributes[index.ToString()] = StyleUtils.ParseStrokeStyle(strokeStyle);
            }

            (List<XElement>?, List<XElement>?) pathElements = 
                EdgeUtils.ConvertEdgesToSvgPath(shapeElement.Edges, fillStylesAttributes, strokeStylesAttributes);

            XElement? fillsG = null;
            XElement? strokesG = null;

            if (pathElements.Item1 != null)
            {
                fillsG = new XElement("g");
                foreach(XElement fillPathElement in pathElements.Item1)
                {
                    fillsG.Add(fillPathElement);
                }
            }

            if (pathElements.Item2 != null)
            {
                strokesG = new XElement("g");
                foreach (XElement strokePathElement in pathElements.Item2)
                {
                    strokesG.Add(strokePathElement);
                }
            }

            return (fillsG, strokesG, extraDefElements);
        }
    }
}
