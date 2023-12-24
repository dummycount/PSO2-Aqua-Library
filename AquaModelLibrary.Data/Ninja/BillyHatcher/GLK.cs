﻿using Reloaded.Memory.Streams;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AquaModelLibrary.Extra.Ninja.BillyHatcher
{
    public class GLK
    {
        public List<GLKEntry> entries = new List<GLKEntry>();
        public List<byte[]> files = new List<byte[]>();
        public GLK() { }
        public GLK(BufferedStreamReader sr)
        {
            sr._BEReadActive = true;
            NinjaHeader header = sr.Read<NinjaHeader>();
            byte unkByte0 = sr.Read<byte>();
            byte unkByte1 = sr.Read<byte>();
            ushort fileCount = sr.ReadBE<ushort>();
            ushort unkSht = sr.ReadBE<ushort>();

            for(int i = 0; i < fileCount; i++)
            {
                GLKEntry entry = new GLKEntry();
                entry.fileName = AquaMethods.AquaGeneralMethods.ReadCString(sr, 0x1C);
                sr.Seek(0x1C, System.IO.SeekOrigin.Current);
                entry.unk0 = sr.ReadBE<ushort>();
                entry.unk1 = sr.ReadBE<ushort>();
                entry.unk2 = sr.ReadBE<ushort>();
                entries.Add(entry);
            }

            sr.Seek(header.fileSize + 8, System.IO.SeekOrigin.Begin);
            for(int i = 0; i < fileCount; i++)
            {
                var magic = sr.Peek<int>();
                if(magic == 0x484D5647)
                {
                    files.Add(GVMUtil.ReadGVMBytes(sr));
                } else
                {
                    var fileHeader = sr.Read<NinjaHeader>();

                    //Some filenames got broken due to the 0x1B character limit, but we can restore these based on the magic
                    switch(fileHeader.magic)
                    {
                        case 0x4D434A47: //GJCM
                            entries[i].fileName = Path.ChangeExtension(entries[i].fileName, ".gj");
                            break;
                        case 0x4C544A47: //GJTL
                            entries[i].fileName = Path.ChangeExtension(entries[i].fileName, ".gjt");
                            break;
                        case 0x4D444D4E: //NMDM
                            entries[i].fileName = Path.ChangeExtension(entries[i].fileName, ".njm");
                            break;
                    }
                    var bytes = new List<byte>();
                    bytes.AddRange(sr.ReadBytes(sr.Position() - 0x8, fileHeader.fileSize + 0x8));
                    sr.Seek(fileHeader.fileSize, System.IO.SeekOrigin.Current);

                    //Handle POF0
                    if(sr.Peek<int>() == 0x30464F50)
                    {
                        fileHeader = sr.Read<NinjaHeader>();
                        bytes.AddRange(sr.ReadBytes(sr.Position() - 0x8, fileHeader.fileSize + 0x8));
                        sr.Seek(fileHeader.fileSize, System.IO.SeekOrigin.Current);
                    }
                    files.Add(bytes.ToArray());
                }
            }
        }

        public class GLKEntry
        {
            public string fileName; //0x1C bytes with null terminator
            public ushort unk0;
            public ushort unk1;
            public ushort unk2;
        }
    }
}
