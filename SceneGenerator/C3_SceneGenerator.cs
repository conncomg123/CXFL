using CsXFL;
using SceneGenerator.API;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SixLabors.Fonts;
using System.Runtime.CompilerServices;
using System.Globalization;
namespace SceneGenerator;

// <!> Feature Todo:
// 1. Evidence API (Soundman will do this after 3-4)
// 2. Interjection! + Courtroom swipe support for SFX api, if Courtmode. (Soundman will do this after 3-4)
// 3. Typewriter exclusion for (Not Shown) Don't know how to not do this without destabilizing effects + using shitty goto

// <!> Errors/Style Todo:
// 1. Extreme ambiguity between config.ViewMode, string ModeText, string ViewMode. Consolidate most instances into config.ViewMode for better readability, I was rushing :(
// 2. JamFades desync all dialogue by six frames due to execution order, can't figure out an elegant solution until 3-5.
// 3. CFG warning happens twice because SFX API accesses CFGs. It is minorly annoying.
// 4. Console display is ugly and misaligned, didn't have time for cleaner implementation
// 5. Want a warning if a rig doesn't have emotion engine data.
// 6. Sometimes rigs double up in the library. When this happens, the main rig is in the RIG folder, and has an appropriate use count such as 1900. Secondary rig is in the library root with a 0 use count.
// 7. Sometimes rig importing puts things in the library root that shouldn't be. When importing Philo, PhiloReed►HindLeg_3-4rearnomark may be stranded in library root
// Any cleanup Connor wants to do.

public class NameswapConfig
{
    public string OriginalName { get; }
    public string TruncatedName { get; }

    public NameswapConfig(string originalName, string truncatedName)
    {
        OriginalName = originalName;
        TruncatedName = truncatedName;
    }
}

public class LetterspacingConfig
{
    public string CharacterName { get; }
    public int CorrectedSpacing { get; }

    public LetterspacingConfig(string characterName, int correctedSpacing)
    {
        CharacterName = characterName;
        CorrectedSpacing = correctedSpacing;
    }
}

public class SymbolConfig
{
    public string LibraryPath { get; set; }
    public double Tx { get; set; }
    public double Ty { get; set; }

    public SymbolConfig(string libraryPath, double tx, double ty)
    {
        LibraryPath = libraryPath;
        Tx = tx;
        Ty = ty;
    }
}

public class CharacterConfig
{
    public List<CharacterInfo> Characters { get; }

    public CharacterConfig()
    {
        Characters = new List<CharacterInfo>();
    }

    public void AddCharacter(string simplifiedName, string libraryPath, string? pathToRigFile = null, string? libraryPathInRigFile = null, int transX = 0, int transY = 0)
    {
        Characters.Add(new CharacterInfo(simplifiedName, libraryPath, pathToRigFile, libraryPathInRigFile, transX, transY));
    }

    public class CharacterInfo
    {
        public string SimplifiedName { get; }
        public string LibraryPath { get; }
        public string? PathToRigFile { get; }
        public string? LibraryPathInRigFile { get; }
        public int TransX { get; }
        public int TransY { get; }

        public CharacterInfo(string simplifiedName, string libraryPath, string? pathToRigFile, string? libraryPathInRigFile, int transX, int transY)
        {
            SimplifiedName = simplifiedName;
            LibraryPath = libraryPath;
            PathToRigFile = pathToRigFile;
            LibraryPathInRigFile = libraryPathInRigFile;
            TransX = transX;
            TransY = transY;
        }
    }
}

public class SceneData
{
    public Dictionary<string, DialogueLine>? Dialogue { get; set; }
    public Dictionary<string, SFXLine>? SFX { get; set; }
    public TypewriterData? Typewriter { get; set; }
    public DataLabels? DataLabels { get; set; }
}

public class DialogueLine
{
    public string? CharacterName { get; set; }
    public string? LineText { get; set; }
    public string? Emotion { get; set; }
}

public class SFXLine
{
    public string? Alignment { get; set; }
    public string? SFX { get; set; }
}

public class TypewriterData
{
    public string? Time { get; set; }
    public string? Location { get; set; }
}

public class DataLabels
{
    public string? EpisodeText { get; set; }
    public string? SceneText { get; set; }
    public string? ModeText { get; set; }
}

public class DoubleIntPair
{
    public double DoubleValue { get; set; }
    public int IntValue { get; set; }

    public DoubleIntPair(double d, int i)
    {
        DoubleValue = d;
        IntValue = i;
    }
}

