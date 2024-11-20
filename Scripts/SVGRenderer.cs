using CsXFL;
using SixLabors.Fonts;
using System.Collections.Concurrent;
using System.Data;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Rendering;
public partial class Regexes
{
    [GeneratedRegex(@"^.*\.([a-zA-Z]+)\(([0-9]+)\);$")]
    public static partial Regex AS3CommandRegex();
}
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
    private ConcurrentDictionary<BitmapItem, string> ImageCache = new();
    public string? ImageFolder;
    public ConcurrentDictionary<(Timeline, Frame), (string actionscript, bool isOnMainTimeline)> ActionscriptCache = new();
    public ConcurrentDictionary<Timeline, ConcurrentDictionary<int, ConcurrentBag<string>>> CompiledActionscriptCache = new();
    public ConcurrentDictionary<SymbolInstance, (Timeline timeline, Layer layer, Frame frame)> SymbolCache = new();
    private void LoadDocumentCaches()
    {
        void ParseTimelines(List<Timeline> timelines, bool isOnMainTimeline)
        {
            foreach (Timeline t in timelines)
            {
                CompiledActionscriptCache.TryAdd(t, new ConcurrentDictionary<int, ConcurrentBag<string>>());
                var currentCompiledTimeline = CompiledActionscriptCache[t];
                foreach (Layer l in t.Layers)
                {
                    foreach (Frame f in l.KeyFrames)
                    {
                        if (f.ActionScript is not null)
                        {
                            ActionscriptCache.TryAdd((t, f), (f.ActionScript, isOnMainTimeline));
                            if (currentCompiledTimeline.TryGetValue(f.StartFrame, out var actionscriptList))
                            {
                                actionscriptList.Add(f.ActionScript);
                            }
                            else
                            {
                                ConcurrentBag<string> cache = new ConcurrentBag<string> { f.ActionScript };
                                currentCompiledTimeline.TryAdd(f.StartFrame, cache);
                            }
                        }
                        foreach (SymbolInstance si in f.Elements.OfType<SymbolInstance>()) SymbolCache.TryAdd(si, (t, l, f));
                    }
                }
            }
        }
        ParseTimelines(Document.Timelines, true);
        ParseTimelines(Document.Library.Items.Values.OfType<SymbolItem>().Select(si => si.Timeline).ToList(), false);
    }
    public SVGRenderer(Document document, string? imageFolder = null, bool repalceMasksWithClipPaths = true)
    {
        Document = document;
        RepalceMasksWithClipPaths = repalceMasksWithClipPaths;
        HREF = XName.Get("href", xlink.ToString());
        nsmgr.AddNamespace("xlink", xlink.ToString());
        ImageFolder = imageFolder;
        LoadDocumentCaches();
    }
    private static bool IsColorIdentity(Color color)
    {
        return color.RedMultiplier == Color.DefaultValues.RedMultiplier
            && color.GreenMultiplier == Color.DefaultValues.GreenMultiplier
            && color.BlueMultiplier == Color.DefaultValues.BlueMultiplier
            && color.AlphaMultiplier == Color.DefaultValues.AlphaMultiplier
            && color.RedOffset == Color.DefaultValues.RedOffset
            && color.GreenOffset == Color.DefaultValues.GreenOffset
            && color.BlueOffset == Color.DefaultValues.BlueOffset
            && color.AlphaOffset == Color.DefaultValues.AlphaOffset
            && color.TintColor == Color.DefaultValues.TintColor
            && color.TintMultiplier == Color.DefaultValues.TintMultiplier;
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
            for (int i = 0; i < pathElements.Count; i++)
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
    private (Dictionary<string, XElement>, List<XElement>) RenderTimeline(Timeline timeline, int frameIndex, Color colorEffect, bool insideMask, string type = "symbol", bool isMaskLayer = false, List<(SymbolInstance, int)>? symbolHierarchy = null)
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
                (d, b) = RenderLayer(layer, frameIndex, maskId + "_MASK", colorEffect, maskIsActive, maskId, true, symbolHierarchy);
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
                if (RepalceMasksWithClipPaths) mask = ConvertMaskToClipPath(mask, defs);
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

            XElement g = new XElement(svgNs + "g", new XAttribute("name", $"{id}_Layer{layerIdx}"));
            (d, b) = RenderLayer(layer, frameIndex, $"{id}_Layer{layerIdx}", colorEffect, maskIsActive, maskId, isMaskLayer, symbolHierarchy);
            Frame? frame = (frameIndex >= layer.GetFrameCount()) ? null : layer.GetFrame(frameIndex);

            // Soundman's Blend Mode / Color Effect / Filter lollapalooza
            if (frame is not null)
            {
                if (frame.BlendMode != "normal")
                {
                    string[] standardBlendModes = { "multiply", "screen", "overlay", "darken", "lighten", "hard-light", "difference" };

                    if (standardBlendModes.Contains(frame.BlendMode)) { g.SetAttributeValue("style", $"mix-blend-mode: {frame.BlendMode};"); };
                    if (frame.BlendMode == "hardlight") g.SetAttributeValue("style", $"mix-blend-mode: hard-light;");
                    if (frame.BlendMode == "add") g.SetAttributeValue("style", $"mix-blend-mode: color-dodge;");

                    // Requires dedicated invert ComponentTransfer + sRGB color space
                    if (frame.BlendMode == "subtract")
                    {
                        string filterName = "genericFilter_InvertColors";
                        var f_InvertColors = new FilterUtils.CompoundFilter
                        {
                            Name = filterName,
                            Filters = new List<FilterUtils.AtomicFilter>()
                        };
                        f_InvertColors.Filters.Add(new FilterUtils.FeComponentTransfer
                        {
                            Functions = new List<FilterUtils.FeFunc>
                        {
                            new FilterUtils.FeFuncImpl("R")
                            {
                                Operation = "table",
                                TableValues = "1 0"
                            },
                            new FilterUtils.FeFuncImpl("G")
                            {
                                Operation = "table",
                                TableValues = "1 0"
                            },
                            new FilterUtils.FeFuncImpl("B")
                            {
                                Operation = "table",
                                TableValues = "1 0"
                            }
                        }
                        });
                        (var fDefs, g) = FilterUtils.ApplyFilter(g, f_InvertColors);
                        defs[filterName] = fDefs;
                        g.SetAttributeValue("style", $"mix-blend-mode: multiply;");
                    };

                    // This is Difference, but flooded white
                    if (frame.BlendMode == "invert")
                    {
                        string filterName = "genericFilter_FloodWhite";
                        var f_FloodWhite = new FilterUtils.CompoundFilter
                        {
                            Name = filterName,
                            Filters = new List<FilterUtils.AtomicFilter>()
                        };
                        f_FloodWhite.Filters.Add(new FilterUtils.FeColorMatrix("1 0 0 0 255 0 1 0 0 255 0 0 1 0 255 0 0 0 1 0", "matrix"));
                        (var fDefs, g) = FilterUtils.ApplyFilter(g, f_FloodWhite);
                        defs[filterName] = fDefs;
                        g.SetAttributeValue("style", $"mix-blend-mode: difference;");
                    };

                    // SWF documentation for BlendMode logic
                    // https://www.m2osw.com/mo_references_view/libsswf/classsswf_1_1BlendMode

                    // SVG Porter-Duff operations
                    // https://drafts.fxtf.org/compositing-2/#ltblendmodegt

                    // Erase appears to be Destination-Out, between SourceGraphic and Backdrop. However, I know of no way to reference the Backdrop other than the CSS mix-blend-mode styling.
                    // We will probably end up using an inverse clipPath or Mask to make this work instead of trying to get the backdrop.

                    // https://youtu.be/UBCS5EPkiQ0
                    // This is the only video I've found that cleanly explains what the Alpha blend mode does. It's bizzare and useless and I don't want to program it.

                    // For our purposes Layer blend mode is useless, it forces the creation of an alpha channel so erase and alpha blend modes can be used. This is stupid, we're not doing that.

                    if (frame.BlendMode == "erase")
                    {

                    };

                    if (frame.BlendMode == "alpha")
                    {

                    }

                }

                var masterFilter = new FilterUtils.CompoundFilter
                {
                    Name = $"Filter_{id}_Layer{layerIdx}",
                    Filters = new List<FilterUtils.AtomicFilter>()
                };

                if (frame.Filters.Count > 0)
                {
                    foreach (var (filter, filterIndex) in frame.Filters.Select((filter, index) => (filter, index)))
                    {
                        // These reimplementations of native Animate filters will always look different in some capacity because as far as I can tell, Animate internally uses a
                        // fast box convolution blur with subpixel (twip) precision where we use a gaussian blur. I don't know of any way to get subpixel precision with a convolution matrix
                        // in SVG, but for all intents and purposes, a gaussian blur will do just fine.
                        switch (filter)
                        {
                            case DropShadowFilter dropShadowFilter:
                                var anDropShadow = new FilterUtils.AnDropShadow(dropShadowFilter.BlurX, dropShadowFilter.BlurY, dropShadowFilter.Distance, dropShadowFilter.Angle, dropShadowFilter.Strength, dropShadowFilter.Color, dropShadowFilter.Knockout, dropShadowFilter.Inner, dropShadowFilter.HideObject);
                                foreach (var _filter in anDropShadow.Filters)
                                {
                                    masterFilter.Filters.Add(_filter);
                                    if (_filter == anDropShadow.Filters.Last())
                                    {
                                        _filter.Attributes["result"] = $"filter_output_{filterIndex}";
                                    }
                                }
                                break;
                            case AdjustColorFilter adjustColorFilter:
                                // Process AnAdjustColor
                                // Oh boy, where to begin. HueRotation is correct, everything else is close enough. I gave it a sporting chance, but it's just too stupid trying to get this thing one-to-one.
                                // Brightness, Contrast and Saturation are all messily interlinked in Animate and changing any of them changes the other. Bad matrix math, not my fault.
                                var styleAttribute1 = g.Attribute("style");
                                if (styleAttribute1 != null)
                                {
                                    styleAttribute1.Value += $" filter: hue-rotate({adjustColorFilter.Hue}deg) brightness({(adjustColorFilter.Brightness + 100) / 100}) contrast({(adjustColorFilter.Contrast + 100) / 100}) saturate({(adjustColorFilter.Saturation + 100) / 100});";
                                }
                                else
                                {
                                    g.SetAttributeValue("style", $"filter: hue-rotate({adjustColorFilter.Hue}deg) brightness({(adjustColorFilter.Brightness + 100) / 100}) contrast({(adjustColorFilter.Contrast + 100) / 100}) saturate({(adjustColorFilter.Saturation + 100) / 100});");
                                }
                                break;
                            case BlurFilter blurFilter:
                                // Process BlurFilter
                                masterFilter.Filters.Add(new FilterUtils.FeGaussianBlur(blurFilter.BlurX / 2, blurFilter.BlurY / 2));
                                break;
                            // <!> Why does this not work when it's red?
                            case GlowFilter glowFilter:
                                // Process GlowFilter
                                var anGlow = new FilterUtils.AnDropShadow(glowFilter.BlurX, glowFilter.BlurY, 0, 0, glowFilter.Strength, glowFilter.Color, glowFilter.Knockout, glowFilter.Inner, false);
                                foreach (var _filter in anGlow.Filters)
                                { masterFilter.Filters.Add(_filter); };
                                break;
                            default:
                                throw new ArgumentException($"Unknown filter type {filter}");
                        }
                    }
                }

                // Correctly merge filters lol
                var mergeFilter = new FilterUtils.FeMerge();
                for (int i = frame.Filters.Count - 1; i >= 0; i--)
                {
                    mergeFilter.AddNode(new FilterUtils.FeMergeNode($"filter_output_{i}"));
                }

                masterFilter.Filters.Add(mergeFilter);

                if (frame.FrameColor != null)
                {
                    var multiplierList = ColorEffectUtils.GetMultipliers(frame.FrameColor);
                    var multiplier = multiplierList[0];
                    var offset = multiplierList[1];

                    var feColorMatrix = new FilterUtils.FeColorMatrix(
                        string.Format(
                            "{0} 0 0 0 {4} " +
                            "0 {1} 0 0 {5} " +
                            "0 0 {2} 0 {6} " +
                            "0 0 0 {3} 0",
                            multiplier.Item1, multiplier.Item2, multiplier.Item3, multiplier.Item4,
                            offset.Item1, offset.Item2, offset.Item3
                        ),
                        "matrix"
                    );
                    masterFilter.Filters.Add(feColorMatrix);
                }

                // If a CSS Style exists, inject the SVG filter into the style, otherwise, apply the filter to the SVG group.
                var styleAttribute = g.Attribute("style");
                if (styleAttribute != null)
                {
                    var styleValue = styleAttribute.Value;
                    var filterIndex = styleValue.IndexOf("filter: ");
                    if (filterIndex != -1)
                    {
                        styleAttribute.Value = styleValue.Insert(filterIndex + 8, $"url(#{masterFilter.Name}) ");
                        defs[masterFilter.Name] = masterFilter.ToXElement();
                    }
                    else if (frame.FrameColor != null)
                    {
                        styleAttribute.Value += $" filter: url(#{masterFilter.Name})";
                        defs[masterFilter.Name] = masterFilter.ToXElement();
                    }
                }
                else if (frame.Filters.Count > 0 || frame.FrameColor != null)
                {
                    g.Add(new XAttribute("filter", $"url(#{masterFilter.Name})"));
                    defs[masterFilter.Name] = masterFilter.ToXElement();
                }
            }
            foreach (XElement e in b)
            {
                g.Add(e);
            }

            foreach (var def in d)
            {
                defs[def.Key] = def.Value;
            }

            body.Add(g);
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
    private (Dictionary<string, XElement>, List<XElement>) RenderLayer(Layer layer, int frameIndex, string id, Color colorEffect, bool insideMask, string? maskId = null, bool isMaskLayer = false, List<(SymbolInstance, int)>? symbolHierarchy = null)
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
            (d, b) = RenderElement(frame.Elements[i], $"{id}_{i}", frameOffset, interpColor ?? colorEffect, insideMask, isMaskLayer, interpMat, interpShape, symbolHierarchy);
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
    private (Dictionary<string, XElement>, List<XElement>) RenderElement(Element element, string id, int frameOffset, Color colorEffect, bool insideMask, bool isMaskShape = false, Matrix? interpMat = null, Shape? interpShape = null, List<(SymbolInstance, int)>? symbolHierarchy = null)
    {
        Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        List<XElement> body = new List<XElement>();
        if (element is SymbolInstance si)
        {
            symbolHierarchy ??= new List<(SymbolInstance, int)>();
            symbolHierarchy.Add((si, frameOffset)); // push
            int loopFrame = default;
            if (si.SymbolType == "graphic") loopFrame = GetLoopFrame(si, frameOffset);
            else if (si.SymbolType == "movie clip")
            {
                loopFrame = GetMovieClipLoopFrame(si, symbolHierarchy);
            }
            (defs, body) = RenderTimeline((si.CorrespondingItem as SymbolItem)!.Timeline, loopFrame, Color.DefaultColor(), insideMask, "symbol", isMaskShape, symbolHierarchy);
            symbolHierarchy.RemoveAt(symbolHierarchy.Count - 1); // pop
        }
        else if (element is Text text)
        {
            // <!> Hi Soundman!
            body.Add(HandleText(text));
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
                var (d, b) = RenderElement(children[i], memId, frameOffset, colorEffect, insideMask, isMaskShape, null, null, symbolHierarchy);
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
        else if (element is PrimitiveOval primitiveOval)
        {
            body.Add(HandleOval(primitiveOval));
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

    public XElement GenerateEllipsePath(double cx, double cy, double rx, double ry, double t1, double delta, double phi, bool reverse = false)
    {
        const double pi = Math.PI;
        delta = delta % (2 * pi);
        double rotX = Math.Cos(phi);
        double rotY = Math.Sin(phi);
        double startX = cx + (rx * Math.Cos(t1) * rotX - ry * Math.Sin(t1) * rotY);
        double startY = cy + (rx * Math.Cos(t1) * rotY + ry * Math.Sin(t1) * rotX);

        if (delta == 0) {
            return new XElement(svgNs + "path",
                new XAttribute("d", $"M {cx} {cy - ry} " + 
                                $"C {cx + rx * 0.55} {cy - ry} {cx + rx} {cy - ry * 0.55} {cx + rx} {cy} " + 
                                $"C {cx + rx} {cy + ry * 0.55} {cx + rx * 0.55} {cy + ry} {cx} {cy + ry} " + 
                                $"C {cx - rx * 0.55} {cy + ry} {cx - rx} {cy + ry * 0.55} {cx - rx} {cy} " + 
                                $"C {cx - rx} {cy - ry * 0.55} {cx - rx * 0.55} {cy - ry} {cx} {cy - ry}"));
        } else {
            double endX = cx + (rx * Math.Cos(t1 + delta) * rotX - ry * Math.Sin(t1 + delta) * rotY);
            double endY = cy + (rx * Math.Cos(t1 + delta) * rotY + ry * Math.Sin(t1 + delta) * rotX);
            int fA = (delta > pi) ? 1 : 0;
            int fS = (delta > 0) ? 1 : 0;

        if (reverse) {
            (startX, startY, endX, endY) = (endX, endY, startX, startY);
            fS = ~fS & 1;
        }

        return new XElement(svgNs + "path",
            new XAttribute("d", $"M {startX} {startY} A {rx} {ry} {phi / (2 * pi) * 360} {fA} {fS} {endX} {endY}"));
        }
    }

    //Shearing wrong    
    private XElement HandleOval(PrimitiveOval primitiveOval) 
    {
        Dictionary<string, XElement> tmpDefs = new Dictionary<string, XElement>();
        XElement? svgEllipse;

        double centerX = primitiveOval.X + primitiveOval.ObjectWidth / 2;
        double centerY = primitiveOval.Y + primitiveOval.ObjectHeight / 2;
        double radiusX = primitiveOval.ObjectWidth / 2;
        double radiusY = primitiveOval.ObjectHeight / 2;
        double anulusRadiusX = radiusX * primitiveOval.InnerRadius / 100.0;
        double anulusRadiusY = radiusY * primitiveOval.InnerRadius / 100.0;
        double startRads = primitiveOval.StartAngle * Math.PI / 180;
        double endRads = primitiveOval.EndAngle * Math.PI / 180;
        double delta = endRads - startRads;

        bool simpleEllipse = (startRads == endRads) && (primitiveOval.InnerRadius == 0);

        if (startRads > endRads)
        {
            startRads -= 2 * Math.PI;
            delta = endRads - startRads;
        }

        if (simpleEllipse)
        {
            svgEllipse = new XElement(svgNs + "ellipse",
                new XAttribute("cx", centerX),
                new XAttribute("cy", centerY),
                new XAttribute("rx", radiusX),
                new XAttribute("ry", radiusY)
            );
        }
        else
        {
            svgEllipse = GenerateEllipsePath(centerX, centerY, radiusX, radiusY, startRads, delta, 0);
        }
        XElement svgAnnulus = GenerateEllipsePath(centerX, centerY, anulusRadiusX, anulusRadiusY, startRads, delta, 0, true);

        if (!simpleEllipse && primitiveOval.ClosePath && !(startRads == endRads))
        {
            XElement newPath = new XElement(svgNs + "path");
            string[] ovalPoints = (svgEllipse.Attribute("d")?.Value ?? "").Split(' ');
            string[] anulusPoints = (svgAnnulus.Attribute("d")?.Value ?? "").Split(' ');
            string ovalStartPoint = ovalPoints[1] + " " + ovalPoints[2];
            string anulusStartPoint = anulusPoints[1] + " " + anulusPoints[2];
            string anulusEndPoint = anulusPoints[anulusPoints.Length - 2] + " " + anulusPoints[anulusPoints.Length - 1];
            string pathValue = $"M {anulusEndPoint} L {ovalStartPoint}" + (svgEllipse.Attribute("d")?.Value ?? "").Replace("M", "") + " ";
            pathValue += $"L {anulusStartPoint}";
            pathValue += (svgAnnulus.Attribute("d")?.Value ?? "").Replace("M", "") + " Z";
            newPath.Add(new XAttribute("d", pathValue));
            svgEllipse = newPath;
        } else if (!simpleEllipse && (startRads == endRads)) {
            XElement newPath = new XElement(svgNs + "path");
            string pathValue = (svgEllipse.Attribute("d")?.Value ?? "") + (svgAnnulus.Attribute("d")?.Value ?? "");
            newPath.Add(new XAttribute("d", pathValue));
            newPath.Add(new XAttribute("fill-rule", "evenodd"));
            svgEllipse = newPath;
        }
 
        if (primitiveOval.Stroke != null)
        {
            if (primitiveOval.Stroke.Stroke is SolidStroke solidStroke)
            {
                svgEllipse.Add(new XAttribute("stroke", solidStroke.SolidColor.Color));
                svgEllipse.Add(new XAttribute("stroke-width", solidStroke.Weight));
                if (primitiveOval.InnerRadius > 0) {
                    svgAnnulus.Add(new XAttribute("stroke", solidStroke.SolidColor.Color));
                    svgAnnulus.Add(new XAttribute("stroke-width", solidStroke.Weight));
                }
            }
        }
        if (primitiveOval.Fill != null)
        {
            if (primitiveOval.Fill.SolidColor != null)
            {
                svgEllipse.Add(new XAttribute("fill", primitiveOval.Fill.SolidColor.Color));
            }
            else
            {
                var gradientElement = primitiveOval.Fill.LinearGradient != null
                    ? GradientUtils.ConvertLinearGradientToSVG(primitiveOval.Fill.LinearGradient)
                    : primitiveOval.Fill.RadialGradient != null
                        ? GradientUtils.ConvertRadialGradientToSVG(primitiveOval.Fill.RadialGradient)
                        : null;

                if (gradientElement != null)
                {
                    svgEllipse.Add(new XAttribute("fill", $"url(#{gradientElement.Attribute("id")?.Value})"));
                    tmpDefs.Add(gradientElement.Attribute("id")?.Value ?? string.Empty, gradientElement);
                }
            }
        } else {
            svgEllipse.Add(new XAttribute("fill", "none"));
        }
        svgAnnulus.Add(new XAttribute("fill", "none"));

        if (!primitiveOval.ClosePath) {
        XElement svgGroup = new XElement(svgNs + "g");
        svgGroup.Add(svgEllipse);
        svgGroup.Add(svgAnnulus);

        return svgGroup;
        } else {
            return svgEllipse;
        }
    }

    private XElement HandleBitmap(BitmapInstance bitmap)
    {
        BitmapItem correspondingItem = (BitmapItem)bitmap.CorrespondingItem!;
        string dataUrl;
        lock (correspondingItem)
        {
            if (ImageCache.TryGetValue(correspondingItem, out var result))
            {
                dataUrl = result;
            }
            else
            {
                dataUrl = GetBitmapData(bitmap);
                if (ImageFolder is not null)
                {
                    // now copy that data to a file that can be pointed to
                    string imgPath = Path.Combine(ImageFolder, Path.GetFileName(correspondingItem.Href));
                    if (!Directory.Exists(Path.GetDirectoryName(imgPath)!)) Directory.CreateDirectory(Path.GetDirectoryName(imgPath)!);
                    File.WriteAllBytes(imgPath, Convert.FromBase64String(dataUrl[(dataUrl.IndexOf(',') + 1)..]));
                    dataUrl = "file:///" + imgPath;
                }
                ImageCache[correspondingItem] = dataUrl;
            }
        }
        XNamespace xlinkNs = "http://www.w3.org/1999/xlink";
        XElement imageElement = new XElement(svgNs + "image",
        new XAttribute(xlinkNs + "href", dataUrl),
        new XAttribute("width", bitmap.HPixels.ToString()),
        new XAttribute("height", bitmap.VPixels.ToString()));
        return imageElement;
    }
    private static double GetTextRenderSize(string text, Font font)
    {
        TextOptions textOptions = new TextOptions(font);
        return TextMeasurer.MeasureSize(text, textOptions).Width;
    }
    private static string BestMatch(IEnumerable<string> strings, string target)
    {
        return strings.OrderByDescending(s => s.Zip(target, (c1, c2) => c1 == c2).TakeWhile(b => b).Count()).First();
    }
    private static Font GetFontFromNameWithoutSpaces(string fontNameWithoutSpaces, float size)
    {
        // sometimes these have dashes
        fontNameWithoutSpaces = fontNameWithoutSpaces.Replace("-", "");
        string bestMatch = BestMatch(SystemFonts.Families.Select(f => f.Name), fontNameWithoutSpaces);
        return SystemFonts.CreateFont(bestMatch, size);
        throw new ArgumentException($"The font '{fontNameWithoutSpaces}' could not be found.");
    }

    // Animate mangles font names. Is it possible to take TextAttrs.Face and get the corresponding Windows font? Will be needed for font embedding.

    // Intended approach for Animate is to create a mask of text bounding box dimensions, and mask the text to the bounding box.
    // This logic is not present at the moment, so text will never cut off if it goes out of bounds
    private XElement HandleText(Text TextElement)
    {
        XElement textElement = new XElement(svgNs + "text",
            new XAttribute("writing-mode", "lr") // Force writing mode to left-right. Circle back to this later.
        );
        if (TextElement is DynamicText)
        {
            // has no newlines, so let's make them
            TextElement = new DynamicText(TextElement); // don't destroy the original
            var textRun = TextElement.TextRuns[0]; // dynamic text only has one run
            string fontName = textRun.TextAttrs.Face;
            double fontSize = textRun.TextAttrs.Size;
            double letterSpacing = textRun.TextAttrs.LetterSpacing;
            if (fontName.EndsWith("Regular")) fontName = fontName[..^"Regular".Length]; // why does animate do this to me :(
            Font font = GetFontFromNameWithoutSpaces(fontName, (float)fontSize);
            string textString = TextElement.GetTextString();
            // iterate over the words and insert newlines once the next word would fill the box
            string[] words = textString.Split(' ');
            StringBuilder sb = new StringBuilder();
            foreach (string word in words)
            {
                int indexBefore = sb.Length;
                sb.Append(word + ' ');
                string curLine = sb.ToString()[(sb.ToString().LastIndexOf('\r') + 1)..];
                double width = GetTextRenderSize(curLine, font);
                width += curLine.Length * letterSpacing;
                if (width > TextElement.Width)
                {
                    sb.Insert(indexBefore, '\r');
                }
            }
            textString = sb.ToString().TrimEnd();
            TextElement.SetTextString(textString);
        }
        for (int i = 0; i < TextElement.TextRuns.Count; i++)
        {
            var textRun = TextElement.TextRuns[i];
            string[] characters = textRun.Characters.Split('\r');

            double carriage_y = 1;
            double anticipated_x = textRun.TextAttrs.LeftMargin + textRun.TextAttrs.Indent;
            string face = textRun.TextAttrs.Face;
            Font font = GetFontFromNameWithoutSpaces(face, textRun.TextAttrs.Size);
            double anticipated_y = (double)font.FontMetrics.HorizontalMetrics.Ascender / font.FontMetrics.UnitsPerEm * textRun.TextAttrs.Size;
            if (face.EndsWith("Regular")) face = face[..^"Regular".Length];
            for (int j = 0; j < characters.Length; j++)
            {
                var tspan = new XElement(svgNs + "tspan",
                    new XAttribute("baseline-shift", "0%"),
                    new XAttribute("font-family", face),
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
                if (j > 0)
                {
                    tspan.SetAttributeValue("y", anticipated_y + j * (TextElement.TextRuns[i].TextAttrs.LineHeight + TextElement.TextRuns[i].TextAttrs.LineSpacing));
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
    private static int GetLoopFrame(SymbolInstance instance, int frameOffset)
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
        else loopLength = int.IsNegative(lastFrame.Value) ? numFrames : lastFrame.Value - firstFrame + 1;
        if (loopLength < 0) loopLength += numFrames;
        string loopType = instance.Loop;
        if (loopType == "single frame") return firstFrame;
        if (loopType == "loop")
        {
            return (firstFrame + (frameOffset % loopLength)) % numFrames;
        }
        if (loopType == "play once")
        {
            if (lastFrame >= firstFrame) return Math.Min(firstFrame + frameOffset, lastFrame.Value);
            else return firstFrame + frameOffset < numFrames ? (firstFrame + frameOffset) : Math.Min(lastFrame.Value, (firstFrame + frameOffset) % numFrames);
        }
        if (loopType == "loop reverse")
        {
            if (lastFrame >= firstFrame) loopLength = numFrames - lastFrame.Value + firstFrame;
            else loopLength = firstFrame - lastFrame.Value;
            int result = (firstFrame - (frameOffset % loopLength)) % numFrames;
            if (result < 0) result += numFrames;
            return result;
        }
        if (loopType == "play once reverse")
        {
            if (lastFrame >= firstFrame) loopLength = numFrames - lastFrame.Value + firstFrame;
            else loopLength = firstFrame - lastFrame.Value;
            if (frameOffset >= loopLength) return lastFrame.Value;
            int result = (firstFrame - (frameOffset % loopLength)) % numFrames;
            if (result < 0) result += numFrames;
            return result;
        }
        else throw new Exception("Invalid loop type: " + loopType);
    }
    private bool IsInstanceVisible(SymbolInstance instance, Frame targetFrame, int frameOffset, List<(SymbolInstance, int)> containersAndOffsets, int depth = 0)
    {
        // TODO: return whether or not instance is visible (at the end of the queue) within the targetFrame 
        // at the frameOffset by going through the containersAndOffsets queue
        if (containersAndOffsets.Count == depth + 1)
        {
            // base case: we're at the end of the queue, so we can just check if the targetFrame contains the instance
            if (targetFrame.Elements.OfType<SymbolInstance>().Where(x => x.CorrespondingItem == instance.CorrespondingItem && x.SymbolType == "movie clip").Any()) return true;
            return false;
        }
        // recursive case: need to change targetFrame, frameOffset, and containersAndOffsets
        (var container, _) = containersAndOffsets[depth];
        // first, verify that the targetFrame contains the container (comparing SymbolItems cuz it persists across keyframes as long as it's the same one)
        if (!targetFrame.Elements.OfType<SymbolInstance>().Where(x => x.CorrespondingItem == container.CorrespondingItem).Any()) return false;
        if (container.SymbolType == "graphic") frameOffset = GetLoopFrame(container, frameOffset);
        else if (container.SymbolType == "movie clip")
        {
            // only pass in up to the container
            frameOffset = GetMovieClipLoopFrame(container, containersAndOffsets.Take(depth + 1).ToList());
        }
        Timeline containerTimeline = (container.CorrespondingItem as SymbolItem)!.Timeline;
        Layer targetLayer = SymbolCache[containersAndOffsets[depth + 1].Item1].layer;
        // verify that containerTimeline contains targetLayer
        if (!containerTimeline.Layers.Contains(targetLayer)) return false;
        targetFrame = targetLayer.GetFrame(frameOffset);
        return IsInstanceVisible(instance, targetFrame, frameOffset - targetFrame.StartFrame, containersAndOffsets, depth + 1);
    }

    class AS3Command
    {
        public const string GOTOANDPLAY = "gotoAndPlay", GOTOANDSTOP = "gotoAndStop";
        // contains the instance name this acts upon, the command, and the frame index
        private readonly string actor;
        private readonly string command;
        private readonly int startFrame;
        public string Actor { get => actor; }
        public string Command { get => command; }
        public int StartFrame { get => startFrame; }
        public AS3Command(string AS3text)
        {
            // TODO: AS3 text will look like instanceName.gotoAndPlay(500); or instanceName.gotoAndStop(9001);
            // goal is to extract the instance name, command, and frame index
            actor = AS3text.Split('.')[0];
            command = AS3text.Split('.')[1].Split('(')[0];
            startFrame = int.Parse(AS3text.Split('.')[1].Split('(')[1].Split(')')[0]) - 1; // as3 is 1-indexed
        }
    }
    private static AS3Command? GetAS3Result(ConcurrentBag<string> AS3Commands, SymbolInstance targetInstance)
    {
        // TODO: go through each command and return whether it's gotoAndStop (false) or gotoAndPlay (true),
        // and the frame index within that command
        // example is instanceName.gotoAndPlay(500); or instanceName.gotoAndStop(9001);
        string instanceName = targetInstance.Name!;
        foreach (string AS3 in AS3Commands)
        {
            // could contain multiple lines, so split by newline
            string[] lines = AS3.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                // first, validate that the line is a valid AS3 command
                // using a regex that matches instanceName.gotoAndPlay(500); or instanceName.gotoAndStop(9001);
                // (semicolon at the end is required here)
                string line = lines[i].Trim();
                if (Regexes.AS3CommandRegex().IsMatch(line))
                {
                    AS3Command result = new(line);
                    if (result.Actor == instanceName) return result;
                }
            }
        }
        return null;
    }
    private int? CheckForActionscript(SymbolInstance instance, Timeline timeline, int startFrame, int endFrame)
    {
        int loopLength = (instance.CorrespondingItem as SymbolItem)!.Timeline.GetFrameCount();
        var compiliedActionscripts = CompiledActionscriptCache[timeline];
        if (compiliedActionscripts is not null)
        {
            for (int curAS3Frame = endFrame; curAS3Frame >= startFrame; curAS3Frame--)
            {
                // var AS3AtThisFrame = compiliedActionscripts[curAS3Frame];
                if (!compiliedActionscripts.TryGetValue(curAS3Frame, out ConcurrentBag<string>? AS3AtThisFrame)) continue;
                if (AS3AtThisFrame is null || AS3AtThisFrame.IsEmpty) continue;
                AS3Command? curCommand = GetAS3Result(AS3AtThisFrame, instance);
                if (curCommand is not null)
                {
                    if (curCommand.Command == AS3Command.GOTOANDSTOP) return curCommand.StartFrame;
                    else if (curCommand.Command == AS3Command.GOTOANDPLAY)
                    {
                        int framesElapsed = endFrame - curAS3Frame;
                        return (curCommand.StartFrame + framesElapsed) % loopLength;
                    }
                }
            }
        }
        return null;
    }
    private int GetMovieClipLoopFrame(SymbolInstance instance, List<(SymbolInstance, int)> containersAndOffsets)
    {
        // TODO: instance is a movie clip either on the main timeline or within a container
        // regardless, we need to find how long the movie clip has been visible on the main timeline
        // and that modulo the loop length will be our offset
        // Idea 1: on the main timeline, propogate backwards to see what the first frame the movie clip is not visible is
        // then use the difference as the offset
        // TODO: this is already pretty fast but can be improved with caching and memoization
        int loopLength = (instance.CorrespondingItem as SymbolItem)!.Timeline.GetFrameCount();
        (var mainTimelineInstance, var mainTimelineOffset) = containersAndOffsets[0];
        bool instanceIsVisible = true; // guaranteed to be visible on the frame this is called
        Layer layer = SymbolCache[mainTimelineInstance].layer;
        Frame frame = SymbolCache[mainTimelineInstance].frame;
        int curFrameIndex = frame.StartFrame + mainTimelineOffset - 1;
        if (curFrameIndex < 0) return 0; // first frame of the main timeline and the first frame it's visible, so no offset
        int numContiguousFrames = 0;
        while (curFrameIndex >= 0)
        {
            Frame curFrame = layer.GetFrame(curFrameIndex);
            if (!curFrame.Elements.OfType<SymbolInstance>().Where(x => x.CorrespondingItem == mainTimelineInstance.CorrespondingItem && x.SymbolType == mainTimelineInstance.SymbolType).Any()) break;
            int remainingFrames = curFrameIndex - curFrame.StartFrame + 1;
            curFrameIndex -= remainingFrames;
            numContiguousFrames += remainingFrames;
        }
        curFrameIndex = frame.StartFrame + mainTimelineOffset;
        int high = curFrameIndex;
        int low = curFrameIndex - numContiguousFrames;
        while (high > low)
        {
            int mid = (high + low) / 2;
            Frame curFrame = layer.GetFrame(mid);
            instanceIsVisible = IsInstanceVisible(instance, curFrame, mid - curFrame.StartFrame, containersAndOffsets);
            if (instanceIsVisible) high = mid;
            else low = mid + 1;
        }
        // high is the first frame the instance is visible
        // see if there is any actionscript on the main timeline that changes the instance's frame
        // (will implement as3 within symbols in the future)
        if (instance.Name is not null)
        {
            int? AS3Frame = CheckForActionscript(instance, SymbolCache[mainTimelineInstance].timeline, high, curFrameIndex);
            if (AS3Frame is not null) return AS3Frame.Value;
        }
        int loopFrame = (curFrameIndex - high) % loopLength;
        return loopFrame;
    }
}