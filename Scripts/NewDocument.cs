using System.IO.Compression;
using System.Xml.Linq;

public static class ZipArchiveBuilder
{
    public static ZipArchive CreateNewDocument(int width, int height, double frameRate, MemoryStream memoryStream)
    {
        try
        {
            ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
            XDocumentGenerator xDocumentGenerator = new XDocumentGenerator();

            AddFolder(zipArchive, "bin");
            AddFolder(zipArchive, "LIBRARY");
            AddFolder(zipArchive, "META-INF");
            AddFolder(zipArchive, "publishtemplates");

            AddFile(zipArchive, "NewDocument.xfl", "PROXY-CS5");
            AddFile(zipArchive, "META-INF/metadata.xml", "");
            AddFile(zipArchive, "MobileSettings.xml", "");

            AddFile(zipArchive, "DOMDocument.xml", xDocumentGenerator.GenerateXDocumentString(width, height, frameRate));
            // Don't make PublishSettings.xml
            // Don't make SymDepend.cache

            return zipArchive;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create the new document.", ex);
        }
    }

    private static void AddFolder(ZipArchive zipArchive, string folderName)
    {
        zipArchive.CreateEntry(folderName + "/");
    }

    private static void AddFile(ZipArchive zipArchive, string filePath, string content)
    {
        ZipArchiveEntry entry = zipArchive.CreateEntry(filePath);
        using (StreamWriter writer = new StreamWriter(entry.Open()))
        {
            writer.Write(content);
        }
    }
}

public class XDocumentGenerator
{
    public string GenerateXDocumentString(int width, int height, double frameRate)
    {
        XNamespace ns = "http://ns.adobe.com/xfl/2008/";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        XDocument xDocument = new XDocument(
            new XElement(ns + "DOMDocument",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute("width", width.ToString()),
                new XAttribute("height", height.ToString()),
                new XAttribute("frameRate", frameRate.ToString()),
                new XAttribute("currentTimeline", "1"),
                new XAttribute("xflVersion", "22.02"),
                new XAttribute("creatorInfo", "Adobe Animate"),
                new XAttribute("platform", "Windows"),
                new XAttribute("versionInfo", "Saved by Animate Windows 22.0 build 179"),
                new XAttribute("majorVersion", "22"),
                new XAttribute("buildNumber", "179"),
                new XAttribute("snapAlign", "false"),
                new XAttribute("objectsSnapTo", "false"),
                new XAttribute("viewAngle3D", "100.925968316201"),
                new XAttribute("vanishingPoint3DX", "640"),
                new XAttribute("vanishingPoint3DY", "360"),
                new XAttribute("nextSceneIdentifier", "2"),
                new XAttribute("playOptionsPlayLoop", "false"),
                new XAttribute("playOptionsPlayPages", "false"),
                new XAttribute("playOptionsPlayFrameActions", "false"),
                new XAttribute("filetypeGUID", "DD0DDBBF-5BEF-45B2-9F24-A3048D2A676F"),
                new XAttribute("fileGUID", "66D37584D244CF4CAC1E9E4DD43A0229"),
                new XAttribute("forceSimple", "true"),
                new XAttribute("hasAccessibleData", "true"),
                new XElement("timelines",
                    new XElement("DOMTimeline",
                        new XAttribute("name", "Scene 1"),
                        new XAttribute("layerDepthEnabled", "true"),
                        new XElement("layers",
                            new XElement("DOMLayer",
                                new XAttribute("name", "Layer_1"),
                                new XAttribute("color", "#00FFFF"),
                                new XAttribute("current", "true"),
                                new XAttribute("isSelected", "true"),
                                new XElement("frames",
                                    new XElement("DOMFrame",
                                        new XAttribute("index", "0"),
                                        new XAttribute("keyMode", "9728"),
                                        new XElement("elements")
                                    )
                                )
                            )
                        )
                    )
                ),
                new XElement("scripts"),
                new XElement("PrinterSettings"),
                new XElement("publishHistory")
            )
        );

        return xDocument.ToString();
    }
}