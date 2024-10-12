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
        /// <remarks>
        /// This method performs three actions:
        /// 1. Gather fill and stroke ids
        /// 2. Uses EdgeUtils to get the fill and stroke path lists- list of pointList, boundingBox tuples
        /// 3. Generates the SVG, including style information.
        /// </remarks>
        /// <param name="shapeElement">The XFL DOMShape being converted.</param>
        /// <param name="mask">If true, all fill colors will be set to #FFFFFF. This ensures
        /// that the resulting mask is fully transparent.</param>
        /// <returns>A 4-tuple consisting of: SVG g element containing filled paths elements,
        /// SVG g element containing stroked path elements, and
        /// dict of extra elements to put in SVG defs element (e.g.filters and gradients),
        /// bounding box of entire shape.</returns>
        public static (XElement?, XElement?, Dictionary<string, XElement>?, Rectangle?) 
            ConvertShapeToSVG(Shape shapeElement, bool mask = false)
        {

            Dictionary<int, FillStyle> fillStyles = new Dictionary<int, FillStyle>();
            Dictionary<int, StrokeStyle> strokeStyles = new Dictionary<int, StrokeStyle>();
            
            // The python code stores the styles directly into Dictionary, do the same here
            foreach(FillStyle style in shapeElement.Fills)
            {
                int fillIndex = style.Index;
                fillStyles[fillIndex] = style;
            }

            foreach(StrokeStyle style in shapeElement.Strokes)
            {
                int strokeIndex = style.Index;
                strokeStyles[strokeIndex] = style;
            }

            Rectangle? boundingBox = null;
            List<XElement> fillPathElements = new List<XElement>();
            List<XElement> strokePathElements = new List<XElement>();
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();

            (var fillShapes, var strokes) = EdgeUtils.ConvertXFLEdgesToShapes(shapeElement.Edges, fillStyles, strokeStyles);

            foreach(KeyValuePair<int, (List<List<string>>, Rectangle?)> fillShapeEntry in fillShapes)
            {
                List<List<string>> currentPointLists = fillShapeEntry.Value.Item1;
                Rectangle? currentBoundingBox = fillShapeEntry.Value.Item2;
                FillStyle currentStyle = fillStyles[fillShapeEntry.Key];

                Dictionary<string, string> fillStyleSVGAttributes = new Dictionary<string, string>();

                if(mask)
                {
                    // Set the fill to white so that the mask is fully transparent
                    fillStyleSVGAttributes["fill"] = "#FFFFFF";
                    fillStyleSVGAttributes["stroke"] = "none";
                }
                else
                {
                    (fillStyleSVGAttributes, Dictionary<string, XElement> fillStyleExtraElements) = StyleUtils.ParseFillStyle(currentStyle);
                    
                    // Add or update any extra SVG elements that are created to be stored in def
                    foreach(KeyValuePair<string, XElement> extraElement in fillStyleExtraElements)
                    {
                        extraDefElements[extraElement.Key] = extraElement.Value;
                    }
                }

                fillStyleSVGAttributes["d"] = string.Join(" ", currentPointLists.Select(ConvertPointListToPathString));
                XElement pathElement =  CreateSVGPathElement(fillStyleSVGAttributes);
                fillPathElements.Add(pathElement);
                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            foreach(KeyValuePair<int, (List<List<string>>, Rectangle?)> strokeEntry in strokes)
            {
                List<List<string>> currentPointLists = strokeEntry.Value.Item1;
                Rectangle currentBoundingBox = strokeEntry.Value.Item2!;
                StrokeStyle currentStyle = strokeStyles[strokeEntry.Key];
                
                Dictionary<string, string> strokeStyleSVGAttributes = new Dictionary<string, string>();
                if(mask)
                {
                    //Insert warning here
                }

                (strokeStyleSVGAttributes, Dictionary<string, XElement> strokeStyleExtraElements) = StyleUtils.ParseStrokeStyle(currentStyle);
                
                // Add or update any extra SVG elements that are created to be stored in def
                foreach(KeyValuePair<string, XElement> extraElement in strokeStyleExtraElements)
                {
                    extraDefElements[extraElement.Key] = extraElement.Value;
                }

                double strokeWidth = strokeStyleSVGAttributes.ContainsKey("stroke-width") ? 
                    Convert.ToDouble(strokeStyleSVGAttributes["stroke-width"]) : 1;

                currentBoundingBox = BoxUtils.ExpandBoundingBox(currentBoundingBox, strokeWidth);
                strokeStyleSVGAttributes["d"] = string.Join(" ", currentPointLists.Select(ConvertPointListToPathString));
                
                XElement pathElement =  CreateSVGPathElement(strokeStyleSVGAttributes);
                strokePathElements.Add(pathElement);
                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            // Create SVG group elements to store both fill SVG path elements and stroke SVG path elements
            XElement? fillsG = null;
            XElement? strokesG = null;
            
            if(fillPathElements.Count != 0)
            {
                fillsG = new XElement("g");
                foreach(XElement fillPathElement in fillPathElements)
                {
                    fillsG.Add(fillPathElement);
                }
            }

            if(strokePathElements.Count != 0)
            {
                strokesG = new XElement("g");
                foreach(XElement strokePathElement in strokePathElements)
                {
                    strokesG.Add(strokePathElement);
                }
            }

            return (fillsG, strokesG, extraDefElements, boundingBox);
        }

        public static XElement CreateSVGPathElement(Dictionary<string, string> attributes)
        {
            XElement newPathElement = new XElement("path");
            foreach(var attribute in attributes)
            {
                newPathElement.SetAttributeValue(attribute.Key, attribute.Value);
            }

            return newPathElement;
        }
    }
}
