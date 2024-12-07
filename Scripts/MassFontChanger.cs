using CsXFL;
using SixLabors.Fonts;
public class MassFontChanger
{
    const string USAGE = "Usage: MassFontChanger <input_file>";
    public static void Run(string[] args)
    {
        if (args.Length != 1)
        {
            System.Console.WriteLine(USAGE);
            return;
        }
        CsXFL.Document doc = new(args[0]);
        string? inp;
        Console.WriteLine("Available Fonts:");
        foreach (var font in SystemFonts.Families)
        {
            System.Console.WriteLine($"{font.Name}");
        }
        System.Console.WriteLine("Enter font name to replace all fonts with (or '!q' to quit):");
        do
        {
            inp = System.Console.ReadLine();
            if (inp != null && inp != "!q")
            {
                try
                {
                    var font = SystemFonts.CreateFont(inp, 1337);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Invalid font name: {e.Message}");
                    inp = null;
                    continue;
                }
                string fontName = inp.Trim();
                // iterate over all timelines, layers, frames and elements to replace all fonts with the new font
                foreach (var timeline in doc.Timelines)
                {
                    foreach (var layer in timeline.Layers)
                    {
                        foreach (var frame in layer.KeyFrames)
                        {
                            foreach (var element in frame.Elements)
                            {
                                if (element is Text text)
                                {
                                    text.SetTextAttr("face", fontName);
                                }
                            }
                        }
                    }
                }
                System.Console.WriteLine("Would you like to also replace fonts in all library items? (y/n)");
                do
                {
                    inp = System.Console.ReadLine();
                } while (string.IsNullOrEmpty(inp));
                if (inp.ToLower() == "y")
                {
                    foreach (var libraryItem in doc.Library.Items.Values)
                    {
                        if (libraryItem is SymbolItem si)
                        {
                            var timeline = si.Timeline;
                            foreach (var layer in timeline.Layers)
                            {
                                foreach (var frame in layer.KeyFrames)
                                {
                                    foreach (var element in frame.Elements)
                                    {
                                        if (element is Text text)
                                        {
                                            text.SetTextAttr("face", fontName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        } while (string.IsNullOrEmpty(inp));
        System.Console.WriteLine("All fonts replaced. Would you like to save the file? (y/n)");
        inp = System.Console.ReadLine();
        if (inp?.ToLower() != "y") return;
        try
        {
            doc.Save();
        }
        catch (Exception e)
        {
            System.Console.Error.WriteLine($"Error saving file: {e.Message}");
            System.Console.Error.WriteLine($"You likely have the file open in Adobe Animate. Please close it and try again.");
        }
        System.Console.WriteLine("Press any key to exit...");
        System.Console.ReadKey();
    }
}