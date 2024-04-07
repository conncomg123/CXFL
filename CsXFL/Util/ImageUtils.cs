using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CsXFL
{
    public static class ImageUtils
    {
        public static byte[] ConvertRawImageToDat(string pngFilePath)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(pngFilePath);
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            // Write the header to the .dat file
            // based on https://github.com/jtothebell/Silenus/blob/master/trunk/silenus/src/com/silenistudios/silenus/dat/DatPNGReader.java
            bw.Write((short)0x0503); // 0
            bw.Write((short)0x0000); // 2
            bw.Write((short)image.Width); // 4
            bw.Write((short)image.Height); // 6
            bw.Write(0); // 8
            bw.Write(image.Width * 20); // 12
            bw.Write(0); // 16
            bw.Write(image.Height * 20); // 20
            bw.Write((byte)0x01); // 24
            bw.Write((byte)0); // 25

            // Iterate over each pixel
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the ARGB value of the pixel
                    Rgba32 pixel = image[x, y];

                    // Write the ARGB components to the .dat file
                    bw.Write(pixel.A);
                    bw.Write(pixel.R);
                    bw.Write(pixel.G);
                    bw.Write(pixel.B);
                }
            }

            // Return the .dat file data as a byte array
            byte[] array = ms.ToArray();
            return array;
        }
    }
}