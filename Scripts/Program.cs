using Scripts;

namespace Rendering;
class Program
{
    static void Main(string[] args)
    {
        string testProject = @"D:\Documents\EOJAssets\TestFiles\VPWTest\VPWTest.xfl";
        CsXFL.Document doc = new CsXFL.Document(testProject);
        const string targetPath = @"D:\Documents\EOJAssets\TestFolder\";
        SVGRenderer renderer = new SVGRenderer(doc, null, true);
        renderer.Render(doc.GetTimeline(0), 0).Save($"{targetPath}{1}.svg");

        //FfmpegLauncher.Run(args);
    }
}