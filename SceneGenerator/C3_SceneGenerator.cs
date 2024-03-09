using CsXFL;
using SceneGenerator.API;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SixLabors.Fonts;
using static SceneGenerator.SceneGenerator;
namespace SceneGenerator;

// <!> Todo:
// 2. Evidence API (Soundman will do this EVENTUALLY?)
// 3. Fade API (Soundman will do this)
// 4. Jam Fade API (Soundman will do this)
// ∞. Do a full test

static class SceneGenerator
{
    public sealed class SingletonConfig
    {
        private static readonly SingletonConfig instance = new SingletonConfig();

        private SingletonConfig() { }

        public static SingletonConfig Instance
        {
            get { return instance; }
        }

        //Core Settings
        public int DefaultFrameDuration { get; set; }
        public string? ViewMode { get; set; }
        public string? Defense { get; set; }
        public string? Prosecutor { get; set; }
        public string? Judge { get; set; }
        public string? Cocouncil { get; set; }
        public List<string>? Witnesses { get; set; }

        public string? EEBias { get; set; }
        public int ChunkSize { get; set; }

        //Skip Settings
        public bool SkipRigs { get; set; }
        public bool SkipBGs { get; set; }
        public bool SkipTypewriter { get; set; }
        public bool SkipLines { get; set; }
        public bool SkipFades { get; set; }
        public bool SkipBlinks { get; set; }

        //Paths
        public string? PathToOperatingDocument { get; set; }
        public string? PathToSceneData { get; set; }
        public string? PathToCFGs { get; set; }
        public string? PathToLines { get; set; }

        //Backgrounds
        public SymbolConfig DefenseBackground { get; } = new SymbolConfig("BACKGROUNDS/Full-Courtroom", 7254, -738);
        public SymbolConfig ProsecutorBackground { get; } = new SymbolConfig("BACKGROUNDS/Full-Courtroom", -4694, -738);
        public SymbolConfig JudgeBackground { get; } = new SymbolConfig("BACKGROUNDS/JudgeBackground", 1280, 720);
        public SymbolConfig CocouncilBackground { get; } = new SymbolConfig("BACKGROUNDS/CocouncilBG", 1280, 720);
        public SymbolConfig WitnessBackground { get; } = new SymbolConfig("BACKGROUNDS/Full-Courtroom", 1280, 720);

        //Desks
        public SymbolConfig DefenseDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/desk_Defense", 1180, 1290);
        public SymbolConfig ProsecutorDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/desk_Prosecution", 1390, 1290);
        public SymbolConfig JudgeDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/desk_Judge", 1280, 720);
        public SymbolConfig WitnessDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/desk_Witness", 1280, 720);

        //Characters
        private Dictionary<string, CharacterConfig> characterConfigs = new Dictionary<string, CharacterConfig>();

        //Nameswaps
        private Dictionary<string, NameswapConfig> nameswapConfigs = new Dictionary<string, NameswapConfig>();

        //Letterspacing
        private Dictionary<string, LetterspacingConfig> letterspacingConfigs = new Dictionary<string, LetterspacingConfig>();

        public void AddCharacter(string configName, string simplifiedName, string libraryPath, string? pathToRigFile = null, string? libraryPathInRigFile = null)
        {
            if (!characterConfigs.ContainsKey(configName))
            {
                characterConfigs[configName] = new CharacterConfig();
            }
            characterConfigs[configName].AddCharacter(simplifiedName, libraryPath, pathToRigFile, libraryPathInRigFile);
        }

        public CharacterConfig GetCharacterConfig(string configName)
        {
            if (characterConfigs.ContainsKey(configName))
            {
                return characterConfigs[configName];
            }
            else
            {
                throw new Exception($"Character configuration {configName} did not return anything. Check your configuration.");
            }
        }

        public string GetLibraryPathByName(string characterName)
        {
            foreach (var config in characterConfigs.Values)
            {
                foreach (var character in config.Characters)
                {
                    if (character.SimplifiedName == characterName)
                    {
                        return character.LibraryPath;
                    }
                }
            }
            SingletonConfig readConfig = SingletonConfig.Instance;
            if (characterName != readConfig.Defense)
            {
                throw new Exception($"Library path for {characterName}'s rig was not located.");
            }
            else
            {
                return "DefenseRig";
            }

        }

        public void AddNameswap(string originalName, string truncatedName)
        {
            nameswapConfigs[originalName] = new NameswapConfig(originalName, truncatedName);
        }

