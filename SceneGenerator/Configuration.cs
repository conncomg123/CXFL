using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SceneGenerator.SceneGenerator;
namespace SceneGenerator;

public sealed class SingletonConfig
{
    public static readonly SingletonConfig instance = new SingletonConfig();

    public SingletonConfig() { }

    public static SingletonConfig Instance
    {
        get { return instance; }
    }

    // Core Settings
    public int DefaultFrameDuration { get; set; }
    public string? ViewMode { get; set; }
    public string? Defense { get; set; }
    public string? Prosecutor { get; set; }
    public string? Judge { get; set; }
    public string? Cocouncil { get; set; }
    public List<string>? Witnesses { get; set; }

    public string? EEBias { get; set; }
    public int ChunkSize { get; set; }

    // Skip Settings
    public bool SkipRigs { get; set; }
    public bool SkipBGs { get; set; }
    public bool SkipTypewriter { get; set; }
    public bool SkipLines { get; set; }
    public bool SkipFades { get; set; }
    public bool SkipBlinks { get; set; }

    // Paths
    public string? PathToOperatingDocument { get; set; }
    public string? PathToSceneData { get; set; }
    public string? PathToCFGs { get; set; }
    public string? PathToLines { get; set; }

    // Backgrounds
    public SymbolConfig DefenseBackground { get; } = new SymbolConfig("BACKGROUNDS/Courtroom-Tinted", 5973.3, -23.55);
    public SymbolConfig ProsecutorBackground { get; } = new SymbolConfig("BACKGROUNDS/Courtroom-Tinted", -5973.3, -23.55);
    public SymbolConfig JudgeBackground { get; } = new SymbolConfig("BACKGROUNDS/JudgeBG-Tinted", 1280, 720);
    public SymbolConfig CocouncilBackground { get; } = new SymbolConfig("BACKGROUNDS/CocouncilBG-Tinted", 1280, 720);
    public SymbolConfig WitnessBackground { get; } = new SymbolConfig("BACKGROUNDS/Courtroom-Tinted", 0, -23.55);

    // Desks
    public SymbolConfig DefenseDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/DEFENSE-BENCH", 560, 1290);
    public SymbolConfig ProsecutorDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/PROSECUTION-BENCH", 1220, 1290);
    public SymbolConfig JudgeDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/Judge-Table-Final", 1280, 720);
    public SymbolConfig WitnessDesk { get; } = new SymbolConfig("OTHER ASSETS/DESKS/WitnessStand", 1280, 735);

    // Set up class configurations
    public Dictionary<string, CharacterConfig> characterConfigs = new Dictionary<string, CharacterConfig>();
    public Dictionary<string, NameswapConfig> nameswapConfigs = new Dictionary<string, NameswapConfig>();
    public Dictionary<string, LetterspacingConfig> letterspacingConfigs = new Dictionary<string, LetterspacingConfig>();

