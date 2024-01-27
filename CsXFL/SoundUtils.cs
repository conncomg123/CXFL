using NAudio.Wave;
using NAudio.Flac;
internal class SoundUtils
{
    internal static byte[] ConvertFlacToWav(string flacFilePath)
    {
        var memStream = new MemoryStream();
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
        return memStream.ToArray();
    }
    internal static string GetSoundFormat(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        string format = $"{file.Properties.AudioSampleRate / 1000}kHz {file.Properties.BitsPerSample}bit {(file.Properties.AudioChannels == 1 ? "Mono" : "Stereo")}";
        return format;
    }
    internal static int GetSoundSampleCount(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        int sampleCount = (int) Math.Round(file.Properties.AudioSampleRate * file.Properties.Duration.TotalSeconds);
        return sampleCount;
    }
}