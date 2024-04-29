using System.Xml.Linq;
using SixLabors.ImageSharp;

namespace CsXFL;
public class BitmapItem : Item
{
    internal const string BITMAP_NODE_IDENTIFIER = "DOMBitmapItem";
    public const int SUBPIXELS_PER_PIXEL = 20;
    internal static class DefaultValues
    {
        public const string Href = "";
        public const string BitmapDataHRef = "";
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
    private string href, bitmapDataHRef;
    private bool allowSmoothing, useDeblocking, useImportedJPEGQuality;
    private readonly int hPixels, vPixels;
    private int quality;
    private string compressionType;
    private readonly string originalCompressionType, sourceFilePath;
    public string Href { get { return href; } set { href = value; Root?.SetAttributeValue("href", value); } }
    internal string BitmapDataHRef { get { return bitmapDataHRef; } set { bitmapDataHRef = value; Root?.SetAttributeValue("bitmapDataHRef", value); } }
    public bool AllowSmoothing { get { return allowSmoothing; } set { allowSmoothing = value; Root?.SetOrRemoveAttribute("allowSmoothing", value, DefaultValues.AllowSmoothing); } }
    public bool UseDeblocking { get { return useDeblocking; } set { useDeblocking = value; Root?.SetOrRemoveAttribute("useDeblocking", value, DefaultValues.UseDeblocking); } }
    public bool UseImportedJPEGQuality { get { return useImportedJPEGQuality; } set { useImportedJPEGQuality = value; Root?.SetOrRemoveAttribute("useImportedJPEGQuality", value, DefaultValues.UseImportedJPEGQuality); } }
    public int HPixels { get { return hPixels; } }
    public int VPixels { get { return vPixels; } }
    public int Quality { get { return quality; } set { quality = value; Root?.SetAttributeValue("quality", value); } }
    public string CompressionType { get { return compressionType; } set { compressionType = value; Root?.SetOrRemoveAttribute("compressionType", value, DefaultValues.CompressionType); } }
    public string OriginalCompressionType { get { return originalCompressionType; } }
    public string SourceFilePath { get { return sourceFilePath; } }
    internal BitmapItem() : base()
    {
        href = DefaultValues.Href;
        bitmapDataHRef = DefaultValues.BitmapDataHRef;
        allowSmoothing = DefaultValues.AllowSmoothing;
        useDeblocking = DefaultValues.UseDeblocking;
        hPixels = DefaultValues.HPixels;
        vPixels = DefaultValues.VPixels;
        quality = DefaultValues.Quality;
        compressionType = DefaultValues.CompressionType;
        originalCompressionType = DefaultValues.OriginalCompressionType;
        sourceFilePath = DefaultValues.SourceFilePath;
    }
    internal BitmapItem(in XElement bitmapItemNode) : base(bitmapItemNode, "bitmap")
    {
        href = (string?)bitmapItemNode.Attribute("href") ?? DefaultValues.Href;
        bitmapDataHRef = (string?)bitmapItemNode.Attribute("bitmapDataHRef") ?? DefaultValues.BitmapDataHRef;
        allowSmoothing = (bool?)bitmapItemNode.Attribute("allowSmoothing") ?? DefaultValues.AllowSmoothing;
        useDeblocking = (bool?)bitmapItemNode.Attribute("useDeblocking") ?? DefaultValues.UseDeblocking;
        hPixels = ((int?)bitmapItemNode.Attribute("frameRight") ?? DefaultValues.HPixels * SUBPIXELS_PER_PIXEL) / SUBPIXELS_PER_PIXEL;
        vPixels = ((int?)bitmapItemNode.Attribute("frameBottom") ?? DefaultValues.VPixels * SUBPIXELS_PER_PIXEL) / SUBPIXELS_PER_PIXEL;
        quality = (int?)bitmapItemNode.Attribute("quality") ?? DefaultValues.Quality;
        compressionType = (string?)bitmapItemNode.Attribute("compressionType") ?? DefaultValues.CompressionType;
        originalCompressionType = (string?)bitmapItemNode.Attribute("originalCompressionType") ?? DefaultValues.OriginalCompressionType;
        sourceFilePath = (string?)bitmapItemNode.Attribute("sourceExternalFilepath") ?? DefaultValues.SourceFilePath;
    }
    internal BitmapItem(in BitmapItem other) : base(other)
    {
        href = other.href;
        bitmapDataHRef = other.bitmapDataHRef;
        allowSmoothing = other.allowSmoothing;
        useDeblocking = other.useDeblocking;
        hPixels = other.hPixels;
        vPixels = other.vPixels;
        quality = other.quality;
        compressionType = other.compressionType;
        originalCompressionType = other.originalCompressionType;
        sourceFilePath = other.sourceFilePath;
    }
    internal static BitmapItem FromFile(string path, XNamespace ns)
    {
        XElement bitmapItemNode = new(ns + BitmapItem.BITMAP_NODE_IDENTIFIER);
        bitmapItemNode.SetAttributeValue("href", path);
        bitmapItemNode.SetAttributeValue("name", Path.GetFileName(path));
        using Image image = Image.Load(path);
        bitmapItemNode.SetAttributeValue("frameRight", image.Width * SUBPIXELS_PER_PIXEL);
        bitmapItemNode.SetAttributeValue("frameBottom", image.Height * SUBPIXELS_PER_PIXEL);
        return new BitmapItem(bitmapItemNode);
    }
}