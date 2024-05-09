using CsXFL;
class RCompCreate
{
    const string USAGE = "Usage: RCompCreate <input_file>";
    const string RCOMP_SYMBOL_NAME = "RCOMP►CHARACTERS";
    const string RCOMP_SHADING_NAME = "RCOMP►SHADING";
    public static void Main(string[] args)
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
            var VCFolderSymbol = scene.ConvertLayersToSymbol(VCFolderList, RCOMP_SYMBOL_NAME);
            // find the newly created layer and duplicate it
            var RCompLayerIndex = scene.FindLayerIndex(RCOMP_SYMBOL_NAME);
            if (RCompLayerIndex.Count != 1)
            {
                System.Console.WriteLine("RCOMP►CHARACTERS layer not found in scene OR multiple layers found.");
                continue;
            }
            var RCompLayer = scene.Layers[RCompLayerIndex[0]];
            scene.DuplicateLayer(RCompLayerIndex[0]);
            // find the duplicated layer and rename it to RCOMP►SHADING
            var RCompShadingLayerIndex = scene.FindLayerIndex(RCOMP_SYMBOL_NAME + "_copy");
            if (RCompShadingLayerIndex.Count != 1)
            {
                System.Console.WriteLine("RCOMP►SHADING layer not found in scene OR multiple layers found.");
                continue;
            }
            scene.Layers[RCompShadingLayerIndex[0]].Name = RCOMP_SHADING_NAME;
            // find the RCOMP►SHADING layer put it right above the RCOMP►CHARACTERS layer
            scene.ReorderLayer(RCompShadingLayerIndex[0], RCompLayerIndex[0]);
        } while (true);
        inp = null;
        do
        {
            Console.WriteLine("Save document? (y/n)");
            inp = Console.ReadLine();
            if(inp == null) continue;
            if (inp.ToLower().StartsWith('y'))
            {
                doc.Save();
                break;
            }
        } while (inp is null);
    }
}