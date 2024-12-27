using CsXFL;
using System.Xml.Linq;

namespace Rendering
{
    /// <summary>
    /// Utils for converting a XFL DOMShape element into its equivalent SVG path elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Base logic and documentation was largely taken from PluieElectrique's
    /// <see href="https://github.com/PluieElectrique/xfl2svg/blob/master/xfl2svg/shape/shape.py">edge.py</see>
    /// and SynthBot-Anon's updated version of
    /// <see href="https://github.com/synthbot-anon/PluieElectrique-xfl2svg/blob/radial-gradient-take2/xfl2svg/shape/shape.py">edge.py.</see>
    /// </para>
    /// <para>
    /// To account for the original code's usage of the python dict's update() method which does not have a direct
    /// equivalent in .NET, I created my own method.
    /// </para>
    /// </remarks>
    internal class ShapeUtilsNew
    {
        /// <summary>
        /// Converts a pointList into a SVG path string.
        /// </summary>
        /// <remarks>
        /// This method converts a pointList into the "d" attribute of a path element,
        /// NOT into an entire path element itself (with proper opening and closing path tags,
        /// d=, style= etc).
        /// </remarks>
        /// <param name="pointList">The pointList that is being converted.</param>
        /// <returns>The equivalent "d" string for the given pointList.</returns>
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

            // TODO: It is likely that stroked paths can be broken into multiple pointLists
            // and spread across multiple XFL Edge elements, which would require something
            // like ConvertPointListsToShapes() but for stroked paths.
            // It seems like closing a path seems good enough for now
            // Code was commented out in SynthBot-Anon's version, so I replicated it here.

            if (pointList[0] == pointList[pointList.Count - 1])
            {
                // If starting point == ending point i.e completes a closed shape/stroke,
                // Add Z command
                //svgPath.Add("Z");
            }

            // Combine list into space separated string to create SVG path string
            return string.Join(" ", svgPath);
        }

        /// <summary>
        /// Converts XFL DOMShape element into its equivalent SVG path elements.
        /// </summary>
        /// <remarks>
        /// This method performs three actions:
        /// 1. Gathers fillStyle and strokeStyle indexes
        /// 2. Uses EdgeUtils to get pointLists and bounding boxes assoicated with these fill/strokeStyle indexes
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

        /// <summary>
        /// Updates the contents of a dictionary based on the ones found in another.
        /// </summary>
        /// <remarks>
        /// This method both inserts new key value pairs that are not found in the original dictionary
        /// as well as updates ones that were already included.
        /// </remarks>
        /// <typeparam name="T1">The key's type.</typeparam>
        /// <typeparam name="T2">The value's type.</typeparam>
        /// <param name="dictionary">The original dictionary that will be updated.</param>
        /// <param name="dictionaryToAdd">The dictionary whose contents will be added to the original
        /// dictionary.</param>
        /// <returns>The original dictionary with both the new key value pairs as well as the updated
        /// ones. </returns>
        private static Dictionary<T1, T2> UpdateDictionary<T1,  T2>(Dictionary<T1, T2> dictionary,
            Dictionary<T1, T2> dictionaryToAdd) where T1 : notnull
        {
            foreach(KeyValuePair<T1, T2> keyValuePair in dictionaryToAdd)
            {
                dictionary[keyValuePair.Key] = keyValuePair.Value;
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a SVG path element.
        /// </summary>
        /// <param name="attributes">Attributes that will be included in the SVG path element.</param>
        /// <returns>The SVG path element with the associated set of attributes.</returns>
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
