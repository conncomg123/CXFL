using System.Xml.Linq;

namespace CsXFL;
public class SoundItem : Item // all the properties are propietary bullshit except for href
{
    private string href, soundDataHRef, format;
    private int sampleCount;
    internal string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    internal string SoundDataHRef { get { return soundDataHRef; } set { soundDataHRef = value; Root?.SetAttributeValue("soundDataHRef", value); } }
    public string Format { get { return format; } set { format = value; Root?.SetAttributeValue("format", value); } }
    public int SampleCount { get { return sampleCount; } internal set { sampleCount = value; Root?.SetAttributeValue("sampleCount", value); } }
    public int SampleRate { get { return int.Parse(new(format.TakeWhile(char.IsDigit).ToArray())) * 1000; } }
    public double Duration { get; init; }
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
        // sample rate is either the first two or first character of format string followed by "kHz"
        Duration = (double)sampleCount / SampleRate;
    }
    internal SoundItem(in SoundItem other) : base(other)
    {
        href = other.href;
        soundDataHRef = other.soundDataHRef;
        format = other.format;
        sampleCount = other.sampleCount;
        Duration = other.Duration;
    }
    static internal SoundItem FromFile(string path, XNamespace ns)
    {
        XElement soundItemNode = new(ns + "DOMSoundItem");
        soundItemNode.SetAttributeValue("href", path);
        soundItemNode.SetAttributeValue("name", Path.GetFileName(path));
        soundItemNode.SetAttributeValue("format", SoundUtils.GetSoundFormatString(path));
        soundItemNode.SetAttributeValue("sampleCount", SoundUtils.GetSoundSampleCount(path));
        return new SoundItem(soundItemNode) { Duration = SoundUtils.GetSoundDuration(path) };
    }
}