using CsXFL;
/*
class LineAdderAPI
{
    static Document Doc = new("C:\\Stuff\\CXFL\\SceneGenerator\\LipsyncingTest\\DOMDocument.xml");
    int SafeFrames = 12000;

    int ExtendVoiceLine(int Duration, int FrameReference)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        CurrentTimeline.InsertFrames(3 + (int)(Math.Ceiling(Doc.FrameRate * Duration / 1000)) - CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Duration);

        return 0;
    }

    int PlaceLine(string AttemptFile, int FrameReference)
    {
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
        string FrameReferenceName = CurrentTimeline.Layers[Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0]].GetFrame(FrameReference).Name;
        string LayerName = FrameReferenceName.Substring(7).Replace(". ", "").Replace(" ", "_").ToUpper() + "_VOX";        

        if (CurrentTimeline.FindLayerIndex(LayerName).Count == 0) CurrentTimeline.AddNewLayer(LayerName, "normal");

        Doc.ImportFile(AttemptFile);
        ExtendVoiceLine((int)(SoundUtils.GetSoundDuration(AttemptFile)), FrameReference);

        return 0;
    }

    int InsertLinesChunked(string FolderPath, int ChunkSizes, int TotalChunks)
    {
        int SceneNumber = 0;
        int Count = 0;

        //Fake as fuck logic, fix this
        while (1 < 5) 
        {
            Timeline CurrentTimeline = Doc.GetTimeline(SceneNumber);
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];

            string ReadTextName = CurrentTimeline.Layers[TextLayerIndex].GetFrame(0).Name;

            for (int i = 0; i < ReadTextName.Split(" & ").Length; i++)
            {
                string AttemptFile = FolderPath 
            }
        }
        //Write it lol
        return 0;
    }
}
*/