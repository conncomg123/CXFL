using System.Xml.Linq;

public class Document
{
    private string filename;
    private XDocument? xflTree;
    private List<Timeline> timelines;
    private XNamespace ns;
    private XElement? root;
    public XElement? Root { get { return root; } }
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
        if (!filename.EndsWith(".xml"))
        {
            throw new ArgumentException("The filename must end with .xml");
        }
        this.filename = filename;
        LoadXFL(filename);
        ns = root!.Name.Namespace;
        timelines = new List<Timeline>();
        LoadTimelines(root!);
    }
    public void Save(string filename)
    {
        SaveXFL(filename);
    }
    public Timeline GetTimeline(int timelineIndex)
    {
        return timelines[timelineIndex];
    }
}