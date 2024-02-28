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

        static void AddAudioData(Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id, int fileId)>> audioData, string layerName, string audioFileName, int startSample, int endSample)
        {
            if (!audioData.ContainsKey(layerName))
            {
                audioData[layerName] = new Dictionary<string, (int startSample, int endSample, int Id, int fileId)>();
            }

            int id = audioData[layerName].Count; // Get the current count of audio clips in the layer
            int fileId = audioData.Values.SelectMany(dict => dict.Values).Any() ? audioData.Values.SelectMany(dict => dict.Values).Max(x => x.fileId) + 1 : 0; // Increment fileId across all layers or start from 0 if no elements
            audioData[layerName][audioFileName] = (startSample, endSample, id, fileId);
        }

        public static int FrameToSamples(Document doc, int sampleRate, int frameNumber)
        {
            double frameRate = doc.FrameRate;
            double frameDuration = 1.0 / frameRate;

            int samplesPerFrame = (int)(sampleRate * frameDuration);
            int startSample = frameNumber * samplesPerFrame;

            return startSample;
        }

        static string RemoveFileExtension(string fileName)
        {
            int lastDotIndex = fileName.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return fileName;
            }
            else
            {
                return fileName.Substring(0, lastDotIndex);
            }
        }

        static void Main()
        {
            Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id, int fileId)>> AUDIO_DATA = new Dictionary<string, Dictionary<string, (int startSample, int endSample, int Id, int fileId)>>();
            int SampleRate = 48000;

            string inputFile = @"C:\Users\Administrator\Elements of Justice\303_Autogen_FLAs\Test.fla";
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
                            SoundItem AudioFile = Doc.Library.Items[OperatingFrame.SoundName] as SoundItem;

                            int SampleCount = AudioFile.SampleCount;
                            int startSample = FrameToSamples(Doc, SampleRate, OperatingFrame.StartFrame);
                            int endSample = startSample + SampleCount;

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
            sessionElement.SetAttribute("sampleRate", SampleRate.ToString());
            sessionElement.SetAttribute("duration", "1945359");
            root.AppendChild(sessionElement);

            XmlElement trackList = SESX.CreateElement("tracks");
            sessionElement.AppendChild(trackList);

            int trackIndex = 0;
            foreach (var layer in AUDIO_DATA)
            {
                XmlElement audioTrackElement = SESX.CreateElement("audioTrack");
                audioTrackElement.SetAttribute("id", "1000" + (trackIndex + 1).ToString());
                audioTrackElement.SetAttribute("index", trackIndex.ToString());
                audioTrackElement.SetAttribute("select", "false");
                audioTrackElement.SetAttribute("visible", "true");
                trackList.AppendChild(audioTrackElement);

                XmlElement trackParametersElement = SESX.CreateElement("trackParameters");
                trackParametersElement.SetAttribute("trackHeight", "150");
                trackParametersElement.SetAttribute("trackHue", "100");
                trackParametersElement.SetAttribute("trackMinimized", "false");
                audioTrackElement.AppendChild(trackParametersElement);

                XmlElement nameElement = SESX.CreateElement("name");
                nameElement.InnerText = layer.Key;
                trackParametersElement.AppendChild(nameElement);

                XmlElement trackAudioParameters = SESX.CreateElement("trackAudioParameters");
                trackAudioParameters.SetAttribute("audioChannelType", "stereo");
                trackAudioParameters.SetAttribute("automationMode", "1");
                trackAudioParameters.SetAttribute("monitoring", "false");
                trackAudioParameters.SetAttribute("audioChannelType", "stereo");
                trackAudioParameters.SetAttribute("recordArmed", "false");
                trackAudioParameters.SetAttribute("solo", "false");
                trackAudioParameters.SetAttribute("soloSafe", "false");
                audioTrackElement.AppendChild(trackAudioParameters);

                XmlElement trackOutput = SESX.CreateElement("trackOutput");
                XmlElement trackInput = SESX.CreateElement("trackInput");
                trackOutput.SetAttribute("outputID", "10000");
                trackOutput.SetAttribute("type", "trackID");
                trackInput.SetAttribute("inputID", "-1");
                trackAudioParameters.AppendChild(trackOutput);
                trackAudioParameters.AppendChild(trackInput);

                foreach (var audioFile in layer.Value)
                {
                    XmlElement audioElement = SESX.CreateElement("audioClip");

                    audioElement.SetAttribute("clipAutoCrossfade", "true");
                    audioElement.SetAttribute("crossFadeHeadClipID", "-1");
                    audioElement.SetAttribute("crossFadeTailClipID", "-1");
                    audioElement.SetAttribute("crossFadeTailClipID", "-1");
                    audioElement.SetAttribute("endPoint", audioFile.Value.endSample.ToString());
                    audioElement.SetAttribute("fileId", audioFile.Value.fileId.ToString());
                    audioElement.SetAttribute("hue", "-1");
                    audioElement.SetAttribute("id", audioFile.Value.Id.ToString());
                    audioElement.SetAttribute("lockedInTime", "false");
                    audioElement.SetAttribute("looped", "false");
                    audioElement.SetAttribute("name", audioFile.Key);
                    audioElement.SetAttribute("offline", "false");
                    audioElement.SetAttribute("select", "false");
                    // <?>
                    audioElement.SetAttribute("sourceInPoint", "0");
                    audioElement.SetAttribute("sourceOutPoint", audioFile.Value.endSample.ToString());

                    audioElement.SetAttribute("startPoint", audioFile.Value.startSample.ToString());
                    audioElement.SetAttribute("zOrder", audioFile.Value.Id.ToString());
 
                    audioTrackElement.AppendChild(audioElement);
                }

                trackIndex++;
            }

            XmlElement filesElement = SESX.CreateElement("files");
            int globalFileId = 0; // Keep track of the fileId globally

            foreach (var layer in AUDIO_DATA)
            {
                foreach (var audioFile in layer.Value)
                {
                    string pathToAudioFile = Path.Combine(directory_SESX, "Imported Files\\" + audioFile.Key);
                    XmlElement fileElement = SESX.CreateElement("file");
                    fileElement.SetAttribute("absolutePath", pathToAudioFile);
                    fileElement.SetAttribute("id", globalFileId.ToString()); // Use globalFileId as fileId
                    //fileElement.SetAttribute("importerPrivateSettings", "ByteOrdering:0:0;Channels:0:1;EncodingType:0:3;FormatType:0:1507328;SampleRate:0:48000;StartOffset:0:0;VBRQuality:0:100;Title:1:s5_001_overall;TrackNumber:1:1;");
                    fileElement.SetAttribute("mediaHandler", "AmioLSF");
                    fileElement.SetAttribute("relativePath", "Imported Files/" + audioFile.Key);
                    filesElement.AppendChild(fileElement);

                    globalFileId++; // Increment globalFileId for each file
                }
            }

            root.AppendChild(filesElement);

            string outputFile = Path.Combine(directory_SESX, Path.GetFileNameWithoutExtension(Doc.Filename) + ".sesx");
            SESX.Save(outputFile);

        }
    }
}