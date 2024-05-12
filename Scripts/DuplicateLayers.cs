using CsXFL;

class DuplicateLayers
{
    const string USAGE = "Usage: DuplicateLayers <input_file>";
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
        sceneList:
            System.Console.WriteLine("Available Scenes:");
            for (int i = 0; i < doc.Timelines.Count; i++)
            {
                System.Console.WriteLine($"{i}: {doc.Timelines[i].Name}");
            }
            System.Console.WriteLine("Enter scene number to duplicate layers in (or 'q' to quit):");
            inp = System.Console.ReadLine();
            if (inp == "q")
            {
                break;
            }
            if (inp == null)
            {
                goto sceneList;
            }
            int? sceneNum = int.Parse(inp);
            if (sceneNum < 0 || sceneNum >= doc.Timelines.Count || sceneNum == null)
            {
                System.Console.WriteLine("Invalid scene number.");
                goto sceneList;
            }
            var scene = doc.Timelines[sceneNum.Value];
        layerList:
            System.Console.WriteLine($"Available Layers in {scene.Name}:");
            for (int i = 0; i < scene.Layers.Count; i++)
            {
                System.Console.WriteLine($"{i}: {scene.Layers[i].Name}");
            }
            System.Console.WriteLine("Enter layer number to duplicate (or 'q' to quit):");
            inp = System.Console.ReadLine();
            if (inp == "q")
            {
                break;
            }
            if (inp == null)
            {
                goto layerList;
            }
            int? layerNum = int.Parse(inp);
            if (layerNum < 0 || layerNum >= scene.Layers.Count || layerNum == null)
            {
                System.Console.WriteLine("Invalid layer number.");
                goto layerList;
            }
            var layer = scene.Layers[layerNum.Value];
            scene.DuplicateLayer(layerNum.Value);
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