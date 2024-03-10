using CsXFL;
namespace SceneGenerator.API;
static class LineAdderAPI
{

    public static void ExtendVoiceLine(double Duration, int FrameReference, int SceneNumber, Document Doc)
    {
        Doc.CurrentTimeline = (SceneNumber);
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        CurrentTimeline.InsertFrames(3 + (int)Math.Ceiling(Doc.FrameRate * Duration / 1000) - CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Duration, true, FrameReference);
    }

    public static void PlaceLine(string AttemptFile, int FrameReference, int SceneNumber, Document Doc)
    {
        Doc.CurrentTimeline = (SceneNumber);
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        string FrameReferenceName = CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Name;
        string LayerName = FrameReferenceName.Substring(7).Replace(". ", "").Replace(" ", "_").ToUpper() + "_VOX";        

        if (CurrentTimeline.FindLayerIndex(LayerName).Count == 0) CurrentTimeline.AddNewLayer(LayerName, "normal");

        Doc.ImportFile(AttemptFile);
        CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].ConvertToKeyframes(FrameReference);
        Doc.Library.AddItemToDocument(FrameReferenceName + ".flac", CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].GetFrame(FrameReference));
        Doc.Library.MoveToFolder("AUDIO\\VOX", Doc.Library.Items[FrameReferenceName + ".flac"]);
        ExtendVoiceLine(SoundUtils.GetSoundDuration(AttemptFile), FrameReference, SceneNumber, Doc);
        CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex(LayerName)[0]].GetFrame(FrameReference).SoundSync = "stream";
        ExtendVoiceLine(SoundUtils.GetSoundDuration(AttemptFile), FrameReference, SceneNumber, Doc);
    }

    public static void InsertLinesChunked(this Document Doc, string FolderPath)
    {
        for (int OperatingScene = 0; OperatingScene < Doc.Timelines.Count; OperatingScene++)
        {
            Doc.CurrentTimeline = (OperatingScene);
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];
            foreach (Frame FrameToConsider in CurrentTimeline.Layers[TextLayerIndex].KeyFrames)
            {
                string ReadTextName = FrameToConsider.Name;
                for (int MultipleSpeakingChars = 0; MultipleSpeakingChars < ReadTextName.Split(" & ").Length; MultipleSpeakingChars++)
                {
                    string AttemptFile = FolderPath + "\\" + ReadTextName.Split(" & ")[MultipleSpeakingChars] + ".flac";
                    if (File.Exists(AttemptFile))
                    {
                        // Soundman, what the fuck is the number 5 doing here?
                        // Well, observant programmer, 5 is one less than half of a jam fade duration. This is a quick
                        // and dirty trick to re-align the audio after we do jam fading, but it works. 
                        PlaceLine(AttemptFile, FrameToConsider.StartFrame + 5, OperatingScene, Doc);
                    } else
                    {
                        Console.WriteLine(AttemptFile + " Does not Exist.");
                    }
                }
            }
        }
    }
}