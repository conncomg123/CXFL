using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using CsXFL;

namespace XFL2SESX
{
    static class XFL2SESX
    {
        static int audioIdCounter = 1;
        static int fileIdCounter = 1;

        public static void AddAudioData(Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id)>> audioData, string layerName, string soundName, int startSample, int endSample)
        {
            if (!audioData.ContainsKey(layerName))
            {
                audioData[layerName] = new Dictionary<string, (int startSample, int endSample, int Id)>();
            }

            int audioId = audioIdCounter++;
            audioData[layerName][soundName] = (startSample, endSample, audioId);
        }
        public static (int startSample, int endSample) FrameToSamples(Document Doc, int sampleRate, double duration, int frameNumber)
        {
            int totalSamples = (int)(duration * sampleRate);
            int samplesPerFrame = totalSamples / Doc.Timelines[0].GetFrameCount();

            int startSample = (frameNumber - 1) * samplesPerFrame;
            int endSample = startSample + samplesPerFrame;

            return (startSample, endSample);
        }

        static void Main()
        {
            Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id)>> AUDIO_DATA = new Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id)>>();
            int SampleRate = 48000;

            string inputFile = @"C:\Users\Administrator\Elements of Justice\303_Autogen_FLAs\303_S5.fla";
            string directory = Path.GetDirectoryName(inputFile);

            string directory_SESX = directory + @"\Sesx Conversion\Imported Files";
            if (!Directory.Exists(directory_SESX)) { Directory.CreateDirectory(directory_SESX); }
            directory_SESX = directory + @"\Sesx Conversion";

            XmlDocument SESX = new XmlDocument();
            Document Doc = new(inputFile);

            // Extract Audio Data
            for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
            {
                foreach (var OperatingLayer in Doc.Timelines[SceneIndex].Layers)
                {
                    if (OperatingLayer.Name.Contains("VOX") || OperatingLayer.Name.Contains("SFX"))
                    {
                        foreach (var OperatingFrame in OperatingLayer.KeyFrames)
                        {
                            if (OperatingFrame.SoundName == "") { continue; }
                            (int startSample, int endSample) = FrameToSamples(Doc, SampleRate, 5, OperatingFrame.StartFrame);
                            AddAudioData(AUDIO_DATA, OperatingLayer.Name, OperatingFrame.SoundName.Substring(OperatingFrame.SoundName.LastIndexOf('/') + 1), startSample, endSample);
                        }
                    }
                }
            }

            // XML Processing
            XmlDeclaration declaration = SESX.CreateXmlDeclaration("1.0", "UTF-8", null);
            SESX.AppendChild(declaration);

            XmlElement root = SESX.CreateElement("sesx");
            root.SetAttribute("version", "1.9");
            SESX.AppendChild(root);

            XmlElement sessionElement = SESX.CreateElement("session");
            sessionElement.SetAttribute("appBuild", "13.0.3.60");
            sessionElement.SetAttribute("appVersion", "13.0");
            sessionElement.SetAttribute("audioChannelType", "stereo");
            sessionElement.SetAttribute("bitDepth", "32");
            // <!> Refactor
            sessionElement.SetAttribute("sampleRate", SampleRate.ToString());
            sessionElement.SetAttribute("duration", "1945359");
            root.AppendChild(sessionElement);

            int trackIndex = 0;
            foreach (var layer in AUDIO_DATA)
            {
                XmlElement audioTrackElement = SESX.CreateElement("audioTrack");
                audioTrackElement.SetAttribute("index", trackIndex.ToString());
                audioTrackElement.SetAttribute("visible", "true");
                root.AppendChild(audioTrackElement);

                XmlElement trackParametersElement = SESX.CreateElement("trackParameters");
                trackParametersElement.SetAttribute("trackHeight", "150");
                trackParametersElement.SetAttribute("trackHue", "100");
                trackParametersElement.SetAttribute("trackMinimized", "false");
                audioTrackElement.AppendChild(trackParametersElement);

                XmlElement nameElement = SESX.CreateElement("name");
                nameElement.InnerText = layer.Key;
                trackParametersElement.AppendChild(nameElement);

                XmlElement audioFilesElement = SESX.CreateElement("audioFiles");
                audioTrackElement.AppendChild(audioFilesElement);

                foreach (var audioFile in layer.Value)
                {
                    XmlElement audioElement = SESX.CreateElement("audioClip");
                    audioElement.SetAttribute("name", audioFile.Key);
                    audioElement.SetAttribute("startPoint", audioFile.Value.startSample.ToString());
                    audioElement.SetAttribute("endPoint", audioFile.Value.endSample.ToString());
                    // <!> Refactor
                    audioElement.SetAttribute("Id", audioFile.Value.Id.ToString());
                    trackParametersElement.AppendChild(audioElement);
                }

                trackIndex++;
            }

            XmlElement filesElement = SESX.CreateElement("files");
            foreach (var layer in AUDIO_DATA)
            {
                foreach (var audioFile in layer.Value)
                {
                    string pathToAudioFile = Path.Combine(directory_SESX, "Imported Files\\" + audioFile.Key);
                    XmlElement fileElement = SESX.CreateElement("file");
                    fileElement.SetAttribute("id", audioFile.Value.Id.ToString());
                    fileElement.SetAttribute("mediaHandler", "AmioWav");
                    fileElement.SetAttribute("absolutePath", pathToAudioFile);
                    fileElement.SetAttribute("relativePath", pathToAudioFile);
                    filesElement.AppendChild(fileElement);
                }
            }

            root.AppendChild(filesElement);

            string outputFile = Path.Combine(directory_SESX, Path.GetFileNameWithoutExtension(Doc.Filename) + ".sesx");
            SESX.Save(outputFile);

        }
    }
}