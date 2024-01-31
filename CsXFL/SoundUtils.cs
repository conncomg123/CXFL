using NAudio.Wave;
using NAudio.Flac;
namespace CsXFL;
public class SoundUtils
{
    public static ArraySegment<byte> ConvertFlacToWav(string flacFilePath)
    {
        using var memStream = new MemoryStream();
        using var reader = new FlacReader(flacFilePath);
        using var writer = new WaveFileWriter(memStream, reader.WaveFormat);
        Span<byte> buffer = new byte[8192];
        while (reader.Position < reader.Length)
        {
            if (reader.Length - reader.Position < buffer.Length)
                buffer = new byte[reader.Length - reader.Position];
            reader.ReadExactly(buffer);
            writer.Write(buffer);
        }
        bool success = memStream.TryGetBuffer(out ArraySegment<byte> segment);
        if (!success)
            throw new InvalidOperationException("Failed to convert FLAC to WAV.");
        return segment;
    }
    public static string GetSoundFormat(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        string format = $"{file.Properties.AudioSampleRate / 1000}kHz {file.Properties.BitsPerSample}bit {(file.Properties.AudioChannels == 1 ? "Mono" : "Stereo")}";
        return format;
    }
    public static int GetSoundSampleCount(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        int sampleCount = (int)Math.Round(file.Properties.AudioSampleRate * file.Properties.Duration.TotalSeconds);
        return sampleCount;
    }
    public static double GetSoundDuration(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        return file.Properties.Duration.TotalSeconds;
    }
}