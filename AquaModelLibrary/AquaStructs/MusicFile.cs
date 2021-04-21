﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AquaModelLibrary.AquaCommon;

namespace AquaModelLibrary.AquaStructs
{
    //These are the .mus files used by the Sympathy system. The format seems to match what SEGA described overall:
    //https://www.bumped.org/psublog/phantasy-star-online-2s-dynamic-music-system-sympathy/
    public class MusicFile
    {
        public bool isAlpha = false;
        public NIFL nifl;
        public REL0 rel0;

        public NOF0 nof0;
        public NEND nend;

        public struct musHeader
        {
            public int unkStruct0Offset;

            public byte unkByte0;
            public byte unkStruct0Count;
            public byte unkByte1;
            public byte unkByte2;
        }

        public struct unkStruct0            //Sympathy Part?
        {
            public int unkStruct1Offset;
            public int unkStruct2Offset;

            public byte subStruct1Count; //In the alpha version, this is used for both structs seemingly
            public byte id;
            public byte subStruct2Count;
            public byte unkByte2;
        }

        public struct unkStruct1            //Sympathy Movement?
        {
            public int unkOffset;

            public byte unkByte0;
            public byte id;
            public byte unkByte1;
            public byte unkByte2;

            public int unkStringAddress;

            public int unkInt0;
            public int unkInt1;
            public int unkInt2;
            public int unkInt3;

            public int unkInt4;
            public int unkInt5;
            public int unkInt6;
            public int unkInt7;
        }

        public struct unkStruct2
        {
            public int categoryStringOffset;

            public byte unkByte0;
            public byte unkByte1;
            public byte unkByte2;
            public byte unkByte3;

            public int unkInt0;
            public int unkInt1;
            public int unkInt2;
        }

        //Alpha versions of these were different
        public struct unkStruct1Alpha
        {
            public int categoryStringOffset;
        }

        public struct unkStruct2Alpha                  //Sympathy Movement?
        {
            public int unkStruct3Offset;

            public byte unkStruct3Count; //Maybe?
            public byte id;
            public byte unkByte1;
            public byte unkByte2;
        }

        public struct unkStruct3Alpha
        {
            public int unkStruct4Offset;
            public int unkInt0;
        }

        public struct unkStruct4Alpha
        {
            public int unkStruct5Offset;
            public int unkInt0;
        }

        public struct unkStruct5Alpha
        {
            public int unkStruct6Offset;
            public int unkInt0;
        }

        public struct unkStruct6Alpha
        {
            public int phraseOffset;
            public int phrase_2Offset;
            public int unkInt0;
            public int unkInt1;

            public int unkInt2;
        }

        public struct phraseAlpha
        {
            public int unkStruct9Offset;
            public float unkFloat0;
            public byte unkByte0;
            public byte unkByte1;
            public byte unkByte2;
            public byte unkByte3;
        }

        public struct unkStruct9Alpha
        {
            public int unkStruct10Offset;
            public int unkInt0;
        }

        public struct unkStruct10Alpha
        {
            public int clipInfoOffset;
        }

        public struct clipInfoAlpha
        {
            public int  clipStringOffsetOffset; 
            public int unkFloat0;
            public byte unkByte0;
            public byte unkByte1;
            public byte unkByte2;
            public byte unkByte3;
        }

        //There's no outright tell on these which version they'll be, but the alpha versions always seem to lay the first struct after the second... for some reason
        public void AlphaCheck(unkStruct0 unk)
        {
            if(unk.unkStruct1Offset > unk.unkStruct2Offset)
            {
                isAlpha = true;
            } else
            {
                isAlpha = false;
            }
        }
    }
}
