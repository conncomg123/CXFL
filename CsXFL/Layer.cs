using System.Xml.Linq;

public class Layer
{
    private static readonly List<string> AcceptableLayerTypes = new List<string> { "normal", "guide", "guided", "mask", "masked", "folder", "camera" };
    public static class DefaultValues
    {
        public const string Color = "#000000";
        public const string LayerType = "normal";
        public const string Name = "layer";
        public const bool Locked = false;
        public const bool Current = false;
        public const bool Selected = false;
    }
    private XElement? root;
    private XNamespace ns;
    private string color, layerType, name;
    private List<Frame> frames;
    private bool locked, current, selected;
    uint? parentLayerIndex;
    private void SetOrRemoveAttribute(in string attributeName, object? value, object? defaultValue)
    {
        if (value == defaultValue)
        {
            root?.Attribute(attributeName)?.Remove();
        }
        else
        {
            root?.SetAttributeValue(attributeName, value);
        }
    }
    public XElement? Root { get { return root; } }
    public string Color { get { return color; } set { color = value; root?.SetAttributeValue("color", value); } }
    public string LayerType
    {
        get { return layerType; }
        set
        {
            if (!AcceptableLayerTypes.Contains(value))
            {
                throw new ArgumentException("Invalid layer type: " + value);
            }
            layerType = value;
            SetOrRemoveAttribute("layerType", value, DefaultValues.LayerType);
        }
    }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public bool Locked { get { return locked; } set { locked = value; SetOrRemoveAttribute("locked", value, DefaultValues.Locked); } }
    public bool Current { get { return current; } set { current = value; SetOrRemoveAttribute("current", value, DefaultValues.Current); } }
    public bool Selected { get { return selected; } set { selected = value; SetOrRemoveAttribute("isSelected", value, DefaultValues.Selected); } }
    public uint? ParentLayerIndex { get { return parentLayerIndex; } set { parentLayerIndex = value; SetOrRemoveAttribute("parentLayerIndex", value, null); } }

    private void LoadFrames(XElement layerNode)
    {
        List<XElement>? frameNodes = layerNode?.Element(ns + "frames")?.Elements().ToList();
        if (frameNodes is null) return;
        foreach (XElement frameNode in frameNodes)
        {
            frames.Add(new Frame(frameNode));
        }
    }
    public Layer(XElement layerNode)
    {
        root = layerNode;
        ns = root.Name.Namespace;
        color = (string?)layerNode.Attribute("color") ?? DefaultValues.Color;
        layerType = (string?)layerNode.Attribute("layerType") ?? DefaultValues.LayerType;
        if(!AcceptableLayerTypes.Contains(layerType))
        {
            throw new ArgumentException("Invalid layer type: " + layerType);
        }
        name = (string?)layerNode.Attribute("name") ?? DefaultValues.Name;
        locked = (bool?)layerNode.Attribute("locked") ?? DefaultValues.Locked;
        current = (bool?)layerNode.Attribute("current") ?? DefaultValues.Current;
        selected = (bool?)layerNode.Attribute("isSelected") ?? DefaultValues.Selected;
        parentLayerIndex = (uint?)layerNode.Attribute("parentLayerIndex");
        frames = new List<Frame>();
        LoadFrames(root);
    }

    public Layer(ref Layer other)
    {
        root = other.root is null ? null : new XElement(other.root);
        ns = other.ns;
        color = other.color;
        layerType = other.layerType;
        name = other.name;
        locked = other.locked;
        current = other.current;
        selected = other.selected;
        parentLayerIndex = other.parentLayerIndex;
        frames = new List<Frame>();
        if (root is not null) LoadFrames(root);
    }
    private void RemoveKeyframe(int keyframeIndex)
    {
        root?.Element(ns + "frames")?.Elements().ToList()[keyframeIndex].Remove();
        frames.RemoveAt(keyframeIndex);
    }
    int GetKeyframeIndex(int frameIndex)
    {
        // return the nth keyframe where n.StartFrame <= frameIndex < (n.StartFrame + n.Duration) with binary search
        int left = 0, right = frames.Count- 1;   
        while (left <= right)
        {
            int mid = (left + right) / 2;
            Frame midFrame = frames[mid];
            if (midFrame.StartFrame <= frameIndex && frameIndex < (midFrame.StartFrame + midFrame.Duration))
            {
                return mid;
            }
            else if (midFrame.StartFrame > frameIndex)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }
        throw new Exception("Keyframe not found");
    }
    public int GetFrameCount()
    {
        return frames.Count > 0 ? frames[^1].StartFrame + frames[^1].Duration : 0;
    }
    public Frame GetFrame(int frameIndex)
    {
        return frames[GetKeyframeIndex(frameIndex)];
    }
    public bool InsertKeyframe(int frameIndex, bool isBlank)
    {
        int index = GetKeyframeIndex(frameIndex);
        Frame frame = frames[index];
        if(frameIndex == frame.StartFrame)
        {
            frameIndex++;
            if(frameIndex >= GetFrameCount()) return false;
            int newIndex = GetKeyframeIndex(frameIndex);
            if(newIndex != index) return false;
        }
        Frame newFrame = new Frame(ref frame, isBlank)
        {
            Name = Frame.DefaultValues.Name,
            Duration = frame.Duration + frame.StartFrame - frameIndex
        };
        frame.Duration = frameIndex - frame.StartFrame;
        newFrame.StartFrame = frameIndex;
        frames.Insert(index + 1, newFrame);
        frame.Root!.AddAfterSelf(newFrame.Root);
        return true;
    }
}