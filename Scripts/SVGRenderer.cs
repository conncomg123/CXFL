using CsXFL;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Rendering;

public class SVGRenderer
{
    XNamespace xlink = "http://www.w3.org/1999/xlink";
    public static XNamespace svgNs = "http://www.w3.org/2000/svg";
    XName HREF;
    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
    Document Document { get; set; }
    public bool RepalceMasksWithClipPaths { get; set; }
    ConcurrentDictionary<Shape, (XElement?, XElement?, Dictionary<string, XElement>?)> ShapeCache = new(),
    MaskCache = new();

    public SVGRenderer(Document document, bool repalceMasksWithClipPaths = true)
    {
        Document = document;
        RepalceMasksWithClipPaths = repalceMasksWithClipPaths;
        HREF = XName.Get("href", xlink.ToString());
        nsmgr.AddNamespace("xlink", xlink.ToString());
    }
    private static bool IsColorIdentity(Color color)
    {
        return color.RedMultiplier == 1 && color.GreenMultiplier == 1 && color.BlueMultiplier == 1 && color.AlphaMultiplier == 1
        && color.RedOffset == 0 && color.GreenOffset == 0 && color.BlueOffset == 0 && color.AlphaOffset == 0;
    }
    private static List<double> MatrixToList(Matrix matrix)
    {
        return new List<double> { matrix.A, matrix.B, matrix.C, matrix.D, matrix.Tx, matrix.Ty };
    }
    private string GetImageType(string filename)
    {
        if (filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return "png";
        }
        else if (filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                 filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return "jpeg";
        }
        else
        {
            throw new NotSupportedException("Unsupported image type");
        }
    }
    private string GetBitmapData(BitmapInstance bitmap)
    {
        if (Document.IsXFL)
        {
            string imgPath = Path.Combine(Path.GetDirectoryName(Document.Filename)!, Library.LIBRARY_PATH, (bitmap.CorrespondingItem as BitmapItem)!.Href);
            byte[] data = File.ReadAllBytes(imgPath);
            string dataUrl = $"data:image/{GetImageType(imgPath)};base64,{Convert.ToBase64String(data)}";
            return dataUrl;
        }
        else
        {
            using (ZipArchive archive = ZipFile.Open(Document.Filename, ZipArchiveMode.Read))
            {
                string imgPath = Path.Combine(Library.LIBRARY_PATH, (bitmap.CorrespondingItem as BitmapItem)!.Href).Replace("\\", "/");
                ZipArchiveEntry? entry = archive.GetEntry(imgPath);
                if (entry is null)
                {
                    // try to find it while removing slashes from both paths
                    imgPath = imgPath.Replace('/', '\\').Replace('\\', '_');
                    entry = archive.Entries.Where(x => x.FullName.Replace('/', '\\').Replace('\\', '_') == imgPath).FirstOrDefault();
                    if (entry is null) throw new Exception($"Bitmap not found: {imgPath}");
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    entry.Open().CopyTo(ms);
                    byte[] imageData = ms.ToArray();

                    string dataUrl = $"data:image/{GetImageType(imgPath)};base64,{Convert.ToBase64String(imageData)}";
                    return dataUrl;
                }
            }
        }
    }
    public XDocument Render(Timeline timeline, int frameIndex, int? width = null, int? height = null, string type = "symbol")
    {
        width ??= Document.Width;
        height ??= Document.Height;
        Color colorEffect = Color.DefaultColor();
        Dictionary<string, XElement> defs;
        List<XElement> body;
        (defs, body) = RenderTimeline(timeline, frameIndex, colorEffect, false, type);
        XElement svg = new XElement(svgNs + "svg",
        new XAttribute("version", "1.1"),
        new XAttribute("preserveAspectRatio", "none"),
        new XAttribute("x", "0px"),
        new XAttribute("y", "0px"),
        new XAttribute("width", $"{width}px"),
        new XAttribute("height", $"{height}px"),
        new XAttribute("viewBox", $"0 0 {width} {height}"),
        new XAttribute(XNamespace.Xmlns + "xlink", xlink.ToString())
        );

        XElement defsElement = new XElement(svgNs + "defs");
        foreach (XElement element in defs.Values)
        {
            defsElement.Add(element);
        }

        svg.Add(defsElement);
        svg.Add(body);
        return new XDocument(svg);
    }
    private static Matrix4x4 TransformStringToMatrix(string transform)
    {
        string[] parts = transform.Replace("matrix(", "").Replace(")", "").Split(',');
        float[] values = parts.Select(float.Parse).ToArray();
        Matrix4x4 matrix = new Matrix4x4
        {
            M11 = values[0],
            M12 = values[1],
            M21 = values[2],
            M22 = values[3],
            M41 = values[4],
            M42 = values[5],
            M33 = 1,
            M44 = 1
        };
        return matrix;
    }
    private static string TransformMatrixToString(Matrix4x4 matrix)
    {
        return $"matrix({matrix.M11},{matrix.M12},{matrix.M21},{matrix.M22},{matrix.M41},{matrix.M42})";
    }
    private (List<XElement>, Matrix4x4) ParseMaskGAndUseNodes(XElement element, Matrix4x4 matrix, Dictionary<string, XElement> defs)
    {
        List<XElement> newElements = new List<XElement>();
        Matrix4x4 curMatrix = Matrix4x4.Multiply(matrix, Matrix4x4.Identity);
        foreach (XElement child in element.Elements(svgNs + "g"))
        {
            if (child.Attribute("transform") is not null)
            {
                string transform = child.Attribute("transform")!.Value;
                Matrix4x4 childMatrix = TransformStringToMatrix(transform);
                curMatrix = Matrix4x4.Multiply(childMatrix, curMatrix);
            }

            (List<XElement> newElement, Matrix4x4 newMatrix) = ParseMaskGAndUseNodes(child, curMatrix, defs);
            newElements.AddRange(newElement);
            curMatrix = newMatrix;
        }

        foreach (XElement child in element.Elements(svgNs + "use"))
        {
            Matrix4x4 useMatrix = Matrix4x4.Multiply(curMatrix, Matrix4x4.Identity);
            if (child.Attribute("transform") is not null)
            {
                string transform = child.Attribute("transform")!.Value;
                Matrix4x4 childMatrix = TransformStringToMatrix(transform);
                useMatrix = Matrix4x4.Multiply(childMatrix, curMatrix);
            }

            string href = child.Attribute(HREF)!.Value;
            XElement usedElement = defs[href.StartsWith("#") ? href.Substring(1) : href];
            List<XElement>? pathElements = usedElement.Elements(svgNs + "path")?.ToList();
            if (pathElements is null) continue;
            for(int i = 0; i < pathElements.Count; i++) 
            {
                pathElements[i] = new XElement(pathElements[i]);
                pathElements[i].SetAttributeValue("transform", TransformMatrixToString(useMatrix));
            }
            newElements.AddRange(pathElements);
        }

        return (newElements, curMatrix);
    }
    private XElement ConvertMaskToClipPath(XElement maskElement, Dictionary<string, XElement> defs)
    {
        maskElement.Name = svgNs + "clipPath";
        string? maskMatrix = maskElement.Attribute("transform")?.Value;
        Matrix4x4 matrix = maskMatrix is null ? Matrix4x4.Identity : TransformStringToMatrix(maskMatrix);
        (List<XElement> paths, _) = ParseMaskGAndUseNodes(maskElement, matrix, defs);
        maskElement.RemoveNodes();
        maskElement.Add(paths);
        return maskElement;
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderTimeline(Timeline timeline, int frameIndex, Color colorEffect, bool insideMask, string type = "symbol", bool isMaskLayer = false)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        string idName = Regex.Replace(timeline.Name, "[^A-Za-z0-9]", "_");
        string id = $"{idName}_{frameIndex}";
        if (insideMask) id = $"Mask_{id}";
        List<Layer> layers = timeline.Layers;
        bool maskIsActive = false;
        string maskId = string.Empty;
        // Process layers from back to front
        for (int layerIdx = layers.Count - 1; layerIdx >= 0; layerIdx--)
        {
            Layer layer = layers[layerIdx];
            string layerType = layer.LayerType;
            Dictionary<string, XElement> d;
            List<XElement> b;
            if (layerType == "guide")
            {
                continue;
            }
            else if (layerType == "mask")
            {
                (d, b) = RenderLayer(layer, frameIndex, maskId + "_MASK", colorEffect, maskIsActive, maskId, true);
                // End the mask we started earlier
                maskIsActive = false;
                maskId = $"Mask_{id}_{layerIdx}";
                foreach (var def in d)
                {
                    defs[def.Key] = def.Value;
                }
                XElement mask = new XElement(svgNs + "mask", new XAttribute("id", maskId));
                foreach (XElement e in b)
                {
                    mask.Add(e);
                }
                if(RepalceMasksWithClipPaths) mask = ConvertMaskToClipPath(mask, defs);
                defs[maskId] = mask;
                continue;
            }
            else if (layerType == "folder" || layerType == null)
            {
                continue;
            }
            else if (layerType != "normal")
            {
                System.Console.WriteLine($"Unknown layer type: {layerType}");
            }

            // Check if we need to start a mask
            int? pLayerIndex = layer.ParentLayerIndex;
            if (!maskIsActive && pLayerIndex != null)
            {
                Layer parentLayer = layers[pLayerIndex.Value];
                if (parentLayer.LayerType == "mask")
                {
                    maskIsActive = true;
                    maskId = $"Mask_{id}_{pLayerIndex.Value}";
                }
            }


            (d, b) = RenderLayer(layer, frameIndex, $"{id}_Layer{layerIdx}", colorEffect, maskIsActive, maskId, isMaskLayer);
            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }
            body.AddRange(b);

        }

