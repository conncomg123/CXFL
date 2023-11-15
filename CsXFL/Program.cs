using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;

public class Program
{
    public static void Main(string[] args)
    {
        Document doc = new Document("C:\\Stuff\\CXFL\\test\\DOMDocument.xml");
        // load test: insert 1000 layers and keyframe on every frame and time it
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            doc.GetTimeline(0).AddNewLayer("layer" + i, "normal");
        }
        stopwatch.Stop();
        Console.WriteLine("Insert 1000 layers: " + stopwatch.ElapsedMilliseconds + "ms");
        // stopwatch.Restart();
        // for (int i = 0; i < doc.GetTimeline(0).GetLayerCount(); i++)
        // {
        //     for(int j = 0; j < doc.GetTimeline(0).GetFrameCount(); j++) 
        //     {
        //         doc.GetTimeline(0).GetLayer(i).InsertKeyframe(j, false);
        //     }
        // }
        // stopwatch.Stop();
        // int totalFrames = doc.GetTimeline(0).GetFrameCount() * doc.GetTimeline(0).GetLayerCount();
        // Console.WriteLine("Insert " + totalFrames + " keyframes: " + stopwatch.ElapsedMilliseconds + "ms");
        // doc.Write("C:\\Stuff\\CXFL\\test\\DOMDocument.xml");
    }
}