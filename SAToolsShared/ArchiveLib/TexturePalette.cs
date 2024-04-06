﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArchiveLib
{
    public class TexturePalette
    {
        public enum PalettedTextureFormat
        {
            NotIndexed = 0,
            Index4 = 1,
            Index8 = 2
        }

        const uint Magic_PVP = 0x4C505650; // PVPL
        const uint Magic_GVP = 0x4C505647; // GVPL

        public bool IsGVP { get; set; }
        public bool IsSACompatible { get; set; }
        public PalettePixelCodec PixelCodec { get; set; }
        public short StartBank { get; set; }
        public short StartColor { get; set; }
        public List<Color> Colors { get; set; }

        public enum PalettePixelCodec : short
        {
            ARGB1555 = 0, // Probably Intensity8 in other Gamecube games
            RGB565 = 1,
            ARGB4444 = 2, // RGB5A3 in other Gamecube games
            ARGB8888 = 6,
            Intensity8A = 8,
            RGB5A3 = 9
        }

        public TexturePalette(byte[] palettedata, bool saCompatible = true)
        {
            bool bigendbk = ByteConverter.BigEndian;
            IsGVP = BitConverter.ToUInt32(palettedata, 0) == Magic_GVP;
            ByteConverter.BigEndian = IsGVP;
            PixelCodec = (PalettePixelCodec)ByteConverter.ToInt16(palettedata, 0x8);

            // SADX/SA2 formats
            if (!saCompatible)
                switch (PixelCodec)
                {
                    case PalettePixelCodec.ARGB1555:
                        PixelCodec = PalettePixelCodec.Intensity8A;
                        break;
                    case PalettePixelCodec.ARGB4444:
                        PixelCodec = PalettePixelCodec.RGB5A3;
                        break;
                    default:
                        break;
                }
            StartBank = ByteConverter.ToInt16(palettedata, 0xA);
            StartColor = ByteConverter.ToInt16(palettedata, 0xC);
            short numColors = ByteConverter.ToInt16(palettedata, 0xE);
            Colors = new List<Color>();
            for (int i = 0; i < numColors; i++)
            {
                int id = (i * (PixelCodec == PalettePixelCodec.ARGB8888 ? 4 : 2)) + 16;
                byte[] colorb = GetEncodedColorData(palettedata, id, IsGVP, PixelCodec);
                Color result = DecodeColor(colorb, PixelCodec);
                Colors.Add(result);
            }
            ByteConverter.BigEndian = bigendbk;
        }

        public TexturePalette(Bitmap bitmap, bool saCompatible = true)
        {
            IsGVP = false;
            /*
            System.Windows.Forms.DialogResult rd = System.Windows.Forms.MessageBox.Show("Import as ARGB8888?", "Texture Editor", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
            if (rd == System.Windows.Forms.DialogResult.No)
            {
                int tlevel = TextureFunctions.GetAlphaLevelFromBitmap(bitmap);
                switch (tlevel)
                {
                    // No transparency
                    case 0:
                        PixelCodec = PalettePixelCodec.RGB565;
                        break;
                    // 1 bit transparency
                    case 1:
                        if (saCompatible)
                            PixelCodec = PalettePixelCodec.ARGB1555;
                        else
                            PixelCodec = PalettePixelCodec.RGB5A3;
                        break;
                    // Full transparency
                    case 2:
                        if (saCompatible)
                            PixelCodec = PalettePixelCodec.ARGB4444;
                        else
                            PixelCodec = PalettePixelCodec.RGB5A3;
                        break;
                }
            }
            else*/
            PixelCodec = PalettePixelCodec.ARGB8888;
            Colors = new List<Color>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                    if (PixelCodec == PalettePixelCodec.ARGB8888) // This is done separately because ARGB888 is encoded in a different order in PVP/GVP
                        Colors.Add(bitmap.GetPixel(x, y));
                else
                    Colors.Add(DecodeColor(EncodeColor(bitmap.GetPixel(x, y), PixelCodec), PixelCodec));
            }
        }

        public TexturePalette(bool gvp = false, PalettePixelCodec codec = PalettePixelCodec.RGB565)
        {
            StartBank = 0;
            StartColor = 0;
            PixelCodec = codec;
            IsGVP = gvp;
            Colors = new List<Color>();
            int numcolor = 256;
            int red;
            int blue;
            int green;
            int gencolor = 0;
            for (int y = 0; y < 16; y++)
            {
                if (gencolor >= numcolor)
                    break;
                for (int x = 0; x < 16; x++)
                {
                    if (gencolor >= numcolor)
                        break;
                    int colorindex = x + 16 * y;
                    green = colorindex;
                    red = x * 16;
                    blue = y * 16;
                    Color rgb8 = Color.FromArgb(red, green, blue);
                    Color result = DecodeColor(EncodeColor(rgb8, PixelCodec), PixelCodec);
                    Colors.Add(result);
                    gencolor++;
                }
            }
        }

        public byte[] GetBytes()
        {
            bool bigendianbk = ByteConverter.BigEndian;
            ByteConverter.BigEndian = IsGVP;
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(IsGVP ? Magic_GVP : Magic_PVP));
            int sizenoheader = Colors.Count * (PixelCodec == PalettePixelCodec.ARGB8888 ? 4 : 2) + 8; // Size without header
            result.AddRange(BitConverter.GetBytes((uint)sizenoheader));
            // Endianness varies from here
            // Intensity8A (0) is ARGB1555 is SADX/SA2 and RGB5A3 (2) is ARGB4444 is SADX/SA2
            PalettePixelCodec PixelCodec_real = PixelCodec;
            if (PixelCodec == PalettePixelCodec.Intensity8A)
                PixelCodec_real = PalettePixelCodec.ARGB1555;
            else if (PixelCodec == PalettePixelCodec.RGB5A3)
                PixelCodec_real = PalettePixelCodec.ARGB4444;
            result.AddRange(ByteConverter.GetBytes((ushort)PixelCodec_real));
            result.AddRange(ByteConverter.GetBytes(StartBank));
            result.AddRange(ByteConverter.GetBytes(StartColor));
            result.AddRange(ByteConverter.GetBytes((ushort)Colors.Count));
            foreach (Color color in Colors)
            {
                byte[] encodedColor = EncodeColor(color, PixelCodec);
                if (PixelCodec == PalettePixelCodec.ARGB8888)
                {
                    uint value = BitConverter.ToUInt32(encodedColor, 0); // Little Endian
                    result.AddRange(ByteConverter.GetBytes(value)); // May be converted to Big Endian
                }
                else
                {
                    ushort value = BitConverter.ToUInt16(encodedColor, 0); // Little Endian
                    result.AddRange(ByteConverter.GetBytes(value)); // May be converted to Big Endian
                }
            }
            ByteConverter.BigEndian = bigendianbk;
            return result.ToArray();
        }

        public Bitmap GetBitmap()
        {
            Bitmap result = new Bitmap(Colors.Count, 1);
            for (int x = 0; x < result.Width; x++)
            {
                result.SetPixel(x, 0, Colors[x]);
            }
            return result;
        }

        public byte[] EncodeColor(Color color, PalettePixelCodec PixelCodec)
        {
            byte[] result = new byte[PixelCodec == PalettePixelCodec.ARGB8888 ? 4 : 2];
            ushort pixel = 0x0000;
            switch (PixelCodec)
            {
                case PalettePixelCodec.ARGB1555:
                    pixel |= (ushort)((color.A >> 7) << 15);
                    pixel |= (ushort)((color.R >> 3) << 10);
                    pixel |= (ushort)((color.G >> 3) << 5);
                    pixel |= (ushort)((color.B >> 3) << 0);
                    result[1] = (byte)((pixel >> 8) & 0xFF);
                    result[0] = (byte)(pixel & 0xFF);
                    break;
                case PalettePixelCodec.ARGB4444:
                    pixel |= (ushort)((color.A >> 4) << 12);
                    pixel |= (ushort)((color.R >> 4) << 8);
                    pixel |= (ushort)((color.G >> 4) << 4);
                    pixel |= (ushort)((color.B >> 4) << 0);
                    result[1] = (byte)((pixel >> 8) & 0xFF);
                    result[0] = (byte)(pixel & 0xFF);
                    break;
                case PalettePixelCodec.ARGB8888:
                    result[3] = color.A;
                    result[2] = color.R;
                    result[1] = color.G;
                    result[0] = color.B;
                    break;
                case PalettePixelCodec.Intensity8A:
                    result[1] = color.A;
                    result[0] = (byte)((0.30 * color.R) + (0.59 * color.G) + (0.11 * color.B));
                    break;
                case PalettePixelCodec.RGB5A3:
                    if (color.A <= 0xDA) // Argb3444
                    {
                        pixel |= (ushort)((color.A >> 5) << 12);
                        pixel |= (ushort)((color.R >> 4) << 8);
                        pixel |= (ushort)((color.G >> 4) << 4);
                        pixel |= (ushort)((color.B >> 4) << 0);
                    }
                    else // Rgb555
                    {
                        pixel |= 0x8000;
                        pixel |= (ushort)((color.R >> 3) << 10);
                        pixel |= (ushort)((color.G >> 3) << 5);
                        pixel |= (ushort)((color.B >> 3) << 0);
                    }
                    result[1] = (byte)(pixel & 0xFF);
                    result[0] = (byte)((pixel >> 8) & 0xFF);
                    break;
                case PalettePixelCodec.RGB565:
                default:
                    pixel |= (ushort)((color.R >> 3) << 11);
                    pixel |= (ushort)((color.G >> 2) << 5);
                    pixel |= (ushort)((color.B >> 3) << 0);
                    result[1] = (byte)((pixel >> 8) & 0xFF);
                    result[0] = (byte)(pixel & 0xFF);
                    break;
            }
            return result;
        }

        public Color DecodeColor(byte[] rgb, PalettePixelCodec codec)
        {
            int a = 255;
            int r;
            int g;
            int b;
            switch (codec)
            {
                case PalettePixelCodec.ARGB1555:
                    a = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 15) & 0x01) * 0xFF);
                    r = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 10) & 0x1F) * 0xFF / 0x1F);
                    g = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 5) & 0x1F) * 0xFF / 0x1F);
                    b = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 0) & 0x1F) * 0xFF / 0x1F);
                    break;
                case PalettePixelCodec.ARGB4444:
                    a = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 12) & 0x0F) * 0xFF / 0x0F);
                    r = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 8) & 0x0F) * 0xFF / 0x0F);
                    g = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 4) & 0x0F) * 0xFF / 0x0F);
                    b = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 0) & 0x0F) * 0xFF / 0x0F);
                    break;
                case PalettePixelCodec.RGB5A3:
                    ushort pixel = BitConverter.ToUInt16(rgb, 0);
                    if ((pixel & 0x8000) != 0) // RGB555
                    {
                        a = 255;
                        r = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 10) & 0x1F) * 0xFF / 0x1F);
                        g = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 5) & 0x1F) * 0xFF / 0x1F);
                        b = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 0) & 0x1F) * 0xFF / 0x1F);
                    }
                    else // ARGB34444
                    {
                        a = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 12) & 0x07) * 0xFF / 0x07);
                        r = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 8) & 0x0F) * 0xFF / 0x0F);
                        g = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 4) & 0x0F) * 0xFF / 0x0F);
                        b = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 0) & 0x0F) * 0xFF / 0x0F);
                    }
                    break;
                case PalettePixelCodec.Intensity8A:
                    a = rgb[0];
                    r = rgb[1];
                    g = rgb[1];
                    b = rgb[1];
                    break;
                case PalettePixelCodec.ARGB8888:
                    a = rgb[0];
                    r = rgb[1];
                    g = rgb[2];
                    b = rgb[3];
                    break;
                case PalettePixelCodec.RGB565:
                default:
                    r = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 11) & 0x1F) * 0xFF / 0x1F);
                    g = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 5) & 0x3F) * 0xFF / 0x3F);
                    b = (byte)(((BitConverter.ToUInt16(rgb, 0) >> 0) & 0x1F) * 0xFF / 0x1F);
                    break;
            }
            return Color.FromArgb(a, r, g, b);
        }

        public byte[] GetEncodedColorData(byte[] color, int index, bool bigEndian, PalettePixelCodec PixelCodec)
        {
            byte[] data = new byte[PixelCodec == PalettePixelCodec.ARGB8888 ? 4 : 2];
            switch (PixelCodec)
            {
                case PalettePixelCodec.ARGB8888:
                    if (bigEndian)
                    {
                        data[0] = color[index];
                        data[1] = color[index + 1];
                        data[2] = color[index + 2];
                        data[3] = color[index + 3];
                    }
                    else
                    {
                        data[0] = color[index + 3];
                        data[1] = color[index + 2];
                        data[2] = color[index + 1];
                        data[3] = color[index];
                    }
                    return data;
                case PalettePixelCodec.ARGB1555:
                case PalettePixelCodec.ARGB4444:
                case PalettePixelCodec.RGB565:
                default:
                    if (bigEndian)
                    {
                        data[0] = color[index + 1];
                        data[1] = color[index];
                    }
                    else
                    {
                        data[0] = color[index];
                        data[1] = color[index + 1];
                    }
                    return data;
            }
        }

		public int GetMaxBanks(bool index8)
		{
			int colr = Colors.Count;
			return (index8 == true) ? colr / 256 : colr / 16;
		}
    }
}