using System.Data;
using System.Xml.Linq;

public class Timeline
{
    public static class DefaultValues
    {
        public const string Name = "timeline";
        public const int CurrentFrame = 0;
    }
    private XElement? root;
    private XNamespace ns;
    private List<Layer> layers;
    private string name;
    private int currentFrame;
    public XElement? Root { get { return root; } }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; root?.SetAttributeValue("currentFrame", value); } }
    private void LoadLayers(XElement timelineNode)
    {
        List<XElement>? layerNodes = timelineNode.Element(ns + "layers")?.Elements().ToList();
        if (layerNodes is null) return;
        foreach (XElement layerNode in layerNodes)
        {
            layers.Add(new Layer(layerNode));
        }
    }
    public Timeline(in XElement timelineNode)
    {
        root = timelineNode;
        ns = root.Name.Namespace;
        name = (string?)timelineNode.Attribute("name") ?? DefaultValues.Name;
        currentFrame = (int?)timelineNode.Attribute("currentFrame") ?? DefaultValues.CurrentFrame;
        layers = new List<Layer>();
        LoadLayers(root);
    }
    public int GetFrameCount()
    {
        int frameCount = 0;
        foreach (Layer layer in layers)
        {
           if(layer.GetFrameCount() > frameCount) frameCount = layer.GetFrameCount();
        }
        return frameCount;
    }
    public int AddNewLayer(string name, string layerType)
    {
        XElement newLayer = new XElement(ns + "DOMLayer");
        newLayer.SetAttributeValue("name", name);
        newLayer.SetAttributeValue("layerType", layerType);
        if (layerType != "folder")
        {
            XElement frames = new XElement(ns + "frames");
            newLayer.Add(frames);
            XElement frame = new XElement(ns + "DOMFrame");
            frame.SetAttributeValue("index", 0);
            frame.SetAttributeValue("duration", GetFrameCount());
            frames.Add(frame);
            XElement elements = new XElement(ns + "elements");
            frame.Add(elements);
        }
        root?.Element(ns + "layers")?.Add(newLayer);
        layers.Add(new Layer(newLayer));
        return layers.Count - 1;
    }
    public Layer GetLayer(int layerIndex)
    {
        return layers[layerIndex];
    }
    public int GetLayerCount()
    {
        return layers.Count;
    }
}