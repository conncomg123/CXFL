using CSCore.Streams.SampleConverter;
using CsXFL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rendering
{
    internal class ShapeUtilsNew
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
                //svgPath.Add("Z");
            }

            // Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
        }

        public static (XElement?, XElement?, Dictionary<string, XElement>?, Rectangle?)
            ConvertShapeToSVG(Shape shapeElement, bool mask = false)
        {
            // Don't know if this is really necessary or not
            Dictionary<int, FillStyle> fillStyles = new Dictionary<int, FillStyle>();
            Dictionary<int, StrokeStyle> strokeStyles = new Dictionary<int, StrokeStyle>();

            foreach (FillStyle style in shapeElement.Fills)
            {
                fillStyles[style.Index] = style;
            }

            foreach (StrokeStyle style in shapeElement.Strokes)
            {
                strokeStyles[style.Index] = style;
            }

            List<XElement> filledPaths = new List<XElement>();
            List<XElement> strokedPaths = new List<XElement>();
            Dictionary<string, XElement> extraDefElements = new Dictionary<string, XElement>();
            Rectangle? boundingBox = null;

            (Dictionary<int, (List<List<string>>, Rectangle?)>,
            Dictionary<int, (List<List<string>>, Rectangle?)>) shapesAndStrokes =
                EdgeUtilsNew.ConvertEdgesToSvgPathNew(shapeElement.Edges, fillStyles, strokeStyles);

            foreach (KeyValuePair<int, (List<List<string>>, Rectangle?)> shapePair in shapesAndStrokes.Item1)
            {
                int fillStyleIndex = shapePair.Key;
                List<List<string>> pointLists = shapePair.Value.Item1;
                Rectangle? currentBoundingBox = shapePair.Value.Item2;
                FillStyle style = fillStyles[fillStyleIndex];
                Dictionary<string, string> styleSVGAttributes = new Dictionary<string, string>();

                if (mask)
                {
                    // Set the fill to white so that the mask is fully transparent
                    styleSVGAttributes["fill"] = "#FFFFFF";
                    styleSVGAttributes["stroke"] = "none";
                }
                else
                {
                    // Get this fillStyle's SVG attributes and any extra SVG elements
                    // Create the proper path element for this fillStyle
                    (Dictionary<string, string> properAttributes,
                        Dictionary<string, XElement> styleExtraElements) = StyleUtils.ParseFillStyle(style);

                    styleSVGAttributes = UpdateDictionary(styleSVGAttributes, properAttributes);
                    extraDefElements = UpdateDictionary(extraDefElements, styleExtraElements);
                }

                // Create XML path element with its proper attributes
                var pathElement = CreatePathElement(styleSVGAttributes);
                pathElement.SetAttributeValue("d", string.Join(" ", pointLists.Select(pl => ConvertPointListToPathString(pl))));
                filledPaths.Add(pathElement);

                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            foreach (KeyValuePair<int, (List<List<string>>, Rectangle?)> strokePair in shapesAndStrokes.Item2)
            {
                int strokeStyleIndex = strokePair.Key;
                List<List<string>> pointLists = strokePair.Value.Item1;
                Rectangle? currentBoundingBox = strokePair.Value.Item2;
                StrokeStyle style = strokeStyles[strokeStyleIndex];
                Dictionary<string, string> styleSVGAttributes = new Dictionary<string, string>();

                // TODO: Figure out how strokes are supposed to behave in masks
                if(mask)
                {
                    Console.WriteLine("Strokes in masks are not supported!");
                }

                // Get this strokeStyle's SVG attributes and any extra SVG elements
                // Create the proper path element for this strokeStyle
                (Dictionary<string, string> properAttributes,
                    Dictionary<string, XElement> styleExtraElements) = StyleUtils.ParseStrokeStyle(style);

                styleSVGAttributes = UpdateDictionary(styleSVGAttributes, properAttributes);
                extraDefElements = UpdateDictionary(extraDefElements, styleExtraElements);

                // Update bounding box for strokeStyle based on its width
                double strokeWidth = float.Parse(styleSVGAttributes["stroke-width"]);
                currentBoundingBox = BoxUtils.ExpandBoundingBox(currentBoundingBox!, strokeWidth);

                // Create XML path element with its proper attributes
                var pathElement = CreatePathElement(styleSVGAttributes);
                pathElement.SetAttributeValue("d", string.Join(" ", pointLists.Select(pl => ConvertPointListToPathString(pl))));
                // pathElement.SetAttributeValue("d", string.Join(" ", pointLists.Select(pl => pl[0])));

                strokedPaths.Add(pathElement);
                boundingBox = BoxUtils.MergeBoundingBoxes(boundingBox, currentBoundingBox);
            }

            XElement? fillsG = null;
            XElement? strokesG = null;

            if (filledPaths != null)
            {
                fillsG = new XElement("g");
                foreach (XElement fillPathElement in filledPaths)
                {
                    fillsG.Add(fillPathElement);
                }
            }

            if (strokedPaths != null)
            {
                strokesG = new XElement("g");
                foreach (XElement strokePathElement in strokedPaths)
                {
                    strokesG.Add(strokePathElement);
                }
            }

            return (fillsG, strokesG, extraDefElements, boundingBox);
        }

        private static Dictionary<T1, T2> UpdateDictionary<T1,  T2>(Dictionary<T1, T2> dictionary,
            Dictionary<T1, T2> dictionaryToAdd) where T1 : notnull
        {
            foreach(KeyValuePair<T1, T2> keyValuePair in dictionaryToAdd)
            {
                dictionary[keyValuePair.Key] = keyValuePair.Value;
            }

            return dictionary;
        }

        private static XElement CreatePathElement(Dictionary<string, string> attributes)
        {
            XElement newPathElement = new XElement("path");
            foreach (var attribute in attributes)
            {
                newPathElement.SetAttributeValue(attribute.Key, attribute.Value);
            }

            return newPathElement;
        }
    }
}
