
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using CsXFL;

namespace Rendering;

public class RenderingManager
{
    const string DEFAULT_FFMEPG_ARGS_BEFORE_INPUT = "-y -hwaccel_device 0 -hwaccel_output_format cuda -hwaccel cuda -framerate 23.976",
    DEFAULT_FFMEPG_ARGS_AFTER_INPUT = "-c:v h264_nvenc -preset fast -b:v 10M -pix_fmt yuv420p";
    private readonly Document doc;
    private readonly SVGRenderer renderer;
    private readonly AudioManager audioManager;
    readonly int numCores;
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
        while (numFramesLeft >= doc.GetTimeline(++curTimelineIndex).GetFrameCount())
        {
            numFramesLeft -= doc.GetTimeline(curTimelineIndex).GetFrameCount();
        }
        return RenderFrame(curTimelineIndex, numFramesLeft, name);
    }
    public bool RenderDocument(string name, string ffmpegArgsBeforeinput = DEFAULT_FFMEPG_ARGS_BEFORE_INPUT, string ffmpegArgsAfterinput = DEFAULT_FFMEPG_ARGS_AFTER_INPUT)
    {
        int numFrames = 0;
        foreach (Timeline tl in doc.Timelines)
        {
            numFrames += tl.GetFrameCount();
        }
        int framesPerCore = numFrames / numCores;
        int framesLeftover = numFrames % numCores;
        // make subfolders for each core
        for (int i = 0; i < numCores; i++)
        {
            Directory.CreateDirectory($"{Path.Combine(outputPath, i.ToString())}");
        }
        List<Task> renderTasks = new();
        for (int i = 0; i < numCores; i++)
        {
            int folderIndex = i;
            int frameIndex = i * framesPerCore + (i < framesLeftover ? 1 : 0);
            renderTasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < (folderIndex < framesLeftover ? framesPerCore + 1 : framesPerCore); j++)
                {
                    string outputFile = Path.Combine(folderIndex.ToString(), (j + 1).ToString() + ".svg");
                    RenderFrame(frameIndex, outputFile);
                    frameIndex++;
                }
            }));
        }
        Task<MemoryStream> audioTask = Task.Run(audioManager.GetMixedAudio);
        renderTasks.Add(audioTask);
        try
        {
            Task.WaitAll(renderTasks);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return false;
        }
        MemoryStream audio = audioTask.Result;
        Parallel.For(0, numCores, i =>
        {
            string subfolderPath = Path.Combine(outputPath, i.ToString());
            ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"{ffmpegArgsBeforeinput} -i {subfolderPath}\\%d.svg {ffmpegArgsAfterinput} {outputPath}\\{i}.mp4");
            startInfo.UseShellExecute = false;
            Process? process = Process.Start(startInfo);
            process?.WaitForExit();
        });
        // Create a file containing the list of input files
        string inputFileList = Path.Combine(outputPath, "input.txt");
        string tmpOutputFile = Path.Combine(outputPath, "tmp_" + name);
        string outputFile = Path.Combine(outputPath, name);
        using (StreamWriter writer = new(inputFileList))
        {
            foreach (string file in Directory.GetFiles(outputPath, "*.mp4").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))))
            {
                writer.WriteLine($"file '{file}'");
            }
        }
        ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"-y -f concat -safe 0 -i {inputFileList} -c copy {tmpOutputFile}")
        {
            UseShellExecute = false
        };
        Process? process = Process.Start(startInfo);
        process?.WaitForExit();
        // now add the audio
        startInfo = new ProcessStartInfo(ffmpegPath, $"-y -i {tmpOutputFile} -i pipe:0 -c copy -map 0:v:0 -map 1:a:0 {outputFile}")
        {
            UseShellExecute = false,
            RedirectStandardInput = true
        };
        process = Process.Start(startInfo);
        if (process is not null)
        {
            audio.CopyTo(process.StandardInput.BaseStream);
            process.StandardInput.Close();
            process.WaitForExit();
        }

        return true;
    }
}