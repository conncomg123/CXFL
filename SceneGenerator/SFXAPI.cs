using CsXFL;
using System;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace SceneGenerator.API;

static class SFXAPI
{
    static string SFXFolderPath = "AUDIO/SFX/";
    static string FlashPath = "OTHER ASSETS/Standard_Flash";
    static string InterjectionPath = "OTHER ASSETS/INTERJECTION/INTERJECTIONS";
    static bool TaperOff = true;
    static int FlashRange = 5;
    static int ShakeRange = 10;
    static int InterjectionRange = 30; // Originally 43
    static int ShakeItensity = 20;
    static List<Tuple<double, double>> ShakeOffsets = new List<Tuple<double, double>>();

    static HashSet<string> Flash = new()
        {
            "sfx-huh.wav", "sfx-high.wav", "sfx-higher.wav", "sfx-highest.wav", "sfx-low.wav",
            "sfx-lightbulb.wav", "sfx-damage.wav", "sfx-dramaticshock.wav", "sfx-hit1.wav",
            "sfx-punch.wav", "sfx-punch2.wav", "sfx-shocked.wav", "sfx-shouting.wav",
            "sfx-stab.wav", "sfx-stab2.wav", "sfx-furious.wav"
        };

    static HashSet<string> Shake = new()
        {
            "sfx-deskslam.wav", "sfx-thud.wav", "sfx-thud2.wav", "sfx-damage.wav",
            "sfx-dramaticshock.wav", "sfx-hit1.wav", "sfx-punch.wav", "sfx-punch2.wav",
            "sfx-shocked.wav", "sfx-shouting.wav", "sfx-stab.wav", "sfx-stab2.wav",
            "sfx-furious.wav"
        };

    static HashSet<string> ShakeIgnoreLayers = new()
        {
            "FLASH", "INTERJECTION", "FADE", "GAVEL", "EVIDENCE", "TEXT"
        };

    static HashSet<string> Interjections = new()
        {
            "OBJECTION", "TAKE THAT", "HOLD IT", "BE STILL"
        };

