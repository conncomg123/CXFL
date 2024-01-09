using System.Xml.Linq;

namespace CsXFL;
public class SoundItem : Item // all the properties are propietary bullshit except for href
{
    private string href;
    public string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    public SoundItem() : base()
    {
        href = string.Empty;
    }
    public SoundItem(in XElement soundItemNode) : base(soundItemNode, "sound")
    {
        href = (string?)soundItemNode.Attribute("href") ?? string.Empty;
    }
    public SoundItem(in SoundItem other) : base(other)
    {
        href = other.href;
    }
}