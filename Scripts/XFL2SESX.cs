using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using CsXFL;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.FLAC;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using static XFL2SESX.XFL2SESX;
using System.Diagnostics;
using CSCore.Codecs.AAC;
using System.Text;

namespace XFL2SESX
{
    static class XFL2SESX
    {
        static void AddAudioData(Dictionary<string, List<AudioClip>> audioData, Dictionary<string, int> fileIds, string layerName, string audioFileName, int startSample, int endSample, int sourceInPoint, int sourceOutPoint)
        {
            if (!audioData.ContainsKey(layerName))
            {
                audioData[layerName] = new List<AudioClip>();
            }

            int fileId;
            if (fileIds.ContainsKey(audioFileName))
            {
                fileId = fileIds[audioFileName];
            }
            else
            {
                fileId = fileIds.Count;
                fileIds[audioFileName] = fileId;
            }

            int id = audioData[layerName].Count;
            audioData[layerName].Add(new AudioClip
            {
                id = id,
                name = audioFileName,
                fileID = fileId,
                startPoint = startSample,
                sourceInPoint = sourceInPoint,
                endPoint = endSample,
                sourceOutPoint = sourceOutPoint,
                zOrder = id
            });
        }

        static int ConvertSamples(int oldSampleCount, int oldSampleRate, int newSampleRate)
        {
            return (int)Math.Round(oldSampleCount * (double)newSampleRate / oldSampleRate);
        }

        public static int FrameToSamples(Document doc, int sampleRate, int frameNumber)
        {
            // I reject your reality and substitute my own
            double frameRate = doc.FrameRate;
            frameRate = 23.976;
            double frameDuration = 1.0 / frameRate;

            int samplesPerFrame = (int)(sampleRate * frameDuration);
            int startSample = frameNumber * samplesPerFrame;

            return startSample;
        }

        static string GetAfterLastSlash(string input)
        {
            int lastIndex = input.LastIndexOf('/');
            if (lastIndex == -1)
            {
                return input;
            }

            return input.Substring(lastIndex + 1);
        }

        public class AudioClip
        {
            public int id { get; set; }
            public string name { get; set; }
            public int fileID { get; set; }
            public int startPoint { get; set; }
            public int sourceInPoint { get; set; }
            public int endPoint { get; set; }
            public int sourceOutPoint { get; set; }
            public int zOrder { get; set; }
        }

        static void Run(string[] args)
        {
            List<string> FLAfileList = new List<string>();
            string FLAfilePaths_ARGS = "";
            string audioFolderPath_ARGS = "";
            bool displayHelp = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "--help" || arg == "-h" || arg == "-?")
                {
                    displayHelp = true;
                }
                else if (i == 0)
                {
                    // Process FLAs
                    FLAfilePaths_ARGS = arg;
                    string[] files = arg.Split('|');
                    foreach (var file in files)
                    {
                        FLAfileList.Add(file);
                    }
                }
                else if (i == 1)
                {
                    // Process Audio Folder
                    audioFolderPath_ARGS = arg;
                }
            }

