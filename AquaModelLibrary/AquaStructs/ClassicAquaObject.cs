﻿using System.Collections.Generic;
using System.Numerics;

namespace AquaModelLibrary
{
    //Though the NIFL format is used for storage, VTBF format tag references for data will be commented where appropriate. Some offset/reserve related things are NIFL only, however.
    public unsafe class ClassicAquaObject : AquaObject
    {
        public enum VertFlags : int
        {
            VertPosition = 0x0,
            VertWeight = 0x1,
            VertNormal = 0x2,
            VertColor = 0x3,
            VertColor2 = 0x4,
            VertWeightIndex = 0xb,
            VertUV1 = 0x10,
            VertUV2 = 0x11,
            VertUV3 = 0x12,
            VertUV4 = 0x13,
            VertTangent = 0x20,
            VertBinormal = 0x21
        }

        public static Dictionary<int, int> VertDataTypes = new Dictionary<int, int>()
        {
            { (int)VertFlags.VertPosition, 0x3 }, //(0x0 Vertex Position) 
            { (int)VertFlags.VertWeight, 0x4 }, //(0x1 Vertex Weights)
            { (int)VertFlags.VertNormal, 0x3 }, //(0x2 Vertex Normal)
            { (int)VertFlags.VertColor, 0x5 }, //(0x3 Vertex Color)
            { (int)VertFlags.VertColor2, 0x5 }, //(0x4 Vertex Color 2?)
            { (int)VertFlags.VertWeightIndex, 0x7 }, //(0xb Weight Index)
            { (int)VertFlags.VertUV1, 0x2 }, //(0x10 UV1 Buffer)
            { (int)VertFlags.VertUV2, 0x2 }, //(0x11 UV2 Buffer)
            { (int)VertFlags.VertUV3, 0x2 }, //(0x12 UV3 Buffer)
            { (int)VertFlags.VertUV4, 0x2 }, //(0x12 UV4 Buffer)
            { (int)VertFlags.VertTangent, 0x3 }, //(0x20 Tangents)
            { (int)VertFlags.VertBinormal, 0x3 } //(0x21 Binormals)
        };

        public override AquaObject getShallowCopy()
        {
            ClassicAquaObject aqp = new ClassicAquaObject();
            aqp.afp = afp;
            aqp.objc = objc;
            aqp.vsetList = vsetList;
            aqp.vtxeList = vtxeList;
            aqp.vtxlList = vtxlList;
            aqp.psetList = psetList;
            aqp.meshList = meshList;
            aqp.mateList = mateList;
            aqp.rendList = rendList;
            aqp.shadList = shadList;
            aqp.tstaList = tstaList;
            aqp.tsetList = tsetList;
            aqp.texfList = texfList;
            aqp.unrms = unrms;
            aqp.strips = strips;

            //*** 0xC33 only
            aqp.bonePalette = bonePalette;

            //Unclear the purpose of these, but when present they have a smaller count than initial mesh and psets. 
            aqp.unkStruct1List = unkStruct1List;
            aqp.mesh2List = mesh2List;
            aqp.pset2List = pset2List;
            aqp.strips2 = strips2; //Strip set 2 is from the same array as the first, just split differently, potentially.

            aqp.strips3Lengths = strips3Lengths;
            aqp.strips3 = strips3;
            aqp.unkPointArray1 = unkPointArray1; //Noooooooo idea what these are. Count matches the strips3Lengths count
            aqp.unkPointArray2 = unkPointArray2;
            //***

            aqp.applyNormalAveraging = applyNormalAveraging;

            //Custom model related data
            aqp.tempTris = tempTris;
            aqp.tempMats = tempMats;

            return aqp;
        }
    }
}
