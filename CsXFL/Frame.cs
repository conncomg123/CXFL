using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
namespace CsXFL;
using System.Xml.Linq;

public class Frame
{
    private static readonly List<string> AcceptableLabelTypes = new List<string> {"none", "name", "comment", "anchor"};
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
    }
    private readonly XElement? root;
    private readonly XNamespace ns;
    private readonly List<Element> elements;
    private int startFrame, duration, keyMode;
    private string labelType, name;
    public XElement? Root { get { return root; } }
    public int StartFrame { get { return startFrame; } set { startFrame = value; root?.SetAttributeValue("index", value); } }
    public int Duration { get { return duration; } set { duration = value; root?.SetOrRemoveAttribute("duration", value, DefaultValues.Duration); } }
    public int KeyMode { get { return keyMode; } set { keyMode = value; root?.SetOrRemoveAttribute("keyMode", value, DefaultValues.KeyMode); } }
    public string LabelType { get { return labelType; } set { if(!AcceptableLabelTypes.Contains(value)) throw new ArgumentException(); labelType = value; root?.SetOrRemoveAttribute("labelType", value, DefaultValues.LabelType); } }
    public string Name { get { return name; } set { name = value; root?.SetOrRemoveAttribute("name", value, DefaultValues.Name); } }
    public List<Element> Elements { get { return elements; } }
    private void LoadElements(in XElement frameNode)
    {
        List<XElement>? elementNodes = frameNode.Element(ns + "elements")?.Elements().ToList();
        if (elementNodes is null) return;
        foreach (XElement elementNode in elementNodes)
        {
            if (elementNode.Name.ToString().Contains("SymbolInstance"))
            {
                elements.Add(new SymbolInstance(elementNode));
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
        elements = new List<Element>();
        if (!isBlank) LoadElements(root);
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
        elements = new List<Element>();
        if (root is not null && !isBlank) LoadElements(root);
    }

    public bool IsEmpty()
    {
        return elements.Any();
    }
    public void ClearElements()
    {
        elements.Clear();
        root?.Element(ns + "elements")?.Remove();
    }
}