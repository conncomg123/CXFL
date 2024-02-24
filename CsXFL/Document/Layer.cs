using System.Collections.ObjectModel;
using System.Xml.Linq;
namespace CsXFL;

public class Layer
{
    private static readonly HashSet<string> AcceptableLayerTypes = new HashSet<string> { "normal", "guide", "guided", "mask", "masked", "folder", "camera" };
    internal static class DefaultValues
    {
        public const string Color = "#000000";
        public const string LayerType = "normal";
        public const string Name = "layer";
        public const bool Locked = false;
        public const bool Current = false;
        public const bool Selected = false;
    }
    private readonly XElement? root;
    private readonly XNamespace ns;
    private string color, layerType, name;
    private readonly List<Frame> frames;
    private bool locked, current, selected;
    int? parentLayerIndex;
    private readonly Library library;
    internal XElement? Root { get { return root; } }
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
            root?.SetOrRemoveAttribute("layerType", value, DefaultValues.LayerType);
        }
    }
    public string Name { get { return name; } set { name = value; root?.SetAttributeValue("name", value); } }
    public bool Locked { get { return locked; } set { locked = value; root?.SetOrRemoveAttribute("locked", value, DefaultValues.Locked); } }
    public bool Current { get { return current; } set { current = value; root?.SetOrRemoveAttribute("current", value, DefaultValues.Current); } }
    public bool Selected { get { return selected; } set { selected = value; root?.SetOrRemoveAttribute("isSelected", value, DefaultValues.Selected); } }
    public int? ParentLayerIndex { get { return parentLayerIndex; } set { parentLayerIndex = value; root?.SetOrRemoveAttribute("parentLayerIndex", value, null); } }
    public ReadOnlyCollection<Frame> KeyFrames { get { return frames.AsReadOnly(); } }
    private void LoadFrames(XElement layerNode)
    {
        List<XElement>? frameNodes = layerNode?.Element(ns + "frames")?.Elements().ToList();
        if (frameNodes is null) return;
        foreach (XElement frameNode in frameNodes)
        {
            frames.Add(new Frame(frameNode));
        }
    }
    internal Layer(XElement layerNode, Library library)
    {
        root = layerNode;
        ns = root.Name.Namespace;
        color = (string?)layerNode.Attribute("color") ?? DefaultValues.Color;
        layerType = (string?)layerNode.Attribute("layerType") ?? DefaultValues.LayerType;
        if (!AcceptableLayerTypes.Contains(layerType))
        {
            throw new ArgumentException("Invalid layer type: " + layerType);
        }
        name = (string?)layerNode.Attribute("name") ?? DefaultValues.Name;
        locked = (bool?)layerNode.Attribute("locked") ?? DefaultValues.Locked;
        current = (bool?)layerNode.Attribute("current") ?? DefaultValues.Current;
        selected = (bool?)layerNode.Attribute("isSelected") ?? DefaultValues.Selected;
        parentLayerIndex = (int?)layerNode.Attribute("parentLayerIndex");
        frames = new List<Frame>();
        this.library = library;
        LoadFrames(root);
    }

    internal Layer(Layer other)
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
        library = other.library;
        if (root is not null) LoadFrames(root);
    }
    private void RemoveKeyframe(int keyframeIndex)
    {
        root?.Element(ns + "frames")?.Elements().ToList()[keyframeIndex].Remove();
        frames.RemoveAt(keyframeIndex);
    }

    public bool ClearKeyframe(int frameIndex)
    {
        int index = GetKeyframeIndex(frameIndex);
        Frame frame = frames[index];
        if (frameIndex != frame.StartFrame) return false;
        if (frames.Count == 1)
        {
            if (frame.IsEmpty()) return false;
            frame.ClearElements();
            return true;
        }
        if (index == 0)
        {
            Frame nextFrame = frames[index + 1];
            nextFrame.Duration = nextFrame.Duration + frame.Duration;
            nextFrame.StartFrame = 0;
            RemoveKeyframe(index);
            return true;
        }
        Frame previousFrame = frames[index - 1];
        previousFrame.Duration += frame.Duration;
        RemoveKeyframe(index);
        return true;
    }
    private int GetKeyframeIndex(int frameIndex)
    {
        // return the nth keyframe where n.StartFrame <= frameIndex < (n.StartFrame + n.Duration) with binary search
        int left = 0, right = frames.Count - 1;
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
    private bool InsertKeyframe(int frameIndex, bool isBlank)
    {
        int index = GetKeyframeIndex(frameIndex);
        Frame frame = frames[index];
        bool frameHasSymbolInstanceInLoop = frame.Elements.Any(e => e is SymbolInstance si && (si.Loop == "loop" || si.Loop == "play once" || si.Loop == "loop reverse" || si.Loop == "play once reverse"));
        if (frameIndex == frame.StartFrame)
        {
            frameIndex++;
            if (frameIndex >= GetFrameCount()) return false;
            int newIndex = GetKeyframeIndex(frameIndex);
            if (newIndex != index) return false;
        }
        Frame newFrame = new(frame, isBlank)
        {
            Name = Frame.DefaultValues.Name,
            Duration = frame.Duration + frame.StartFrame - frameIndex
        };
        frame.Duration = frameIndex - frame.StartFrame;
        newFrame.StartFrame = frameIndex;
        frames.Insert(index + 1, newFrame);
        frame.Root!.AddAfterSelf(newFrame.Root);
        if (frameHasSymbolInstanceInLoop)
        {
            UpdateSymbolInstanceFirstFrame(frame, newFrame);
        }
        return true;
    }

    private void UpdateSymbolInstanceFirstFrame(Frame frame, Frame newFrame)
    {
        int distanceBetweenFrames = newFrame.StartFrame - frame.StartFrame;
        foreach (Element e in newFrame.Elements)
        {
            if (e is SymbolInstance si)
            {
                Item item = library.Items[si.LibraryItemName];
                switch (si.Loop)
                {
                    case "loop":
                        if (si.LastFrame is null)
                        {
                            si.FirstFrame += distanceBetweenFrames;
                            si.FirstFrame %= (item as SymbolItem)!.Timeline.GetFrameCount();
                        }
                        else
                        {
                            si.FirstFrame += distanceBetweenFrames;
                            si.FirstFrame %= si.LastFrame.Value + 1;
                        }
                        break;
                    case "play once":
                        int maxFrame = si.LastFrame ?? (item as SymbolItem)!.Timeline.GetFrameCount() - 1;
                        si.FirstFrame = Math.Clamp(si.FirstFrame + distanceBetweenFrames, 0, maxFrame);
                        break;
                    case "loop reverse":
                        if (si.LastFrame is null)
                        {
                            int frameCount = (item as SymbolItem)!.Timeline.GetFrameCount();
                            si.FirstFrame += frameCount - distanceBetweenFrames;
                            si.FirstFrame %= frameCount;
                        }
                        else
                        {
                            int loopTime = (item as SymbolItem)!.Timeline.GetFrameCount() - si.LastFrame.Value;
                            distanceBetweenFrames %= loopTime;
                            si.FirstFrame -= distanceBetweenFrames;
                            if (si.FirstFrame < si.LastFrame.Value)
                            {
                                si.FirstFrame += loopTime;
                            }
                        }
                        break;
                    case "play once reverse":
                        int minFrame = si.LastFrame ?? 0;
                        si.FirstFrame = Math.Clamp(si.FirstFrame - distanceBetweenFrames, minFrame, (item as SymbolItem)!.Timeline.GetFrameCount() - 1);
                        break;
                }
            }
        }
    }

    public bool InsertKeyframe(int frameIndex)
    {
        return InsertKeyframe(frameIndex, false);
    }
    public bool InsertBlankKeyframe(int frameIndex)
    {
        return InsertKeyframe(frameIndex, true);
    }
    public bool IsKeyFrame(int frameIndex)
    {
        return GetFrame(frameIndex).StartFrame == frameIndex;
    }
    public bool ConvertToKeyframes(int startFrame, int? endFrame = null)
    {
        endFrame ??= startFrame;
        int numConverted = 0;
        for (int i = startFrame; i <= endFrame; i++)
        {
            if (!IsKeyFrame(i))
            {
                InsertKeyframe(i);
                numConverted++;
            }
        }
        return numConverted > 0;
    }
    internal void InsertFrames(int numFrames, int frameIndex)
    {
        int index = GetKeyframeIndex(frameIndex);
        Frame frame = frames[index];
        frame.Duration += numFrames;
        // increment startFrame for all frames after the insertion point
        for (int i = index + 1; i < frames.Count; i++)
        {
            frames[i].StartFrame += numFrames;
        }
    }
    internal void RemoveFrames(int numFrames, int frameIndex)
    {
        int framesRemaining = numFrames, currentFrameIndex = frameIndex;
        while (framesRemaining > 0)
        {
            int index = GetKeyframeIndex(currentFrameIndex);
            Frame frame = frames[index];
            bool isStartFrame = frame.StartFrame == currentFrameIndex;
            if (isStartFrame && framesRemaining >= frame.Duration)
            {
                RemoveKeyframe(index);
                framesRemaining -= frame.Duration;
                for (int i = index; i < frames.Count; i++)
                {
                    frames[i].StartFrame -= frame.Duration;
                }
            }
            else if (isStartFrame)
            {
                frame.Duration -= framesRemaining;
                framesRemaining = 0;
                for (int i = index + 1; i < frames.Count; i++)
                {
                    frames[i].StartFrame -= framesRemaining;
                }
            }
            else
            {
                // one-time case where not on a start frame
                int startFrameDiff = currentFrameIndex - frame.StartFrame;
                frame.Duration = startFrameDiff;
                framesRemaining -= startFrameDiff;
                for (int i = index + 1; i < frames.Count; i++)
                {
                    frames[i].StartFrame -= startFrameDiff;
                }
            }
        }
    }
}