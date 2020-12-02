﻿using System.Collections.Generic;
using System.Numerics;
using static AquaModelLibrary.AquaCommon;

namespace AquaModelLibrary
{
    //Though the NIFL format is used for storage, VTBF format tag references for data will be commented where appropriate. Some offset/reserve related things are NIFL only, however.

    //Cameras, UV, and standard motions are essentially the same format.
    public unsafe class AquaMotion
    {
        public const int CAMO = 0x4F4D4143; //Camera animation
        public const int SPMO = 0x4F4D5053; //UV animation
        public const int NDMO = 0x4F4D444E; //Motion animation
        public const int stdAnim = 0x10002;
        public const int stdPlayerAnim = 0x10012;
        public const int cameraAnim = 0x10004;
        public const int materialAnim = 0x20;
        public Dictionary<int, string> keyTypeNames = new Dictionary<int, string>() 
        {
            {0x1, "0x1 Position" },
            {0x2, "0x2 Rotation" },
            {0x3, "0x3 Scale" },
            {0x4, "0x4 Unk Floats" },
            //Are 0x5-0x13 reserved for effects? Effects seem to use values in this range for equivalent area there. 
            //In addition, the way those are constructed is akin to an alternative or early version of the overall motion format, based on namings and other bits.  
            {0x14, "0x14 Unk Floats" },
            {0x15, "0x15 Unk Floats" },
            {0x16, "0x16 Unk Floats" },
            {0x17, "0x17 Unk Floats" },
            {0x18, "0x18 Unk Floats" },
            {0x19, "0x19 Unk Floats" },
            {0x1A, "0x1A Unk Floats" },
            {0x1B, "0x1B Unk Floats" },
            {0x1C, "0x1C Unk Floats" },
            {0x1D, "0x1D Unk Floats" }
        };
        public AquaPackage.AFPBase afp;
        public NIFL nifl;
        public REL0 rel0;
        public MOheader moHeader;
        public List<KeyData> motionKeys;
        public NOF0 nof0;
        public NEND nend;

        //MO Header
        public struct MOheader
        {
            public int variant; //0xE0, type 0x9 //Seems to be different pending type of anim. 0x10002 standard anims, 0x10012 player anims, 0x10004 camera, 0x20 texture anims
            public int loopPoint; //0xE1, type 0x9 //Loop point? Only seen in live dance aqms and lower than anim range. Otherwise, 0.
            public int endFrame; //0xE2, type 0x9 //Last frame of the animation (0 based)
            public float frameSpeed; //0xE3, type 0xA //FPS of the animation. PSO2's default is 30.0f
            public int unkInt0;      //0xE4, type 0x9 //Always 0x4 for VTBF, always 0x2 for NIFL anims
            public int nodeCount;    //0xE5, type 0x9 //Number of animated nodes (effect type nodes not included) plus one for NodeTreeFlag stuff if a player anim.
            public int boneTableOffset; //NIFL only, always 0x50. Points to start of the bone name table in NIFL anims
            public PSO2String testString; //0xE6, type 0x2 //Always a string of just "test". Technically a standard 0x20 length PSO2 string, but always observed as "test"
            public int reserve0;
        }

        //Motion segment. Denotes a node's animations. Camera files will only have one of these. 
        public unsafe struct MSEG
        {
            public int nodeType; //0xE7, type 0x9 
            public int nodeDataCount; //0xE8, type 0x9 
            public int nodeOffset;
            public PSO2String boneName; //0xE9, type 0x2  
            public int nodeId; //0xEA, type 0x9           //0 on material entries
        }

        //Motion Key
        public class MKEY
        {
            public int keyType;  //0xEB, type 0x9 
            public int dataType; //0xEC, type 0x9
            public int unkInt0;  //0xF0, type 0x9
            public int keyCount; //0xED, type 0x9
            public int frameAddress;
            public int timeAddress;
            public List<Vector4> vector4Keys = new List<Vector4>(); //0xEE, type 0x4A or 0xCA if multiple
            public List<ushort> frameTimings = new List<ushort>(); //0xEF, type 0x06 or 0x86 if multiple //Frame timings start with 0 + 0x1 to represent the first frame.
                                                                                //Subsequent frames are multiplied by 0x10 and the final frame will have 0x2 added.
            public List<float> floatKeys = new List<float>(); //0xF1, type 0xA or 0x8A if multiple
                                          //0xF2. Theoretical. 
            public List<int> intKeys = new List<int>(); //0xF3, type 0x8 or 0x88 if multiple
        }

        public class KeyData
        {
            public MSEG mseg = new MSEG();
            public List<MKEY> keyData = new List<MKEY>();
            //Typically, keyData is generally stored this way, but technically doesn't have to follow this convention:

            //Player/Standard animation data
            //Pos, Rot, Scale data

            //Camera animation Data - Unlike most animation data, only seems to ever contain one node. Seemingly just for fixed cameras. 
            //Pos, unk, unk, unk
            
            //Texture/UV anim data - Seems to contain many types of data, though somewhat untested
            //Seemingly 8 data sets. 

            //Node Tree Flag - Special subsec tion of data for player animations with an unknown purpose. Not necessary to include, but can be filled
            //with somewhat valid data if the user wishes
            //Pos, Rot data
        }

    }
}
