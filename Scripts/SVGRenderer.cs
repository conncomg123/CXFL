using CsXFL;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SkiaRendering;

public class SvgRenderer
{
    XNamespace xlink = "http://www.w3.org/1999/xlink";
    XNamespace svgNs = "http://www.w3.org/2000/svg";
    XName HREF;
    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
    Document Document { get; set; }
    // probably don't need the lru cache here
    public SvgRenderer(Document document)
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
    public XDocument Render(Timeline timeline, int frameIndex, int width, int height, string type = "symbol")
    {
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
    private (Dictionary<string, XElement>, List<XElement>) RenderTimeline(Timeline timeline, int frameIndex, Color colorEffect, bool insideMask, string type = "symbol")
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        string idName = Regex.Replace(timeline.Name, "[^A-Za-z0-9]", "_");
        string id = $"{idName}_{frameIndex}";
        if (insideMask) id = $"Mask_{id}";
        List<Layer> layers = timeline.Layers;
        bool maskIsActive = false;
        // Process layers from back to front
        for (int layerIdx = layers.Count - 1; layerIdx >= 0; layerIdx--)
        {
            Layer layer = layers[layerIdx];
            string layerType = layer.LayerType;
            if (layerType == "guide")
            {
                continue;
            }
            else if (layerType == "mask")
            {
                // End the mask we started earlier
                maskIsActive = false;
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
            Dictionary<string, XElement> d;
            List<XElement> b;
            if (!maskIsActive && pLayerIndex != null)
            {
                Layer parentLayer = layers[pLayerIndex.Value];
                if (parentLayer.LayerType == "mask")
                {
                    maskIsActive = true;
                    string maskId = $"Mask_{id}_{layerIdx}";
                    (d, b) = RenderLayer(layer, frameIndex, maskId + "_MASK", colorEffect, maskIsActive);

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

                    XElement g = new XElement(svgNs + "g", new XAttribute("mask", $"url(#{maskId})"));
                    body.Append(g);
                }
            }


            (d, b) = RenderLayer(layer, frameIndex, $"{id}_Layer{layerIdx}", colorEffect, maskIsActive);
            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }
            if (maskIsActive)
            {
                foreach (XElement e in b)
                {
                    body[-1].Add(e);
                }
            }
            else
            {
                body.AddRange(b);
            }
        }

        return (defs, body);
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderLayer(Layer layer, int frameIndex, string id, Color colorEffect, bool insideMask)
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
            Dictionary<string, XElement> d = new Dictionary<string, XElement>();
            List<XElement> b = new List<XElement>();
            (d, b) = RenderElement(frame.Elements[i], $"{id}_{i}", frameOffset, colorEffect, insideMask);
            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }
            body.AddRange(b);
        }
        return (defs, body);
    }
    private (Dictionary<string, XElement>, List<XElement>) RenderElement(Element element, string id, int frameOffset, Color colorEffect, bool insideMask)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        if (element is SymbolInstance si)
        {
            if (si.SymbolType != "graphic") throw new NotImplementedException("Only graphic symbols are supported for now");
            (defs, body) = RenderTimeline((si.CorrespondingItem as SymbolItem)!.Timeline, GetLoopFrame(si, frameOffset), colorEffect, insideMask);
        }
        else if (element is Shape shape)
        {
            (defs, body) = HandleDomShape(shape, id, colorEffect, insideMask);
        }
        else if (element is CsXFL.Group group)
        {
            List<Element> children = group.Members;
            bool hasMoreThanOneChild = children.Count > 1;
            for (int i = 0; i < children.Count; i++)
            {
                string memId = hasMoreThanOneChild ? $"{id}_MEMBER_{i}" : id;
                (defs, body) = RenderElement(children[i], memId, frameOffset, colorEffect, insideMask);
            }
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

    private (Dictionary<string, XElement>, List<XElement>) HandleDomShape(Shape shape, string id, Color colorEffect, bool insideMask)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();

        (XElement? fill_g, XElement? stroke_g, Dictionary<string, XElement>? extra_defs) = ShapeUtils.ConvertShapeToSVG(shape, insideMask);

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
            if (!insideMask && !IsColorIdentity(colorEffect))
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