        public string GetTruncatedName(string originalName)
        {
            if (nameswapConfigs.ContainsKey(originalName))
            {
                return nameswapConfigs[originalName].TruncatedName;
            }
            else
            {
                return originalName;
            }
        }

        public void AddLetterspacing(string referenceName, int correctedSpacing)
        {
            letterspacingConfigs[referenceName] = new LetterspacingConfig(referenceName, correctedSpacing);
        }

        public int GetLetterspacing(string referenceName)
        {
            if (letterspacingConfigs.ContainsKey(referenceName))
            {
                return letterspacingConfigs[referenceName].CorrectedSpacing;
            }
            else
            {
                return 2;
            }
        }
    }
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
        public List<(string SimplifiedName, string LibraryPath, string? pathToRigFile, string? libraryPathInRigFile)> Characters { get; }

        public CharacterConfig()
        {
            Characters = new List<(string, string, string?, string?)>();
        }

        public void AddCharacter(string simplifiedName, string libraryPath, string? pathToRigFile = null, string? libraryPathInRigFile = null)
        {
            Characters.Add((simplifiedName, libraryPath, pathToRigFile, libraryPathInRigFile));
        }
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

    static void SetConfiguration()
    {
        SingletonConfig config = SingletonConfig.Instance;

        //Core Settings
        config.ViewMode = "InvestigationMode";
        config.DefaultFrameDuration = 24;

        config.Defense = "Apollo";
        config.Prosecutor = "Luna";
        config.Judge = "Judge";
        config.Cocouncil = "Phoenix";
        config.Witnesses = new List<string> { "Witness 1, Witness 2, Witness 3" };

        config.EEBias = "";
        config.ChunkSize = 70;

        //Skip Settings
        config.SkipRigs = false;
        config.SkipBGs = false;
        config.SkipTypewriter = false;
        config.SkipLines = false;
        config.SkipFades = false;
        config.SkipBlinks = false;

        //Paths
        config.PathToOperatingDocument = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\303_S1.fla";
        config.PathToSceneData = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\303S1_output.json";
        config.PathToCFGs = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\CFGs\\Scene 1";
        config.PathToLines = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\SCENE 1";

        //Characters
        config.AddCharacter("Investigation", "Trucy", "RIGS/Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Rarity", "RIGS/RARITY►/Rarity►PoseScaled");
        config.AddCharacter("Investigation", "Equity", "RIGS/EquityArmored►/EquityArmored►PoseScaled");
        config.AddCharacter("Investigation", "Applejack", "RIGS/APPLEJACK►/APPLEJACK►PoseScaled");
        config.AddCharacter("Investigation", "Guard", "RIGS/Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Suri", "RIGS/Trucy►/Suri►ScaledPoses", "C:\\Stuff\\SceneGenTest\\RIGS\\INDEV_MV_Suri.fla", "SURI►");

        //Nameswaps
        config.AddNameswap("Turning Page", "Turning");
        config.AddNameswap("Sweetie Belle", "Sweetie");
        config.AddNameswap("Diamond Tiara", "Diamond");

        //Letter Spacing
        config.AddLetterspacing("Royal Order", 1);
    }

    static void InputValidation(this Document Doc)
    {
        SingletonConfig config = SingletonConfig.Instance;
        CharacterConfig RigConfig = config.GetCharacterConfig("Investigation");

        foreach (var character in RigConfig.Characters)
        {
            string simplifiedName = character.SimplifiedName;
            string libraryPath = character.LibraryPath;

            if (!Doc.Library.ItemExists(character.LibraryPath)) { Debug.Assert(true, $"{character}'s rig ({character.LibraryPath}) does not exist."); }
        }

        //Paths Exist
        if (!File.Exists(config.PathToSceneData)) throw new Exception("Scene data file does not exist.");

        if (!config.SkipLines)
        {
            if (!Directory.Exists(config.PathToCFGs)) throw new FileNotFoundException("CFG directory does not exist.");
            if (!Directory.Exists(config.PathToLines)) throw new FileNotFoundException("Line directory does not exist.");
        }

        //Skip logic
        if (!config.SkipLines && config.SkipRigs) throw new Exception("Cannot generate when skipping rigs but not line placement.");
    }

    static void CreateLayerIfDoesntExist(this Document Doc, string LayerName, string LayerType = "normal")
    {
        if (Doc.Timelines[Doc.CurrentTimeline].FindLayerIndex(LayerName).Count == 0) { Doc.Timelines[Doc.CurrentTimeline].AddNewLayer(LayerName, LayerType); }
    }

