using System.Xml.Linq;
namespace CsXFL;

public class Timeline : IDisposable
{
    internal const string TIMELINES_NODEGROUP_IDENTIFIER = "timelines",
    TIMELINE_NODE_IDENTIFIER = "DOMTimeline";
    public static class DefaultValues
    {
        public const string Name = "timeline";
        public const int CurrentFrame = -1;
    }
    private readonly XElement? root;
    private readonly XNamespace ns;
    private readonly List<Layer> layers;
    private string name;
    private int currentFrame;
    private readonly Library? library;
    internal XElement? Root { get { return root; } }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; root?.SetAttributeValue("currentFrame", value); } }
    public int CurrentLayer { get { return layers.FindIndex(layer => layer.Current); } set { SetCurrentLayer(value); } }
    public List<Layer> Layers { get { return layers; } }
    private void LoadLayers(XElement? timelineNode)
    {
        List<XElement>? layerNodes = timelineNode?.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER)?.Elements().ToList();
        if (layerNodes is null) return;
        foreach (XElement layerNode in layerNodes)
        {
            layers.Add(new Layer(layerNode, library));
        }
    }
    internal Timeline(in XElement timelineNode, Library? library)
    {
        root = timelineNode;
        ns = root.Name.Namespace;
        name = (string?)timelineNode.Attribute("name") ?? DefaultValues.Name;
        currentFrame = (int?)timelineNode.Attribute("currentFrame") ?? DefaultValues.CurrentFrame;
        layers = new List<Layer>();
        this.library = library;
        LoadLayers(root);
    }
    internal Timeline() : this(new XElement(Timeline.TIMELINE_NODE_IDENTIFIER), null) { }
    internal Timeline(XNamespace ns, Library library, string name) : this(new XElement(ns + Timeline.TIMELINE_NODE_IDENTIFIER), library)
    {
        Name = name;
    }
    internal Timeline(Timeline other)
    {
        root = other.root is null ? null : new XElement(other.root);
        ns = other.ns;
        name = other.name;
        currentFrame = other.currentFrame;
        layers = new List<Layer>();
        library = other.library;
        LoadLayers(root);
    }
    public void Dispose()
    {
        foreach (Layer layer in layers)
        {
            layer.Dispose();
        }
    }
    public void SetSelectedLayer(int layerIndex, bool appendToCurrentSelection = false)
    {
        if (!appendToCurrentSelection)
        {
            foreach (Layer layer in layers)
            {
                layer.Selected = false;
            }
        }
        layers[layerIndex].Selected = true;
    }
    public void SetCurrentLayer(int layerIndex)
    {
        foreach (Layer layer in layers)
        {
            layer.Current = false;
        }
        layers[layerIndex].Current = true;
    }
    public int GetFrameCount()
    {
        int frameCount = 0;
        foreach (Layer layer in layers)
        {
            if (layer.GetFrameCount() > frameCount) frameCount = layer.GetFrameCount();
        }
        return frameCount;
    }
    public int AddNewLayer(string name, string layerType = "normal")
    {
        XElement newLayer = new(ns + Layer.LAYER_NODE_IDENTIFIER);
        newLayer.SetAttributeValue("name", name);
        newLayer.SetAttributeValue("layerType", layerType);
        if (layerType != "folder")
        {
            XElement frames = new(ns + Frame.FRAMES_NODEGROUP_IDENTIFIER);
            newLayer.Add(frames);
            XElement frame = new(ns + Frame.FRAME_NODE_IDENTIFIER);
            frame.SetAttributeValue("index", 0);
            frame.SetAttributeValue("duration", GetFrameCount());
            frames.Add(frame);
            XElement elements = new(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER);
            frame.Add(elements);
        }
        if (root?.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER) is null) root?.Add(new XElement(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER));
        root?.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER)?.Add(newLayer);
        layers.Add(new Layer(newLayer, library!));
        return layers.Count - 1;
    }
    public int GetLayerCount()
    {
        return layers.Count;
    }
    // dupe single layer
    private Layer DuplicateSingleLayer(int layerIndex, int whereToInsert)
    {
        Layer dupedLayer = new(layers[layerIndex]);
        dupedLayer.Name += "_copy";
        layers[whereToInsert].Root?.AddBeforeSelf(dupedLayer.Root);
        layers.Insert(whereToInsert, dupedLayer);
        SetSelectedLayer(whereToInsert);
        SetCurrentLayer(whereToInsert);
        // increment parentLayerIndex for all layers after the insertion point
        for (int i = whereToInsert + 1; i < layers.Count; i++)
        {
            if (layers[i].ParentLayerIndex is not null && layers[i].ParentLayerIndex >= whereToInsert)
            {
                layers[i].ParentLayerIndex++;
            }
        }
        return dupedLayer;
    }
    // dupe layer, including children
    public void DuplicateLayer(int layerIndex)
    {
        Layer originalLayer = layers[layerIndex];
        if (originalLayer.LayerType != "folder")
        {
            DuplicateSingleLayer(layerIndex, layerIndex);
            return;
        }
        // todo: iterate through all children, dupe them and retain their hierarchy and order
        // lots of ugly looking logic, but it's all needed since we're inserting elements into the list as we iterate through it, probably a better way to do this
        List<int> parentLayerIndices = new();
        parentLayerIndices.Add(layerIndex);
        int insertionPoint = layerIndex, currentIndex = layerIndex + 2;
        int numDupes = 0;
        // dupe original folder
        DuplicateSingleLayer(layerIndex, insertionPoint);
        numDupes++;
        insertionPoint++;
        Layer currentLayer = layers[currentIndex];
        int currentLayerParentIndex = (int)currentLayer.ParentLayerIndex!;
        int currentParentLayerIndicesArrayIndex = 0;
        while (parentLayerIndices.Contains(currentLayer.ParentLayerIndex - numDupes ?? -1))
        {
            if (currentLayer.ParentLayerIndex != currentLayerParentIndex) // will always run after a folder is duplicated
            {
                currentLayerParentIndex = (int)currentLayer.ParentLayerIndex!;
                currentParentLayerIndicesArrayIndex--;
            }
            DuplicateSingleLayer(currentIndex, insertionPoint).ParentLayerIndex = parentLayerIndices[currentParentLayerIndicesArrayIndex];
            numDupes++;
            insertionPoint++;
            currentLayerParentIndex++;
            if (currentLayer.LayerType == "folder")
            {
                parentLayerIndices.Add(insertionPoint - 1);
                currentParentLayerIndicesArrayIndex += 2;
            }
            currentIndex += 2;
            if (currentIndex >= Layers.Count) break;
            currentLayer = layers[currentIndex];

        }
    }
    public List<int> FindLayerIndex(string name)
    {
        List<int> indices = new();
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].Name == name) indices.Add(i);
        }
        return indices;
    }
    public void DeleteLayer(int index)
    {
        Layer currentLayer = layers[index];
        if (currentLayer.LayerType == "folder")
        {
            for (int i = index + 1; i < layers.Count; i++)
            {
                if (layers[i].ParentLayerIndex != index) continue;
                DeleteLayer(i);
                i--;
            }
        }
        currentLayer.Root?.Remove();
        layers.RemoveAt(index);
        for (int i = index; i < layers.Count; i++)
        {
            if (layers[i].ParentLayerIndex is not null && layers[i].ParentLayerIndex > index)
            {
                layers[i].ParentLayerIndex--;
            }
        }
        currentLayer.Dispose();
    }
    public void InsertFrames(int numFrames, bool allLayers = false, int? frameNumIndex = null, Layer? whereToInsert = null)
    {
        frameNumIndex ??= CurrentFrame;
        if (allLayers)
        {
            foreach (Layer layer in layers)
            {
                if (layer.LayerType == "folder") continue;
                layer.InsertFrames(numFrames, frameNumIndex.Value);
            }
        }
        else
        {
            whereToInsert ??= layers[CurrentLayer];
            if (whereToInsert.LayerType == "folder") throw new ArgumentException("Cannot insert frames into a folder layer");
            whereToInsert.InsertFrames(numFrames, frameNumIndex.Value);
        }
    }
    public void RemoveFrames(int numFrames, bool allLayers = false, int? frameNumIndex = null, Layer? whereToRemove = null)
    {
        frameNumIndex ??= CurrentFrame;
        if (allLayers)
        {
            foreach (Layer layer in layers)
            {
                if (layer.LayerType == "folder") continue;
                layer.RemoveFrames(numFrames, frameNumIndex.Value);
            }
        }
        else
        {
            whereToRemove ??= layers[CurrentLayer];
            whereToRemove.RemoveFrames(numFrames, frameNumIndex.Value);
        }
    }
    private void ReorderLayerSingle(int layerToMove, int layerToPutItBy, bool addBefore)
    {
        Layer layer = layers[layerToMove], layerToPutItByLayer = layers[layerToPutItBy];
        layerToPutItBy += addBefore ? 0 : 1;
        if (layerToMove == layerToPutItBy) return;
        layers.RemoveAt(layerToMove);
        layer.Root?.Remove();
        if (layerToMove < layerToPutItBy)
        {
            layerToPutItBy--;
        }
        // update parentLayerIndices of all layers after the removal point
        for (int i = layerToMove; i < layers.Count; i++)
        {
            if (layers[i].ParentLayerIndex is not null && layers[i].ParentLayerIndex > layerToMove)
            {
                layers[i].ParentLayerIndex--;
            }
        }
        layers.Insert(layerToPutItBy, layer);
        if (layerToPutItByLayer.LayerType == "folder" && !addBefore)
            layer.ParentLayerIndex = layerToPutItBy - 1;
        else
            layer.ParentLayerIndex = layerToPutItByLayer.ParentLayerIndex;
        XElement? layersElement = root?.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER);
        if (layersElement != null)
        {
            XElement? layerElement = layer.Root;
            if (addBefore) layersElement.Elements().ElementAt(layerToPutItBy).AddBeforeSelf(layerElement);
            else layersElement.Elements().ElementAt(layerToPutItBy - 1).AddAfterSelf(layerElement);
        }
        for (int i = layerToPutItBy + 1; i < layers.Count; i++)
        {
            Layer currentLayer = layers[i];
            if (currentLayer.ParentLayerIndex is not null && currentLayer.ParentLayerIndex != layer.ParentLayerIndex)
            {
                currentLayer.ParentLayerIndex++;
            }
        }
        return;
    }
    private void ReorderFolder(int folderIndex, int layerToPutItBy, bool addBefore)
    {
        Layer layer = layers[folderIndex], layerToPutItByLayer = layers[layerToPutItBy];
        layerToPutItBy += addBefore ? 0 : 1;
        if (folderIndex == layerToPutItBy) return;
        int? originalParentLayerIndex = layer.ParentLayerIndex;
        // gather all layers within the folder
        List<(int, Layer, int?)> layersToMove = [(folderIndex, layer, originalParentLayerIndex)];
        int lastItemInFolder = folderIndex;
        for (int i = folderIndex + 1; i < layers.Count; i++)
        {
            if (layers[i].ParentLayerIndex == folderIndex)
            {
                lastItemInFolder = i;
            }
        }
        if ((layerToPutItBy - (addBefore ? 0 : 1)) <= lastItemInFolder && layerToPutItBy > folderIndex)
        {
            return;
        }
        for (int i = folderIndex + 1; i <= lastItemInFolder; i++)
        {
            layersToMove.Add((i, layers[i], layers[i].ParentLayerIndex));
        }
        // remove the layers from the list, updating parentLayerIndices of all layers after the last layer to be moved
        for (int i = layersToMove.Count - 1; i >= 0; i--)
        {
            Layer toRemove = layersToMove[i].Item2;
            layers.RemoveAt(layersToMove[i].Item1);
            toRemove.Root?.Remove();
        }
        if (folderIndex < layerToPutItBy)
        {
            layerToPutItBy -= layersToMove.Count;
        }
        int distance = layerToPutItBy - folderIndex;
        foreach (Layer l in layers)
        {
            if (l.ParentLayerIndex is not null && l.ParentLayerIndex > lastItemInFolder && l.ParentLayerIndex != originalParentLayerIndex)
            {
                l.ParentLayerIndex -= layersToMove.Count;
            }
        }
        if (layerToPutItByLayer.LayerType == "folder" && !addBefore)
            layer.ParentLayerIndex = layerToPutItBy - 1;
        else
            layer.ParentLayerIndex = layerToPutItByLayer.ParentLayerIndex;
        // insert the layers into the new position, updating parentLayerIndices of all layers after the insertion point
        for (int i = 0; i < layersToMove.Count; i++)
        {
            Layer toInsert = layersToMove[i].Item2;
            layers.Insert(layerToPutItBy + i, toInsert);
            if (i > 0) toInsert.ParentLayerIndex = layersToMove[i].Item3 + distance;
            XElement? layersElement = root?.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER);
            if (layersElement != null)
            {
                XElement? layerElement = toInsert.Root;
                if (addBefore) layersElement.Elements().ElementAt(layerToPutItBy + i).AddBeforeSelf(layerElement);
                else layersElement.Elements().ElementAt(layerToPutItBy + i - 1).AddAfterSelf(layerElement);
            }
        }
        for (int i = layerToPutItBy + layersToMove.Count; i < layers.Count; i++)
        {
            Layer currentLayer = layers[i];
            if (currentLayer.ParentLayerIndex is not null && currentLayer.ParentLayerIndex != layer.ParentLayerIndex)
            {
                currentLayer.ParentLayerIndex += layersToMove.Count;
            }
        }
    }
    public void ReorderLayer(int layerToMove, int layerToPutItBy, bool addBefore = true)
    {
        // need to parentLayerIndices, moving folders, moving layers into folders, moving layers out of folders
        Layer layer = layers[layerToMove];
        if (layer.LayerType == "folder") ReorderFolder(layerToMove, layerToPutItBy, addBefore);
        else ReorderLayerSingle(layerToMove, layerToPutItBy, addBefore);
    }
    private List<Layer> GetLayersInFolder(int folderIndex)
    {
        List<Layer> layersInFolder = new();
        for (int i = folderIndex + 1; i < layers.Count; i++)
        {
            if (layers[i].ParentLayerIndex == folderIndex)
            {
                layersInFolder.Add(layers[i]);
            }
        }
        return layersInFolder;
    }
    public SymbolItem ConvertLayersToSymbol(List<Layer> layers, string symbolName, string symbolType = "movie clip")
    {
        // if any of the layers are folders, add them to the list of layers to convert while retaining order
        List<Layer> newLayers = new List<Layer>(layers);
        for (int i = 0; i < newLayers.Count; i++)
        {
            Layer currentLayer = newLayers[i];
            if (currentLayer.LayerType == "folder")
            {
                var layersInFolder = GetLayersInFolder(this.layers.IndexOf(currentLayer));
                foreach (var layerInFolder in layersInFolder)
                {
                    if (!newLayers.Contains(layerInFolder))
                    {
                        newLayers.Add(layerInFolder);
                    }
                }
            }
        }
        newLayers = newLayers.OrderBy(this.layers.IndexOf).ToList();
        int firstLayerIndex = this.layers.IndexOf(newLayers[0]);
        SymbolItem symbol = (library!.AddNewItem(symbolType, symbolName) as SymbolItem)!;
        symbol.Timeline.Layers.AddRange(newLayers);
        var symbolTimelineRoot = symbol.Timeline.Root;
        if (symbolTimelineRoot!.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER) is null)
            symbolTimelineRoot!.Add(new XElement(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER));
        // after adding the layers to the symbol, need to update the parentLayerIndices of the layers that were moved into the symbol
        for (int i = 0; i < newLayers.Count; i++)
        {
            Layer curLayer = newLayers[i];
            if (curLayer.ParentLayerIndex is not null)
            {
                curLayer.ParentLayerIndex -= this.layers.IndexOf(curLayer) - i;
            }
        }
        foreach (Layer layer in newLayers)
        {
            symbol.Timeline.Root!.Element(ns + Layer.LAYERS_NODEGROUP_IDENTIFIER)?.Add(layer.Root);
            layer.Root!.Remove();
        }
        this.layers.RemoveAll(newLayers.Contains);
        int newLayerIndex = AddNewLayer(symbolName);
        Layer newLayer = this.layers[newLayerIndex];
        ReorderLayer(newLayerIndex, firstLayerIndex);
        library.AddItemToDocument(symbolName, newLayer.KeyFrames[0]);
        return symbol;
    }
}