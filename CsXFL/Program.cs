using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using CsXFL;
public class Program
{
    public static void Main(string[] args)
    {
        CsXFL.Document doc = new("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\DOMDocument.xml");
        var tl = doc.GetTimeline(0);
        var layer = tl.Layers[2];
        var frame = layer.Frames[0];
        Console.WriteLine(doc.Library.AddItemToDocument(200, 200, "Bracelet.png", frame));
        doc.Save("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\DOMDocument.xml");
    }
}