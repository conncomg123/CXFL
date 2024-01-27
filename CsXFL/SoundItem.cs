using System.Xml.Linq;

namespace CsXFL;
public class SoundItem : Item // all the properties are propietary bullshit except for href
{
    private string href, soundDataHRef, format;
    private int sampleCount;
    internal string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    internal string SoundDataHRef { get { return soundDataHRef; } set { soundDataHRef = value; Root?.SetAttributeValue("soundDataHRef", value); } }
    internal string Format { get { return format; } set { format = value; Root?.SetAttributeValue("format", value); } }
    internal int SampleCount { get { return sampleCount; } set { sampleCount = value; Root?.SetAttributeValue("sampleCount", value); } }
     internal SoundItem() : base()
    {
        href = string.Empty;
        soundDataHRef = string.Empty;
        format = string.Empty;
        sampleCount = 0;
    }
    internal SoundItem(in XElement soundItemNode) : base(soundItemNode, "sound")
    {
        href = (string?)soundItemNode.Attribute("href") ?? string.Empty;
        soundDataHRef = (string?)soundItemNode.Attribute("soundDataHRef") ?? string.Empty;
        format = (string?)soundItemNode.Attribute("format") ?? string.Empty;
        sampleCount = (int?)soundItemNode.Attribute("sampleCount") ?? 0;
    }
    internal SoundItem(in SoundItem other) : base(other)
    {
        href = other.href;
        soundDataHRef = other.soundDataHRef;
        format = other.format;
        sampleCount = other.sampleCount;
    }
    static internal SoundItem FromFile(string path, XNamespace ns)
    {
        XElement soundItemNode = new(ns + "DOMSoundItem");
        soundItemNode.SetAttributeValue("href", path);
        soundItemNode.SetAttributeValue("name", Path.GetFileName(path));
        soundItemNode.SetAttributeValue("format", SoundUtils.GetSoundFormat(path));
        soundItemNode.SetAttributeValue("sampleCount", SoundUtils.GetSoundSampleCount(path));
        return new SoundItem(soundItemNode);
    }
}