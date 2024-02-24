using CsXFL;
using System.Drawing.Printing;
using System.Security.Cryptography.X509Certificates;
namespace SceneGenerator.API;

static class BlinkAPI
{
    private static int BLINK_DURATION = 6;

    private static double GammaVariable(double mean, int k)
    {
        double theta = mean / k;
        double sum = 0;

        for (int i = 0; i < k; i++)
        {
            sum -= Math.Log(Random.Shared.NextDouble()) * theta;
        }

        return sum;
    }

    public static void AutomaticBlinking(this Document Doc, double mean)
    {
        int k = 25;
        double FPS = Doc.FrameRate;

        for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
        {
            Doc.CurrentTimeline = SceneIndex;
            Timeline CurrentTimeline = Doc.GetTimeline(Doc.CurrentTimeline);

            foreach (Layer CurrentLayer in CurrentTimeline.Layers)
            {
                if (CurrentLayer.ParentLayerIndex is null) continue;
                if (CurrentTimeline.Layers[CurrentLayer.ParentLayerIndex.Value].Name == "VECTOR_CHARACTERS")
                {
                    for (int FrameIndex = 0; FrameIndex < CurrentLayer.GetFrameCount();)
                    {
                        int Stare = (int)(GammaVariable(mean, k) * FPS), blinkFrame = Stare + FrameIndex;
                        // Terrible job, supershit!
                        if (blinkFrame >= CurrentLayer.GetFrameCount()) { break; }
                        Frame ConsiderFrame = CurrentLayer.GetFrame(blinkFrame);

                        if (!ConsiderFrame.IsEmpty())
                        {
                            CurrentLayer.ConvertToKeyframes(blinkFrame);
                            CurrentLayer.GetFrame(blinkFrame).Name = "Blink";
                            CurrentLayer.GetFrame(blinkFrame).LabelType = "anchor";
                        }

                        FrameIndex += Stare;
                    }
                }
            }
        }
    }
}
