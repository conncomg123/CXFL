using System.Xml.Linq;

namespace CsXFL;
public class BitmapInstance : Instance
{
    new public static class DefaultValues
    {
        public const int HPixels = 0;
        public const int VPixels = 0;
    }

    private readonly int hPixels, vPixels;

    public int HPixels { get { return hPixels; } }
    public int VPixels { get { return vPixels; } }
    internal BitmapInstance(in XElement instanceNode, Library? library) : base(instanceNode, library)
    {
        hPixels = (int?)instanceNode.Attribute("hPixels") ?? DefaultValues.HPixels;
        vPixels = (int?)instanceNode.Attribute("vPixels") ?? DefaultValues.VPixels;
    }
    internal BitmapInstance(ref BitmapInstance other) : base(other)
    {
        hPixels = other.hPixels;
        vPixels = other.vPixels;
    }
    // cast from BitmapItem to BitmapInstance
    internal BitmapInstance(in BitmapItem item, Library? library) : base(item, "bitmap", "DOMBitmapInstance", library)
    {
        hPixels = item.HPixels;
        vPixels = item.VPixels;
    }
}