    static void ImportRigs(this Document doc)
    {
        SingletonConfig config = SingletonConfig.Instance;
        CharacterConfig RigConfig = config.GetCharacterConfig("Investigation");
        doc.Library.NewFolder("RIGS");
        foreach (var character in RigConfig.Characters)
        {
            string libraryPath = character.LibraryPath;
            string? pathToRigFile = character.pathToRigFile;
            string? libraryPathInRigFile = character.libraryPathInRigFile;

            if (pathToRigFile != null && libraryPathInRigFile != null && !doc.Library.ItemExists(libraryPath))
            {
                // assuming path to rig is a FOLDER
                doc.ImportFolderFromOtherDocument(pathToRigFile, libraryPathInRigFile);
                Item imported = doc.Library.Items[libraryPathInRigFile];
                doc.Library.MoveToFolder("RIGS", imported);
            }
        }
    }
    // <!> Ported bounding boxes from JSFL scene gen, bounding boxes seem to be slightly off from 3-1 to 3-3 scenes if you compare
    static void PlaceText(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        CsXFL.Rectangle DialogueBounding = new(40.05 * 2, 549.5 * 2, 1212.95 * 2, 708.92 * 2);
        CsXFL.Rectangle SpeakerBounding = new(20.05 * 2, 482.5 * 2, 254.4 * 2, 540.2 * 2);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(SceneData, options)!;

        foreach (var dialogueKey in deserializedJson["Dialogue"].Keys)
        {
            var dialogueLine = deserializedJson["Dialogue"][dialogueKey];

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
        // remove one frame from the end of each chunk
        for (int i = 0; i < Doc.Timelines.Count; i++)
        {
            Timeline CurrentTimeline = Doc.GetTimeline(i);
            int LastFrameIndex = CurrentTimeline.Layers[0].GetFrameCount() - 1;
            CurrentTimeline.RemoveFrames(1, true, LastFrameIndex);
        }
    }

    // <!> Need logic to extend the last placed rig when config.Defense is the character
    static void PlaceRigs(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(SceneData, options)!;
        string previousCharacter = "";
        var keys = deserializedJson["Dialogue"].Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            var dialogueKey = keys[i];
            var dialogueLine = deserializedJson["Dialogue"][dialogueKey];
            string Character = dialogueLine.CharacterName!;
            if (Character == config.Defense)
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

            bool WasRigPlaced = Doc.Library.AddItemToDocument(config.GetLibraryPathByName(Character), CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex));
            if (WasRigPlaced)
            {
                SymbolInstance CharacterRig = (CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance)!;
                CharacterRig.TransformationPoint.X = 0;
                CharacterRig.TransformationPoint.Y = 0;
                CharacterRig.Loop = "single frame";
                CharacterRig.FirstFrame = PoseAutomation(Doc, config.GetLibraryPathByName(Character), Emotion);
            }
            else { throw new Exception("An error occured when attempting rig placement."); }

            CurrentTimeline.Layers[CharacterLayerIndex].ParentLayerIndex = CurrentTimeline.FindLayerIndex("VECTOR_CHARACTERS")[0];
            previousCharacter = Character;
        }
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

