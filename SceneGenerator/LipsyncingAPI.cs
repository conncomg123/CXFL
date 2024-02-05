using CsXFL;
using System.Text.RegularExpressions;

static class LipsyncAPI
{
    // <!> Try-catch exception handling eventually

    private static Dictionary<string, int> OFFSET_MAP = new Dictionary<string, int>
    {
    {"No Talking", 0},
    {"Closed Mouth No Teeth", 0},
    {"Open Mouth Big", 6},
    {"Open Mouth Teeth", 6},
    {"Open Mouth Wide", 16},
    {"Open Mouth Round", 16},
    {"Closed Mouth Teeth", 1},
    {"Ajar Mouth Tongue", 26},
    {"Ajar Mouth Teeth Together", 21},
    {"Ajar Mouth Teeth Seperate", 3}
    };

    private static Dictionary<string, int> LENGTH_MAP = new Dictionary<string, int>
    {
    {"No Talking", 1},
    {"Closed Mouth No Teeth", 1},
    {"Open Mouth Big", 3},
    {"Open Mouth Teeth", 3},
    {"Open Mouth Wide", 3},
    {"Open Mouth Round", 3},
    {"Closed Mouth Teeth", 1},
    {"Ajar Mouth Tongue", 1},
    {"Ajar Mouth Teeth Together", 1},
    {"Ajar Mouth Teeth Separate", 1}
    };

    private static Dictionary<string, string> PHONEME_TO_MOUTH_SHAPE = new Dictionary<string, string>
    {
    {"AA", "Open Mouth Big"},
    {"AE", "Open Mouth Big"},
    {"AH", "Open Mouth Big"},
    {"AO", "Open Mouth Big"},
    {"B", "Closed Mouth No Teeth"},
    {"CH", "Ajar Mouth Teeth Together"},
    {"D", "Ajar Mouth Teeth Together"},
    {"DH", "Ajar Mouth Tongue"},
    {"EH", "Open Mouth Teeth"},
    {"ER", "Open Mouth Round"},
    {"EY", "Open Mouth Teeth"},
    {"F", "Closed Mouth Teeth"},
    {"G", "Ajar Mouth Teeth Together"},
    {"HH", "Ajar Mouth Teeth Seperate"},
    {"IH", "Open Mouth Teeth"},
    {"IY", "Open Mouth Teeth"},
    {"JH", "Ajar Mouth Teeth Together"},
    {"K", "Ajar Mouth Teeth Together"},
    {"L", "Ajar Mouth Tongue"},
    {"M", "Closed Mouth No Teeth"},
    {"N", "Ajar Mouth Teeth Together"},
    {"NG", "Open Mouth Teeth"},
    {"OW", "Open Mouth Round"},
    {"P", "Closed Mouth No Teeth"},
    {"R", "Open Mouth Round"},
    {"S", "Ajar Mouth Teeth Together"},
    {"SH", "Ajar Mouth Teeth Together"},
    {"T", "Ajar Mouth Teeth Together"},
    {"TH", "Ajar Mouth Tongue"},
    {"UH", "Open Mouth Round"},
    {"UW", "Open Mouth Round"},
    {"V", "Closed Mouth Teeth"},
    {"W", "Open Mouth Round"},
    {"Y", "Open Mouth Teeth"},
    {"Z", "Ajar Mouth Teeth Together"},
    {"ZH", "Ajar Mouth Teeth Together"},
    {"", "No Talking"},
    {"sp", "No Talking"}
    };

    private static Dictionary<string, List<string>> DIPHTHONG_ORDERING = new Dictionary<string, List<string>>
    {
    {"AW", new List<string> {"Open Mouth Big", "Open Mouth Round"}},
    {"AY", new List<string> {"Open Mouth Big", "Open Mouth Teeth"}},
    {"OY", new List<string> {"Open Mouth Round", "Open Mouth Teeth"}}
    };