    public void AddCharacter(string configName, string simplifiedName, string pathToRigFile, string libraryPathInRigFile, string libraryPath, int TransX = 0, int TransY = 0)
    {
        if (!characterConfigs.ContainsKey(configName))
        {
            characterConfigs[configName] = new CharacterConfig();
        }
        characterConfigs[configName].AddCharacter(simplifiedName, libraryPath, pathToRigFile, libraryPathInRigFile, TransX, TransY);
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

    public static void SetConfiguration()
    {
        SingletonConfig config = SingletonConfig.Instance;

        //Core Settings
        config.ViewMode = "";
        config.DefaultFrameDuration = 24;

        config.Defense = "";
        config.Prosecutor = "";
        config.Judge = "";
        config.Cocouncil = "";
        config.Witnesses = new List<string> { };

        config.EEBias = "";
        config.ChunkSize = 70;

        // Skip Settings
        config.SkipRigs = false;
        config.SkipBGs = false;
        config.SkipTypewriter = false;
        config.SkipLines = false;
        config.SkipFades = false;
        config.SkipBlinks = false;

        // Specify your operating folder path in the startup args
        config.PathToOperatingDocument = "";
        config.PathToSceneData = "";
        config.PathToCFGs = "";
        config.PathToLines = "";

        // Characters
        config.AddCharacter("Courtroom", "Twilight", "CO-COUNCIL_Twilight.fla", "TWILIGHT SPARKLE/TwilightCouncil►", "TWILIGHT SPARKLE/TwilightCouncil►/TwilightCouncil►All");
        config.AddCharacter("Courtroom", "Celestia", "COURTROOM_Celestia.fla", "CELESTIA►", "CELESTIA►/Celestia►ScaledPoses");
        config.AddCharacter("Courtroom", "Luna", "COURTROOM_Luna.fla", "LunaProsecutor►", "LunaProsecutor►/LunaProsecutor►PoseScaled");
        config.AddCharacter("Courtroom", "Sonata", "COURTROOM_Sonata.fla", "SonataDefenseBench►", "SonataDefenseBench►/SonataDefenseBench►ScaledPoses", 1280, 775);
        config.AddCharacter("Courtroom", "Trixie", "COURTROOM_Trixie.fla", "TrixieProsecution►", "TrixieProsecution►/TrixieProsecution►ScaledPoses");
        config.AddCharacter("Courtroom", "Judge", "COURTROOM_Judge.fla", "JUDGE", "JUDGE/JUDGE►ScaledPoses", -9, 0);
        config.AddCharacter("Courtroom", "Phoenix", "CO-COUNCIL_Phoenix.fla", "WRIGHT", "WRIGHT/WRIGHT►ScaledPoses");

        config.AddCharacter("Investigation", "Amber", "INVESTIGATION_AmberGleam.fla", "AmberGleam►", "AmberGleam►/AmberGleam►PoseScaled");
        config.AddCharacter("Investigation", "Applejack", "INVESTIGATION_Applejack.fla", "APPLEJACK►", "APPLEJACK►/APPLEJACK►PoseScaled");
        config.AddCharacter("Investigation", "Athena", "INVESTIGATION_Athena.fla", "Cykes►", "Cykes►/Cykes►PoseOnly"); // <!> This rig needs a pose scaled
        config.AddCharacter("Investigation", "Celestia", "INVESTIGATION_Celestia.fla", "PRINCESS_CELESTIA►", "PRINCESS_CELESTIA►/PrincessCelestia►ScaledPoses");
        config.AddCharacter("Investigation", "Coco", "INVESTIGATION_Coco.fla", "Coco►", "Coco►/Coco►PoseScaled");

        config.AddCharacter("Investigation", "Cruise", "INVESTIGATION_CruiseControl.fla", "CRUISE_CONTROL►", "CRUISE_CONTROL►/Cruise►PoseScaled");
        config.AddCharacter("Investigation", "Equity", "INVESTIGATION_Equity.fla", "EquityArmored►", "EquityArmored►/EquityArmored►PoseScaled");
        config.AddCharacter("Investigation", "Fated Pursuit", "INVESTIGATION_FatedPursuit.fla", "FATED_PURSUIT", "FATED_PURSUIT/FatedPursuit►ScaledPoses");
        config.AddCharacter("Investigation", "Fluttershy", "INVESTIGATION_Fluttershy.fla", "Fluttershy►", "Fluttershy►/Fluttershy►PoseScaled");
        config.AddCharacter("Investigation", "Luna", "INVESTIGATION_Luna.fla", "Luna►", "Luna►/Luna►ScaledPoses"); // <!> C2 standard

        config.AddCharacter("Investigation", "Overall", "INVESTIGATION_OverallConcept.fla", "Overall►", "Overall►/Overall►PoseScaled");
        config.AddCharacter("Investigation", "Philo Reed", "INVESTIGATION_PhiloReed.fla", "PhiloReed►", "PhiloReed►/PoseScaled");
        config.AddCharacter("Investigation", "Philo", "INVESTIGATION_PhiloReed.fla", "PhiloReed►", "PhiloReed►/PhiloReed►PoseScaled");
        config.AddCharacter("Investigation", "Phoenix", "INVESTIGATION_Phoenix.fla", "Wright►", "Wright►/Wright►ScaledPoses");
        config.AddCharacter("Investigation", "Pinkie", "INVESTIGATION_Pinkie.fla", "PinkiePie►", "PinkiePie►/PinkiePie►PoseScaled");
        config.AddCharacter("Investigation", "Private Eye", "INVESTIGATION_PrivateEye.fla", "PRIVATE_EYE►", "PRIVATE_EYE►/Private►PoseScaled", 365, -344);

        config.AddCharacter("Investigation", "Rainbow", "INVESTIGATION_Rainbow_Dash.fla", "RAINBOW►", "RAINBOW►/RAINBOW►PoseScaled");
        config.AddCharacter("Investigation", "Rarity", "INVESTIGATION_Rarity.fla", "RARITY►", "RARITY►/Rarity►PoseScaled");
        config.AddCharacter("Investigation", "Spike", "INVESTIGATION_Spike.fla", "SPIKE►", "SPIKE►/Spike►PoseScaled");
        config.AddCharacter("Investigation", "Spitfire", "INVESTIGATION_Spitfire.fla", "Spitfire►", "Spitfire►/Spitfire►ScaledPoses");
        config.AddCharacter("Investigation", "Suri", "INVESTIGATION_Suri.fla", "SURI►", "SURI►/Suri►PoseScaled");

        config.AddCharacter("Investigation", "Sweetie Belle", "INVESTIGATION_SweetieBelle.fla", "SWEETIEBELLE", "SWEETIEBELLE/SweetieBelle►ScaledPoses");
        config.AddCharacter("Investigation", "Sweetie", "INVESTIGATION_SweetieBelle.fla", "SWEETIEBELLE", "SWEETIEBELLE/SweetieBelle►ScaledPoses");
        config.AddCharacter("Investigation", "Trixie", "INVESTIGATION_Trixie.fla", "Trixie►", "Trixie►/Trixie►ScaledPoses");
        config.AddCharacter("Investigation", "Trucy", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Twilight", "INVESTIGATION_Twilight.fla", "TWILIGHT►", "TWILIGHT►/Twilight►PoseScaled");
        config.AddCharacter("Investigation", "Scootaloo", "INVESTIGATION_Scootaloo.fla", "SCOOTALOO►", "SCOOTALOO►/Scootaloo►ScaledPoses");
        config.AddCharacter("LogicChess", "Sonata", "LOGICCHESS_Sonata.fla", "SonataLogicChess►", "SonataLogicChess►/SonataLogicChess►ScaledPoses");

        // <!> Always fake characters
        config.AddCharacter("Investigation", "Guard", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Guard #1", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Bailiff #1", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Conductor", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Stella", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");
        config.AddCharacter("Investigation", "Atticus", "INVESTIGATION_Trucy.fla", "Trucy►", "Trucy►/Trucy►ScaledPoses");

        // Nameswaps
        config.AddNameswap("Turning Page", "Turning");
        config.AddNameswap("Sweetie Belle", "Sweetie");
        config.AddNameswap("Diamond Tiara", "Diamond");

        // Letter Spacing
        config.AddLetterspacing("Royal Order", 1);
    }
}