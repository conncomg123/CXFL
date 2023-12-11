using System.Xml.Linq;
using System.IO.Compression;
namespace CsXFL;
public class Document
{
    private string filename;
    private XDocument? xflTree;
    private List<Timeline> timelines;
    private XNamespace ns;
    private XElement? root;
    public XElement? Root { get { return root; } }
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
        if (Path.GetExtension(filename) == ".xml")
        {
            this.filename = filename;
            LoadXFL(filename);
            ns = root!.Name.Namespace;
            timelines = new List<Timeline>();
            LoadTimelines(root!);
        }
        else if (Path.GetExtension(filename) == ".fla")
        {
            this.filename = filename;
            LoadFLA(filename);
            ns = root!.Name.Namespace;
            timelines = new List<Timeline>();
            LoadTimelines(root!);
        }
        else
        {
            throw new ArgumentException("Invalid file extension");
        }
    }
    public void Save(string filename)
    {
        if(Path.GetExtension(filename) == ".fla") SaveFLA(filename);
        else if(Path.GetExtension(filename) == ".xml") SaveXFL(filename);
        else throw new Exception("Invalid file extension");
    }
    public Timeline GetTimeline(int timelineIndex)
    {
        return timelines[timelineIndex];
    }
}