
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
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
    public XDocument? RenderFrame(int timelineIndex, int frameIndex)
    {
        XDocument? rendered = null;
        try
        {
            rendered = renderer.Render(timelineIndex, frameIndex);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not render frame {frameIndex} of timeline {timelineIndex}: {e.Message}");
        }
        return rendered;
    }
    public XDocument? RenderFrame(int absoluteFrameIndex)
    {
        int curTimelineIndex = -1,
        numFramesLeft = absoluteFrameIndex;
        while (numFramesLeft >= doc.GetTimeline(++curTimelineIndex).GetFrameCount())
        {
            numFramesLeft -= doc.GetTimeline(curTimelineIndex).GetFrameCount();
        }
        return RenderFrame(curTimelineIndex, numFramesLeft);
    }
    // private static XDocument PadSvg(XDocument svg, int maxSvgSize)
    // {
    //     if(svg.ToString().Length > maxSvgSize) throw new ArgumentException("svg.ToString().Length > maxSvgSize");
    //     XDocument toReturn = new(svg);
    //     int diff = maxSvgSize - svg.ToString().Length;

    // }
    public bool RenderDocumentWithPipes(string name, string ffmpegArgsBeforeinput = DEFAULT_FFMEPG_ARGS_BEFORE_INPUT, string ffmpegArgsAfterinput = DEFAULT_FFMEPG_ARGS_AFTER_INPUT)
    {
        // TODO: create n memorystreams and forward them to ffmpeg pipes; need to pad each svg file so that
        // they're all the same size in bytes due to ffmpeg quirk
        const byte PADDING_CHARACTER = 0x20; // space
        int numFrames = 0;
        foreach (Timeline tl in doc.Timelines)
        {
            numFrames += tl.GetFrameCount();
        }
        int framesPerCore = numFrames / numCores;
        int framesLeftover = numFrames % numCores;
        List<Task> renderTasks = new();
        ConcurrentBag<List<XDocument>> svgDataLists = new();
        for (int i = 0; i < numCores; i++)
        {
            List<XDocument> svgDataList = new();
            svgDataLists.Add(svgDataList);
            int folderIndex = i;
            int frameIndex = i * framesPerCore + (i < framesLeftover ? 1 : 0);
            renderTasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < (folderIndex < framesLeftover ? framesPerCore + 1 : framesPerCore); j++)
                {
                    var rendered = RenderFrame(frameIndex) ?? throw new InvalidDataException("Unable to render frame " + frameIndex);
                    svgDataList.Add(rendered);
                    frameIndex++;
                }
            }));
        }
        Task<MemoryStream> audioTask = Task.Run(audioManager.GetMixedAudio);
        renderTasks.Add(audioTask);
        Task.WaitAll(renderTasks.ToArray());
        MemoryStream audio = audioTask.Result;
        List<(List<MemoryStream> streams, long maxSize)> svgsAsStreams = new();
        List<(List<MemoryStream> data, long svgSize)> chunks = new();
        ConcurrentBag<MemoryStream> mp4Streams = new();
        const int BOM_SIZE = 3;
        try
        {
            try
            {
                Parallel.ForEach(svgDataLists, svgDataList =>
                {
                    int maxSize = 0;
                    List<MemoryStream> svgStreams = new();
                    for (int i = 0; i < svgDataList.Count; i++)
                    {
                        MemoryStream svgStream = new(maxSize);
                        XDocument? svg = svgDataList[i];
                        svg.Save(svgStream);
                        // ignore BOM
                        svgStream.Position = BOM_SIZE;
                        if (svgStream.Length - BOM_SIZE > maxSize) maxSize = (int)svgStream.Length - BOM_SIZE;
                        svgStreams.Add(svgStream);
                    }
                    int maxSvgsInStream = int.MaxValue / maxSize;
                    int numStreamsInChunk = svgStreams.Count / maxSvgsInStream + 1;
                    List<MemoryStream> chunk = new(numStreamsInChunk);
                    for (int i = 0; i < svgStreams.Count; i++)
                    {
                        if (i % maxSvgsInStream == 0)
                        {
                            int bytesInChunk = (svgStreams.Count - i) % maxSvgsInStream * maxSize;
                            chunk.Add(new MemoryStream(bytesInChunk));
                        }
                        int chunkIndex = i / maxSvgsInStream;
                        MemoryStream chunkStream = chunk[chunkIndex];
                        MemoryStream svgStream = svgStreams[i];
                        svgStream.CopyTo(chunkStream);
                        // pad with spaces (0x20)
                        for (int j = (int)chunkStream.Position; j < maxSize * (i + 1); j++)
                        {
                            chunkStream.WriteByte(PADDING_CHARACTER);
                        }
                    }
                    chunks.Add((chunk, maxSize));
                    svgsAsStreams.Add((svgStreams, maxSize));
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                foreach ((List<MemoryStream> data, long svgSize) in chunks)
                {
                    foreach (MemoryStream stream in data)
                    {
                        stream.Dispose();
                    }
                }
                return false;
            }
            finally
            {
                foreach ((List<MemoryStream> streams, long maxSize) in svgsAsStreams)
                {
                    foreach (MemoryStream stream in streams)
                    {
                        stream.Dispose();
                    }
                }
            }
            Parallel.ForEach(chunks, chunk =>
            {
                MemoryStream mp4Stream = new(framesPerCore * (int)chunk.svgSize);
                List<MemoryStream> data = chunk.data;
                long svgSize = chunk.svgSize;
                ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"{ffmpegArgsBeforeinput} -frame_size {svgSize} -f svg_pipe -i - {ffmpegArgsAfterinput} -f mpegts pipe:1");
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                Process? process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start ffmpeg process");
                foreach (MemoryStream stream in data)
                {
                    stream.Position = 0;
                    process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                    {
                        // put data into mp4Stream
                        if (e.Data is null) return;
                        byte[] data = Encoding.UTF8.GetBytes(e.Data);
                        mp4Stream.Write(data, 0, data.Length);
                    };
                    process.BeginOutputReadLine();
                    stream.WriteTo(process!.StandardInput.BaseStream);
                }
                process.StandardInput.Close();
                process.WaitForExit();
                mp4Streams.Add(mp4Stream);
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            foreach (MemoryStream stream in mp4Streams)
            {
                stream.Dispose();
            }
            return false;
        }
        finally
        {
            foreach ((List<MemoryStream> data, long svgSize) in chunks)
            {
                foreach (MemoryStream stream in data)
                {
                    stream.Dispose();
                }
            }
        }

        return false;
    }
    public bool RenderDocumentWithTmpFiles(string name, string ffmpegArgsBeforeinput = DEFAULT_FFMEPG_ARGS_BEFORE_INPUT, string ffmpegArgsAfterinput = DEFAULT_FFMEPG_ARGS_AFTER_INPUT)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        while (Directory.Exists(tempDir))
        {
            tempDir = Path.Combine(tempDir, Path.GetRandomFileName());
        }
        Directory.CreateDirectory(tempDir);
        try
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
                string path = Path.Combine(tempDir, i.ToString());
                Directory.CreateDirectory(path);
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
                        outputFile = Path.Combine(tempDir, outputFile);
                        RenderFrame(frameIndex)?.Save(outputFile);
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
                string subfolderPath = Path.Combine(tempDir, i.ToString());
                string tempMp4Path = Path.Combine(tempDir, i.ToString() + ".mp4");
                ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"{ffmpegArgsBeforeinput} -i {subfolderPath}\\%d.svg {ffmpegArgsAfterinput} {tempMp4Path}");
                startInfo.UseShellExecute = false;
                Process? process = Process.Start(startInfo);
                process?.WaitForExit();
            });
            // Create a file containing the list of input files
            string inputFileList = Path.Combine(tempDir, "input.txt");
            string tmpOutputFile = Path.Combine(tempDir, "tmp_" + name);
            string outputFile = Path.Combine(outputPath, name);
            using (StreamWriter writer = new(inputFileList))
            {
                foreach (string file in Directory.GetFiles(tempDir, "*.mp4").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))))
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
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}