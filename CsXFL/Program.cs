﻿using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using CsXFL;
public class Program
{
    public static void Main(string[] args)
    {
        CsXFL.Document doc = new("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\test.xfl");
        // // doc.GetTimeline(0).RemoveFrames(5, false, 0, doc.GetTimeline(0).Layers[0]);
        // // // doc.Library.RemoveItem("Loop.wav");
        // // // stress test: rename 1 million times
        // // Stopwatch sw = new();
        // // sw.Start();
        // // for (int i = 0; i < 1000000; i++)
        // // {
        // //     doc.Library.RenameItem("Alert.wav", "Alarm.wav");
        // //     doc.Library.RenameItem("Symbol 1", "Symbol 2");
        // //     doc.Library.RenameItem("Alarm.wav", "Alert.wav");
        // //     doc.Library.RenameItem("Symbol 2", "Symbol 1");
        // // }
        // // sw.Stop();
        // // Console.WriteLine(sw.ElapsedMilliseconds);
        // // doc.Library.RenameItem("Alert.wav", "Alarm.wav");
        // // doc.Library.RenameItem("Symbol 1", "Symbol 2");
        // // Console.WriteLine(doc.Root);
        // doc.ImportFile("C:\\Users\\rct3f\\Downloads\\s4_099_phoenix.flac");
        // doc.Library.RenameItem("s4_099_phoenix.flac", "test.flac");
        // Stopwatch sw = new();
        // sw.Start();
        // doc.ImportFromOtherDocument("C:\\Stuff\\CXFL\\test\\301_S3\\301_S3.xfl", "RIGS/SonataLogicChess►/SonataLogicChess►ScaledPoses");
        // // doc.Save();
        // sw.Stop();
        // Console.WriteLine(sw.ElapsedMilliseconds);
        // Console.WriteLine(CsXFL.An.recursiveCallCount);
        // var tl = doc.GetTimeline(0);
        // var layer = tl.Layers[2];
        // var frame = layer.Frames[0];
        // Console.WriteLine(doc.Library.AddItemToDocument(200, 200, "Bracelet.png", frame));
        // doc.Save("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\DOMDocument.xml");
        doc.ImportFile("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\s1_001_apollo.flac");
        doc.Save();
        // var wavData = SoundUtils.ConvertFlacToWav("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\s1_001_apollo.flac");
        // File.WriteAllBytes("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\s1_001_apollo.wav", wavData.Array);
    }
}