    // <!> Want config.EE_Bias, unsure if/how to implement smoothing for transient emotions
    static int PoseAutomation(this Document Doc, string LibraryPath, string Emotion)
    {
        SymbolItem RigSymbol = (Doc.Library.Items[LibraryPath] as SymbolItem)!;
        Layer RigEE_Data = RigSymbol.Timeline.Layers[RigSymbol.Timeline.Layers.Count - 1];

        if (Emotion == "") { Emotion = "-"; }

        List<DoubleIntPair> pairs = new List<DoubleIntPair>();

        foreach (Frame ConsiderKeyframe in RigEE_Data.KeyFrames)
        {
            pairs.Add(new DoubleIntPair(LevenshteinRatio(Emotion, ConsiderKeyframe.Name), ConsiderKeyframe.StartFrame));
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
            if (CurrentTimeline.FindLayerIndex("Layer_1")[0] != null)
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

    static void ParseSFX(this Document Doc, string SFXData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, SFXLine>>>(SFXData, options)!;

        foreach (var dialogueKey in deserializedJson["SFX"].Keys)
        {
            var dialogueLine = deserializedJson["SFX"][dialogueKey];
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

    static void PlaceIntroTypewriter(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, TypewriterData>>>(SceneData, options)!;

        // Kid named D drive
        FontCollection fonts = new FontCollection();
        string fontsFolder = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Microsoft\\Windows\\Fonts\\";
        string fontPath = Path.Combine(fontsFolder, "suburga-2-semi-condensed-regular.otf");
        fonts.Add(fontPath);

        FontFamily family = fonts.Families.First();
        Font font = family.CreateFont(80f, FontStyle.Regular);

        TextOptions textOptions = new TextOptions(font);

        foreach (var typewriterIntroKey in deserializedJson["Typewriter"].Keys)
        {
            var dialogueLine = deserializedJson["Typewriter"][typewriterIntroKey];
            string Time = dialogueLine.Time!;
            string Location = dialogueLine.Location!;

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
                SymbolInstance FadeInstance = CurrentLayer.GetFrame(0).Elements[0] as SymbolInstance;
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
                SymbolInstance FadeInstance = CurrentLayer.GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance;
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

    static void JamMaskFades(this Document Doc, string sceneData)
    {
        int JAM_FADE_DURATION = 12;
        string JAM_FADE_PATH = "OTHER ASSETS/Jam_Fade";
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(sceneData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var keys = deserializedJson["Dialogue"].Keys;
        string? previousCharacter = null; // Variable to store the previous character
        string? nextCharacter = null;
        for (int i = 0; i < keys.Count; i++)
        {
            var dialogueKey = keys.ElementAt(i);
            var dialogueLine = deserializedJson["Dialogue"][dialogueKey];
            string LineID = dialogueKey;
            string Character = dialogueLine.CharacterName!;
            int Id = int.Parse(dialogueKey.Substring(3, 3));

            if (i + 1 < keys.Count)
            {
                nextCharacter = deserializedJson["Dialogue"][keys.ElementAt(i + 1)].CharacterName;
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
                SymbolInstance JamInstance = JamLayer.GetFrame(CurrentTimeline.CurrentFrame).Elements[0] as SymbolInstance;
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

    static void PlaceDesks(this Document doc, string sceneData)
    {
        var characterToDeskMap = new Dictionary<string, SymbolConfig?>
        {
            {SingletonConfig.Instance.Defense!, SingletonConfig.Instance.DefenseDesk },
            {SingletonConfig.Instance.Prosecutor!, SingletonConfig.Instance.ProsecutorDesk },
            {SingletonConfig.Instance.Judge!, SingletonConfig.Instance.JudgeDesk },
            {SingletonConfig.Instance.Cocouncil!, null }
        };
        foreach (string witness in SingletonConfig.Instance.Witnesses!)
        {
            characterToDeskMap.Add(witness, SingletonConfig.Instance.WitnessDesk);
        }
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(sceneData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        for (int i = 0; i < deserializedJson.Count; i++)
        {
            if (i % SingletonConfig.Instance.ChunkSize == 0)
            {
                doc.CurrentTimeline = i / SingletonConfig.Instance.ChunkSize;
                doc.Timelines[doc.CurrentTimeline].CurrentFrame = 0;
                doc.CreateLayerIfDoesntExist("DESKS");
            }
            var currentTimeline = doc.Timelines[doc.CurrentTimeline];
            var currentLayer = currentTimeline.Layers[currentTimeline.FindLayerIndex("DESKS")[0]];
            if (currentTimeline.CurrentFrame != 0)
            {
                currentLayer.InsertBlankKeyframe(currentTimeline.CurrentFrame);
            }
            string character = deserializedJson["Dialogue"].Values.ElementAt(i).CharacterName!;
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

    static void PlaceCourtBGs(this Document doc, string sceneData)
    {
        var characterToBgMap = new Dictionary<string, SymbolConfig>
        {
            { SingletonConfig.Instance.Defense!, SingletonConfig.Instance.DefenseBackground },
            {SingletonConfig.Instance.Prosecutor!, SingletonConfig.Instance.ProsecutorBackground },
            {SingletonConfig.Instance.Judge!, SingletonConfig.Instance.JudgeBackground },
            {SingletonConfig.Instance.Cocouncil!, SingletonConfig.Instance.CocouncilBackground }
        };
        foreach (string witness in SingletonConfig.Instance.Witnesses!)
        {
            characterToBgMap.Add(witness, SingletonConfig.Instance.WitnessBackground);
        }
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(sceneData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        for (int i = 0; i < deserializedJson.Count; i++)
        {
            if (i % SingletonConfig.Instance.ChunkSize == 0)
            {
                doc.CurrentTimeline = i / SingletonConfig.Instance.ChunkSize;
                doc.Timelines[doc.CurrentTimeline].CurrentFrame = 0;
                doc.CreateLayerIfDoesntExist("BACKGROUNDS");
            }
            var currentTimeline = doc.Timelines[doc.CurrentTimeline];
            var currentLayer = currentTimeline.Layers[currentTimeline.FindLayerIndex("BACKGROUNDS")[0]];
            if (currentTimeline.CurrentFrame != 0)
            {
                currentLayer.InsertBlankKeyframe(currentTimeline.CurrentFrame);
            }
            string character = deserializedJson["Dialogue"].Values.ElementAt(i).CharacterName!;
            SymbolConfig bg = characterToBgMap[character];
            doc.Library.AddItemToDocument(bg.LibraryPath, currentLayer.GetFrame(currentTimeline.CurrentFrame), bg.Tx, bg.Ty);
            currentTimeline.CurrentFrame += SingletonConfig.Instance.DefaultFrameDuration;
        }
    }
    static void Main()
    {
        // <!> If you can figure out what type the deserialized JSON is, you can deserialize it here
        // and pass it in to the required functions instead of deserializing at the start of each function.

        Trace.Listeners.Add(new ConsoleTraceListener());
        Trace.AutoFlush = true;
        Stopwatch stpw = new Stopwatch();

        stpw.Start();
        SetConfiguration();
        SingletonConfig config = SingletonConfig.Instance;
        string json = File.ReadAllText(config.PathToSceneData!);
        Document Doc = new(config.PathToOperatingDocument!);
        InputValidation(Doc);
        stpw.Stop();
        Trace.WriteLine("Setup took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();
        stpw.Start();

        // <!> Fix me hahahaha
        //Doc.ImportRigs();
        //stpw.Stop();
        //Trace.WriteLine("Rig Import took " + stpw.ElapsedMilliseconds + " ms.");

        // Text Placement
        stpw.Start();
        PlaceText(Doc, json);
        stpw.Stop();
        Trace.WriteLine("Text Placement took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Rig Placement
        stpw.Start();
        if (!config.SkipRigs) { PlaceRigs(Doc, json); };
        stpw.Stop();
        Trace.WriteLine("Rig Placement took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Line Insertion
        stpw.Start();
        Doc.InsertLinesChunked(config.PathToLines!);
        stpw.Stop();
        Trace.WriteLine("Line Insertion took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Automatic Lipsyncing
        stpw.Start();
        string[] IgnoreLipsync = new string[] { config.Defense!.ToUpper() };
        Doc.LipsyncChunkedDocument(config.PathToCFGs!, IgnoreLipsync);
        stpw.Stop();
        Trace.WriteLine("Lipsyncing took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Organize Layers
        stpw.Start();
        string[] LayerOrder = new string[] { "FLASH", "INTERJECTION", "FADE", "GAVEL", "TEXT", "TEXTBOX", "EVIDENCE", "DESKS", "JAM_MASK", "BACKGROUNDS" };
        OrganizeLayerStructure(Doc, LayerOrder);
        stpw.Stop();
        Trace.WriteLine("Layer Organization took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Jam Mask Fading
        stpw.Start();
        JamMaskFades(Doc, json);
        stpw.Stop();
        Trace.WriteLine("Jam Mask Fading took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Typewriter Intro
        
        PlaceIntroTypewriter(Doc, json);
        Doc.ReorderScene(Doc.Timelines.Count - 1, 0, true);
        stpw.Stop();
        Trace.WriteLine("Typewriter Automation took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // BG Placement
        stpw.Start();
        PlaceInvestigationBG(Doc);
        stpw.Stop();
        Trace.WriteLine("Background Placement took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        //SFX Automation
        stpw.Start();
        //ParseSFX(Doc, json);
        stpw.Stop();
        Trace.WriteLine("SFX Placement took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Blink Automation
        stpw.Start();
        Doc.AutomaticBlinking(3);
        stpw.Stop();
        Trace.WriteLine("Automatic Blinking took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Scene Fading
        stpw.Start();
        SceneFadeInOut(Doc);
        stpw.Stop();
        Trace.WriteLine("Scene Fading took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        // Doc Saving
        stpw.Start();
        Doc.Save();
        stpw.Stop();
        Trace.WriteLine("Document Saving took " + stpw.ElapsedMilliseconds + " ms.");
        stpw.Reset();

        Trace.Close();
    }
}