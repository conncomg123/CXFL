using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CsXFL;
public class BitmapItem : Item
{
    public const int SUBPIXELS_PER_PIXEL = 20;
    public static class DefaultValues
    {
        public const string Href = "";
        public const bool AllowSmoothing = false;
        public const bool UseDeblocking = false;
        public const bool UseImportedJPEGQuality = false;
        public const int HPixels = 0;
        public const int VPixels = 0;
        public const int Quality = 0;
        public const string CompressionType = "photo";
        public const string OriginalCompressionType = "photo";
        public const string SourceFilePath = "";
    }
    private string href;
    private bool allowSmoothing, useDeblocking, useImportedJPEGQuality;
    private readonly int hPixels, vPixels;
    private int quality;
    private string compressionType;
    private readonly string originalCompressionType, sourceFilePath;
    public string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    public bool AllowSmoothing { get { return allowSmoothing; } set { allowSmoothing = value; Root?.SetOrRemoveAttribute("allowSmoothing", value, DefaultValues.AllowSmoothing); } }
    public bool UseDeblocking { get { return useDeblocking; } set { useDeblocking = value; Root?.SetOrRemoveAttribute("useDeblocking", value, DefaultValues.UseDeblocking); } }
    public bool UseImportedJPEGQuality { get { return useImportedJPEGQuality; } set { useImportedJPEGQuality = value; Root?.SetOrRemoveAttribute("useImportedJPEGQuality", value, DefaultValues.UseImportedJPEGQuality); } }
    public int HPixels { get { return hPixels; } }
    public int VPixels { get { return vPixels; } }
    public int Quality { get { return quality; } set { quality = value; Root?.SetAttributeValue("quality", value); } }
    public string CompressionType { get { return compressionType; } set { compressionType = value; Root?.SetOrRemoveAttribute("compressionType", value, DefaultValues.CompressionType); } }
    public string OriginalCompressionType { get { return originalCompressionType; } }
    public string SourceFilePath { get { return sourceFilePath; } }
    public BitmapItem() : base()
    {
        href = DefaultValues.Href;
        allowSmoothing = DefaultValues.AllowSmoothing;
        useDeblocking = DefaultValues.UseDeblocking;
        hPixels = DefaultValues.HPixels;
        vPixels = DefaultValues.VPixels;
        quality = DefaultValues.Quality;
        compressionType = DefaultValues.CompressionType;
        originalCompressionType = DefaultValues.OriginalCompressionType;
        sourceFilePath = DefaultValues.SourceFilePath;
    }
    public BitmapItem(in XElement bitmapItemNode) : base(bitmapItemNode, "bitmap")
    {
        href = (string?)bitmapItemNode.Attribute("href") ?? DefaultValues.Href;
        allowSmoothing = (bool?)bitmapItemNode.Attribute("allowSmoothing") ?? DefaultValues.AllowSmoothing;
        useDeblocking = (bool?)bitmapItemNode.Attribute("useDeblocking") ?? DefaultValues.UseDeblocking;
        hPixels = ((int?)bitmapItemNode.Attribute("frameRight") ?? DefaultValues.HPixels * SUBPIXELS_PER_PIXEL) / SUBPIXELS_PER_PIXEL;
        vPixels = ((int?)bitmapItemNode.Attribute("frameBottom") ?? DefaultValues.VPixels * SUBPIXELS_PER_PIXEL) / SUBPIXELS_PER_PIXEL;
        quality = (int?)bitmapItemNode.Attribute("quality") ?? DefaultValues.Quality;
        compressionType = (string?)bitmapItemNode.Attribute("compressionType") ?? DefaultValues.CompressionType;
        originalCompressionType = (string?)bitmapItemNode.Attribute("originalCompressionType") ?? DefaultValues.OriginalCompressionType;
        sourceFilePath = (string?)bitmapItemNode.Attribute("sourceExternalFilepath") ?? DefaultValues.SourceFilePath;
    }
    public BitmapItem(in BitmapItem other) : base(other)
    {
        href = other.href;
        allowSmoothing = other.allowSmoothing;
        useDeblocking = other.useDeblocking;
        hPixels = other.hPixels;
        vPixels = other.vPixels;
        quality = other.quality;
        compressionType = other.compressionType;
        originalCompressionType = other.originalCompressionType;
        sourceFilePath = other.sourceFilePath;
    }
}