static class SceneGenerator
{
    // Doesn't align correctly, don't care for right now lol
    static void MeasureAndTrace<T>(Action<T> action, T item, string description)
    {
        Stopwatch stpw = new Stopwatch();
        stpw.Start();
        action(item);
        stpw.Stop();

        int tabLength = 7;
        int descriptionLength = description.Length + (description.Count(c => c == '\t') * (tabLength - 1));
        int spacerLength = Math.Max(0, tabLength - (descriptionLength + stpw.ElapsedMilliseconds.ToString().Length) / 8);
        string spacer = new string('\t', spacerLength);

        Trace.WriteLine(description + spacer + " took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();
    }

    static double LevenshteinRatio(string s1, string s2)
    {
        int maxLen = Math.Max(s1.Length, s2.Length);
        if (maxLen == 0)
            return 1.0;

        int distance = LevenshteinDistance(s1, s2);
        return 1.0 - (double)distance / maxLen;
    }

    //Yeah a human wrote this
    static int LevenshteinDistance(string s1, string s2)
    {
        int[,] matrix = new int[s1.Length + 1, s2.Length + 1];
        for (int i = 0; i <= s1.Length; i++) { matrix[i, 0] = i; }
        for (int j = 0; j <= s2.Length; j++) { matrix[0, j] = j; }
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                                         matrix[i - 1, j - 1] + cost);
            }
        }
        return matrix[s1.Length, s2.Length];
    }

    static DoubleIntPair FindMaxDouble(List<DoubleIntPair> pairs)
    {
        if (pairs.Count == 0)
            throw new ArgumentException("List is empty");

        DoubleIntPair maxPair = pairs[0];
        foreach (var pair in pairs)
        {
            if (pair.DoubleValue > maxPair.DoubleValue)
            {
                maxPair = pair;
            }
        }
        return maxPair;
    }

    static void TypewriterFormat(this Document Doc, Text TextOp, int LetterSpacing)
    {
        TextOp.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
        TextOp.SetTextAttr("size", 80);
        TextOp.SetTextAttr("fillColor", "#00FF33");
        TextOp.SetTextAttr("letterSpacing", LetterSpacing);
        TextOp.SetTextAttr("lineSpacing", 2);
        TextOp.SetTextAttr("alignment", "left");
        TextOp.FontRenderingMode = "standard";
        TextOp.TextType = "static";
    }

    static void InputValidation(this Document Doc, string ModeText)
    {
        SingletonConfig config = SingletonConfig.Instance;
        CharacterConfig RigConfig = config.GetCharacterConfig(ModeText);

        foreach (var character in RigConfig.Characters)
        {
            string simplifiedName = character.SimplifiedName;
            string libraryPath = character.LibraryPath;

            if (!Doc.Library.ItemExists(character.LibraryPath)) { Debug.Assert(true, $"{character}'s rig ({character.LibraryPath}) does not exist."); }
        }

        // Do our paths exist?
        if (!File.Exists(config.PathToSceneData)) throw new Exception("Scene data file does not exist.");

        if (!config.SkipLines)
        {
            if (!Directory.Exists(config.PathToCFGs)) throw new FileNotFoundException("CFG directory does not exist.");
            if (!Directory.Exists(config.PathToLines)) throw new FileNotFoundException("Line directory does not exist.");
        }

        // Skip logic
        if (!config.SkipLines && config.SkipRigs) throw new Exception("Cannot generate when skipping rigs but not line placement.");
    }

    static void CreateLayerIfDoesntExist(this Document Doc, string LayerName, string LayerType = "normal")
    {
        if (Doc.Timelines[Doc.CurrentTimeline].FindLayerIndex(LayerName).Count == 0) { Doc.Timelines[Doc.CurrentTimeline].AddNewLayer(LayerName, LayerType); }
    }

    static void goToVoiceLine(this Document Doc, string inputString)
    {
        int LineIndex = int.Parse(inputString.Substring(3, 3));
        int SceneIndex = (int)(Math.Ceiling((double)LineIndex / (double)SingletonConfig.Instance.ChunkSize)) - 1;

        Timeline CurrentTimeline = Doc.Timelines[SceneIndex];
        Layer TextLayer = CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXT")[0]];

        for (int i = 0; i < TextLayer.KeyFrames.Count; i++)
        {
            if (TextLayer.KeyFrames[i].Name == inputString)
            {
                CurrentTimeline.CurrentFrame = TextLayer.KeyFrames[i].StartFrame;
                break;
            }
        }
    }

    static void ImportRigs(this Document doc, string SceneData, string ViewMode)
    {
        SingletonConfig config = SingletonConfig.Instance;
        CharacterConfig RigConfig = config.GetCharacterConfig(ViewMode);
        CharacterConfig Investigation_RigConfig = config.GetCharacterConfig("Investigation");

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var dialogueLines = deserializedJson.Dialogue!;

        HashSet<string> charactersThatAppear = new HashSet<string>();

        foreach (var dialogueKey in dialogueLines.Keys)
        {
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string CapitalizedCharacterName = textInfo.ToTitleCase(dialogueLines[dialogueKey].CharacterName!);

            // Skip gallery ponies
            if (CapitalizedCharacterName.Contains("Gallery")) { continue; }

            charactersThatAppear.Add(CapitalizedCharacterName);
        }

        foreach (var character in RigConfig.Characters)
        {
            if (charactersThatAppear.Contains(character.SimplifiedName))
            {
                string libraryPath = character.LibraryPath;
                string? pathToRigFile = Path.Combine(Path.GetDirectoryName(config.PathToOperatingDocument)!, "rigs\\" + character.PathToRigFile);
                string? libraryPathInRigFile = character.LibraryPathInRigFile;

                if (pathToRigFile != null && libraryPathInRigFile != null && !doc.Library.ItemExists(libraryPath))
                {
                    // Assuming path to rig is a FOLDER
                    doc.ImportFolderFromOtherDocument(pathToRigFile, libraryPathInRigFile);
                    Item imported = doc.Library.Items[libraryPathInRigFile];
                    doc.Library.MoveToFolder("RIGS", imported);
                }
            }
        }

        // If any rig was not found in RigConfig, search in Investigation_RigConfig
        var remainingCharacters = charactersThatAppear.Except(RigConfig.Characters.Select(c => c.SimplifiedName));
        foreach (var characterName in remainingCharacters)
        {
            var character = Investigation_RigConfig.Characters.FirstOrDefault(c => c.SimplifiedName == characterName);
            // This probably won't work as a check
            if (character != null)
            {
                string libraryPath = character.LibraryPath;
                string? pathToRigFile = Path.Combine(Path.GetDirectoryName(config.PathToOperatingDocument)!, "rigs\\" + character.PathToRigFile);
                string? libraryPathInRigFile = character.LibraryPathInRigFile;

                if (pathToRigFile != null && libraryPathInRigFile != null && !doc.Library.ItemExists(libraryPath))
                {
                    // Assuming path to rig is a FOLDER
                    doc.ImportFolderFromOtherDocument(pathToRigFile, libraryPathInRigFile);
                    Item imported = doc.Library.Items[libraryPathInRigFile];
                    doc.Library.MoveToFolder("RIGS", imported);
                }
            } else { throw new Exception("Character rig not found for " + characterName + " in either " + ViewMode + " config or Investigation config."); }
        }
    }

    static void PlaceText(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        CsXFL.Rectangle DialogueBounding = new(40.05 * 2, 549.5 * 2, 1212.95 * 2, 708.92 * 2);
        CsXFL.Rectangle SpeakerBounding = new(20.05 * 2, 482.5 * 2, 254.4 * 2, 540.2 * 2);

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var dialogueLines = deserializedJson.Dialogue;

       foreach (var dialogueKey in dialogueLines!.Keys)
        {
            var dialogueLine = dialogueLines[dialogueKey];

            string LineID = dialogueKey;
            string Character = dialogueLine.CharacterName!;
            string Dialogue = dialogueLine.LineText!;

            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));
            int SceneIndex = (int)Math.Ceiling((double)LineIndex / config.ChunkSize);

            if (Doc.Timelines.Count < SceneIndex)
            {
                Doc.AddNewScene("Scene " + SceneIndex);
            }

            Doc.CurrentTimeline = (SceneIndex - 1);
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            CreateLayerIfDoesntExist(Doc, "TEXTBOX");
            CreateLayerIfDoesntExist(Doc, "TEXT");

            int TextboxLayerIndex = CurrentTimeline.FindLayerIndex("TEXTBOX")[0];
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];
            int OperatingFrameIndex = config.DefaultFrameDuration * ((LineIndex - 1) % config.ChunkSize);

            if ((OperatingFrameIndex + config.DefaultFrameDuration) > CurrentTimeline.Layers[TextLayerIndex].GetFrameCount())
            {
                CurrentTimeline.InsertFrames(config.DefaultFrameDuration, true, OperatingFrameIndex);
            }

            CurrentTimeline.Layers[TextLayerIndex].ConvertToKeyframes(OperatingFrameIndex);

            if (CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(0).Elements.Count == 0)
            {
                Doc.Library.AddItemToDocument("OTHER ASSETS/Textbox", CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(OperatingFrameIndex), 0, 0);
                SymbolInstance TextboxElement = (CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance)!;
                TextboxElement.Loop = "single frame";
                TextboxElement.ScaleX = 1.34164876055;
                TextboxElement.ScaleY = 1.34152669671;
                TextboxElement.Matrix.Tx = -7.3;
                TextboxElement.Matrix.Ty = -6.9;
            }

            CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).ClearElements();
            Text DialogueText = CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).AddNewText(DialogueBounding, Dialogue);
            Text SpeakerText = CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).AddNewText(SpeakerBounding, config.GetTruncatedName(Character).Trim());

            DialogueText.SetTextAttr("alignment", "left");
            DialogueText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            DialogueText.SetTextAttr("size", 80);
            DialogueText.SetTextAttr("fillColor", "#ffffff");
            DialogueText.SetTextAttr("letterSpacing", 2);
            DialogueText.SetTextAttr("lineSpacing", 1);
            DialogueText.TextType = "dynamic";
            DialogueText.LineType = "multiline";
            DialogueText.Name = "DialogueText";
            DialogueText.FontRenderingMode = "standard";

            if (Dialogue.IndexOf('(') > -1)
            {
                DialogueText.SetTextAttr("size", 84);
                DialogueText.SetTextAttr("fillColor", "#008fff");
            }

            if (Character == "Widget") { DialogueText.SetTextAttr("fillColor", "#D7D700"); }

            SpeakerText.SetTextAttr("alignment", "left");
            SpeakerText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            SpeakerText.SetTextAttr("size", 84);
            SpeakerText.SetTextAttr("fillColor", "#ffffff");
            SpeakerText.SetTextAttr("letterSpacing", 2);
            SpeakerText.SetTextAttr("lineSpacing", 1);
            SpeakerText.TextType = "dynamic";
            SpeakerText.LineType = "multiline";
            SpeakerText.Name = "SpeakerText";
            SpeakerText.FontRenderingMode = "standard";

            if (config.GetLetterspacing(Character) != 0) { SpeakerText.SetTextAttr("lineSpacing", config.GetLetterspacing(Character)); }

            CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).Name = LineID;
        }
        // Remove one frame from the end of each chunk
        for (int i = 0; i < Doc.Timelines.Count; i++)
        {
            Timeline CurrentTimeline = Doc.GetTimeline(i);
            int LastFrameIndex = CurrentTimeline.Layers[0].GetFrameCount() - 1;
            CurrentTimeline.RemoveFrames(1, true, LastFrameIndex);
        }
    }

    static void PlaceRigs(this Document Doc, string SceneData, string ModeText)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var dialogueLines = deserializedJson.Dialogue!;

        string previousCharacter = "";
        var keys = dialogueLines.Keys.ToList();

        for (int i = 0; i < keys.Count; i++)
        {
            var dialogueKey = keys[i];
            var dialogueLine = dialogueLines[dialogueKey];
            string Character = dialogueLine.CharacterName!;

            if ((Character == config.Defense) && ModeText == "Investigation")
            {
                if (string.IsNullOrEmpty(previousCharacter)) continue;
                Character = previousCharacter;
            }

            string CharacterLayerName = Character.ToUpper();
            string Emotion = dialogueLine.Emotion!;

            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));
            int SceneIndex = (int)Math.Ceiling((double)LineIndex / config.ChunkSize);

            Doc.CurrentTimeline = (SceneIndex - 1);
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            CreateLayerIfDoesntExist(Doc, "VECTOR_CHARACTERS", "folder");
            CreateLayerIfDoesntExist(Doc, CharacterLayerName);

            int CharacterLayerIndex = CurrentTimeline.FindLayerIndex(CharacterLayerName)[0];
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];
            int OperatingFrameIndex = config.DefaultFrameDuration * ((LineIndex - 1) % config.ChunkSize);

            CurrentTimeline.Layers[CharacterLayerIndex].ConvertToKeyframes(OperatingFrameIndex);
            CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex).ClearElements();

            // Do some logic here to fix end caps for defense
            if (i != keys.Count - 1 && i % config.ChunkSize != config.ChunkSize - 1)
                CurrentTimeline.Layers[CharacterLayerIndex].ConvertToKeyframes(OperatingFrameIndex + config.DefaultFrameDuration);

            CharacterConfig CurrentCharacterConfiguration = config.GetCharacterConfig(ModeText);

            string LibraryPath = "";
            int TransX = 0;
            int TransY = 0;

            // Search for the character's rig in the specified view mode
            foreach (var character in CurrentCharacterConfiguration.Characters)
            {
                if (Character.Contains(character.SimplifiedName))
                {
                    LibraryPath = character.LibraryPath;
                    TransX = character.TransX;
                    TransY = character.TransY;
                    break;
                }
            }

            // If rig is not found in the specified view mode, search in the Investigation configuration
            if (string.IsNullOrEmpty(LibraryPath))
            {
                CharacterConfig InvestigationConfig = config.GetCharacterConfig("Investigation");
                foreach (var character in InvestigationConfig.Characters)
                {
                    if (Character.Contains(character.SimplifiedName))
                    {
                        LibraryPath = character.LibraryPath;
                        TransX = character.TransX;
                        TransY = character.TransY;
                        break; // Found the rig in Investigation, no need to search further
                    }
                }
            }

            // Skip gallery ponies
            if (Character.Contains("Gallery")) { continue; }

            if (!string.IsNullOrEmpty(LibraryPath))
            {
                bool WasRigPlaced = Doc.Library.AddItemToDocument("RIGS/" + LibraryPath, CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex), TransX, TransY);
                if (WasRigPlaced)
                {
                    SymbolInstance CharacterRig = (CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance)!;
                    CharacterRig.Loop = "single frame";
                    CharacterRig.FirstFrame = PoseAutomation(Doc, "RIGS/" + LibraryPath, Emotion);
                }
                else { throw new Exception("An error occurred when attempting " + Character + "'s rig placement."); }
            }
            else { throw new Exception("Character rig not found for " + Character + " in view mode " + ModeText + " or Investigation config."); }

            CurrentTimeline.Layers[CharacterLayerIndex].ParentLayerIndex = CurrentTimeline.FindLayerIndex("VECTOR_CHARACTERS")[0];
            previousCharacter = Character;
        }
    }

    static int PoseAutomation(this Document Doc, string LibraryPath, string Emotion)
    {
        SymbolItem RigSymbol = (Doc.Library.Items[LibraryPath] as SymbolItem)!;
        Layer RigEE_Data = RigSymbol.Timeline.Layers[RigSymbol.Timeline.Layers.Count - 1];

        // Assume nonexistent emotion is neutral.
        if (Emotion == "") { Emotion = "-"; }

        List<DoubleIntPair> pairs = new List<DoubleIntPair>();

        foreach (Frame ConsiderKeyframe in RigEE_Data.KeyFrames)
        {
            pairs.Add(new DoubleIntPair(LevenshteinRatio(Emotion, ConsiderKeyframe.Name), ConsiderKeyframe.StartFrame));
        }

        if (pairs.Count == 0)
        {
            Console.WriteLine("Warning: No EmotionEngine data for " + LibraryPath);
        }

        DoubleIntPair maxPair = FindMaxDouble(pairs);

        return maxPair.IntValue;
    }

    static void OrganizeLayerStructure(this Document Doc, string[] DesiredLayerOrder)
    {
        for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
        {
            Doc.CurrentTimeline = SceneIndex;
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            foreach (var LayerName in DesiredLayerOrder)
            {
                CreateLayerIfDoesntExist(Doc, LayerName);

                int LayerIndex = Doc.Timelines[SceneIndex].FindLayerIndex(LayerName)[0];
                int TargetIndex = Array.IndexOf(DesiredLayerOrder, LayerName);

                if (LayerIndex != TargetIndex)
                {
                    // Determine the reference index for reordering
                    int ReferenceIndex = TargetIndex > 0 ? Doc.Timelines[SceneIndex].FindLayerIndex(DesiredLayerOrder[TargetIndex - 1])[0] + 1 : 0;

                    // Ensure the reference index is within bounds
                    ReferenceIndex = Math.Min(ReferenceIndex, Doc.Timelines[SceneIndex].Layers.Count);

                    bool AddBefore = true; // Always add before for correct ordering

                    Doc.Timelines[SceneIndex].ReorderLayer(LayerIndex, ReferenceIndex, AddBefore);
                }
            }

            // Create an AUDIO layer if it doesn't exist
            CreateLayerIfDoesntExist(Doc, "AUDIO", "folder");
            int audioLayerIndex = Doc.Timelines[SceneIndex].FindLayerIndex("AUDIO")[0];

            // Define the array to store the Layer names
            List<string> layerNames = new List<string>();

            // Iterate through the layers and add the names to the array
            foreach (var CurrentLayer in CurrentTimeline.Layers)
            {
                string LayerName = CurrentLayer.Name;
                if (LayerName.Contains("_VOX") || LayerName.Contains("SFX"))
                {
                    layerNames.Add(LayerName);
                }
            }

            // Move the Background layer above the Audio layer
            int backgroundLayerIndex = CurrentTimeline.FindLayerIndex("BACKGROUNDS")[0];
            audioLayerIndex = CurrentTimeline.FindLayerIndex("AUDIO")[0];
            CurrentTimeline.ReorderLayer(backgroundLayerIndex, audioLayerIndex, true);

            // Iterate through the array and reorder the layers
            foreach (var LayerName in layerNames)
            {
                // Get the index of the layer using FindLayerIndex and reorder it
                int layerIndex = CurrentTimeline.FindLayerIndex(LayerName)[0];
                CurrentTimeline.ReorderLayer(layerIndex, audioLayerIndex, false);
                audioLayerIndex = CurrentTimeline.FindLayerIndex("AUDIO")[0];
            }

            // Delete the default Layer_1
            if (CurrentTimeline.FindLayerIndex("Layer_1").Any())
            {
                int DefaultLayerIndex = CurrentTimeline.FindLayerIndex("Layer_1")[0];
                CurrentTimeline.DeleteLayer(DefaultLayerIndex);
            }
        }
    }

    static double ReturnSFXAlignmentOffset(string filePath, string Alignment)
    {
        double offset = 0;

        try
        {
            string targetVariable = "words";
            string fileContent = File.ReadAllText(filePath);

            // Extract the specific variable content using regex
            // I can't believe this shit actually works
            string pattern = $@"var\s+{Regex.Escape(targetVariable)}\s*=\s*{{\s*(.*?)}};";
            Match match = Regex.Match(fileContent, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                string variableContent = match.Groups[1].Value;

                // Parse variable content into dictionary
                pattern = @"(\d+\.\d+)\s*:\s*\[([^\]]*)\]";
                MatchCollection matches = Regex.Matches(variableContent, pattern);

                foreach (Match m in matches)
                {
                    double key = double.Parse(m.Groups[1].Value);
                    string[] values = m.Groups[2].Value.Split(',');

                    if (values.Length >= 2 && values[1].Trim('"') == Alignment)
                    {
                        offset = key;
                        break; // Stop searching after finding the first instance
                    }
                }
            }
            else
            {
                Console.WriteLine($"Variable '{targetVariable}' not found in the file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return offset;
    }

    static void ParseSFX(this Document Doc, string sceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(sceneData)!;
        var SFXData = deserializedJson.SFX!;

        foreach (var dialogueKey in SFXData.Keys)
        {
            var dialogueLine = SFXData[dialogueKey];
            string Alignment = dialogueLine.Alignment!;
            string SFX = dialogueLine.SFX!;

            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));
            int SceneIndex = (int)Math.Ceiling((double)LineIndex / config.ChunkSize);

            // Skip non-scenes like Typewriters
            if (!Doc.GetTimeline(SceneIndex).Name.Contains("Scene")) SceneIndex++;

            Doc.CurrentTimeline = SceneIndex;
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            double SFX_Offset = ReturnSFXAlignmentOffset(config.PathToCFGs + "\\" + dialogueKey + ".cfg", Alignment);
            int StartingPosition = 0;

            foreach (Frame FrameToConsider in CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXT")[0]].KeyFrames)
            {
                if (FrameToConsider.Name == dialogueKey) { StartingPosition = FrameToConsider.StartFrame; }
            }

            Doc.PlaceSFX(SFX + ".wav", (int)(StartingPosition + (SFX_Offset * Doc.FrameRate)));

        }
    }

    static void PlaceIntroTypewriter(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var typewriterData = deserializedJson.Typewriter!;

        // Kid named D drive
        FontCollection fonts = new FontCollection();
        string fontsFolder = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Microsoft\\Windows\\Fonts\\";
        string fontPath = Path.Combine(fontsFolder, "suburga-2-semi-condensed-regular.otf");
        fonts.Add(fontPath);

        FontFamily family = fonts.Families.First();
        Font font = family.CreateFont(80f, FontStyle.Regular);

        TextOptions textOptions = new TextOptions(font);

        string Time = typewriterData.Time!;
        string Location = typewriterData.Location!;

        int FRAMES_BETWEEN_LETTERS = 3;
        int FRAMES_BETWEEN_WORDS = 6;
        int LETTER_SPACING = 2;
        int BOUNDING_CUSHION = 5;

        string TYPEWRITER_SFX = "AUDIO/SFX/sfx-typewriter.wav";

        CsXFL.Rectangle TimeBounding = TextMeasurer.MeasureSize(Time, textOptions);
        CsXFL.Rectangle LocationBounding = TextMeasurer.MeasureSize(Location, textOptions);

        TimeBounding.Left = ((Doc.Width - TimeBounding.Right) / 2) - BOUNDING_CUSHION;
        TimeBounding.Right += (TimeBounding.Left + (Time.Length * LETTER_SPACING)) + BOUNDING_CUSHION;
        TimeBounding.Top = 560 * 2;
        TimeBounding.Bottom = 620 * 2;

        LocationBounding.Left = ((Doc.Width - LocationBounding.Right) / 2) - BOUNDING_CUSHION;
        LocationBounding.Right += (LocationBounding.Left + (Location.Length * LETTER_SPACING)) + BOUNDING_CUSHION;
        LocationBounding.Top = 620 * 2;
        LocationBounding.Bottom = 670 * 2;

        Doc.AddNewScene("Typewriter Intro");

        Doc.CurrentTimeline = (Doc.Timelines.Count - 1);
        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

        CreateLayerIfDoesntExist(Doc, "TEXTBOX");
        CreateLayerIfDoesntExist(Doc, "TEXT1");
        CreateLayerIfDoesntExist(Doc, "TEXT2");
        CreateLayerIfDoesntExist(Doc, "SFX");
        CreateLayerIfDoesntExist(Doc, "BACKGROUNDS");

        int TEXTBOX_LAYER_INDEX = CurrentTimeline.FindLayerIndex("TEXTBOX")[0];
        int TEXT1_LAYER_INDEX = CurrentTimeline.FindLayerIndex("TEXT1")[0];
        int TEXT2_LAYER_INDEX = CurrentTimeline.FindLayerIndex("TEXT2")[0];
        int SFX_LAYER_INDEX = CurrentTimeline.FindLayerIndex("SFX")[0];

        if (CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXTBOX")[0]].GetFrame(0).Elements.Count == 0)
        {
            Doc.Library.AddItemToDocument("OTHER ASSETS/Textbox", CurrentTimeline.Layers[TEXTBOX_LAYER_INDEX].GetFrame(0), 0, 0);
            SymbolInstance TextboxElement = (CurrentTimeline.Layers[TEXTBOX_LAYER_INDEX].GetFrame(0).Elements[0] as SymbolInstance)!;
            TextboxElement.Loop = "single frame";
            TextboxElement.FirstFrame = 1;
            TextboxElement.ScaleX = 1.34164876055;
            TextboxElement.ScaleY = 1.34152669671;
            TextboxElement.Matrix.Tx = -7.3;
            TextboxElement.Matrix.Ty = -6.9;
        }

        int CurrentFrame = 0;

        for (var OperatingCharacter = 1; OperatingCharacter <= Time.Length; OperatingCharacter++)
        {
            string CurrentCharacter = Time.Substring(0, OperatingCharacter);
            int FrameOp = CurrentCharacter.EndsWith(" ") ? FRAMES_BETWEEN_WORDS : FRAMES_BETWEEN_LETTERS;
            CurrentTimeline.InsertFrames(CurrentCharacter.EndsWith(" ") ? FRAMES_BETWEEN_WORDS : FRAMES_BETWEEN_LETTERS, true, CurrentFrame);
            CurrentTimeline.Layers[TEXT1_LAYER_INDEX].InsertBlankKeyframe(CurrentFrame);
            CurrentTimeline.Layers[SFX_LAYER_INDEX].InsertBlankKeyframe(CurrentFrame);
            CurrentFrame += FrameOp;

            Text TimeText = CurrentTimeline.Layers[TEXT1_LAYER_INDEX].GetFrame(CurrentFrame).AddNewText(TimeBounding, CurrentCharacter);
            TypewriterFormat(Doc, TimeText, LETTER_SPACING);

            if (!CurrentCharacter.EndsWith(" "))
            {
                Doc.Library.AddItemToDocument(TYPEWRITER_SFX, CurrentTimeline.Layers[SFX_LAYER_INDEX].GetFrame(CurrentFrame), 0, 0);
                CurrentTimeline.Layers[SFX_LAYER_INDEX].GetFrame(CurrentFrame).SoundSync = "stream";
            }
        }

        CurrentTimeline.InsertFrames(12, true, CurrentFrame);
        CurrentFrame += 12;

        for (var OperatingCharacter = 1; OperatingCharacter <= Location.Length; OperatingCharacter++)
        {
            string CurrentCharacter = Location.Substring(0, OperatingCharacter);
            int FrameOp = CurrentCharacter.EndsWith(" ") ? (FRAMES_BETWEEN_WORDS) : (FRAMES_BETWEEN_LETTERS);
            CurrentTimeline.InsertFrames(CurrentCharacter.EndsWith(" ") ? FRAMES_BETWEEN_WORDS : FRAMES_BETWEEN_LETTERS, true, CurrentFrame);
            CurrentTimeline.Layers[TEXT2_LAYER_INDEX].InsertBlankKeyframe(CurrentFrame);
            CurrentTimeline.Layers[SFX_LAYER_INDEX].InsertBlankKeyframe(CurrentFrame);
            CurrentFrame += FrameOp;

            Text LocationText = CurrentTimeline.Layers[TEXT2_LAYER_INDEX].GetFrame(CurrentFrame).AddNewText(LocationBounding, CurrentCharacter);
            TypewriterFormat(Doc, LocationText, LETTER_SPACING);

            if (!CurrentCharacter.EndsWith(" "))
            {
                Doc.Library.AddItemToDocument(TYPEWRITER_SFX, CurrentTimeline.Layers[SFX_LAYER_INDEX].GetFrame(CurrentFrame), 0, 0);
                CurrentTimeline.Layers[SFX_LAYER_INDEX].GetFrame(CurrentFrame).SoundSync = "stream";
            }
        }

        CurrentTimeline.InsertFrames(45, true, CurrentFrame);
        CurrentTimeline.ReorderLayer(TEXTBOX_LAYER_INDEX, CurrentTimeline.FindLayerIndex("SFX")[0], true);
        CurrentTimeline.DeleteLayer(CurrentTimeline.FindLayerIndex("Layer_1")[0]);
    }

    static void SceneFadeInOut(this Document Doc)
    {
        string FadePath = "OTHER ASSETS/Standard_Fade";
        for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
        {
            Timeline CurrentTimeline = Doc.Timelines[SceneIndex];
            Doc.CurrentTimeline = SceneIndex;
            CreateLayerIfDoesntExist(Doc, "FADE");
            Layer CurrentLayer = CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("FADE")[0]];

            // Fade scene in
            if (SceneIndex == 0)
            {
                Doc.Library.AddItemToDocument(FadePath, CurrentLayer.GetFrame(0), Doc.Width/2, Doc.Height/2);
                SymbolInstance FadeInstance = (CurrentLayer.GetFrame(0).Elements[0] as SymbolInstance)!;
                FadeInstance.Loop = "play once reverse";
                FadeInstance.FirstFrame = 20;

                //Hardcode Andy
                CurrentTimeline.ReorderLayer(CurrentTimeline.FindLayerIndex("FADE")[0], CurrentTimeline.FindLayerIndex("TEXTBOX")[0], false);
            }

            // Fade scene out
            if (SceneIndex == Doc.Timelines.Count - 1)
            {
                int OperatingFrameIndex = CurrentLayer.GetFrameCount() - 1;
                CurrentLayer.ConvertToKeyframes(OperatingFrameIndex);
                Doc.Library.AddItemToDocument(FadePath, CurrentLayer.GetFrame(OperatingFrameIndex), Doc.Width / 2, Doc.Height / 2);
                SymbolInstance FadeInstance = (CurrentLayer.GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance)!;
                FadeInstance.Loop = "loop";
                FadeInstance.FirstFrame = 0;
                CurrentTimeline.InsertFrames(20, true, OperatingFrameIndex);
                CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXT")[0]].ConvertToKeyframes(OperatingFrameIndex);
                CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXT")[0]].GetFrame(OperatingFrameIndex).ClearElements();
                CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXTBOX")[0]].ConvertToKeyframes(OperatingFrameIndex);
                CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("TEXTBOX")[0]].GetFrame(OperatingFrameIndex).ClearElements();
            }
        }
    }

    // <!> Person in the future who came here to fix the pointless Defense => Other Character fade:
    // The problem is not going from Defense => Other Character, because that is intended behavior, but when
    // the visible character is a leftover from when the Defense appeared. To illustrate:
    //
    // Trucy - Apollo - Applejack - Apollo - Applejack - Trucy
    //
    // When we go from Trucy to Apollo, we fade. When we go from Apollo to Applejack we fade. But because
    // Apollo is nested between two Applejacks, a fade will happen when it technically shouldn't. To properly
    // fix this, we'd need to look behind and in front of us within a certain range to fix this and I cannot
    // be bothered to do this right now.
    //
    static void JamMaskFades(this Document Doc, string sceneData)
    {
        int JAM_FADE_DURATION = 12;
        string JAM_FADE_PATH = "OTHER ASSETS/Jam_Fade";

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(sceneData)!;
        var dialogueLines = deserializedJson.Dialogue!;

        var keys = dialogueLines.Keys;
        string? previousCharacter = null;
        string? nextCharacter = null;

        for (int i = 0; i < keys.Count; i++)
        {
            var dialogueKey = keys.ElementAt(i);
            var dialogueLine = dialogueLines[dialogueKey];
            string LineID = dialogueKey;
            string Character = dialogueLine.CharacterName!;
            int Id = int.Parse(dialogueKey.Substring(3, 3));

            if (i + 1 < keys.Count)
            {
                nextCharacter = dialogueLines[keys.ElementAt(i + 1)].CharacterName;
            }

            // Check if the previous character is the same as the current character or if the next character is equal to SingletonConfig.Instance.Defense
            if ((previousCharacter == Character) || Character == SingletonConfig.Instance.Defense && (previousCharacter == null || previousCharacter != SingletonConfig.Instance.Defense))
            {
                previousCharacter = Character;
                continue;
            }

            previousCharacter = Character;

            Doc.CurrentTimeline = (int)Math.Ceiling((double)Id / (double)SingletonConfig.Instance.ChunkSize) - 1;
            Timeline CurrentTimeline = Doc.Timelines[Doc.CurrentTimeline];
            Layer JamLayer = CurrentTimeline.Layers[Doc.Timelines[Doc.CurrentTimeline].FindLayerIndex("JAM_MASK")[0]];

            // Don't fade if it's the same character
            if (Id % SingletonConfig.Instance.ChunkSize == 1)
            {
                // First fade within a chunk
                CurrentTimeline.CurrentFrame = 0;
                JamLayer.ConvertToKeyframes(CurrentTimeline.CurrentFrame);
                Doc.Library.AddItemToDocument(JAM_FADE_PATH, JamLayer.GetFrame(CurrentTimeline.CurrentFrame), Doc.Width / 2, Doc.Height / 2);
                CurrentTimeline.InsertFrames(JAM_FADE_DURATION / 2, true, CurrentTimeline.CurrentFrame);
                SymbolInstance JamInstance = (JamLayer.GetFrame(CurrentTimeline.CurrentFrame).Elements[0] as SymbolInstance)!;
                JamInstance.FirstFrame = (JAM_FADE_DURATION / 2) + 1;
                JamLayer.ConvertToKeyframes((JAM_FADE_DURATION / 2) - 1);
                JamLayer.GetFrame((JAM_FADE_DURATION / 2) - 1).ClearElements();
            } else if (Id % SingletonConfig.Instance.ChunkSize == 0)
            {
                // Last fade within a chunk
                CurrentTimeline.CurrentFrame = JamLayer.GetFrameCount() - 1;
                JamLayer.ConvertToKeyframes(CurrentTimeline.CurrentFrame);
                Doc.Library.AddItemToDocument(JAM_FADE_PATH, JamLayer.GetFrame(CurrentTimeline.CurrentFrame), Doc.Width / 2, Doc.Height / 2);
                CurrentTimeline.InsertFrames(JAM_FADE_DURATION / 2, true, CurrentTimeline.CurrentFrame);
            } else
            {
                // Regular chunk
                goToVoiceLine(Doc, LineID);
                CurrentTimeline.InsertFrames((JAM_FADE_DURATION / 2), true, CurrentTimeline.CurrentFrame - 1);
                JamLayer.ConvertToKeyframes(CurrentTimeline.CurrentFrame);
                Doc.Library.AddItemToDocument(JAM_FADE_PATH, JamLayer.GetFrame(CurrentTimeline.CurrentFrame), Doc.Width / 2, Doc.Height / 2);
                goToVoiceLine(Doc, LineID);
                CurrentTimeline.InsertFrames((JAM_FADE_DURATION / 2), true, CurrentTimeline.CurrentFrame);
                JamLayer.ConvertToKeyframes(CurrentTimeline.CurrentFrame + (JAM_FADE_DURATION / 2));
                JamLayer.GetFrame(CurrentTimeline.CurrentFrame + (JAM_FADE_DURATION / 2)).ClearElements();
            }
        }
    }

    static void PlaceDesks(this Document doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var characterToDeskMap = new Dictionary<string, SymbolConfig?>
        {
            {config.Defense!, config.DefenseDesk },
            {config.Prosecutor!, config.ProsecutorDesk },
            {config.Judge!,config.JudgeDesk },
            {config.Cocouncil!, null }
        };

        foreach (string witness in SingletonConfig.Instance.Witnesses!)
        {
            characterToDeskMap.Add(witness, SingletonConfig.Instance.WitnessDesk);
        }

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var dialogueLines = deserializedJson.Dialogue;

        foreach (var dialogueKey in dialogueLines!.Keys)
        {
            var dialogueLine = dialogueLines[dialogueKey];
            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));

            if ((LineIndex % SingletonConfig.Instance.ChunkSize == 1) || LineIndex == 1)
            {
                doc.CurrentTimeline = LineIndex / SingletonConfig.Instance.ChunkSize;
                doc.Timelines[doc.CurrentTimeline].CurrentFrame = 0;
                doc.CreateLayerIfDoesntExist("DESKS");
            }
            var currentTimeline = doc.Timelines[doc.CurrentTimeline];
            var currentLayer = currentTimeline.Layers[currentTimeline.FindLayerIndex("DESKS")[0]];

            // End of file edge case
            if (currentTimeline.CurrentFrame == currentLayer.GetFrameCount()) { continue; }

            if (currentTimeline.CurrentFrame != 0)
            {
                currentLayer.InsertBlankKeyframe(currentTimeline.CurrentFrame);
                currentLayer.GetFrame(currentTimeline.CurrentFrame).ClearElements();
            }

            string character = dialogueLine.CharacterName!;
            SymbolConfig? desk = characterToDeskMap[character];
            if (desk != null)
            {
                doc.Library.AddItemToDocument(desk.LibraryPath, currentLayer.GetFrame(currentTimeline.CurrentFrame), desk.Tx, desk.Ty);
            }

            currentTimeline.CurrentFrame += SingletonConfig.Instance.DefaultFrameDuration;
        }
    }

    static void PlaceInvestigationBG(this Document Doc)
    {
        string PATH_TO_DEFAULT_BACKGROUND = "BACKGROUNDS/Background";

        for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
        {
            Doc.CurrentTimeline = SceneIndex;
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);
            CreateLayerIfDoesntExist(Doc, "BACKGROUNDS");
            Doc.Library.AddItemToDocument(PATH_TO_DEFAULT_BACKGROUND, CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("BACKGROUNDS")[0]].GetFrame(0), 1280, 720);
        }
    }

    // <!> Fucked up
    static void PlaceCourtBGs(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var characterToBgMap = new Dictionary<string, SymbolConfig>
        {
            {config.Defense!, config.DefenseBackground },
            {config.Prosecutor!, config.ProsecutorBackground },
            {config.Judge!, config.JudgeBackground },
            {config.Cocouncil!, config.CocouncilBackground }
        };

        foreach (string witness in SingletonConfig.Instance.Witnesses!)
        {
            characterToBgMap.Add(witness, SingletonConfig.Instance.WitnessBackground);
        }

        var deserializedJson = JsonSerializer.Deserialize<SceneData>(SceneData)!;
        var dialogueLines = deserializedJson.Dialogue;

        foreach (var dialogueKey in dialogueLines!.Keys)
        {
            var dialogueLine = dialogueLines[dialogueKey];
            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));

            if ((LineIndex % SingletonConfig.Instance.ChunkSize == 1) || LineIndex == 1)
            {
                Doc.CurrentTimeline = LineIndex / SingletonConfig.Instance.ChunkSize;

                // Skip non-scenes like Typewriters
                if (!Doc.Timelines[Doc.CurrentTimeline].Name.Contains("Scene")) { Doc.CurrentTimeline = (LineIndex / SingletonConfig.Instance.ChunkSize) + 1; } ;

                Doc.Timelines[Doc.CurrentTimeline].CurrentFrame = 0;
                Doc.CreateLayerIfDoesntExist("BACKGROUNDS");
            }

            var currentTimeline = Doc.Timelines[Doc.CurrentTimeline];
            var currentLayer = currentTimeline.Layers[currentTimeline.FindLayerIndex("BACKGROUNDS")[0]];

            // End of file edge case
            if (currentTimeline.CurrentFrame == currentLayer.GetFrameCount()) { continue; }

            if (currentTimeline.CurrentFrame != 0)
            {
                currentLayer.InsertBlankKeyframe(currentTimeline.CurrentFrame);
                currentLayer.GetFrame(currentTimeline.CurrentFrame).ClearElements();
            }

            string character = dialogueLine.CharacterName!;
            SymbolConfig bg = characterToBgMap[character];
            Doc.Library.AddItemToDocument(bg.LibraryPath, currentLayer.GetFrame(currentTimeline.CurrentFrame), bg.Tx, bg.Ty);

            SymbolInstance BG = currentLayer.GetFrame(currentTimeline.CurrentFrame).Elements[0] as SymbolInstance;
            BG.Loop = "single frame";

            currentTimeline.CurrentFrame += SingletonConfig.Instance.DefaultFrameDuration;
        }
    }

    static void PlaceLabels(this Document Doc, string EPISODE_STRING, string SCENE_STRING, string MODE_STRING)
    {
        for (int SceneIndex = 1; SceneIndex < Doc.Timelines.Count; SceneIndex++)
        {
            Doc.CurrentTimeline = SceneIndex;
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            CreateLayerIfDoesntExist(Doc, "NOTES", "guide");
            int NotesLayerIndex = CurrentTimeline.FindLayerIndex("NOTES")[0];

            CsXFL.Rectangle EpisodeBounding = new(-481.1, -112.4, 481.1, 112.4);
            CsXFL.Rectangle SceneBounding = new(-744.6, -113.4, 744.6, 113.4);
            CsXFL.Rectangle ModeBounding = new(-492.1, -190.2, 492.1, 190.2);

            Text EpisodeText = CurrentTimeline.Layers[NotesLayerIndex].GetFrame(0).AddNewText(EpisodeBounding, EPISODE_STRING);
            Text SceneText = CurrentTimeline.Layers[NotesLayerIndex].GetFrame(0).AddNewText(SceneBounding, SCENE_STRING);
            Text ModeText = CurrentTimeline.Layers[NotesLayerIndex].GetFrame(0).AddNewText(ModeBounding, MODE_STRING);

            EpisodeText.SetTextAttr("alignment", "left");
            EpisodeText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            EpisodeText.SetTextAttr("size", 84);
            EpisodeText.SetTextAttr("fillColor", "#ffffff");
            EpisodeText.SetTextAttr("letterSpacing", -1);
            EpisodeText.TextType = "dynamic";
            EpisodeText.LineType = "multiline";
            EpisodeText.Name = "EpisodeText";
            EpisodeText.FontRenderingMode = "standard";
            EpisodeText.Matrix.Tx = -250;
            EpisodeText.Matrix.Ty = -150;

            SceneText.SetTextAttr("alignment", "right");
            SceneText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            SceneText.SetTextAttr("size", 84);
            SceneText.SetTextAttr("fillColor", "#ffffff");
            SceneText.SetTextAttr("letterSpacing", -1);
            SceneText.TextType = "dynamic";
            SceneText.LineType = "multiline";
            SceneText.Name = "SceneText";
            SceneText.FontRenderingMode = "standard";
            SceneText.Matrix.Tx = 1100;
            SceneText.Matrix.Ty = -150;

            ModeText.SetTextAttr("alignment", "left");
            ModeText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            ModeText.SetTextAttr("size", 144);
            ModeText.SetTextAttr("fillColor", "#ffffff");
            ModeText.SetTextAttr("letterSpacing", -1);
            ModeText.TextType = "dynamic";
            ModeText.LineType = "multiline";
            ModeText.Name = "ModeText";
            ModeText.FontRenderingMode = "standard";
            ModeText.Matrix.Tx = 1650;
            ModeText.Matrix.Ty = 1500;

            CurrentTimeline.ReorderLayer(NotesLayerIndex, 0, true);
        }
    }

    static void GenerateScene(string PathToOperatingDocument, string PathToSceneData, string PathToCFGs, string PathToLines)
    {
        var deserializedJson = JsonSerializer.Deserialize<SceneData>(File.ReadAllText(PathToSceneData))!;
        var ReadDialogue = deserializedJson.Dialogue!;
        var ReadSFX = deserializedJson.SFX!;
        var ReadTypewriter = deserializedJson.Typewriter!;
        var ReadDataLabel = deserializedJson.DataLabels!;

        // Stupid quick fix
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        ReadDataLabel.ModeText = textInfo.ToTitleCase(ReadDataLabel.ModeText.ToLower());

        Trace.AutoFlush = true;
        Stopwatch stpw = new Stopwatch();

        stpw.Start();
        SingletonConfig.SetConfiguration();
        SingletonConfig config = SingletonConfig.Instance;

        // Data injection
        config.PathToOperatingDocument = PathToOperatingDocument;
        config.PathToSceneData = PathToSceneData;
        config.PathToCFGs = PathToCFGs;
        config.PathToLines = PathToLines;

        // THIS IS WHERE WE SET THE MODE
        config.ViewMode = ReadDataLabel.ModeText;

        // Extract the protagonist via arbitrary rules
        // <!> This code snippet may need heavy tending to as the series progresses.
        var characterSet = new HashSet<string>();

        foreach (var dialogueKey in ReadDialogue.Keys)
        {
            characterSet.Add(ReadDialogue[dialogueKey].CharacterName!);
        }

        if (characterSet.Contains("Apollo"))
        {
            config.Defense = "Apollo";
        }
        else if (characterSet.Contains("Sonata"))
        {
            config.Defense = "Sonata";
        }

        if (characterSet.Contains("Celestia"))
        {
            config.Judge = "Celestia";
            config.Prosecutor = "Trixie";
            config.Cocouncil = "";
        }
        else if (characterSet.Contains("Judge"))
        {
            config.Judge = "Judge";
            config.Prosecutor = "Luna";
            config.Cocouncil = "Phoenix";
        }

        foreach (var character in characterSet)
        {
            if (character != config.Defense && character != config.Prosecutor && character != config.Judge && character != config.Cocouncil)
            {
                config.Witnesses.Add(character);
            }
        }

        // After careful deliberation and realizing how short I am on time, I am not refactoring all this shit to take in
        // the correct class from just one deserialized JSON here. But it is possible now!

        Trace.WriteLine("[!] Now working " + ReadDataLabel.EpisodeText + " " + ReadDataLabel.SceneText);

        string json = File.ReadAllText(config.PathToSceneData!);
        Document Doc = new(config.PathToOperatingDocument!);

        InputValidation(Doc, ReadDataLabel.ModeText);
        stpw.Stop();
        Trace.WriteLine("Setup took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // "More human readable"
        MeasureAndTrace(doc => doc.ImportRigs(json, config.ViewMode), Doc, "Importing Rigs");
        MeasureAndTrace(doc => PlaceText(doc, json), Doc, "Text");
        if (!config.SkipRigs) { MeasureAndTrace(doc => PlaceRigs(Doc, json, ReadDataLabel.ModeText), Doc, "Rigs"); };

        if (config.ViewMode == "Investigation")
        {
            MeasureAndTrace(doc => PlaceInvestigationBG(Doc), Doc, "Investigation Backgrounds");
        }
        else if (config.ViewMode == "Courtroom")
        {
            MeasureAndTrace(doc => PlaceCourtBGs(Doc, json), Doc, "Courtroom Backgrounds");
            MeasureAndTrace(doc => PlaceDesks(Doc, json), Doc, "Placing Desks");
        }
        else if (config.ViewMode == "Logic Chess")
        {
            MeasureAndTrace(doc => PlaceCourtBGs(Doc, json), Doc, "Courtroom Backgrounds");
            // <!> May want to nest some additional patching logic in here for forcing the LC scenes to work as Courtroom scenes
            // i.e. binding the first appearing non-config.Defense character to config.Prosecution. Assuming they're the LC opponent.
        }

        MeasureAndTrace(doc => doc.InsertLinesChunked(config.PathToLines!), Doc, "Inserting Chunked Lines");

        // Only ignore the defense for lipsyncing if we're investigation
        if (config.ViewMode == "Investigation")
        {
            string[] IgnoreLipsync = new string[] { config.Defense!.ToUpper() };
            MeasureAndTrace(doc => Doc.LipsyncChunkedDocument(config.PathToCFGs!, IgnoreLipsync), Doc, "Lipsyncing");
        }
        else 
        {
            string[] IgnoreLipsync = new string[] { "GALLERY PONY 1", "GALLERY PONY 2", "GALLERY PONY 3", "GALLERY PONY 4", "GALLERY PONY 5", "GALLERY PONY 6", "GALLERY PONY 7", "GALLERY PONY 8", "GALLERY PONY 9" };
            MeasureAndTrace(doc => Doc.LipsyncChunkedDocument(config.PathToCFGs!, IgnoreLipsync), Doc, "Lipsyncing"); 
        }

        string[] LayerOrder = new string[] { "FLASH", "INTERJECTION", "FADE", "GAVEL", "TEXT", "TEXTBOX", "EVIDENCE", "DESKS", "JAM_MASK", "BACKGROUNDS" };
        MeasureAndTrace(doc => OrganizeLayerStructure(Doc, LayerOrder), Doc, "Organizing Layers");

        if (config.ViewMode == "Investiga   tion") { MeasureAndTrace(doc => JamMaskFades(Doc, json), Doc, "Jam Fading"); }

        MeasureAndTrace(doc => PlaceIntroTypewriter(Doc, json), Doc, "Typewriter Intro");
        Doc.ReorderScene(Doc.Timelines.Count - 1, 0, true);

        MeasureAndTrace(doc => ParseSFX(Doc, json), Doc, "SFX");
        MeasureAndTrace(doc => Doc.AutomaticBlinking(3), Doc, "Blinking");
        MeasureAndTrace(doc => SceneFadeInOut(Doc), Doc, "Scene Fading");
        MeasureAndTrace(doc => PlaceLabels(Doc, ReadDataLabel.EpisodeText!, ReadDataLabel.SceneText!, ReadDataLabel.ModeText!.ToUpper()), Doc, "Labels");

        // <!> Parent SFX track. Why is this fucking up now?
        //for (int i = 0; i < Doc.Timelines.Count; i++)
        //{
        //    if (!Doc.GetTimeline(i).Name.Contains("Scene")) continue;
        //    Doc.CurrentTimeline = i;
        //    Timeline CurrentTimeline = Doc.Timelines[i];
        //    CurrentTimeline.ReorderLayer(CurrentTimeline.FindLayerIndex("SFX")[0], CurrentTimeline.FindLayerIndex("AUDIO")[0], false);
        //    CurrentTimeline.Layers[CurrentTimeline.FindLayerIndex("SFX")[0]].ParentLayerIndex = CurrentTimeline.FindLayerIndex("AUDIO")[0];
        //}

        // User starts at the start.
        Doc.CurrentTimeline = 1;

        // Doc Saving
        stpw.Start();
        Doc.Save();
        stpw.Stop();
        Trace.WriteLine("Document Saving took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();
        Trace.Close();
    }

    // <!> Allow skipping ARGs EVENTUALLY
    static void Main(string[] args)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
        string RootOperatingFolder = "";
        bool displayHelp = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "--help" || arg == "-h" || arg == "-?")
            {
                displayHelp = true;
            }
            else if (i == 0)
            {
                RootOperatingFolder = arg;
            }
        }

        // <!> Technically should run within the same directory. Keep this for testing purposes until final build.
        if (displayHelp)
        {
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("    EpisodeGenerator.exe \"AbsolutePathToOperatingDirectory\"");
            Console.WriteLine("Options:");
            Console.WriteLine("    --help, -h   Display this help message");
            Console.WriteLine("Description:");
            Console.WriteLine("    An implementation of the Elements of Justice scene generator created in Connor's CsXFL framework.");
            Console.WriteLine("This executable requires a specific folder setup as detailed in the ReadMe. All instructions for how");
            Console.WriteLine("to gather and organize the assets required for this executable to run will also be detailed in the ReadMe.");
            Console.WriteLine("");
            return;
        }

        string RootAssetsFolder = RootOperatingFolder + "\\assets";
        string RootSceneDataFolder = RootOperatingFolder + "\\scene_data";
        string RootCFGsFolder = RootOperatingFolder + "\\CFGs";
        string RootVoiceLinesFolder = RootOperatingFolder + "\\voice_lines";

        string[] paths = { RootOperatingFolder, RootAssetsFolder, RootSceneDataFolder, RootCFGsFolder, RootVoiceLinesFolder };

        // Check if paths exist
        foreach (var path in paths)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException($"The path '{path}' does not exist.");
            }
        }

        // Iterate over sceneData
        foreach (var file in Directory.GetFiles(RootSceneDataFolder))
        {
            string json = File.ReadAllText(file);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sceneData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options)!;

            if (sceneData.ContainsKey("DataLabels") && sceneData["DataLabels"] is JsonElement dataLabels)
            {
                string episodeText = dataLabels.GetProperty("EpisodeText").GetString()!;
                string sceneText = dataLabels.GetProperty("SceneText").GetString()!;
                string modeText = dataLabels.GetProperty("ModeText").GetString()!;

                string operatingEpisode = episodeText;
                string operatingScene = sceneText;
                string operatingMode = modeText;

                string PathToCFGs = Path.Combine(RootCFGsFolder, operatingScene);
                string PathToLines = Path.Combine(RootVoiceLinesFolder, operatingScene);

                string BaseFLA = Path.Combine(RootOperatingFolder, "Episode-Generator-Base.fla");
                string NewFLA = Path.Combine(RootOperatingFolder, operatingEpisode + " " + operatingScene + ".fla");

                if (File.Exists(NewFLA)) { File.Delete(NewFLA); }
                File.Copy(BaseFLA, NewFLA);

                // Do it.
                GenerateScene(NewFLA, file, PathToCFGs, PathToLines);
            }
        }
    }
}