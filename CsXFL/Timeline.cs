using System.Xml.Linq;
namespace CsXFL;

public class Timeline
{
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
    internal XElement? Root { get { return root; } }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; root?.SetAttributeValue("currentFrame", value); } }
    public int CurrentLayer { get { return layers.FindIndex(layer => layer.Current); } set { SetCurrentLayer(value); } }
    public List<Layer> Layers { get { return layers; } }
    private void LoadLayers(XElement? timelineNode)
    {
        List<XElement>? layerNodes = timelineNode?.Element(ns + "layers")?.Elements().ToList();
        if (layerNodes is null) return;
        foreach (XElement layerNode in layerNodes)
        {
            layers.Add(new Layer(layerNode));
        }
    }
    internal Timeline(in XElement timelineNode)
    {
        root = timelineNode;
        ns = root.Name.Namespace;
        name = (string?)timelineNode.Attribute("name") ?? DefaultValues.Name;
        currentFrame = (int?)timelineNode.Attribute("currentFrame") ?? DefaultValues.CurrentFrame;
        layers = new List<Layer>();
        LoadLayers(root);
    }
    internal Timeline() : this(new XElement("timeline")) { }
    internal Timeline(Timeline other)
    {
        root = other.root is null ? null : new XElement(other.root);
        ns = other.ns;
        name = other.name;
        currentFrame = other.currentFrame;
        layers = new List<Layer>();
        LoadLayers(root);
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
    public int AddNewLayer(string name, string layerType)
    {
        XElement newLayer = new(ns + "DOMLayer");
        newLayer.SetAttributeValue("name", name);
        newLayer.SetAttributeValue("layerType", layerType);
        if (layerType != "folder")
        {
            XElement frames = new(ns + "frames");
            newLayer.Add(frames);
            XElement frame = new(ns + "DOMFrame");
            frame.SetAttributeValue("index", 0);
            frame.SetAttributeValue("duration", GetFrameCount());
            frames.Add(frame);
            XElement elements = new(ns + "elements");
            frame.Add(elements);
        }
        root?.Element(ns + "layers")?.Add(newLayer);
        layers.Add(new Layer(newLayer));
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
    }
    public void InsertFrames(int numFrames, bool allLayers = false, int? frameNumIndex = null, Layer? whereToInsert = null)
    {
        frameNumIndex ??= CurrentFrame;
        whereToInsert ??= layers[CurrentLayer];
        if(whereToInsert.LayerType == "folder") throw new ArgumentException("Cannot insert frames into a folder layer");
        if (allLayers)
        {
            foreach (Layer layer in layers)
            {
                if(layer.LayerType == "folder") continue;
                layer.InsertFrames(numFrames, frameNumIndex.Value);
            }
        }
        else
        {
            whereToInsert.InsertFrames(numFrames, frameNumIndex.Value);
        }
    }
    public void RemoveFrames(int numFrames, bool allLayers = false, int? frameNumIndex = null, Layer? whereToRemove = null)
    {
        frameNumIndex ??= CurrentFrame;
        whereToRemove ??= layers[CurrentLayer];
        if (allLayers)
        {
            foreach (Layer layer in layers)
            {
                if(layer.LayerType == "folder") continue;
                layer.RemoveFrames(numFrames, frameNumIndex.Value);
            }
        }
        else
        {
            whereToRemove.RemoveFrames(numFrames, frameNumIndex.Value);
        }
    }
}