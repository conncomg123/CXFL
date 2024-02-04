using CsXFL;

class LineAdderAPI
{
    private static Document Doc = new("C:\\Users\\Administrator\\CXFL\\SceneGenerator\\Gaster\\DOMDocument.xml");
    private static int SafeFrames = 12000;

    public static void ExtendVoiceLine(int Duration, int FrameReference)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        CurrentTimeline.InsertFrames(3 + (int)(Math.Ceiling(Doc.FrameRate * Duration / 1000)) - CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Duration, true, FrameReference);
    }

    public static void PlaceLine(string AttemptFile, int FrameReference)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        string FrameReferenceName = CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Name;
        string LayerName = FrameReferenceName.Substring(7).Replace(". ", "").Replace(" ", "_").ToUpper() + "_VOX";        

        if (CurrentTimeline.FindLayerIndex(LayerName).Count == 0) CurrentTimeline.AddNewLayer(LayerName, "normal");

        Doc.ImportFile(AttemptFile);
        Doc.Library.AddItemToDocument(FrameReferenceName + ".flac", CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].GetFrame(FrameReference));
        ExtendVoiceLine((int)(SoundUtils.GetSoundDuration(AttemptFile)) * 1000, FrameReference);
    }

    public static void InsertLinesChunked(string FolderPath)
    {

        for (int OperatingScene = 0; OperatingScene < Doc.Timelines.Count; OperatingScene++)
        {

            Timeline CurrentTimeline = Doc.GetTimeline(OperatingScene);
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];

            foreach (Frame FrameToConsider in CurrentTimeline.Layers[TextLayerIndex].KeyFrames)
            {
                string ReadTextName = FrameToConsider.Name;

                for (int i = 0; i < ReadTextName.Split(" & ").Length; i++)
                {
                    string AttemptFile = FolderPath + "\\" + ReadTextName.Split(" & ")[i] + ".flac";
                    if (File.Exists(AttemptFile))
                    {
                        PlaceLine(AttemptFile, FrameToConsider.StartFrame);
                    } else
                    {
                        Console.WriteLine(AttemptFile + " Does not Exist.");
                    }
                }
            }
        }
    }

    static void Main()
    {
        InsertLinesChunked("C:\\Users\\Administrator\\Elements of Justice\\Dynamically_Linked_Scene\\SonataTest");
        Doc.Save();
    }
}