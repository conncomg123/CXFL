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
    static bool TaperOff = true;
    static int FlashRange = 5;
    static int ShakeRange = 10;
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

    public static void PlaceSFX(this Document Doc, string sfxName, int FrameIndex)
    {

        bool isSFXInFlash = Flash.Contains(sfxName);
        bool isSFXInShake = Shake.Contains(sfxName);

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
        CurrentTimeline.Layers[SFXLayerIndex].ConvertToKeyframes(FrameIndex);
        Doc.Library.AddItemToDocument(SFXFolderPath + sfxName, CurrentTimeline.Layers[SFXLayerIndex].GetFrame(FrameIndex), 0, 0);
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
    }
}