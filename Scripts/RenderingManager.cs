
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using CsXFL;

namespace Rendering;

public class RenderingManager
{
    const string DEFAULT_FFMEPG_ARGS_BEFORE_INPUT = "-y -hwaccel cuda",
    DEFAULT_FFMEPG_ARGS_AFTER_INPUT = "-c:v h264_nvenc -preset fast  -b:v 10M -pix_fmt yuv420p";
    private Document doc;
    private SVGRenderer renderer;
    private AudioManager audioManager; readonly int numCores;
    readonly string outputPath;
    readonly string ffmpegPath;
    public RenderingManager(Document document, int numCores, string outputPath, string? imagesFolder, string ffmpegPath, bool replaceMasksWithClipPaths = true)
    {
        this.doc = document;
        this.renderer = new SVGRenderer(document, imagesFolder, replaceMasksWithClipPaths);
        this.audioManager = new AudioManager(document);
        this.numCores = numCores;
        this.outputPath = outputPath;
        this.ffmpegPath = ffmpegPath;
    }
    public bool RenderFrame(int timelineIndex, int frameIndex, string name)
    {
        try
        {
            XDocument rendered = renderer.Render(timelineIndex, frameIndex);
            rendered.Save(Path.Combine(outputPath, name));
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not render frame {frameIndex} of timeline {timelineIndex}: {e.Message}");
            return false;
        }
    }
    public bool RenderFrame(int absoluteFrameIndex, string name)
    {
        int curTimelineIndex = -1,
        numFramesLeft = absoluteFrameIndex;
        while(numFramesLeft >= doc.GetTimeline(++curTimelineIndex).GetFrameCount())
        {
            numFramesLeft -= doc.GetTimeline(curTimelineIndex).GetFrameCount();
        }
        return RenderFrame(curTimelineIndex, numFramesLeft, name);
    }
    public bool RenderDocument(string name, string ffmpegArgsBeforeinput = DEFAULT_FFMEPG_ARGS_BEFORE_INPUT, string ffmpegArgsAfterinput = DEFAULT_FFMEPG_ARGS_AFTER_INPUT)
    {
        throw new NotImplementedException();
    }
}