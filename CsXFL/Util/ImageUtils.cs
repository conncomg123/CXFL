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
        public static Image<Rgba32> ConvertDatToRawImage(byte[] datImage)
        {
            using MemoryStream ms = new MemoryStream(datImage);
            using BinaryReader br = new BinaryReader(ms);

            // Read the header
            short header1 = br.ReadInt16(); // 0
            short header2 = br.ReadInt16(); // 2
            short width = br.ReadInt16(); // 4
            short height = br.ReadInt16(); // 6
            int unknown1 = br.ReadInt32(); // 8
            int unknown2 = br.ReadInt32(); // 12
            int unknown3 = br.ReadInt32(); // 16
            int unknown4 = br.ReadInt32(); // 20
            byte unknown5 = br.ReadByte(); // 24
            byte unknown6 = br.ReadByte(); // 25

            // Create a new image with the specified width and height
            using Image<Rgba32> image = new Image<Rgba32>(width, height);

            // Iterate over each pixel and set the pixel color in the image
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = br.ReadByte();
                    byte r = br.ReadByte();
                    byte g = br.ReadByte();
                    byte b = br.ReadByte();

                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }
            return image;
        }
    }
}