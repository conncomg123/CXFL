using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rendering
{
    /// <summary>
    /// Utils for converting a XFL DOMShape element into its equivalent SVG path elements.
    /// </summary>
    internal class ShapeUtils
    {
        /// <summary>
        /// Converts a point list into a SVG path string.
        /// </summary>
        /// <remarks>
        /// This method converts a point list into the "d" attribute of a path element,
        /// NOT into an entire path element itself (with proper opening and closing path tags,
        /// d=, style= etc).
        /// </remarks>
        /// <param name="pointList">The point list that is being converted.</param>
        /// <returns>The equivalent "d" string for the given point list.</returns>
        public static string ConvertPointListToPathString(List<string> pointList)
        {
            // Using iterator to match previous method as well as Python implementation
            IEnumerator<string> pointEnumerator = pointList.GetEnumerator();
            // Start SVG path with M command and first point
            pointEnumerator.MoveNext();

            List<string> svgPath = new List<string> { "M", pointEnumerator.Current };
            string lastCommand = "M";

            while (pointEnumerator.MoveNext())
            {
                string currentPoint = pointEnumerator.Current;
                string currentCommand = currentPoint.Contains('[') && currentPoint.Contains(']') ? "Q" : "L";

                // SVG path element allows us to omit command letter if same command is used
                // multiple times in a row, so only add it to svgPath string if new command is found
                if (currentCommand != lastCommand)
                {
                    svgPath.Add(currentCommand);
                    lastCommand = currentCommand;
                }

                if (currentCommand == "Q")
                {
                    // As this is a "quadTo" command, control point is formatted as "[x y]"- need to remove []
                    // add said point, and then add end point (next point)
                    currentPoint = currentPoint.Replace("[", "").Replace("]", "");

                    svgPath.Add(currentPoint);
                    pointEnumerator.MoveNext();
                    svgPath.Add(pointEnumerator.Current);
                }
                else
                {
                    svgPath.Add(currentPoint);
                }
            }

            // Animate adds a "closepath" (Z) command to every filled shape and
            // closed stroke. For shapes, it makes no difference, but for closed
            // strokes, it turns two overlapping line caps into a bevel, miter,
            // or round join, which does make a difference.
            if (pointList[0] == pointList[pointList.Count - 1])
            {
                // If starting point == ending point i.e completes a closed shape/stroke,
                // Add Z command
                svgPath.Add("Z");
            }

            // Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
        }

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
            ConvertShapeToSVG(Shape shapeElement, bool mask = false)
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
                ConvertEdgesToSvgPath(shapeElement.Edges, fillStylesAttributes, strokeStylesAttributes);

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
