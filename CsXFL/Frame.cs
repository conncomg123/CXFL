using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
namespace CsXFL;

using System.Collections.ObjectModel;
using System.Xml.Linq;

public class Frame : ILibraryEventReceiver
{
    private static readonly List<string> AcceptableLabelTypes = new List<string> { "none", "name", "comment", "anchor" };
    public enum KeyModes : int
    {
        Normal = 9728,
        ClassicTween = 22017,
        ShapeTween = 17922,
        MotionTween = 8195,
        ShapeLayers = 8192
    }
    public static class DefaultValues
    {
        public const int StartFrame = 0;
        public const int Duration = 1;
        public const int KeyMode = (int)KeyModes.Normal;
        public const string LabelType = "none";
        public const string Name = "";
        public const string SoundName = "";
    }
    private readonly XElement? root;
    private readonly XNamespace ns;
    private readonly List<Element> elements;
    private int startFrame, duration, keyMode;
    private string labelType, name, soundName;
    private bool subscribed;
    public XElement? Root { get { return root; } }
    public int StartFrame { get { return startFrame; } set { startFrame = value; root?.SetAttributeValue("index", value); } }
    public int Duration { get { return duration; } set { duration = value; root?.SetOrRemoveAttribute("duration", value, DefaultValues.Duration); } }
    public int KeyMode { get { return keyMode; } set { keyMode = value; root?.SetOrRemoveAttribute("keyMode", value, DefaultValues.KeyMode); } }
    public string LabelType { get { return labelType; } set { if (!AcceptableLabelTypes.Contains(value)) throw new ArgumentException(); labelType = value; root?.SetOrRemoveAttribute("labelType", value, DefaultValues.LabelType); } }
    public string Name { get { return name; } set { name = value; root?.SetOrRemoveAttribute("name", value, DefaultValues.Name); } }
    public string SoundName
    {
        get { return soundName; }
        set
        {
            var oldName = soundName;
            soundName = value;
            root?.SetOrRemoveAttribute("soundName", value, DefaultValues.SoundName);
            if (subscribed && value == DefaultValues.SoundName) LibraryEventMessenger.Instance.UnregisterReceiver(oldName, this);
            if (!subscribed && value != DefaultValues.SoundName) LibraryEventMessenger.Instance.RegisterReceiver(value, this);
            subscribed = value != DefaultValues.SoundName;
        }
    }
    public ReadOnlyCollection<Element> Elements { get { return elements.AsReadOnly(); } }
    private void LoadElements(in XElement frameNode)
    {
        List<XElement>? elementNodes = frameNode.Element(ns + "elements")?.Elements().ToList();
        if (elementNodes is null) return;
        foreach (XElement elementNode in elementNodes)
        {
            if (elementNode.Name.ToString().Contains("SymbolInstance"))
            {
                elements.Add(new SymbolInstance(elementNode));
                LibraryEventMessenger.Instance.RegisterReceiver((elements.Last() as SymbolInstance)!.LibraryItemName, this);
            }
        }
    }
    public Frame(in XElement frameNode, bool isBlank = false)
    {
        root = frameNode;
        ns = root.Name.Namespace;
        startFrame = (int?)frameNode.Attribute("index") ?? DefaultValues.StartFrame;
        duration = (int?)frameNode.Attribute("duration") ?? DefaultValues.Duration;
        keyMode = (int?)frameNode.Attribute("keyMode") ?? DefaultValues.KeyMode;
        labelType = (string?)frameNode.Attribute("labelType") ?? DefaultValues.LabelType;
        name = (string?)frameNode.Attribute("name") ?? DefaultValues.Name;
        soundName = (string?)frameNode.Attribute("soundName") ?? DefaultValues.SoundName;
        elements = new List<Element>();
        if (!isBlank) LoadElements(root);
        subscribed = SoundName != DefaultValues.SoundName;
        if (subscribed) LibraryEventMessenger.Instance.RegisterReceiver(SoundName, this);
    }

    public Frame(ref Frame other, bool isBlank = false)
    {
        root = other.root is null ? null : new XElement(other.root);
        ns = other.ns;
        startFrame = other.startFrame;
        duration = other.duration;
        keyMode = other.keyMode;
        labelType = other.labelType;
        name = other.name;
        soundName = other.soundName;
        elements = new List<Element>();
        if (root is not null && !isBlank) LoadElements(root);
        subscribed = SoundName != DefaultValues.SoundName;
        if (subscribed) LibraryEventMessenger.Instance.RegisterReceiver(SoundName, this);
    }
    ~Frame()
    {
        if (subscribed)
        {
            LibraryEventMessenger.Instance.UnregisterReceiver(SoundName, this);
            foreach (Element element in elements)
            {
                if (element is Instance instance)
                {
                    LibraryEventMessenger.Instance.UnregisterReceiver(instance.LibraryItemName, this);
                }
            }
        }
    }

    public bool IsEmpty()
    {
        return elements.Any();
    }
    public void ClearElements()
    {
        // unregister from library events
        foreach (Element element in elements)
        {
            if (element is Instance instance)
            {
                LibraryEventMessenger.Instance.UnregisterReceiver(instance.LibraryItemName, this);
            }
        }
        elements.Clear();
        root?.Element(ns + "elements")?.RemoveAll();
    }
    public Instance? AddItem(Item item)
    {
        // need to create constructors that turn items into instances unless it's a soundItem
        if (item is SoundItem soundItem)
        {
            this.SoundName = soundItem.Href;
            return null;
        }
        if (item is SymbolItem symbolItem)
        {
            SymbolInstance symbolInstance = new SymbolInstance(symbolItem);
            elements.Add(symbolInstance);
            root?.Element(ns + "elements")?.Add(symbolInstance.Root);
            return symbolInstance;
        }
        if (item is BitmapItem bitmapItem)
        {
            BitmapInstance bitmapInstance = new BitmapInstance(bitmapItem);
            elements.Add(bitmapInstance);
            root?.Element(ns + "elements")?.Add(bitmapInstance.Root);
            return bitmapInstance;
        }
        return null;
    }
    public void OnLibraryEvent(object sender, LibraryEventMessenger.LibraryEventArgs e)
    {
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRenamed && soundName == e.OldName)
        {
            SoundName = e.NewName!;
        }
        if (e.EventType == LibraryEventMessenger.LibraryEvent.ItemRemoved)
        {
            if (SoundName == e.Name)
            {
                SoundName = DefaultValues.SoundName;
            }
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                Element element = elements[i];
                if (element is SymbolInstance symbolInstance && symbolInstance.LibraryItemName == e.Name)
                {
                    elements.RemoveAt(i);
                    symbolInstance.Root?.Remove();
                }
            }
        }
    }
}