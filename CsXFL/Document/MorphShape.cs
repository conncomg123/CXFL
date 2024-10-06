using System.Xml.Linq;

namespace CsXFL;

public class MorphShape
{
    public const string MORPHSHAPE_NODE_IDENTIEFIER = "MorphShape";
    public static class DefaultValues
    {
        public const string MappingType = "distributive";
    }
    private XElement root;
    private XNamespace ns;
    private string mappingType;
    List<MorphSegment> morphSegments;
    public XElement Root { get { return root; } }
    public string MappingType { get { return mappingType; } set { mappingType = value; root.SetOrRemoveAttribute("mappingType", mappingType, DefaultValues.MappingType); } }
    public List<MorphSegment> MorphSegments { get { return morphSegments; } }
    private void LoadMorphSegments()
    {
        var morphSegmentsNodes = root.Element(ns + MorphSegment.MORPHSEGEMENT_NODEGROUP_IDENTIFIER)?.Elements(ns + MorphSegment.MORPHSEGMENT_NODE_IDENTIEFIER);
        if (morphSegmentsNodes is null) return;
        foreach (var morphSegmentsNode in morphSegmentsNodes)
        {
            morphSegments.Add(new MorphSegment(morphSegmentsNode));
        }
    }
    public MorphShape(XElement morphShapeNode)
    {
        root = morphShapeNode;
        ns = morphShapeNode.Name.Namespace;
        mappingType = (string?)root.Attribute("mappingType") ?? DefaultValues.MappingType;
        morphSegments = new List<MorphSegment>();
        LoadMorphSegments();
    }
    public MorphShape(MorphShape other)
    {
        root = new XElement(other.root);
        ns = other.ns;
        mappingType = other.mappingType;
        morphSegments = new List<MorphSegment>();
        LoadMorphSegments();
    }
}
public class MorphSegment
{
    public const string MORPHSEGMENT_NODE_IDENTIEFIER = "MorphSegment",
    MORPHSEGEMENT_NODEGROUP_IDENTIFIER = "morphSegments";
    public static class DefaultValues
    {
        public const string StartPointA = "0, 0";
        public const string StartPointB = "0, 0";
    }
    private XElement root;
    private readonly XNamespace ns;
    private string startPointA, startPointB;
    private int strokeIndex1, strokeIndex2, fillIndex1, fillIndex2;
    private List<MorphCurves> morphCurves;
    public XElement Root { get { return root; } }
    public string StartPointA { get { return startPointA; } set { startPointA = value; root.SetOrRemoveAttribute("startPointA", startPointA, DefaultValues.StartPointA); } }
    public string StartPointB { get { return startPointB; } set { startPointB = value; root.SetOrRemoveAttribute("startPointB", startPointB, DefaultValues.StartPointB); } }
    public int StrokeIndex1 { get { return strokeIndex1; } set { strokeIndex1 = value; root.SetAttributeValue("strokeIndex1", strokeIndex1); } }
    public int StrokeIndex2 { get { return strokeIndex2; } set { strokeIndex2 = value; root.SetAttributeValue("strokeIndex2", strokeIndex2); } }
    public int FillIndex1 { get { return fillIndex1; } set { fillIndex1 = value; root.SetAttributeValue("fillIndex1", fillIndex1); } }
    public int FillIndex2 { get { return fillIndex2; } set { fillIndex2 = value; root.SetAttributeValue("fillIndex2", fillIndex2); } }
    public List<MorphCurves> MorphCurves { get { return morphCurves; } set { morphCurves = value; } }
    private void LoadMorphCurves()
    {
        var morphCurvesNodes = root.Elements(ns + CsXFL.MorphCurves.MORPHCURVES_NODE_IDENTIEFIER);
        foreach (var morphCurvesNode in morphCurvesNodes)
        {
            morphCurves.Add(new MorphCurves(morphCurvesNode));
        }
    }
    public MorphSegment(XElement morphSegmentNode)
    {
        root = morphSegmentNode;
        ns = morphSegmentNode.Name.Namespace;
        startPointA = (string?)root.Attribute("startPointA") ?? DefaultValues.StartPointA;
        startPointB = (string?)root.Attribute("startPointB") ?? DefaultValues.StartPointB;
        strokeIndex1 = (int?)root.Attribute("strokeIndex1") ?? 0;
        strokeIndex2 = (int?)root.Attribute("strokeIndex2") ?? 0;
        fillIndex1 = (int?)root.Attribute("fillIndex1") ?? 0;
        fillIndex2 = (int?)root.Attribute("fillIndex2") ?? 0;
        morphCurves = new List<MorphCurves>();
        LoadMorphCurves();
    }
    public MorphSegment(MorphSegment other)
    {
        root = new XElement(other.root);
        ns = other.ns;
        startPointA = other.startPointA;
        startPointB = other.startPointB;
        strokeIndex1 = other.strokeIndex1;
        strokeIndex2 = other.strokeIndex2;
        fillIndex1 = other.fillIndex1;
        fillIndex2 = other.fillIndex2;
        morphCurves = new List<MorphCurves>();
        LoadMorphCurves();
    }
}
public class MorphCurves
{
    public const string MORPHCURVES_NODE_IDENTIEFIER = "MorphCurves";
    public static class DefaultValues
    {

        public const string ControlPointA = "0, 0";
        public const string AnchorPointA = "0, 0";
        public const string ControlPointB = "0, 0";
        public const string AnchorPointB = "0, 0";
        public const bool IsLine = false;
    }
    private XElement root;
    private XNamespace ns;
    private string controlPointA, anchorPointA, controlPointB, anchorPointB;
    private bool isLine;
    public string ControlPointA { get { return controlPointA; } set { controlPointA = value; root.SetOrRemoveAttribute("controlPointA", controlPointA, DefaultValues.ControlPointA); } }
    public string AnchorPointA { get { return anchorPointA; } set { anchorPointA = value; root.SetOrRemoveAttribute("anchorPointA", anchorPointA, DefaultValues.AnchorPointA); } }
    public string ControlPointB { get { return controlPointB; } set { controlPointB = value; root.SetOrRemoveAttribute("controlPointB", controlPointB, DefaultValues.ControlPointB); } }
    public string AnchorPointB { get { return anchorPointB; } set { anchorPointB = value; root.SetOrRemoveAttribute("anchorPointB", anchorPointB, DefaultValues.AnchorPointB); } }
    public bool IsLine { get { return isLine; } set { isLine = value; root.SetOrRemoveAttribute("isLine", isLine, DefaultValues.IsLine); } }

    public MorphCurves(XElement morphCurvesNode)
    {
        root = morphCurvesNode;
        ns = morphCurvesNode.Name.Namespace;
        controlPointA = (string?)root.Attribute("controlPointA") ?? DefaultValues.ControlPointA;
        anchorPointA = (string?)root.Attribute("anchorPointA") ?? DefaultValues.AnchorPointA;
        controlPointB = (string?)root.Attribute("controlPointB") ?? DefaultValues.ControlPointB;
        anchorPointB = (string?)root.Attribute("anchorPointB") ?? DefaultValues.AnchorPointB;
        isLine = (bool?)root.Attribute("isLine") ?? DefaultValues.IsLine;
    }
    public MorphCurves(MorphCurves other)
    {
        root = new XElement(other.root);
        ns = other.ns;
        controlPointA = other.controlPointA;
        anchorPointA = other.anchorPointA;
        controlPointB = other.controlPointB;
        anchorPointB = other.anchorPointB;
        isLine = other.isLine;
    }

}