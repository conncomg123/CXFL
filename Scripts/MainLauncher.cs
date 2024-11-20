using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;

namespace Rendering
{
    internal class MainLauncher
    {

        public static void Run(string[] args)
        {
            // var doc = new Document(@"C:\Users\Administrator\Desktop\FilterTest.fla");
            // var targetPath = @"C:\Users\Administrator\Desktop\ffmpeg+librsvg\";

            // SVGRenderer renderer = new SVGRenderer(doc, @"C:\Users\Administrator\Desktop\ffmpeg+librsvg\image", true);
            // int numCores = 1;
            // int numFrames = 1;
            // int framesPerCore = numFrames / numCores;
            // Stopwatch stopwatch = Stopwatch.StartNew();
            // // make subfolders for each core
            // for (int i = 0; i < numCores; i++)
            // {
            //     Directory.CreateDirectory($"{targetPath}{i}");
            // }
            // Task[] tasks = new Task[numCores];

            // for (int i = 0; i < numCores; i++)
            // {
            //     int frameIndex = i * framesPerCore;
            //     string corePath = $"{targetPath}{i}";
            //     tasks[i] = Task.Run(() =>
            //     {
            //         for (int j = 0; j < framesPerCore; j++)
            //         {
            //             renderer.Render(doc.GetTimeline(0), frameIndex).Save($"{corePath}\\{j + 1}.svg");
            //             frameIndex++;
            //         }
            //     });
            // }

            // Task.WaitAll(tasks);
            // stopwatch.Stop();
            // Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
            // string ffmpegPath = $"{targetPath}ffmpeg2.exe";
            // stopwatch.Restart();
            // Parallel.For(0, numCores, i =>
            // {
            //     ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"-y -hwaccel cuda -framerate 23.976  -i {targetPath}{i}\\%d.svg -c:v h264_nvenc -preset fast  -b:v 10M -pix_fmt yuv420p {targetPath}{i}.mp4");
            //     startInfo.UseShellExecute = false;
            //     Process process = Process.Start(startInfo);
            //     process.WaitForExit();
            // });
            // stopwatch.Stop();
            // Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");

            var doc = new Document(@"C:\Users\Administrator\Desktop\Ellipse.fla");
            var targetPath = @"C:\Users\Administrator\Desktop\ffmpeg+librsvg\";

            SVGRenderer renderer = new SVGRenderer(doc, @"C:\Users\Administrator\Desktop\ffmpeg+librsvg\image", true);
            int numCores = 1;
            int numFrames = 1;
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

            string ffmpegPath = $"{targetPath}ffmpeg2.exe";
            stopwatch.Restart();
            Parallel.For(0, numCores, i =>
            {
                Console.WriteLine($"-y -hwaccel cuda -framerate 23.976  -i {targetPath}{i}\\%d.svg -c:v h264_nvenc -preset fast  -b:v 10M -pix_fmt yuv420p {targetPath}{i}.mp4");
                ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath, $"-y -hwaccel cuda -framerate 23.976  -i {targetPath}{i}\\%d.svg -c:v h264_nvenc -preset fast  -b:v 10M -pix_fmt yuv420p {targetPath}{i}.mp4");
                startInfo.UseShellExecute = false;
                Process process = Process.Start(startInfo);
                process.WaitForExit();
            });
            stopwatch.Stop();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");

        }
    }
}