            if (displayHelp || string.IsNullOrEmpty(FLAfilePaths_ARGS) || string.IsNullOrEmpty(audioFolderPath_ARGS))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("    XFL2SESX.exe [AbsolutePathToFLA|FLAList] AbsolutePathToAudioFolder");
                Console.WriteLine("Options:");
                Console.WriteLine("    --help, -h   Display this help message");
                Console.WriteLine("Description:");
                Console.WriteLine("    An Adobe Animate to Adobe Audition file converter to assist with mastering projects made in Animate.");
                Console.WriteLine("This utility supports single file operation on an FLA, or batch operation across multiple FLAs, yielding");
                Console.WriteLine("one compiled SESX file. When the SESX is compiled, dependent audio files will need to be placed in the");
                Console.WriteLine("'Imported Files' folder where the SESX is located. The required AbsolutePathToAudioFolder will convert all");
                Console.WriteLine("audio files in that folder to a compatible format, and automatically move them into this folder. Audio files");
                Console.WriteLine("not present in this audio folder but required by the SESX will need to placed into the folder manually.");
                Console.WriteLine("");
                Console.WriteLine("To process multiple FLAs, provide the paths inside quotes, delimited by the vertical pipe character.");
                Console.WriteLine("    (ex.) XFL2SESX.exe \"C:\\File\\File01.FLA|C:\\File\\File02.FLA|C:\\File\\File03.FLA\" \"C:\\AudioFolder\"");
                Console.WriteLine("");
                return;
            }

            Dictionary<string, int> fileIds = new Dictionary<string, int>();
            Dictionary<string, List<AudioClip>> AUDIO_DATA = new Dictionary<string, List<AudioClip>>();
            int SampleRate = 48000;

            string primaryFile = "";

            foreach (string file in FLAfileList)
            {
                primaryFile = file;
                break;
            }

            string fileName = Path.GetFileNameWithoutExtension(primaryFile);
            string directory = Path.GetDirectoryName(primaryFile);

            string directory_SESX = directory + @"\" + fileName + @" Sesx Conversion\Imported Files";
            if (!Directory.Exists(directory_SESX)) { Directory.CreateDirectory(directory_SESX); }
            directory_SESX = directory + @"\" + fileName + @" Sesx Conversion\";

            XmlDocument SESX = new XmlDocument();

            // Extract Audio Data
            int totalFrames = 0;
            foreach (string file in FLAfileList)
            {
                Document Doc = new(file);
                for (int SceneIndex = 0; SceneIndex < Doc.Timelines.Count; SceneIndex++)
                {
                    foreach (var OperatingLayer in Doc.Timelines[SceneIndex].Layers)
                    {
                        if (OperatingLayer.Name.Contains("VOX", StringComparison.OrdinalIgnoreCase) || OperatingLayer.Name.Contains("SFX", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var OperatingFrame in OperatingLayer.KeyFrames)
                            {
                                if (OperatingFrame.SoundName == "") { continue; }
                                string SoundName = OperatingFrame.SoundName;
                                SoundItem AudioFile = Doc.Library.Items[SoundName] as SoundItem;

                                // Either get the full the sample count of the file, or get what section of it would be displayed, if it is interrupted by a blank keyframe. Whichever first.
                                int SampleCount = Math.Min(AudioFile.SampleCount, FrameToSamples(Doc, SampleRate, OperatingFrame.Duration));
                                int startSample = FrameToSamples(Doc, SampleRate, OperatingFrame.StartFrame + totalFrames);
                                int endSample = startSample + SampleCount;

                                int SplitAudioOffset = OperatingFrame.InPoint44;

                                if (SplitAudioOffset == 0)
                                {
                                    AddAudioData(AUDIO_DATA, fileIds, OperatingLayer.Name.Replace("_VOX", "").Replace("_1", ""), GetAfterLastSlash(SoundName), startSample, endSample, 0, SampleCount + (SampleRate / 2));
                                }
                                else
                                {
                                    SplitAudioOffset = ConvertSamples(SplitAudioOffset, 44000, SampleRate);
                                    AddAudioData(AUDIO_DATA, fileIds, OperatingLayer.Name.Replace("_VOX", "").Replace("_1", ""), GetAfterLastSlash(SoundName), startSample, endSample, SplitAudioOffset, SampleCount + (SampleRate/2));
                                }
                            }
                        }
                    }
                    totalFrames += Doc.Timelines[SceneIndex].GetFrameCount();
                }
            }

            // XML Processing
            XmlDeclaration declaration = SESX.CreateXmlDeclaration("1.0", "UTF-8", null);
            declaration.Standalone = "no";
            SESX.AppendChild(declaration);

            XmlDocumentType doctype = SESX.CreateDocumentType("sesx", null, null, null);
            SESX.AppendChild(doctype);

            XmlElement root = SESX.CreateElement("sesx");
            root.SetAttribute("version", "1.9");
            SESX.AppendChild(root);

            XmlElement sessionElement = SESX.CreateElement("session");
            sessionElement.SetAttribute("appBuild", "13.0.3.60");
            sessionElement.SetAttribute("appVersion", "13.0");
            sessionElement.SetAttribute("audioChannelType", "stereo");
            sessionElement.SetAttribute("bitDepth", "32");
            sessionElement.SetAttribute("sampleRate", SampleRate.ToString());
            sessionElement.SetAttribute("duration", "100000");
            root.AppendChild(sessionElement);

            XmlElement trackList = SESX.CreateElement("tracks");
            sessionElement.AppendChild(trackList);

            XmlElement filesElement = SESX.CreateElement("files");

            // FILE BLOCK
            foreach (var layer in AUDIO_DATA)
            {
                foreach (var audioFile in layer.Value)
                {
                    string pathToAudioFile = Path.Combine(directory_SESX, "Imported Files\\" + audioFile.name);

                    // Use the file ID from the dictionary
                    int fileId = fileIds[audioFile.name];

                    XmlElement fileElement = SESX.CreateElement("file");
                    fileElement.SetAttribute("absolutePath", pathToAudioFile);
                    fileElement.SetAttribute("id", fileId.ToString());
                    fileElement.SetAttribute("mediaHandler", "AmioLSF");
                    fileElement.SetAttribute("relativePath", "Imported Files/" + audioFile.name);
                    filesElement.AppendChild(fileElement);
                }
            }

            // AUDIO CLIPS
            int trackIndex = 0;
            foreach (var layer in AUDIO_DATA)
            {
                int idCounter = 0;
                int zOrderCounter = 0;

                XmlElement audioTrackElement = SESX.CreateElement("audioTrack");
                audioTrackElement.SetAttribute("automationLaneOpenState", "false");
                audioTrackElement.SetAttribute("id", "1000" + (trackIndex + 1).ToString());
                audioTrackElement.SetAttribute("index", (trackIndex + 1).ToString());
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

                foreach (var audioClip in layer.Value)
                {
                    string pathToAudioFile = Path.Combine(directory_SESX, "Imported Files\\" + audioClip.name);

                    int fileId = fileIds[audioClip.name];

                    XmlElement audioElement = SESX.CreateElement("audioClip");

                    audioElement.SetAttribute("id", audioClip.id.ToString());
                    audioElement.SetAttribute("name", audioClip.name.ToString());
                    audioElement.SetAttribute("fileID", audioClip.fileID.ToString());
                    audioElement.SetAttribute("startPoint", audioClip.startPoint.ToString());
                    audioElement.SetAttribute("sourceInPoint", audioClip.sourceInPoint.ToString());
                    audioElement.SetAttribute("endPoint", audioClip.endPoint.ToString());
                    audioElement.SetAttribute("sourceOutPoint", audioClip.sourceOutPoint.ToString());
                    audioElement.SetAttribute("clipAutoCrossfade", "false");
                    audioElement.SetAttribute("zOrder", audioClip.zOrder.ToString());

                    audioTrackElement.AppendChild(audioElement);
                }

                trackIndex++;
            }

            root.AppendChild(filesElement);

            // Automatically port over provided files
            string outputDirectory = directory + @"\" + fileName + @" Sesx Conversion\Imported Files";
            string[] audioFiles = Directory.GetFiles(audioFolderPath_ARGS, "*.*", SearchOption.AllDirectories);
            foreach (string audioFile in audioFiles)
            {
                string inputAudioFile = audioFile;
                string outputAudioFile = outputDirectory + "\\" + Path.GetFileName(audioFile);

                string ffmpegCommand = $"-i \"{inputAudioFile}\" -c:a flac -ar {SampleRate} -ac 1 -sample_fmt s32 \"{outputAudioFile}\"";

                Process process = new Process();
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
                process.StartInfo.FileName = ffmpegPath;
                process.StartInfo.Arguments = ffmpegCommand;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                StringBuilder error = new StringBuilder();
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Error converting {inputAudioFile}:");
                    Console.WriteLine(error.ToString());
                }
            }

            string outputFile = Path.Combine(directory_SESX, Path.GetFileNameWithoutExtension(primaryFile) + ".sesx");
            Console.WriteLine("SESX created at " + outputFile);
            SESX.Save(outputFile);

        }
    }
}