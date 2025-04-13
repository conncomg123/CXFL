using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace CsXFL
{
    public static class ImageUtils
    {
        // based on https://github.com/jtothebell/Silenus/blob/master/trunk/silenus/src/com/silenistudios/silenus/dat/DatPNGReader.java
        public static byte[] ConvertRawImageToDat(string pngFilePath)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(pngFilePath);
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            // Write the header to the .dat file
            
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
            byte compressed = br.ReadByte(); // 25
            // Check if the image is compressed
            if (compressed == 0x01)
            {
                return ParseCompressed(datImage, width, height, br);
            }
            else
            {
                return ParseUncompressed(datImage, width, height, br);
            }

        }
        private static Image<Rgba32> ParseUncompressed(byte[] datImage, short width, short height, BinaryReader br)
        {
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
        private static Image<Rgba32> ParseCompressed(byte[] datImage, short width, short height, BinaryReader br)
        {
            /*
            // inflater
		Inflater decompresser = new Inflater();
		
		// read length of compressed chunk
		short length = stream.readShort();
		
		// buffer
		byte[] buffer = new byte[BufferSize];
		
		// keep going until we reach the end
		while (length > 0) {
			
			// read the compressed data into a buffer
			byte[] input = new byte[length];
			stream.read(input, 0, length);
			
			// decompress
			decompresser.setInput(input, 0, length);
			int n = decompresser.inflate(buffer,  0, BufferSize);
			while (n != 0) {
				outStream.write(buffer, 0, n);
				n = decompresser.inflate(buffer,  0, BufferSize);
			}
			
			// next piece
			length = stream.readShort();
		}
		
		// save output stream
		outStream.flush();
        }
        */

            Inflater inflater = new Inflater();
            using PNGOutputStream pngData = new PNGOutputStream(new MemoryStream(), width, height);
            short length = br.ReadInt16();
            byte[] buffer = new byte[512];
            while (length > 0)
            {
                // read the compressed data into a buffer
                byte[] input = new byte[length];
                br.Read(input, 0, length);
                // decompress
                inflater.SetInput(input, 0, length);
                int n = inflater.Inflate(buffer, 0, buffer.Length);
                while (n != 0)
                {
                    // write to output stream
                    pngData.Write(buffer, 0, n);
                    n = inflater.Inflate(buffer, 0, buffer.Length);
                }
                // next piece
                length = br.ReadInt16();
            }
            pngData.Flush();
            // decompressed data doesn't header so we need to add it manually
            using Image<Rgba32> image = Image.Load<Rgba32>(((MemoryStream)pngData._outputStream).ToArray());
            return image;
        }

        public class PNGOutputStream : Stream
        {
            private readonly Image<Rgba32> _image;
            private readonly Rgba32[] _pixelArray;
            private readonly int _width;
            private readonly int _height;
            private int _x = 0;
            private int _y = 0;
            private int _bytesRead = 0;
            private byte _red, _green, _blue, _alpha;
            public readonly Stream _outputStream;

            public PNGOutputStream(Stream outputStream, int width, int height)
            {
                _width = width;
                _height = height;
                _outputStream = outputStream;
                _image = new Image<Rgba32>(width, height);
                _pixelArray = new Rgba32[width * height];
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    WriteByte(buffer[i]);
                }
            }

            public override void WriteByte(byte value)
            {
                // Set data
                switch (_bytesRead)
                {
                    case 0: _alpha = value; break;
                    case 1: _red = value; break;
                    case 2: _green = value; break;
                    case 3: _blue = value; break;
                }
                _bytesRead++;

                // Flush byte if fully loaded
                if (_bytesRead == 4)
                {
                    _bytesRead = 0;
                    _pixelArray[_y * _width + _x] = new Rgba32(_red, _green, _blue, _alpha);

                    _x++;
                    if (_x == _width)
                    {
                        _x = 0;
                        _y++;
                    }
                }
            }

            public override void Flush()
            {
                // Write the image to the output stream
                _image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < _height; y++)
                    {
                        Span<Rgba32> row = accessor.GetRowSpan(y);
                        for (int x = 0; x < _width; x++)
                        {
                            row[x] = _pixelArray[y * _width + x];
                        }
                    }
                });

                _image.Save(_outputStream, new PngEncoder());
                _outputStream.Flush();
            }

            public override void Close()
            {
                Flush();
                _image.Dispose();
                base.Close();
            }

            // Required overrides for Stream
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}