        return (defs, body);
    }
    private static (Matrix?, Color?) ParseClassicTween(Frame srcFrame, Frame destFrame, int frameOffset, int elementIndex)
    {
        if (srcFrame.IsEmpty() || destFrame.IsEmpty()) return (null, null);
        // check if both frames have at least one symbolInstance
        if (!srcFrame.Elements.OfType<SymbolInstance>().Any() || !destFrame.Elements.OfType<SymbolInstance>().Any()) return (null, null);
        if (srcFrame.Elements[elementIndex] is not SymbolInstance si) return (null, null);
        int rotation = srcFrame.MotionTweenRotateTimes;
        if (srcFrame.MotionTweenRotate == "clockwise") rotation = -rotation;
        Matrix firstMat = si.Matrix;
        Matrix lastMat = destFrame.Elements.OfType<SymbolInstance>().First().Matrix;
        Color firstColor = si.Color;
        Color lastColor = destFrame.Elements.OfType<SymbolInstance>().First().Color;
        Matrix interpMat = TweenUtils.MatrixInterpolation(firstMat, lastMat, rotation, srcFrame, frameOffset, si.TransformationPoint);
        Color interpColor = TweenUtils.ColorInterpolation(firstColor, lastColor, srcFrame, frameOffset);
        return (interpMat, interpColor);
    }
    private static Shape? ParseShapeTween(Frame srcFrame, Frame destFrame, int frameOffset, int elementIndex)
    {
        if (srcFrame.IsEmpty() || destFrame.IsEmpty()) return null;
        if (!srcFrame.Elements.OfType<Shape>().Any() || !destFrame.Elements.OfType<Shape>().Any()) return null;
        if (srcFrame.Elements[elementIndex] is not Shape shape) return null;
        Shape firstShape = shape;
        Shape lastShape = destFrame.Elements.OfType<Shape>().First();
        Shape interpShape = TweenUtils.ShapeInterpolation(firstShape, lastShape, srcFrame, frameOffset);
        return interpShape;
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderLayer(Layer layer, int frameIndex, string id, Color colorEffect, bool insideMask, string? maskId = null, bool isMaskLayer = false)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        if (layer.GetFrameCount() == 0 || frameIndex >= layer.GetFrameCount())
        {
            return (defs, body);
        }
        Frame frame = layer.GetFrame(frameIndex);
        int frameOffset = frameIndex - frame.StartFrame;
        bool hasValidClassicTween = frame.KeyMode.Equals((int)Frame.KeyModes.ClassicTween) && frame.StartFrame + frame.Duration < layer.GetFrameCount();
        bool hasValidShapeTween = frame.TweenType == "shape" && frame.StartFrame + frame.Duration < layer.GetFrameCount();
        Frame? nextFrame;
        for (int i = 0; i < frame.Elements.Count; i++)
        {
            Dictionary<string, XElement> d;
            List<XElement> b;
            Matrix? interpMat = null;
            Color? interpColor = null;
            Shape? interpShape = null;
            if (hasValidClassicTween)
            {
                nextFrame = layer.GetFrame(frame.StartFrame + frame.Duration);
                (interpMat, interpColor) = ParseClassicTween(frame, nextFrame, frameOffset, i);
            }
            else if (hasValidShapeTween)
            {
                nextFrame = layer.GetFrame(frame.StartFrame + frame.Duration);
                interpShape = ParseShapeTween(frame, nextFrame, frameOffset, i);
            }
            colorEffect = (frame.Elements[i] as SymbolInstance)?.Color ?? colorEffect;
            (d, b) = RenderElement(frame.Elements[i], $"{id}_{i}", frameOffset, interpColor ?? colorEffect, insideMask, isMaskLayer, interpMat, interpShape);
            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }
            // add b to a new XElement g for organization and give it a name attribute
            XElement g = new XElement(svgNs + "g", new XAttribute("name", $"{id}_{i}"));
            if (insideMask && !isMaskLayer) g.SetAttributeValue(RepalceMasksWithClipPaths ? "clip-path" : "mask", $"url(#{maskId})");
            foreach (XElement e in b)
            {
                g.Add(e);
            }
            body.Add(g);
        }
        return (defs, body);
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderElement(Element element, string id, int frameOffset, Color colorEffect, bool insideMask, bool isMaskShape = false, Matrix? interpMat = null, Shape? interpShape = null)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        if (element is SymbolInstance si)
        {
            if (si.SymbolType != "graphic") return (defs, body);
            (defs, body) = RenderTimeline((si.CorrespondingItem as SymbolItem)!.Timeline, GetLoopFrame(si, frameOffset), Color.DefaultColor(), insideMask, "symbol", isMaskShape);
        }
        else if (element is Text text)
        {
            // <!> Hi Soundman!
            body.Add(HandleText((Text)element));
        }
        else if (element is Shape shape)
        {
            (defs, body) = HandleDomShape(interpShape ?? shape, id, colorEffect, insideMask, isMaskShape);
        }
        else if (element is CsXFL.Group group)
        {
            List<Element> children = group.Members;
            bool hasMoreThanOneChild = children.Count > 1;
            for (int i = 0; i < children.Count; i++)
            {
                string memId = hasMoreThanOneChild ? $"{id}_MEMBER_{i}" : id;
                var (d, b) = RenderElement(children[i], memId, frameOffset, colorEffect, insideMask, isMaskShape);
                foreach (var def in d)
                {
                    defs[def.Key] = def.Value;
                }
                body.AddRange(b);
            }
        }
        else if (element is BitmapInstance bitmap)
        {
            body.Add(HandleBitmap(bitmap));
        }
        else
        {
            throw new NotImplementedException($"Unknown element type: {element.GetType()}");
        }
        if (element is not CsXFL.Group)
        {
            Matrix mat = interpMat ?? element.Matrix;
            if (!Matrix.IsDefaultMatrix(mat) || !IsColorIdentity(colorEffect))
            {
                XElement g = new XElement(svgNs + "g");
                if (!Matrix.IsDefaultMatrix(mat))
                {
                    string matrix = string.Join(", ", MatrixToList(mat));
                    g.Add(new XAttribute("transform", $"matrix({matrix})"));
                }
                if (!IsColorIdentity(colorEffect) && !isMaskShape)
                {
                    var colorSVG = ColorEffectUtils.ConvertColorEffectToSVG(colorEffect);
                    string colorId = (string)colorSVG.Attribute("id")!;
                    defs[colorId] = colorSVG;
                    g.Add(new XAttribute("filter", $"url(#{colorId})"));
                }
                foreach (XElement element_ in body)
                {
                    g.Add(element_);
                }
                body = new List<XElement> { g };
            }
        }

        return (defs, body);
    }


    private XElement HandleBitmap(BitmapInstance bitmap)
    {
        string dataUrl = GetBitmapData(bitmap);

        XNamespace xlinkNs = "http://www.w3.org/1999/xlink";
        // x and y handled by the matrix
        XElement imageElement = new XElement(svgNs + "image",
            new XAttribute(xlinkNs + "href", dataUrl),
            new XAttribute("width", bitmap.HPixels.ToString()),
            new XAttribute("height", bitmap.VPixels.ToString())
        );

        return imageElement;
    }

    // Animate mangles font names. Is it possible to take TextAttrs.Face and get the corresponding Windows font? Will be needed for font embedding.

    // Intended approach for Animate is to create a mask of text bounding box dimensions, and mask the text to the bounding box.
    // This logic is not present at the moment, so text will never cut off if it goes out of bounds
    private XElement HandleText(Text TextElement)
    {
        XElement textElement = new XElement(svgNs + "text",
            new XAttribute("writing-mode", "lr") // Force writing mode to left-right. Circle back to this later.
        );

        for (int i = 0; i < TextElement.TextRuns.Count; i++)
        {
            var textRun = TextElement.TextRuns[i];
            string[] characters = textRun.Characters.Split('\r');

            double carriage_y = 1;
            double anticipated_x = textRun.TextAttrs.LeftMargin + textRun.TextAttrs.Indent;
            double anticipated_y = textRun.TextAttrs.Size;

            for (int j = 0; j < characters.Length; j++)
            {
                var tspan = new XElement("tspan",
                    new XAttribute("baseline-shift", "0%"),
                    new XAttribute("font-family", textRun.TextAttrs.Face),
                    new XAttribute("font-size", textRun.TextAttrs.Size),
                    new XAttribute("fill", textRun.TextAttrs.FillColor),
                    new XAttribute("letter-spacing", textRun.TextAttrs.LetterSpacing),
                    new XAttribute("fill-opacity", textRun.TextAttrs.Alpha),
                    new XText(characters[j])
                );

                // ???
                if (textRun.TextAttrs.Bold) { new XAttribute("font-weight", "bold"); };
                if (textRun.TextAttrs.Italic) { new XAttribute("font-style", "italic"); };

                if (textRun.TextAttrs.Face.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tspan.Add(new XAttribute("font-weight", "bold"));
                }

                if (textRun.TextAttrs.Face.IndexOf("Italic", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tspan.Add(new XAttribute("font-style", "italic"));
                }

                // Specify X & Y for first TextRun
                if (i == 0)
                {
                    tspan.Add(new XAttribute("x", anticipated_x));
                    tspan.Add(new XAttribute("y", anticipated_y));
                }

                // If previous TextRuns.Characters had length zero or TextRun.Characters contains escape character \r
                if ((i > 0 && TextElement.TextRuns[i - 1].Characters.Length == 0) || i > 0 && TextElement.TextRuns[i - 1].Characters.Contains("\r"))
                {
                    // Paragraph spacing support, affected by line spacing of previous text run
                    tspan.Add(new XAttribute("dy", carriage_y + (TextElement.TextRuns[i - 1].TextAttrs.LineSpacing / 20) + "em"));
                    tspan.Add(new XAttribute("x", anticipated_x));
                }

                textElement.Add(tspan);
            }
        }

        return textElement;
    }

    private (Dictionary<string, XElement>, List<XElement>) HandleDomShape(Shape shape, string id, Color colorEffect, bool insideMask, bool isMaskShape = false)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        XElement? fill_g, stroke_g;
        Dictionary<string, XElement>? extra_defs;
        if (!isMaskShape && ShapeCache.TryGetValue(shape, out (XElement?, XElement?, Dictionary<string, XElement>?) shapeVal))
        {
            (fill_g, stroke_g, extra_defs) = shapeVal;
            fill_g = fill_g is null ? null : new XElement(fill_g);
            stroke_g = stroke_g is null ? null : new XElement(stroke_g);
            extra_defs = extra_defs is null ? null : new Dictionary<string, XElement>(extra_defs);
        }
        else if (isMaskShape && MaskCache.TryGetValue(shape, out (XElement?, XElement?, Dictionary<string, XElement>?) maskVal))
        {
            (fill_g, stroke_g, extra_defs) = maskVal;
            fill_g = fill_g is null ? null : new XElement(fill_g);
            stroke_g = stroke_g is null ? null : new XElement(stroke_g);
            extra_defs = extra_defs is null ? null : new Dictionary<string, XElement>(extra_defs);
        }
        else
        {
            (fill_g, stroke_g, extra_defs) = ShapeUtils.ConvertShapeToSVG(shape, isMaskShape);
            if (!isMaskShape) ShapeCache[shape] = (fill_g, stroke_g, extra_defs);
            else MaskCache[shape] = (fill_g, stroke_g, extra_defs);
        }
        if (fill_g is not null)
        {
            string fill_id = $"{id}_FILL";
            fill_g.Name = svgNs + "g";
            foreach (var child in fill_g.Descendants())
            {
                child.Name = svgNs + child.Name.LocalName;
            }
            fill_g.SetAttributeValue("id", fill_id);
            defs[fill_id] = fill_g;
            XElement fill_use = new XElement(svgNs + "use", new XAttribute(HREF, $"#{fill_id}"));

            body.Add(fill_use);
            if (stroke_g is not null)
            {
                var stroke_id = $"{id}_STROKE";
                stroke_g.Name = svgNs + "g";
                foreach (var child in stroke_g.Descendants())
                {
                    child.Name = svgNs + child.Name.LocalName;
                }
                stroke_g.SetAttributeValue("id", stroke_id);
                defs[stroke_id] = stroke_g;
                body.Add(new XElement(svgNs + "use", new XAttribute(HREF, $"#{stroke_id}")));
            }

            if (extra_defs is not null)
            {
                foreach (var def in extra_defs)
                {
                    defs[def.Key] = def.Value;
                }
            }
        }

        return (defs, body);
    }
    private int GetLoopFrame(SymbolInstance instance, int frameOffset)
    {
        int firstFrame = instance.FirstFrame;
        int? lastFrame = instance.LastFrame;
        int loopLength;
        int numFrames = (instance.CorrespondingItem as SymbolItem)!.Timeline.GetFrameCount();
        if (lastFrame is null)
        {
            lastFrame = numFrames - 1;
            loopLength = lastFrame.Value + 1;
        }
        else loopLength = int.IsNegative(lastFrame.Value) ? numFrames - firstFrame : lastFrame.Value - firstFrame + 1;
        string loopType = instance.Loop;
        if (loopType == "single frame") return firstFrame;
        if (loopType == "loop") return firstFrame + (frameOffset % loopLength);
        if (loopType == "play once") return Math.Min(firstFrame + frameOffset, lastFrame.Value);
        if (loopType == "loop reverse") return firstFrame + loopLength - (frameOffset % loopLength);
        if (loopType == "play once reverse") return Math.Max(firstFrame - frameOffset, 0);
        else throw new Exception("Invalid loop type: " + loopType);
    }
}