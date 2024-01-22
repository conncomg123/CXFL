using System.Xml.Linq;

namespace CsXFL;
public class SoundItem : Item // all the properties are propietary bullshit except for href
{
    private string href;
    internal string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    internal SoundItem() : base()
    {
        href = string.Empty;
    }
    internal SoundItem(in XElement soundItemNode) : base(soundItemNode, "sound")
    {
        href = (string?)soundItemNode.Attribute("href") ?? string.Empty;
    }
    internal SoundItem(in SoundItem other) : base(other)
    {
        href = other.href;
    }
}