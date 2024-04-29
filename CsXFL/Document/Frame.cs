namespace CsXFL;

using System.Collections.ObjectModel;
using System.Xml.Linq;

public class Frame : ILibraryEventReceiver, IDisposable
{
    internal const string FRAME_NODE_IDENTIFIER = "DOMFrame",
    FRAMES_NODEGROUP_IDENTIFIER = "frames";
    private static readonly HashSet<string> AcceptableLabelTypes = new HashSet<string> { "none", "name", "comment", "anchor" };
    public enum KeyModes : int
    {
        Normal = 9728,
        ClassicTween = 22017,
        ShapeTween = 17922,
        MotionTween = 8195,
        ShapeLayers = 8192
    }
    internal static class DefaultValues
    {
        public const int StartFrame = 0;
        public const int Duration = 1;
        public const int KeyMode = (int)KeyModes.Normal;
        public const int InPoint44 = 0;
        public const string LabelType = "none";
        public const string Name = "";
        public const string SoundName = "";
        public const string SoundSync = "event";
        public const string TweenType = "none";
        public const bool MotionTweenSnap = false;
        public const string EaseMethodName = "classic";
        public const bool HasCustomEase = false;
        public const bool Bookmark = false;
    }
    private readonly XElement? root;
    private readonly XNamespace ns;
    private readonly List<Element> elements;
    private readonly List<IEase> eases;
    private int startFrame, duration, keyMode, inPoint44;
    private string labelType, name, soundName, soundSync, tweenType, easeMethodName;
    private bool registeredForSoundItem, motionTweenSnap, hasCustomEase, bookmark;
    private Library? library;
    internal XElement? Root { get { return root; } }
    public int StartFrame { get { return startFrame; } set { startFrame = value; root?.SetAttributeValue("index", value); } }
    public int Duration { get { return duration; } set { duration = value; root?.SetOrRemoveAttribute("duration", value, DefaultValues.Duration); } }
    public int KeyMode { get { return keyMode; } set { keyMode = value; root?.SetOrRemoveAttribute("keyMode", value, DefaultValues.KeyMode); } }
    public int InPoint44 { get { return inPoint44; } set { inPoint44 = value; root?.SetOrRemoveAttribute("inPoint44", value, DefaultValues.InPoint44); } }
    public string LabelType
    {
        get { return labelType; }
        set
        {
            if (!AcceptableLabelTypes.Contains(value)) throw new ArgumentException();
            labelType = value;
            root?.SetOrRemoveAttribute("labelType", value, DefaultValues.LabelType);
            Bookmark = value == "anchor";
        }
    }
    public string Name { get { return name; } set { name = value; root?.SetOrRemoveAttribute("name", value, DefaultValues.Name); } }
    public string SoundName
    {
        get { return soundName; }
        set
        {
            SoundItem? oldSoundItem = CorrespondingSoundItem;
            soundName = value;
            SoundItem? newSoundItem = CorrespondingSoundItem;
            root?.SetOrRemoveAttribute("soundName", value, DefaultValues.SoundName);
            registeredForSoundItem = value != DefaultValues.SoundName;
            if (oldSoundItem is not null)
            {
                LibraryEventMessenger.Instance.UnregisterReceiver(oldSoundItem, this);
                oldSoundItem.UseCount--;
            }
            if (registeredForSoundItem && newSoundItem is not null)
            {
                LibraryEventMessenger.Instance.RegisterReceiver(newSoundItem!, this);
                newSoundItem.UseCount++;
            }
        }
    }
    public SoundItem? CorrespondingSoundItem { get { return library is not null && library.Items.TryGetValue(SoundName, out Item? item) ? item as SoundItem : null; } }
    public string SoundSync
    {
        get { return soundSync; }
        set
        {
            if (!AcceptableSoundSyncs.Contains(value)) throw new ArgumentException();
            soundSync = value;
            root?.SetOrRemoveAttribute("soundSync", value, DefaultValues.SoundSync);
        }
    }
    public string TweenType { get { return tweenType; } set { tweenType = value; root?.SetOrRemoveAttribute("tweenType", value, DefaultValues.TweenType); } }
    public bool MotionTweenSnap { get { return motionTweenSnap; } set { motionTweenSnap = value; root?.SetOrRemoveAttribute("motionTweenSnap", value, DefaultValues.MotionTweenSnap); } }
    public bool HasCustomEase { get { return hasCustomEase; } set { hasCustomEase = value; root?.SetOrRemoveAttribute("hasCustomEase", value, DefaultValues.HasCustomEase); } }
    public bool Bookmark { get { return bookmark; } set { bookmark = value; root?.SetOrRemoveAttribute("bookmark", value, DefaultValues.Bookmark); } }
    public string EaseMethodName { get { return easeMethodName; } private set { easeMethodName = value; root?.SetOrRemoveAttribute("easeMethodName", value, DefaultValues.EaseMethodName); } }
    private static readonly HashSet<string> AcceptableSoundSyncs = new HashSet<string> { "event", "start", "stop", "stream" };
    public ReadOnlyCollection<Element> Elements { get { return elements.AsReadOnly(); } }
    private void LoadElements(in XElement frameNode)
    {
        List<XElement>? elementNodes = frameNode.Element(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER)?.Elements().ToList();
        if (elementNodes is null) return;
        foreach (XElement elementNode in elementNodes)
        {
            string elementName = elementNode.Name.LocalName.ToString();
            switch (elementName)
            {
                case "DOMBitmapInstance":
                    elements.Add(new BitmapInstance(elementNode, library));
                    var CorrespondingItem = (elements.Last() as BitmapInstance)!.CorrespondingItem;
                    if (CorrespondingItem is not null)
                        LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem, this);
                    break;
                case "DOMSymbolInstance":
                    elements.Add(new SymbolInstance(elementNode, library));
                    CorrespondingItem = (elements.Last() as SymbolInstance)!.CorrespondingItem;
                    if (CorrespondingItem is not null)
                        LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingItem, this);
                    break;
                case Text.STATIC_TEXT_NODE_IDENTIFIER:
                case Text.DYNAMIC_TEXT_NODE_IDENTIFIER:
                case Text.INPUT_TEXT_NODE_IDENTIFIER:
                    elements.Add(new Text(elementNode));
                    break;
            }
        }
    }
    private void LoadEases(in XElement frameNode)
    {
        List<XElement>? easeNodes = frameNode.Element(ns + IEase.EASES_NODEGROUP_IDENTIFIER)?.Elements().ToList();
        if (easeNodes is null) return;
        foreach (XElement easeNode in easeNodes)
        {
            string easeName = easeNode.Name.LocalName.ToString();
            switch (easeName)
            {
                case Ease.EASE_NODE_IDENTIFIER:
                    eases.Add(new Ease(easeNode));
                    break;
                case CustomEase.CUSTOM_EASE_NODE_IDENTIFIER:
                    eases.Add(new CustomEase(easeNode));
                    break;
            }
        }
    }
    internal Frame(in XElement frameNode, Library? library, bool isBlank = false)
    {
        root = frameNode;
        ns = root.Name.Namespace;
        startFrame = (int?)frameNode.Attribute("index") ?? DefaultValues.StartFrame;
        duration = (int?)frameNode.Attribute("duration") ?? DefaultValues.Duration;
        keyMode = (int?)frameNode.Attribute("keyMode") ?? DefaultValues.KeyMode;
        inPoint44 = (int?)frameNode.Attribute("inPoint44") ?? DefaultValues.InPoint44;
        labelType = (string?)frameNode.Attribute("labelType") ?? DefaultValues.LabelType;
        name = (string?)frameNode.Attribute("name") ?? DefaultValues.Name;
        soundName = (string?)frameNode.Attribute("soundName") ?? DefaultValues.SoundName;
        soundSync = (string?)frameNode.Attribute("soundSync") ?? DefaultValues.SoundSync;
        tweenType = (string?)frameNode.Attribute("tweenType") ?? DefaultValues.TweenType;
        motionTweenSnap = (bool?)frameNode.Attribute("motionTweenSnap") ?? DefaultValues.MotionTweenSnap;
        hasCustomEase = (bool?)frameNode.Attribute("hasCustomEase") ?? DefaultValues.HasCustomEase;
        easeMethodName = (string?)frameNode.Attribute("easeMethodName") ?? DefaultValues.EaseMethodName;
        this.library = library;
        elements = new List<Element>();
        eases = new List<IEase>();
        if (!isBlank)
        {
            LoadElements(root);
            LoadEases(root);
        }
        registeredForSoundItem = SoundName != DefaultValues.SoundName;
        if (registeredForSoundItem && library is not null)
        {
            LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingSoundItem!, this);
            CorrespondingSoundItem!.UseCount++;
        }
    }

    internal Frame(Frame other, bool isBlank = false)
    {
        root = other.root is null ? null : new XElement(other.root);
        ns = other.ns;
        startFrame = other.startFrame;
        duration = other.duration;
        keyMode = other.keyMode;
        inPoint44 = other.inPoint44;
        labelType = other.labelType;
        name = other.name;
        soundName = other.soundName;
        soundSync = other.soundSync;
        tweenType = other.tweenType;
        motionTweenSnap = other.motionTweenSnap;
        hasCustomEase = other.hasCustomEase;
        easeMethodName = other.easeMethodName;
        library = other.library;
        elements = new List<Element>();
        eases = new List<IEase>();
        if (root is not null && !isBlank)
        {
            LoadElements(root);
            LoadEases(root);
        }
        registeredForSoundItem = SoundName != DefaultValues.SoundName;
        if (registeredForSoundItem)
        {
            LibraryEventMessenger.Instance.RegisterReceiver(CorrespondingSoundItem!, this);
            CorrespondingSoundItem!.UseCount++;
        }
    }

    public void Dispose()
    {
        if (registeredForSoundItem)
        {
            LibraryEventMessenger.Instance.UnregisterReceiver(CorrespondingSoundItem!, this);
            CorrespondingSoundItem!.UseCount--;
        }
        CleanupElements();
    }

    public bool IsEmpty()
    {
        return !elements.Any();
    }
    private void CleanupElements()
    {
        foreach (Element element in elements)
        {
            if (element is Instance instance)
            {
                if (instance.CorrespondingItem is not null)
                {
                    LibraryEventMessenger.Instance.UnregisterReceiver(instance.CorrespondingItem, this);
                    instance.Dispose();
                }
            }
        }
    }
    // doesn't clear soundName
    public void ClearElements()
    {
        // unregister from library events
        CleanupElements();
        elements.Clear();
        root?.Element(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER)?.RemoveAll();
    }
    public Text AddNewText(Rectangle boundingRect, string characters = "")
    {
        Text text = new(boundingRect, characters, ns);
        elements.Add(text);
        root?.Element(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER)?.Add(text.Root);
        return text;
    }
    internal Instance? AddItem(Item item)
    {
        // need to create constructors that turn items into instances unless it's a soundItem
        if (item is SoundItem soundItem)
        {
            SoundName = soundItem.Name;
            return null;
        }
        if (item is SymbolItem symbolItem)
        {
            SymbolInstance symbolInstance = new SymbolInstance(symbolItem, library);
            elements.Add(symbolInstance);
            root?.Element(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER)?.Add(symbolInstance.Root);
            LibraryEventMessenger.Instance.RegisterReceiver(symbolInstance.CorrespondingItem!, this);
            return symbolInstance;
        }
        if (item is BitmapItem bitmapItem)
        {
            BitmapInstance bitmapInstance = new BitmapInstance(bitmapItem, library);
            elements.Add(bitmapInstance);
            root?.Element(ns + Element.ELEMENTS_NODEGROUP_IDENTIFIER)?.Add(bitmapInstance.Root);
            LibraryEventMessenger.Instance.RegisterReceiver(bitmapInstance.CorrespondingItem!, this);
            return bitmapInstance;
        }
        return null;
    }
    void ILibraryEventReceiver.OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e)
    {
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRenamed && soundName == e.OldName)
        {
            SoundName = e.NewName!;
        }
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRemoved)
        {
            if (SoundName == e.Item!.Name)
            {
                SoundName = DefaultValues.SoundName;
            }
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                Element element = elements[i];
                if (element is Instance instance && instance.CorrespondingItem == e.Item)
                {
                    elements.RemoveAt(i);
                    instance.Root?.Remove();
                }
            }
        }
    }
    internal void CreateMotionTween(string? target = null, string? method = null)
    {
        target ??= "all";
        method ??= "none";
        KeyMode = (int)KeyModes.ClassicTween;
        TweenType = "motion";
        MotionTweenSnap = true;
        if (root?.Element(ns + IEase.EASES_NODEGROUP_IDENTIFIER) is null)
        {
            root?.Add(new XElement(ns + IEase.EASES_NODEGROUP_IDENTIFIER));
        }
        root?.Element(ns + IEase.EASES_NODEGROUP_IDENTIFIER)?.RemoveAll();
        XElement easeNode = new(ns + Ease.EASE_NODE_IDENTIFIER);
        easeNode.SetAttributeValue("target", target);
        easeNode.SetAttributeValue("method", method);
        root?.Element(ns + IEase.EASES_NODEGROUP_IDENTIFIER)?.Add(easeNode);
        eases.Add(new Ease(easeNode));
        EaseMethodName = method;
    }
    public void RemoveTween()
    {
        KeyMode = (int)KeyModes.Normal;
        TweenType = "none";
        MotionTweenSnap = false;
        root?.Element(ns + IEase.EASES_NODEGROUP_IDENTIFIER)?.Remove();
        eases.Clear();
        EaseMethodName = DefaultValues.EaseMethodName;
    }
}