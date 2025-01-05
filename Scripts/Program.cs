using Scripts;

namespace Rendering;
class Program
{
    static void Main(string[] args)
    {
        CsXFL.Document doc = new CsXFL.Document(@"D:\Documents\EOJAssets\TestFiles\VPWTest\VPWTest.xfl");
        const string targetPath = @"D:\Documents\EOJAssets\TestFolder\";
        SVGRenderer renderer = new SVGRenderer(doc, null, true);
        //.Render(doc.GetTimeline(0), 0).Save($"{targetPath}{1}.svg");

        double result = BoxUtils.CalculateSVGPathLength("M 7 25 Q 2 11 22 6 L 317.449982 6");

        /*double result = BoxUtils.CalculateQuadBezierLength((7, 25), (2, 11), (22, 6), 1);
        BoxUtils.OffsetQuadBezierCurve((7, 25), (2, 11), (22, 6));*/
        //FfmpegLauncher.Run(args);
    }
}