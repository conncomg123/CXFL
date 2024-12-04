using Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts
{
    internal class FfmpegLauncher
    {
        public static void Run(string[] args)
        {
            CsXFL.Document doc = new CsXFL.Document(@"D:\Documents\EOJAssets\TestFiles\301_S5.fla");
            const string ffmpegFolder = @"D:\Documents\EOJAssets\ffmpeg\";
            const string targetPath = @"D:\Documents\EOJAssets\TestFolder\";
            SVGRenderer renderer = new SVGRenderer(doc, null, true);
            
            int numCores = 8;
            int numFrames = 8000;
            int framesPerCore = numFrames / numCores;
            Stopwatch stopwatch = Stopwatch.StartNew();
            // make subfolders for each core
            for (int i = 0; i < numCores; i++)
            {
                Directory.CreateDirectory($"{targetPath}{i}");
            }
            Task[] tasks = new Task[numCores];

            for (int i = 0; i < numCores; i++)
            {
                int frameIndex = i * framesPerCore;
                string corePath = $"{targetPath}{i}";
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < framesPerCore; j++)
                    {
                        renderer.Render(doc.GetTimeline(0), frameIndex).Save($"{corePath}\\{j + 1}.svg");
                        frameIndex++;
                    }
                });
            }

            Task.WaitAll(tasks);
            stopwatch.Stop();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
            string ffmpegPath = $"{ffmpegFolder}ffmpeg-eoj.exe";
            stopwatch.Restart();
            Parallel.For(0, numCores, i =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"-y -hwaccel cuda -framerate 23.976  -i {targetPath}{i}\\%d.svg -c:v h264_nvenc -preset fast  -b:v 10M -pix_fmt yuv420p {targetPath}{i}.mp4");
                startInfo.UseShellExecute = false;
                Process process = Process.Start(startInfo);
                process.WaitForExit();
            });
            string inputFileList = $"{targetPath}input.txt";
            string outputFile = $"{targetPath}output.mp4";

            // Create a file containing the list of input files
            using (StreamWriter writer = new StreamWriter(inputFileList))
            {
                foreach (string file in Directory.GetFiles(targetPath, "*.mp4"))
                {
                    writer.WriteLine($"file '{file}'");
                }
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"-y -f concat -safe 0 -i {inputFileList} -c copy {outputFile}");
            startInfo.UseShellExecute = false;

            Process process = Process.Start(startInfo);
            process.WaitForExit();
            stopwatch.Stop();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