    public static void PlaceSFX(this Document Doc, string sfxName, int FrameIndex)
    {

        bool isSFXInFlash = Flash.Contains(sfxName);
        bool isSFXInShake = Shake.Contains(sfxName);
        int underscoreIndex = sfxName.IndexOf("_");
        bool isSFXInterjection = underscoreIndex >= 0 && Interjections.Contains(sfxName.Substring(0, underscoreIndex));

        Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

        if (CurrentTimeline.FindLayerIndex("SFX").Count == 0)
        {
            CurrentTimeline.AddNewLayer("SFX", "normal");
        }

        if (CurrentTimeline.FindLayerIndex("FLASH").Count == 0)
        {
            CurrentTimeline.AddNewLayer("FLASH", "normal");
        }

        int SFXLayerIndex = Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("SFX")[0];
        int FlashLayerIndex = Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("FLASH")[0];
        int InterjectionLayerIndex = Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("INTERJECTION")[0];
        int TextLayerIndex = Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXT")[0];
        int TextboxLayerIndex = Doc.GetTimeline(Doc.CurrentTimeline).FindLayerIndex("TEXTBOX")[0];

        CurrentTimeline.Layers[SFXLayerIndex].ConvertToKeyframes(FrameIndex);
        string AttemptSFXPath = SFXFolderPath + sfxName;

        //Safety checks off
        if (!Doc.Library.ItemExists(AttemptSFXPath)) { AttemptSFXPath = "AUDIO/SFX/sfx-shouting.wav"; }

        Doc.Library.AddItemToDocument(AttemptSFXPath, CurrentTimeline.Layers[SFXLayerIndex].GetFrame(FrameIndex), 0, 0);
        CurrentTimeline.Layers[SFXLayerIndex].GetFrame(FrameIndex).SoundSync = "stream";

        if (isSFXInFlash)
        {
            CurrentTimeline.Layers[FlashLayerIndex].ConvertToKeyframes(FrameIndex);
            Doc.Library.AddItemToDocument(FlashPath, CurrentTimeline.Layers[FlashLayerIndex].GetFrame(FrameIndex), 1278.95, 718.95);
            CurrentTimeline.Layers[FlashLayerIndex].ConvertToKeyframes(FrameIndex + FlashRange);
            CurrentTimeline.Layers[FlashLayerIndex].GetFrame(FrameIndex + FlashRange).ClearElements();
        }

        if (isSFXInShake)
        {
            for (int i = FrameIndex; i < FrameIndex + ShakeRange - 1; i++)
            {
                double randomX = Random.Shared.NextDouble() - 0.5;
                double randomY = Random.Shared.NextDouble() - 0.5;

                double deltaX = 0;
                double deltaY = 0;

                if (TaperOff)
                {
                    deltaX = 2 * ShakeItensity * randomX * (1 - (i - FrameIndex) / (double)ShakeRange);
                    deltaY = 2 * ShakeItensity * randomY * (1 - (i - FrameIndex) / (double)ShakeRange);
                }
                else
                {
                    deltaX = 2 * ShakeItensity * randomX;
                    deltaY = 2 * ShakeItensity * randomY;
                }
                ShakeOffsets.Add(new Tuple<double, double> ( deltaX, deltaY ));
            }

            for (int i = 0; i < CurrentTimeline.Layers.Count; i++)
            {
                if (CurrentTimeline.Layers[i].LayerType == "normal" && !ShakeIgnoreLayers.Contains(CurrentTimeline.Layers[i].Name) && !CurrentTimeline.Layers[i].GetFrame(FrameIndex).IsEmpty())    
                {
                    Matrix modMatrix = CurrentTimeline.Layers[i].GetFrame(FrameIndex).Elements[0].Matrix;
                    for (int j = 1; j < ShakeRange - 1; j++)
                    {
                        if (CurrentTimeline.Layers[i].GetFrame(FrameIndex + j).Elements.Count == 0) { continue; }
                        CurrentTimeline.Layers[i].ConvertToKeyframes(FrameIndex + j);
                        CurrentTimeline.Layers[i].GetFrame(FrameIndex + j).Elements[0].Matrix = modMatrix;
                        CurrentTimeline.Layers[i].GetFrame(FrameIndex + j).Elements[0].Matrix.Tx += ShakeOffsets[j].Item1;
                        CurrentTimeline.Layers[i].GetFrame(FrameIndex + j).Elements[0].Matrix.Ty += ShakeOffsets[j].Item2;
                    };
                    if (CurrentTimeline.Layers[i].GetFrame(FrameIndex + ShakeRange - 1).Elements.Count == 0) { continue; }
                    CurrentTimeline.Layers[i].ConvertToKeyframes(FrameIndex + ShakeRange - 1);
                    CurrentTimeline.Layers[i].GetFrame(FrameIndex + ShakeRange - 1).Elements[0].Matrix = modMatrix;
                }
            }
        }

        if (isSFXInterjection)
        {
            // Add frames to all layers, cut out text for interjection range + 5
            CurrentTimeline.InsertFrames(InterjectionRange + 5, true, FrameIndex);
            CurrentTimeline.Layers[TextLayerIndex].ConvertToKeyframes(FrameIndex + InterjectionRange + 5);
            CurrentTimeline.Layers[TextLayerIndex].GetFrame(FrameIndex).ClearElements();
            CurrentTimeline.Layers[TextboxLayerIndex].ConvertToKeyframes(FrameIndex - 2);
            CurrentTimeline.Layers[TextboxLayerIndex].ConvertToKeyframes(FrameIndex + InterjectionRange + 5);
            CurrentTimeline.Layers[TextboxLayerIndex].GetFrame(FrameIndex - 2).ClearElements();

            // Do flash
            CurrentTimeline.Layers[FlashLayerIndex].ConvertToKeyframes(FrameIndex);
            Doc.Library.AddItemToDocument(FlashPath, CurrentTimeline.Layers[FlashLayerIndex].GetFrame(FrameIndex), 1278.95, 718.95);
            CurrentTimeline.Layers[FlashLayerIndex].ConvertToKeyframes(FrameIndex + FlashRange);
            CurrentTimeline.Layers[FlashLayerIndex].GetFrame(FrameIndex + FlashRange).ClearElements();

            // Do interjection
            CurrentTimeline.Layers[InterjectionLayerIndex].ConvertToKeyframes(FrameIndex);
            Doc.Library.AddItemToDocument(InterjectionPath, CurrentTimeline.Layers[InterjectionLayerIndex].GetFrame(FrameIndex), 263.35, 429.15);
            CurrentTimeline.Layers[InterjectionLayerIndex].ConvertToKeyframes(FrameIndex + InterjectionRange);
            CurrentTimeline.Layers[InterjectionLayerIndex].GetFrame(FrameIndex + InterjectionRange).ClearElements();

            // Cast to symbolinstance and dynamically set firstFrame to correct interjection
            SymbolInstance Interjection = CurrentTimeline.Layers[InterjectionLayerIndex].GetFrame(FrameIndex).Elements[0] as SymbolInstance;

            foreach (var substring in Interjections)
            {
                int index = sfxName.IndexOf(substring);
                if (index != -1)
                {
                    if (sfxName.Contains("OBJECTION_LUNA"))
                    {
                        Interjection.FirstFrame = 221;
                    }
                    else if (substring == "OBJECTION")
                    {
                        Interjection.FirstFrame = 1;
                    }
                    else if (substring == "TAKE THAT")
                    {
                        Interjection.FirstFrame = 89;
                    }
                    else if (substring == "HOLD IT")
                    {
                        Interjection.FirstFrame = 45;
                    }
                    else if (substring == "BE STILL")
                    {
                        Interjection.FirstFrame = 265;
                    }
                }
            }
        }
    }
}