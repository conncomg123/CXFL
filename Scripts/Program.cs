using Scripts;

namespace Rendering;
class Program
{
    static void Main(string[] args)
    {
        CsXFL.Document doc = new CsXFL.Document(@"D:\Documents\EOJAssets\TestFiles\301_S5-NOBACKGROUND\301_S5.xfl");
        const string targetPath = @"D:\Documents\EOJAssets\TestFolder\";
        SVGRenderer renderer = new SVGRenderer(doc, null, true);
        renderer.Render(doc.GetTimeline(0), 1).Save($"{targetPath}{2}.svg");
    }
}