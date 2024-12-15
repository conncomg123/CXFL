using System.IO.Compression;
using CsXFL;
using NAudio.Wave;
using NAudio.Flac;
using NAudio.Wave.SampleProviders;
using WaveWriter = CSCore.Codecs.WAV.WaveWriter;

namespace Rendering;

public class AudioManager
{
    private Document document;
    public Dictionary<int, List<(MemoryStream data, int offset, SoundType type)>> soundPaths;
    public enum SoundType
    {
        MP3,
        FLAC,
        WAV
    }
    public AudioManager(Document document)
    {
        this.document = document;
        soundPaths = GetSoundPathsWithTimestamps();
    }
    private Dictionary<int, List<(MemoryStream, int, SoundType)>> GetSoundPathsWithTimestamps()
    {
        #region GetSoundData
        MemoryStream GetSoundData(string soundName)
        {
            var soundItem = document.Library.Items[soundName] as SoundItem ?? throw new ArgumentException($"Sound {soundName} does not exist.");
            MemoryStream ms;
            if (document.IsXFL)
            {
                string soundPath = Path.Combine(Path.GetDirectoryName(document.Filename)!, Library.LIBRARY_PATH, soundItem!.Href);
                ms = new MemoryStream(File.ReadAllBytes(soundPath));
                return ms;
            }
            else
            {
                using ZipArchive archive = ZipFile.Open(document.Filename, ZipArchiveMode.Read);
                string soundPath = Path.Combine(Library.LIBRARY_PATH, soundItem.Href).Replace("\\", "/");
                ZipArchiveEntry? entry = archive.GetEntry(soundPath);
                if (entry is null)
                {
                    // try to find it while removing slashes from both paths
                    soundPath = soundPath.Replace('/', '\\').Replace('\\', '_');
                    entry = archive.Entries.Where(x => x.FullName.Replace('/', '\\').Replace('\\', '_') == soundPath).FirstOrDefault();
                    if (entry is null) throw new Exception($"Sound not found: {soundPath}");
                }
                ms = new MemoryStream();
                entry.Open().CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
        }
        #endregion
        Dictionary<int, List<(MemoryStream, int, SoundType)>> result = new();
        int timelineFrameOffset = 0;
        foreach (Timeline timeline in document.Timelines)
        {
            foreach (Layer layer in timeline.Layers)
            {
                foreach (CsXFL.Frame frame in layer.KeyFrames)
                {
                    if (frame.SoundName != CsXFL.Frame.DefaultValues.SoundName)
                    {
                        int timestamp = frame.StartFrame + timelineFrameOffset;
                        int offset = frame.InPoint44;
                        MemoryStream soundData = GetSoundData(frame.SoundName);
                        SoundType type = (SoundType)Enum.Parse(typeof(SoundType), Path.GetExtension(frame.SoundName)[1..], true);
                        if (!result.TryGetValue(timestamp, out var list))
                        {
                            list = [];
                        }
                        list.Add((soundData, offset, type));
                        result[timestamp] = list;
                    }
                }
            }
            timelineFrameOffset += timeline.GetFrameCount();
        }
        return result;
    }
    const double FORTY_FOUR_THOUSAND = 44000.0;
    public MemoryStream GetMixedAudio()
    {
        // TODO: create file, write to it to create a file containing all the sounds at the correct timestamps
        List<WaveStream> readers = new();
        List<ISampleProvider> sampleProviders = new();
        int approximateSize = 0;
        try
        {
            foreach (var frameOffset in soundPaths.Keys)
            {
                var data = soundPaths[frameOffset];
                foreach (var sound in data)
                {
                    TimeSpan offset = TimeSpan.FromSeconds(frameOffset / document.FrameRate);
                    TimeSpan splitAudioOffset = TimeSpan.FromSeconds(sound.offset / FORTY_FOUR_THOUSAND);
                    WaveStream reader = sound.type switch
                    {
                        SoundType.FLAC => new FlacReader(sound.data),
                        SoundType.WAV => new WaveFileReader(sound.data),
                        SoundType.MP3 => new Mp3FileReader(sound.data),
                        _ => throw new NotImplementedException(),
                    };
                    readers.Add(reader);
                    ISampleProvider resampler = new WdlResamplingSampleProvider(reader.ToSampleProvider(), 44100);
                    if (resampler.WaveFormat.Channels == 1)
                    {
                        resampler = new MonoToStereoSampleProvider(resampler);
                    }
                    var offsetProvider = new OffsetSampleProvider(resampler)
                    {
                        DelayBy = offset,
                        SkipOver = splitAudioOffset,
                    };
                    approximateSize += Convert.ToInt32(reader.TotalTime.TotalSeconds * offsetProvider.WaveFormat.AverageBytesPerSecond);
                    sampleProviders.Add(offsetProvider);
                }
            }
            var waveFormatNaudio = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            var waveFormatCSCore = new CSCore.WaveFormat(44100, 32, 2);
            var mixer = new MixingSampleProvider(waveFormatNaudio);
            foreach (var provider in sampleProviders)
            {
                mixer.AddMixerInput(provider);
            }
            var memoryStream = new MemoryStream(approximateSize);
            using (var waveWriter = new WaveWriter(memoryStream, waveFormatCSCore))
            {
                float[] buffer = new float[1024];
                int samplesRead;
                while ((samplesRead = mixer.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveWriter.WriteSamples(buffer, 0, samplesRead);
                }
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        finally
        {
            foreach(var reader in readers)
            {
                reader.Dispose();
            }
        }
    }
}