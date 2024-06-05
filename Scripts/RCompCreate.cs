using CsXFL;
class RCompCreate
{
    const string USAGE = "Usage: RCompCreate <input_file>";
    const string RCOMP_SYMBOL_NAME = "RCOMP►CHARACTERS";
    const string RCOMP_SHADING_NAME = "RCOMP►SHADING";
    const string RCOMP_JAM_MASK_NAME = "RCOMP►JamMask";
    const string RCOMP_JAM_CHARS_NAME = "RCOMP►JamChars";
    public static void Run(string[] args)
    {
        if (args.Length != 1)
        {
            System.Console.WriteLine(USAGE);
            return;
        }
        CsXFL.Document doc = new(args[0]);
        string? inp;
        do
        {
            System.Console.WriteLine("Available Scenes:");
            for (int i = 0; i < doc.Timelines.Count; i++)
            {
                System.Console.WriteLine($"{i}: {doc.Timelines[i].Name}");
            }
            System.Console.WriteLine("Enter scene number to create composition layers for (or 'q' to quit):");
            inp = System.Console.ReadLine();
            if (inp == "q")
            {
                break;
            }
            if (inp == null)
            {
                continue;
            }
            int? sceneNum = int.Parse(inp);
            if (sceneNum < 0 || sceneNum >= doc.Timelines.Count || sceneNum == null)
            {
                System.Console.WriteLine("Invalid scene number.");
                continue;
            }
            var scene = doc.Timelines[sceneNum.Value];
            // find VECTOR_CHARACTERS folder and convert to movie clip symbol
            var VCFolders = scene.FindLayerIndex("VECTOR_CHARACTERS");
            if (VCFolders.Count != 1)
            {
                System.Console.WriteLine("VECTOR_CHARACTERS folder not found in scene OR multiple folders found.");
                continue;
            }
            List<Layer> VCFolderList = new List<Layer> { scene.Layers[VCFolders[0]] };
            string rCompSymbolName = RCOMP_SYMBOL_NAME + sceneNum;
            var VCFolderSymbol = scene.ConvertLayersToSymbol(VCFolderList, rCompSymbolName);
            // find the newly created layer and duplicate it
            var RCompLayerIndex = scene.FindLayerIndex(rCompSymbolName);
            if (RCompLayerIndex.Count != 1)
            {
                System.Console.WriteLine("RCOMP►CHARACTERS layer not found in scene OR multiple layers found.");
                continue;
            }
            var RCompLayer = scene.Layers[RCompLayerIndex[0]];
            scene.DuplicateLayer(RCompLayerIndex[0]);
            // find the duplicated layer and rename it to RCOMP►SHADING
            var RCompShadingLayerIndex = scene.FindLayerIndex(rCompSymbolName + "_copy");
            if (RCompShadingLayerIndex.Count != 1)
            {
                System.Console.WriteLine("RCOMP►SHADING layer not found in scene OR multiple layers found.");
                continue;
            }
            scene.Layers[RCompShadingLayerIndex[0]].Name = RCOMP_SHADING_NAME;
            (scene.Layers[RCompShadingLayerIndex[0]].KeyFrames[0].Elements[0] as SymbolInstance)!.BlendMode = "overlay";
            inp = null;
            do
            {
                System.Console.WriteLine("Composite Jam Layer? (y/n)");
                inp = System.Console.ReadLine();
                if (inp == null) continue;
                if (inp.ToLower().StartsWith('y'))
                {
                    break;
                }
            } while (inp is null);
            if (inp.ToLower().StartsWith('y'))
            {
                var jamMaskLayerIndex = scene.FindLayerIndex("JAM_MASK");
                if (jamMaskLayerIndex.Count != 1)
                {
                    System.Console.WriteLine("JAM_MASK layer not found in scene OR multiple layers found.");
                    continue;
                }
                var jamMaskLayer = scene.Layers[jamMaskLayerIndex[0]];
                // convert jam mask layer to symbol
                string rCompJamMaskSymbolName = RCOMP_JAM_MASK_NAME + sceneNum;
                scene.ConvertLayersToSymbol(new List<Layer> { jamMaskLayer }, rCompJamMaskSymbolName);
                jamMaskLayerIndex = scene.FindLayerIndex(rCompJamMaskSymbolName);
                if (jamMaskLayerIndex.Count != 1)
                {
                    throw new Exception("Failed to find jam mask layer after converting it to symbol. Something is wrong.");
                }
                jamMaskLayer = scene.Layers[jamMaskLayerIndex[0]];
                jamMaskLayer.Name = RCOMP_JAM_MASK_NAME;

                (jamMaskLayer.KeyFrames[0].Elements[0] as SymbolInstance)!.BlendMode = "erase";
                // convert jam mask layer AND rcomp characters layer to symbol
                string rCompJamCharsName = RCOMP_JAM_CHARS_NAME + sceneNum;
                scene.ConvertLayersToSymbol(new List<Layer> { jamMaskLayer, RCompLayer }, rCompJamCharsName);
                // find the newly created layer
                var jamCharsRCompLayerIndex = scene.FindLayerIndex(rCompJamCharsName);
                if (jamCharsRCompLayerIndex.Count != 1)
                {
                    throw new Exception("Failed to find jam chars layer after converting it to symbol. Something is wrong.");
                }
                var jamCharsRCompLayer = scene.Layers[jamCharsRCompLayerIndex[0]];
                jamCharsRCompLayer.Name = RCOMP_JAM_CHARS_NAME;
                (jamCharsRCompLayer.KeyFrames[0].Elements[0] as SymbolInstance)!.BlendMode = "layer";
                // put shading layer above jam chars layer
                scene.ReorderLayer(RCompShadingLayerIndex[0], jamCharsRCompLayerIndex[0]);
            }
            // find the RCOMP►SHADING layer put it right above the RCOMP►CHARACTERS layer OR above the jamCharsRComp Layer
            else scene.ReorderLayer(RCompShadingLayerIndex[0], RCompLayerIndex[0]);
        } while (true);
        inp = null;
        do
        {
            Console.WriteLine("Save document? (y/n)");
            inp = Console.ReadLine();
            if (inp == null) continue;
            if (inp.ToLower().StartsWith('y'))
            {
                try
                {
                    doc.Save();
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine(e.Message);
                    System.Console.WriteLine("Failed to save document. You likely have the document open in Adobe Animate, close it and try again.\n Press any key to continue.");
                    // press any key to continue
                    System.Console.ReadKey();
                    continue;
                }
            }
        } while (inp is null);
    }
}