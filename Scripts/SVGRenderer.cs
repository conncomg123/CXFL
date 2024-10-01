using CsXFL;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SkiaRendering;

public class SVGRenderer
{
    XNamespace xlink = "http://www.w3.org/1999/xlink";
    public static XNamespace svgNs = "http://www.w3.org/2000/svg";
    XName HREF;
    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
    Document Document { get; set; }
    ConcurrentDictionary<Shape, (XElement?, XElement?, Dictionary<string, XElement>?)> ShapeCache = new();

    public SVGRenderer(Document document)
    {
        Document = document;
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
        for (int i = 0; i < frame.Elements.Count; i++)
        {
            Dictionary<string, XElement> d;
            List<XElement> b;
            (d, b) = RenderElement(frame.Elements[i], $"{id}_{i}", frameOffset, colorEffect, insideMask, maskId, isMaskLayer);
            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }
            // add b to a new XElement g for organization and give it a name attribute
            XElement g = new XElement(svgNs + "g", new XAttribute("name", $"{id}_{i}"));
            if (insideMask) g.SetAttributeValue("mask", $"url(#{maskId})");
            foreach (XElement e in b)
            {
                g.Add(e);
            }
            body.Add(g);
        }
        return (defs, body);
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderElement(Element element, string id, int frameOffset, Color colorEffect, bool insideMask, string? maskId = null, bool isMaskShape = false)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        if (element is SymbolInstance si)
        {
            if (si.SymbolType != "graphic") return (defs, body);
            (defs, body) = RenderTimeline((si.CorrespondingItem as SymbolItem)!.Timeline, GetLoopFrame(si, frameOffset), colorEffect, insideMask, "symbol", isMaskShape);
        }
        else if (element is Text text)
        {
            // <!> Hi Soundman!
            body.Add(HandleText((Text)element));
        }
        else if (element is Shape shape)
        {
            (defs, body) = HandleDomShape(shape, id, colorEffect, insideMask, maskId, isMaskShape);
        }
        else if (element is CsXFL.Group group)
        {
            List<Element> children = group.Members;
            bool hasMoreThanOneChild = children.Count > 1;
            for (int i = 0; i < children.Count; i++)
            {
                string memId = hasMoreThanOneChild ? $"{id}_MEMBER_{i}" : id;
                var (d, b) = RenderElement(children[i], memId, frameOffset, colorEffect, insideMask, maskId, isMaskShape);
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
            Matrix mat = element.Matrix;
            if (!Matrix.IsDefaultMatrix(mat))
            {
                string matrix = string.Join(", ", MatrixToList(mat));
                XElement transform = new XElement(svgNs + "g", new XAttribute("transform", $"matrix({matrix})"));
                foreach (XElement element_ in body)
                {
                    transform.Add(element_);
                }
                body = new List<XElement> { transform };
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
    private XElement HandleText(Text TextElement)
    {
        // The handling of textAttrs is WRONG, needs to go into a <style> not like thiS1
        //https://developer.mozilla.org/en-US/docs/Web/SVG/Element/text

        // Only handles pure TextAttrs, come back to this later! > - >
        string TextFace = TextElement.TextRuns[0].TextAttrs.Face;
        string TextFillColor = TextElement.TextRuns[0].TextAttrs.FillColor;
        string TextString = TextElement.TextRuns[0].Characters;
        double FontSize = TextElement.TextRuns[0].TextAttrs.Size;
        double LetterSpacing = TextElement.TextRuns[0].TextAttrs.LetterSpacing;
        double x = TextElement.TextRuns[0].TextAttrs.Indent + TextElement.TextRuns[0].TextAttrs.LeftMargin;
        double y = TextElement.TextRuns[0].TextAttrs.Size;


        // Create the SVG text element
        XElement textElement = new XElement(svgNs + "text",
            new XAttribute("x", x),
            new XAttribute("y", y),
            new XAttribute("font-size", FontSize),
            new XAttribute("font", TextFace),
            new XAttribute("fill", TextFillColor),
            new XText(TextString)
        );

        return textElement;
    }

    private (Dictionary<string, XElement>, List<XElement>) HandleDomShape(Shape shape, string id, Color colorEffect, bool insideMask, string? maskId = null, bool isMaskShape = false)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        XElement? fill_g, stroke_g;
        Dictionary<string, XElement>? extra_defs;
        if (!isMaskShape && ShapeCache.TryGetValue(shape, out (XElement?, XElement?, Dictionary<string, XElement>?) value))
        {
            (fill_g, stroke_g, extra_defs) = value;
        }
        else
        {
            (fill_g, stroke_g, extra_defs) = ShapeUtils.ConvertShapeToSVG(shape, insideMask, maskId);
            if(!isMaskShape) ShapeCache[shape] = (fill_g, stroke_g, extra_defs);
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
            if (!IsColorIdentity(colorEffect))
            {
                string colorId = colorEffect.Root?.Attribute("id")?.Value ?? throw new ArgumentNullException();
                // assume this function exists
                defs[colorId] = ColorEffectUtils.ConvertColorEffectToSVG(colorEffect);
                fill_use.SetAttributeValue("filter", $"url(#{colorId})");
            }
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
        if (lastFrame is null)
        {
            lastFrame = (instance.CorrespondingItem as SymbolItem)!.Timeline.GetFrameCount() - 1;
            loopLength = lastFrame.Value + 1;
        }
        else loopLength = lastFrame.Value - firstFrame + 1;
        string loopType = instance.Loop;
        if (loopType == "single frame") return firstFrame;
        if (loopType == "loop") return (firstFrame + frameOffset) % loopLength;
        if (loopType == "play once") return Math.Min(firstFrame + frameOffset, lastFrame.Value);
        if (loopType == "loop reverse") return (firstFrame + loopLength - frameOffset) % loopLength;
        if (loopType == "play once reverse") return Math.Max(firstFrame - frameOffset, 0);
        else throw new Exception("Invalid loop type: " + loopType);
    }
}