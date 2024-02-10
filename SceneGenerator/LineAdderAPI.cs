using CsXFL;

static class LineAdderAPI
{
    public static void ExtendVoiceLine(double Duration, int FrameReference, int SceneNumber, Document Doc)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(SceneNumber);
        CurrentTimeline.InsertFrames(12 + (int)Math.Ceiling(Doc.FrameRate * Duration / 1000) - CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Duration, true, FrameReference);
    }

    public static void PlaceLine(string AttemptFile, int FrameReference, int SceneNumber, Document Doc)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(SceneNumber);
        string FrameReferenceName = CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Name;
        string LayerName = FrameReferenceName.Substring(7).Replace(". ", "").Replace(" ", "_").ToUpper() + "_VOX";        

        if (CurrentTimeline.FindLayerIndex(LayerName).Count == 0) CurrentTimeline.AddNewLayer(LayerName, "normal");

        Doc.ImportFile(AttemptFile);
        CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].ConvertToKeyframes(FrameReference);
        Doc.Library.AddItemToDocument(FrameReferenceName + ".flac", CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].GetFrame(FrameReference));
        ExtendVoiceLine(SoundUtils.GetSoundDuration(AttemptFile), FrameReference, SceneNumber, Doc);
    }

    public static void InsertLinesChunked(this Document Doc, string FolderPath)
    {

        for (int OperatingScene = 0; OperatingScene < Doc.Timelines.Count; OperatingScene++)
        {
            Timeline CurrentTimeline = Doc.GetTimeline(OperatingScene);
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];
            foreach (Frame FrameToConsider in CurrentTimeline.Layers[TextLayerIndex].KeyFrames)
            {
                string ReadTextName = FrameToConsider.Name;
                for (int MultipleSpeakingChars = 0; MultipleSpeakingChars < ReadTextName.Split(" & ").Length; MultipleSpeakingChars++)
                {
                    string AttemptFile = FolderPath + "\\" + ReadTextName.Split(" & ")[MultipleSpeakingChars] + ".flac";
                    if (File.Exists(AttemptFile))
                    {
                        PlaceLine(AttemptFile, FrameToConsider.StartFrame, OperatingScene, Doc);
                    } else
                    {
                        Console.WriteLine(AttemptFile + " Does not Exist.");
                    }
                }
            }
        }
    }

    //static void Main()
    //{
    //    Document Doc = new("C:\\Users\\Administrator\\CXFL\\SceneGenerator\\Gaster\\DOMDocument.xml");
    //    Doc.InsertLinesChunked("C:\\Users\\Administrator\\Elements of Justice\\Dynamically_Linked_Scene\\SonataTest");
    //    Doc.Save();
    //}
}