using System.Xml.Linq;

namespace CsXFL;
public class BitmapInstance : Instance
{
    public const string BITMAPINSTANCE_NODE_IDENTIFIER = "DOMBitmapInstance";
    new public static class DefaultValues
    {
        public const int HPixels = 0;
        public const int VPixels = 0;
    }

    private readonly int hPixels, vPixels;

    public int HPixels { get { return CorrespondingItem as BitmapItem is null ? hPixels : (CorrespondingItem as BitmapItem)!.HPixels; } }
    public int VPixels { get { return CorrespondingItem as BitmapItem is null ? vPixels : (CorrespondingItem as BitmapItem)!.VPixels; } }
    internal BitmapInstance(in XElement instanceNode, Library? library) : base(instanceNode, library)
    {
        hPixels = (int?)instanceNode.Attribute("hPixels") ?? DefaultValues.HPixels;
        vPixels = (int?)instanceNode.Attribute("vPixels") ?? DefaultValues.VPixels;
    }
    internal BitmapInstance(ref BitmapInstance other) : base(other)
    {
        hPixels = other.HPixels;
        vPixels = other.VPixels;
    }
    // cast from BitmapItem to BitmapInstance
    internal BitmapInstance(in BitmapItem item, Library? library) : base(item, "bitmap", BITMAPINSTANCE_NODE_IDENTIFIER, library)
    {
        hPixels = item.HPixels;
        vPixels = item.VPixels;
    }
}