    private static string[] DIPHTHONGS = { "AW", "AY", "OY" };
    private static string[] SINGLE_FRAME_MOUTH_SHAPES = { "Ajar Mouth Teeth Together", "Closed Mouth No Teeth", "Closed Mouth Teeth", "Ajar Mouth Tongue", "Ajar Mouth Teeth Seperate", "No Talking" };

    static Dictionary<double, List<object>> ParseCFG(string filePath)
    {
        {
            Dictionary<double, List<object>> dictionary = new Dictionary<double, List<object>>();

            try
            {
                string targetVariable = "phonemes";
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

                        List<object> entry = new List<object>();
                        foreach (string value in values)
                        {
                            if (double.TryParse(value, out double numericValue))
                            {
                                entry.Add(numericValue);
                            }
                            else
                            {
                                entry.Add(value.Trim('"'));
                            }
                        }

                        dictionary[key] = entry;
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

            return dictionary;
        }
    }

    private static Dictionary<double, List<object>> phonemes2 = new Dictionary<double, List<object>>
    {
    {0.0, new List<object> {0.22, ""}},
    {0.22, new List<object> {0.49, "OW1"}},
    {0.49, new List<object> {1.23, ""}},
    {1.23, new List<object> {1.33, "Y"}},
    {1.33, new List<object> {1.4, "UW1"}},
    {1.4, new List<object> {1.46, "AO1"}},
    {1.46, new List<object> {1.53, "L"}},
    {1.53, new List<object> {1.64, "S"}},
    {1.64, new List<object> {1.7, "IY1"}},
    {1.7, new List<object> {1.77, "M"}},
    {1.77, new List<object> {1.84, "L"}},
    {1.84, new List<object> {1.94, "AO1"}},
    {1.94, new List<object> {2.01, "S"}},
    {2.01, new List<object> {2.08, "T"}},
    {2.08, new List<object> {2.11, "IH0"}},
    {2.11, new List<object> {2.2, "N"}},
    {2.2, new List<object> {2.24, "TH"}},
    {2.24, new List<object> {2.47, "AO1"}},
    {2.47, new List<object> {2.62, "T"}},
    {2.62, new List<object> {5.10197, ""}}
    };

    private static void PlaceKeyframes(Document Doc, int StartFrame, int LayerIndex, int PoseStartFrame, Dictionary<double, List<object>> phonemes)
    {
        Dictionary<int, string> DiphthongMap = new();
        Dictionary<int, string> MouthShapeMap = new();

        foreach (var PhonemeStartTime in phonemes.Keys)
        {
            int Frame2;
            int CurrentFrame = (int)(StartFrame + Math.Round(PhonemeStartTime * Doc.FrameRate));
            bool IsKeyframe = (CurrentFrame == Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).StartFrame);

            if (!IsKeyframe) Doc.GetTimeline(0).Layers[LayerIndex].ConvertToKeyframes(CurrentFrame);

            (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.Loop = "play once";

            Console.WriteLine("Phonemes2");

            foreach (var keyValuePair in phonemes2)
            {
                Console.Write($"{keyValuePair.Key}: ");
                foreach (var item in keyValuePair.Value)
                {
                    Console.Write($"{item} ");
                }
                Console.WriteLine();
            }

            Console.WriteLine("Phonemes1");

            foreach (var keyValuePair in phonemes)
            {
                Console.Write($"{keyValuePair.Key}: ");
                foreach (var item in keyValuePair.Value)
                {
                    Console.Write($"{item} ");
                }
                Console.WriteLine();
            }

            string phoneme = phonemes[PhonemeStartTime][1].ToString()!;
            if (!string.IsNullOrEmpty(phoneme)) phoneme = phoneme.Length > 1 ? phoneme.Substring(0, 2) : phoneme;

            if (DIPHTHONGS.Contains(phoneme))
            {
                DiphthongMap[CurrentFrame] = phoneme;
                continue;
            };

            // Some poses don't have the 'no talking' mouth shape defined. But is this still the case for Case 3 rigs?
            if (PHONEME_TO_MOUTH_SHAPE[phoneme!] == "No Talking" && !OFFSET_MAP.ContainsKey(PHONEME_TO_MOUTH_SHAPE[phoneme!]))
            {
                Frame2 = OFFSET_MAP["Closed Mouth No Teeth"];
            }
            else
            {
                Frame2 = OFFSET_MAP[PHONEME_TO_MOUTH_SHAPE[phoneme]];
            };

            (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.FirstFrame = (uint)(PoseStartFrame + Frame2);
            (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.LastFrame = (uint)(PoseStartFrame + Frame2 + LENGTH_MAP[PHONEME_TO_MOUTH_SHAPE[phoneme]] - 1);
            (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.Loop = "play once";

            if (SINGLE_FRAME_MOUTH_SHAPES.Contains(PHONEME_TO_MOUTH_SHAPE[phoneme]))
            {
                (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.Loop = "single frame";
            }

            MouthShapeMap[CurrentFrame] = PHONEME_TO_MOUTH_SHAPE[phoneme];
        };

        foreach (int Frame3 in DiphthongMap.Keys)
        {
            // For each Mouth Shape in the Diphthong:
            for (int i = 0; i < DIPHTHONG_ORDERING[DiphthongMap[Frame3]].Count; i++)
            {
                int CurFrame = Frame3 + i;
                // If the current frame isn't the first frame in a frame sequence, make a note of that.
                bool IsKeyframe = (CurFrame == Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurFrame).StartFrame);

                if (Frame3 != CurFrame && IsKeyframe)
                {
                    break;
                };

                if (!IsKeyframe) Doc.GetTimeline(0).Layers[LayerIndex].ConvertToKeyframes(CurFrame);

                var MouthShape = DIPHTHONG_ORDERING[DiphthongMap[Frame3]][i];
                var FirstFrame = OFFSET_MAP[MouthShape];

                (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurFrame).Elements[0] as SymbolInstance)!.FirstFrame = (uint)(PoseStartFrame + FirstFrame);
                (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurFrame).Elements[0] as SymbolInstance)!.LastFrame = (uint)(PoseStartFrame + FirstFrame + LENGTH_MAP[MouthShape] - 1);
                (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurFrame).Elements[0] as SymbolInstance)!.Loop = "play once";

                if (SINGLE_FRAME_MOUTH_SHAPES.Contains(MouthShape))
                {
                    (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurFrame).Elements[0] as SymbolInstance)!.Loop = "single frame";
                }

                MouthShapeMap[CurFrame] = DIPHTHONG_ORDERING[DiphthongMap[Frame3]][i];
            };

            int CurrentFrame = (int)(StartFrame + Math.Round(phonemes.Keys.ToList()[phonemes.Keys.Count - 1] * Doc.FrameRate));
            (Doc.GetTimeline(0).Layers[LayerIndex].GetFrame(CurrentFrame).Elements[0] as SymbolInstance)!.Loop = "single frame";
        };
    }

    public static void LipsyncSingle(this Document doc, int startFrame, int layerIndex, int poseStartFrame, Dictionary<double, List<object>> phonemes)
    {
        // <!> Do input validation later.
        // <!> The original copy has xSheet logic. The way Case 3 rigs work, we may just round down to 100 and save some effort.

        PlaceKeyframes(doc, startFrame, layerIndex, poseStartFrame, phonemes);
    }

    //static void Main()
    //{
    //    Document Doc = new("C:\\Users\\Administrator\\CXFL\\SceneGenerator\\LipsyncingTest\\DOMDocument.xml");
    //    Doc.LipsyncSingle(0, 0, 99, ParseCFG("C:\\Users\\Administrator\\Elements of Justice\\Dynamically_Linked_Scene\\SonataTest\\s1_048_sonata.cfg"));
    //    Doc.Save();
    //}
};