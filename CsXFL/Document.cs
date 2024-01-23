using System.Xml.Linq;
using System.IO.Compression;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("CsXFL.Tests")]
namespace CsXFL;
public class Document
{
    private static class DefaultValues
    {
        public const int Width = 1280;
        public const int Height = 720;
        public const double FrameRate = 24;
        public const int CurrentTimeline = 0;
    }
    private readonly string filename;
    private XDocument? xflTree;
    private readonly List<Timeline> timelines;
    private readonly Library library;
    private readonly int width, height;
    private int currentTimeline;
    private readonly double frameRate;
    private readonly XNamespace ns;
    private XElement? root;
    internal XElement? Root { get { return root; } }
    private readonly bool isXFL;
    public bool IsXFL { get { return isXFL; } }
    public Library Library { get { return library; } }
    public string Filename { get { return filename; } }
    public int CurrentTimeline { get { return currentTimeline - 1; } set { value++; currentTimeline = value; root?.SetAttributeValue("currentTimeline", value); } }
    public double FrameRate { get { return frameRate; } set { root?.SetAttributeValue("frameRate", value); } }
    internal List<Timeline> Timelines { get { return timelines; } }
    private void LoadFLA(string filename)
    {
        using ZipArchive archive = ZipFile.Open(filename, ZipArchiveMode.Read);
        ZipArchiveEntry? xflEntry = archive.GetEntry("DOMDocument.xml");
        if (xflEntry is null) throw new Exception("Invalid FLA file");
        xflTree = XDocument.Load(xflEntry.Open());
        root = xflTree.Root;
    }
    private void SaveFLA(string filename)
    {
        using ZipArchive archive = ZipFile.Open(filename, ZipArchiveMode.Update);
        ZipArchiveEntry? xflEntry = archive.GetEntry("DOMDocument.xml");
        if (xflEntry is null) throw new Exception("Invalid FLA file");
        xflEntry.Delete();
        ZipArchiveEntry newEntry = archive.CreateEntry("DOMDocument.xml");
        using StreamWriter writer = new(newEntry.Open());
        xflTree?.Save(writer);
    }
    private void LoadXFL(string filename)
    {
        xflTree = XDocument.Load(filename);
        root = xflTree.Root;
    }
    private void SaveXFL(string filename)
    {
        xflTree?.Save(filename);
        library.SaveXFL(filename);
    }
    private void LoadTimelines(XElement documentNode)
    {
        List<XElement>? timelineNodes = documentNode.Element(ns + "timelines")?.Elements().ToList();
        if (timelineNodes is null) return;
        foreach (XElement timelineNode in timelineNodes)
        {
            timelines.Add(new Timeline(timelineNode));
        }
    }

    public Document(string filename)
    {
        if(Path.GetExtension(filename) == ".xfl")
        {
            // find DOMDocument.xml in the same directory
            string domDocumentPath = Path.Combine(Path.GetDirectoryName(filename)!, "DOMDocument.xml");
            if (!File.Exists(domDocumentPath)) throw new FileNotFoundException("DOMDocument.xml not found in XFL directory");
            this.filename = domDocumentPath;
            isXFL = true;
            LoadXFL(domDocumentPath);
            ns = root!.Name.Namespace;
            timelines = new List<Timeline>();
            library = new Library(this);
            LoadTimelines(root!);
        }
        else if (Path.GetExtension(filename) == ".xml")
        {
            this.filename = filename;
            isXFL = true;
            LoadXFL(filename);
            ns = root!.Name.Namespace;
            timelines = new List<Timeline>();
            library = new Library(this);
            LoadTimelines(root!);
        }
        else if (Path.GetExtension(filename) == ".fla")
        {
            this.filename = filename;
            isXFL = false;
            LoadFLA(filename);
            ns = root!.Name.Namespace;
            timelines = new List<Timeline>();
            library = new Library(this);
            LoadTimelines(root!);
        }
        else
        {
            throw new ArgumentException("Invalid file extension");
        }
        width = (int?)root?.Attribute("width") ?? DefaultValues.Width;
        height = (int?)root?.Attribute("height") ?? DefaultValues.Height;
        frameRate = (double?)root?.Attribute("frameRate") ?? DefaultValues.FrameRate;
        currentTimeline = (int?)root?.Attribute("currentTimeline") ?? DefaultValues.CurrentTimeline;
    }
    public void Save(string filename)
    {
        if (Path.GetExtension(filename) == ".fla") SaveFLA(filename);
        else if (Path.GetExtension(filename) == ".xml") SaveXFL(filename);
        else throw new Exception("Invalid file extension");
    }
    public Timeline GetTimeline(int timelineIndex)
    {
        return timelines[timelineIndex];
    }
}