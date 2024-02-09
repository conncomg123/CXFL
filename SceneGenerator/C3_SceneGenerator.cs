using CsXFL;
using Jint;
using System.Diagnostics;
using System.Text.Json;
using static SceneGenerator;

// <!> Chunking does not work, this is a CurrentTimeline issue
// <!> Undecided about keeping viewmode profiles for CharacterConfig

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
        public string ViewMode { get; set; }
        public string Defense { get; set; }
        public string Prosecutor { get; set; }
        public string Judge { get; set; }
        public string Cocouncil { get; set; }
        public Array Witnesses { get; set; }

        public string EEBias { get; set; }
        public int ChunkSize { get; set; }

        //Skip Settings
        public bool SkipRigs { get; set; }
        public bool SkipBGs { get; set; }
        public bool SkipTypewriter { get; set; }
        public bool SkipLines { get; set; }
        public bool SkipFades { get; set; }
        public bool SkipBlinks { get; set; }

        //Paths
        public string PathToOperatingDocument { get; set; }
        public string PathToSceneData { get; set; }
        public string PathToCFGs { get; set; }
        public string PathToLines { get; set; }

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

        public void AddCharacter(string configName, string simplifiedName, string libraryPath)
        {
            if (!characterConfigs.ContainsKey(configName))
            {
                characterConfigs[configName] = new CharacterConfig();
            }
            characterConfigs[configName].AddCharacter(simplifiedName, libraryPath);
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
        public List<(string SimplifiedName, string LibraryPath)> Characters { get; }

        public CharacterConfig()
        {
            Characters = new List<(string, string)>();
        }

        public void AddCharacter(string simplifiedName, string libraryPath)
        {
            Characters.Add((simplifiedName, libraryPath));
        }
    }

    public class DialogueLine
    {
        public string? CharacterName { get; set; }
        public string? LineText { get; set; }
        public string? Emotion { get; set; }
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
        config.Witnesses = "Witness 1, Witness 2, Witness 3".Split(',');

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
        config.PathToOperatingDocument = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\UltimateCsXFLTest.fla";
        config.PathToSceneData = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\303S1_output.json";
        config.PathToCFGs = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\CFGs\\Scene 1";
        config.PathToLines = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\SCENE 1";

        //Characters
        config.AddCharacter("Investigation", "Trucy", "RIGS/Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Rarity", "RIGS/RARITY►/Rarity►PoseScaled");
        config.AddCharacter("Investigation", "Equity", "RIGS/EquityArmored►/EquityArmored►PoseScaled");
        config.AddCharacter("Investigation", "Applejack", "RIGS/APPLEJACK►/APPLEJACK►PoseScaled");
        config.AddCharacter("Investigation", "Guard", "RIGS/Trucy►/Trucy►ScaledPoses");

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


    // <!> Ported bounding boxes from JSFL scene gen, bounding boxes seem to be slightly off from 3-1 to 3-3 scenes if you compare
    static void PlaceText(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        Rectangle DialogueBounding = new Rectangle(40.05 * 2, 549.5 * 2, 1212.95 * 2, 708.92 * 2);
        Rectangle SpeakerBounding = new Rectangle(20.05 * 2, 482.5 * 2, 254.4 * 2, 540.2 * 2);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(SceneData, options);

        foreach (var dialogueKey in deserializedJson["Dialogue"].Keys)
        {
            var dialogueLine = deserializedJson["Dialogue"][dialogueKey];

            string LineID = dialogueKey;
            string Character = dialogueLine.CharacterName;
            string Dialogue = dialogueLine.LineText;

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
                SymbolInstance TextboxElement = CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance;
                TextboxElement.Loop = "single frame";
                TextboxElement.ScaleX = 1.34164876055;
                TextboxElement.ScaleY = 1.34152669671;
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

            if (config.GetLetterspacing(Character) != null) { SpeakerText.SetTextAttr("lineSpacing", config.GetLetterspacing(Character)); }

            CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).Name = LineID;
        }
    }

    // <!> Need logic to extend the last placed rig when config.Defense is the character
    static void PlaceRigs(this Document Doc, string SceneData)
    {
        SingletonConfig config = SingletonConfig.Instance;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DialogueLine>>>(SceneData, options);

        foreach (var dialogueKey in deserializedJson["Dialogue"].Keys)
        {
            var dialogueLine = deserializedJson["Dialogue"][dialogueKey];
            string Character = dialogueLine.CharacterName;
            string CharacterLayerName = Character.ToUpper();
            string Emotion = dialogueLine.Emotion;

            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));
            int SceneIndex = (int)Math.Ceiling((double)LineIndex / config.ChunkSize);

            Doc.CurrentTimeline = (SceneIndex - 1);
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            if (Character == config.Defense) { continue; } //Wow

            CreateLayerIfDoesntExist(Doc, "VECTOR_CHARACTERS", "folder");
            CreateLayerIfDoesntExist(Doc, CharacterLayerName);

            int CharacterLayerIndex = CurrentTimeline.FindLayerIndex(CharacterLayerName)[0];
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];
            int OperatingFrameIndex = config.DefaultFrameDuration * ((LineIndex - 1) % config.ChunkSize);

            CurrentTimeline.Layers[CharacterLayerIndex].ConvertToKeyframes(OperatingFrameIndex);
            CurrentTimeline.Layers[CharacterLayerIndex].ConvertToKeyframes(OperatingFrameIndex + config.DefaultFrameDuration);
            CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex).ClearElements();

            bool WasRigPlaced = Doc.Library.AddItemToDocument(config.GetLibraryPathByName(Character), CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex));
            if (WasRigPlaced)
            {
                SymbolInstance CharacterRig = CurrentTimeline.Layers[CharacterLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance;
                CharacterRig.TransformationPoint.X = 0;
                CharacterRig.TransformationPoint.Y = 0;
                CharacterRig.Loop = "single frame";
                CharacterRig.FirstFrame = PoseAutomation(Doc, config.GetLibraryPathByName(Character), Emotion);
            }
            else { throw new Exception("An error occured when attempting rig placement."); }

            CurrentTimeline.Layers[CharacterLayerIndex].ParentLayerIndex = CurrentTimeline.FindLayerIndex("VECTOR_CHARACTERS")[0];
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
        SymbolItem RigSymbol = Doc.Library.Items[LibraryPath] as SymbolItem;
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
            //Hi Connor
            //CreateLayerIfDoesntExist(Doc, "Layer_1");
            int DefaultLayerIndex = CurrentTimeline.FindLayerIndex("Layer 1")[0];
            CurrentTimeline.DeleteLayer(DefaultLayerIndex);

        }
    }

    static void Main()
    {

        // <!> If you can figure out what type the deserialized JSON is, you can deserialize it here
        // and pass it in to the required functions instead of deserializing at the start of each function.

        SetConfiguration();
        SingletonConfig config = SingletonConfig.Instance;
        string json = File.ReadAllText(config.PathToSceneData);
        Document Doc = new(config.PathToOperatingDocument);
        InputValidation(Doc);
        PlaceText(Doc, json);
        if (!config.SkipRigs) { PlaceRigs(Doc, json); };

        string[] LayerOrder = new string[] { "FLASH", "INTERJECTION", "FADE", "GAVEL", "TEXT", "TEXTBOX", "EVIDENCE", "DESKS", "JAM_MASK", "BACKGROUNDS" };
        OrganizeLayerStructure(Doc, LayerOrder);

        Doc.Save();
    }
}