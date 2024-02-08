using CsXFL;
using Jint;
using System.Diagnostics;
using System.Text.Json;
using static SceneGenerator;

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

        config.Defense = "Sonata";
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
        config.PathToOperatingDocument = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\UltimateCsXFLTest\\DOMDocument.xml";
        config.PathToSceneData = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\303_Scene_1_Scene_Generation_S1.txt";
        config.PathToCFGs = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\CFGs\\Scene 1";
        config.PathToLines = "C:\\Users\\Administrator\\Elements of Justice\\303_Autogen_FLAs\\SCENE 1";

        //Characters
        //<!> We kind of don't need an Investigation vs Courtroom profile, just throw it all into one
        config.AddCharacter("Investigation", "Fair Devotion", "RIGS/Fluttershy►/Fluttershy►PoseScaled");
        config.AddCharacter("Investigation", "Judge", "RIGS/Fluttershy►/Fluttershy►PoseScaled");

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
        //<!> We kind of don't need an Investigation vs Courtroom profile, just throw it all into one
        CharacterConfig RigConfig = config.GetCharacterConfig("Investigation");

        //<!> Can we more dynamically check for existing things by passing Doc into SymbolConfig & CharacterConfig?
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

    static void CreateLayerIfDoesntExist(this Document Doc, string LayerName)
    {
        if (Doc.Timelines[0].FindLayerIndex(LayerName).Count == 0) { Doc.Timelines[0].AddNewLayer(LayerName); }
    }

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
            string Emotion = dialogueLine.Emotion;

            int LineIndex = int.Parse(dialogueKey.Substring(3, 3));
            int SceneIndex = (int)Math.Ceiling((double)LineIndex / config.ChunkSize);

            if (Doc.Timelines.Count < SceneIndex)
            {
                Doc.AddNewScene("Scene " + SceneIndex);
            }

            Timeline CurrentTimeline = Doc.GetTimeline(SceneIndex - 1);

            CreateLayerIfDoesntExist(Doc, "TEXTBOX");
            CreateLayerIfDoesntExist(Doc, "TEXT");

            int TextboxLayerIndex = CurrentTimeline.FindLayerIndex("TEXTBOX")[0];
            int TextLayerIndex = CurrentTimeline.FindLayerIndex("TEXT")[0];

            int OperatingFrameIndex = config.DefaultFrameDuration * (LineIndex - 1);

            if ((OperatingFrameIndex + config.DefaultFrameDuration) > CurrentTimeline.Layers[TextLayerIndex].GetFrameCount())
            {
                CurrentTimeline.InsertFrames(config.DefaultFrameDuration, true, OperatingFrameIndex);
            }

            CurrentTimeline.Layers[TextLayerIndex].ConvertToKeyframes(OperatingFrameIndex);

            Doc.Library.AddItemToDocument("OTHER ASSETS/Textbox", CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(OperatingFrameIndex), 0, 0);

            SymbolInstance TextboxElement = CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(OperatingFrameIndex).Elements[0] as SymbolInstance;
            TextboxElement.Loop = "single frame";
            //Do stuff here.
            Text DialogueText = CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).AddNewText(DialogueBounding, Dialogue.Trim());
            Text SpeakerText = CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).AddNewText(SpeakerBounding, config.GetTruncatedName(Character).Trim());

            DialogueText.SetTextAttr("alignment", "left");
            DialogueText.SetTextAttr("face", "Suburga 2 Semi-condensed Regular");
            DialogueText.SetTextAttr("size", 80);
            DialogueText.SetTextAttr("fillColor", "#0xffffff");
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
            SpeakerText.SetTextAttr("fillColor", "#ffffff#");
            SpeakerText.SetTextAttr("letterSpacing", 2);
            SpeakerText.SetTextAttr("lineSpacing", 1);
            SpeakerText.TextType = "dynamic";
            SpeakerText.LineType = "multiline";
            SpeakerText.Name = "SpeakerText";
            SpeakerText.FontRenderingMode = "standard";

            if (config.GetLetterspacing(Character) != null)
            {
                SpeakerText.SetTextAttr("lineSpacing", config.GetLetterspacing(Character));
            }

            CurrentTimeline.Layers[TextLayerIndex].GetFrame(OperatingFrameIndex).Name = LineID;
        }
    }

    static void Main()
    {
        string json = @"{
           ""Dialogue"": {
               ""s1_001_apollo"": {
                   ""CharacterName"": ""Apollo"",
                   ""LineText"": ""I just feel like I’m cursed, y’know?"",
                   ""Emotion"": ""C""
               },
               ""s1_002_apollo"": {
                   ""CharacterName"": ""Apollo"",
                   ""LineText"": ""I can't shake this feeling of dread."",
                   ""Emotion"": ""D""
               },
               ""s1_003_apollo"": {
                   ""CharacterName"": ""Apollo"",
                   ""LineText"": ""It's like the weight of the world is on my shoulders."",
                   ""Emotion"": ""E""
               }
           }
        }";

        SetConfiguration();
        SingletonConfig config = SingletonConfig.Instance;
        Document Doc = new(config.PathToOperatingDocument);
        InputValidation(Doc);
        PlaceText(Doc, json);
        Doc.Save();
    }
}