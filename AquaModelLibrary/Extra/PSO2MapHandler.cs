﻿using AquaModelLibrary.AquaMethods;
using AquaModelLibrary.Native.Fbx;
using Pfim;
using Reloaded.Memory.Streams;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Zamboni;
using static AquaModelLibrary.AquaMethods.AquaGeneralMethods;
using static AquaModelLibrary.AquaMethods.AquaNodeParsingMethods;
using static AquaModelLibrary.CharacterMakingIndexMethods;
using static AquaModelLibrary.LHIObjectDetailLayout;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace AquaModelLibrary.Extra
{
    public static class PSO2MapHandler
    {
        public static bool pngMode = false;
        public class SetNode
        {
            public Vector3 position = new Vector3(0, 0, 0);
            public Vector3 eulerRotation = new Vector3(0, 0, 0);
            public Vector3 scale = new Vector3(1, 1, 1);
        }

        public class LHIData
        {
            public Dictionary<string, AquaObject> aqos = new Dictionary<string, AquaObject>();
            public Dictionary<string, AquaNode> aqns = new Dictionary<string, AquaNode>();
            public Dictionary<string, byte[]> ddsFiles = new Dictionary<string, byte[]>();
            public Dictionary<string, byte[]> lhiArrays = new Dictionary<string, byte[]>();
        }

        public static void DumpNGSMapData(string binPath, string outFolder, int id)
        {
            Dictionary<string, LHIData> lhiData = new Dictionary<string, LHIData>();
            binPath = Path.Combine(binPath, "data\\win32reboot\\");

            //Set
            string setIce = $"set/ls_{id:D4}_set.ice";
            DumpFromIce(binPath, Path.Combine(outFolder, "SetObjects"), GetIcePath(setIce, binPath));

            //Detail Objects and Layouts - might be the same number as above, but has way more slots
            for (int i = 0; i < 10000; i++)
            {
                string detModelIce = $"stage/sn_{id:D4}/instancing/ln_{id:D4}_instancing_{i:D4}.ice";
                DumpFromIce(binPath, Path.Combine(outFolder, i.ToString("D2") + "_lhi"), GetIcePath(detModelIce, binPath), null, lhiData);
            }

            foreach(var lhiPair in lhiData)
            {
                var lhiPath = Path.Combine(outFolder, "LhiDetails", lhiPair.Key);
                Directory.CreateDirectory(lhiPath);

                //Models
                foreach(var mdlName in lhiPair.Value.aqos.Keys)
                {
                    FbxExporter.ExportToFile(lhiPair.Value.aqos[mdlName], lhiPair.Value.aqns[mdlName], new List<AquaMotion>(), Path.Combine(lhiPath, mdlName + ".fbx"), new List<string>(), false);
                }

                //Textures
                foreach(var texPair in lhiPair.Value.ddsFiles)
                {
                    if(pngMode)
                    {
                        WritePng(lhiPath, texPair.Key, texPair.Value);
                    } else
                    {
                        File.WriteAllBytes(Path.Combine(lhiPath, texPair.Key), texPair.Value);
                    }
                }

                //LHI data
                foreach(var lhi in lhiPair.Value.lhiArrays)
                {
                    File.WriteAllBytes(Path.Combine(lhiPath, lhi.Key + "_lhi.bin"), lhi.Value);
                }

                lhiPair.Value.aqos = null;
                lhiPair.Value.aqns = null;
                lhiPair.Value.ddsFiles = null;
                lhiPair.Value.lhiArrays = null;
            }
            var keys = lhiData.Keys.ToArray();
            foreach(var key in keys)
            {
                lhiData[key] = null;
            }
            
            //Terrain
            for (int i = 0; i < 100; i++)
            {
                //High detail model
                string hdModelIce = $"stage/sn_{id:D4}/ln_{id:D4}_f0_{i:D2}.ice";
                DumpFromIce(binPath, Path.Combine(outFolder, i.ToString("D2")), GetIcePath(hdModelIce, binPath));

                //Collision
                string colModelIce = $"stage/sn_{id:D4}/ln_{id:D4}_f0_{i:D2}_col.ice";
                DumpFromIce(binPath, Path.Combine(outFolder, i.ToString("D2")), GetIcePath(colModelIce, binPath));

                //TXL - Not sure why this is here when there's one usually IN the model files already, but whatever sega
                string txlModelIce = $"stage/sn_{id:D4}/ln_{id:D4}_f0_{i:D2}_txl.ice";
                DumpFromIce(binPath, Path.Combine(outFolder, i.ToString("D2")), GetIcePath(txlModelIce, binPath));
            }

            //Common: Ususally for out of bounds stuff
            string commonModelIce = $"stage/sn_{id:D4}/ln_{id:D4}_common.ice";
            DumpFromIce(binPath, Path.Combine(outFolder, "common"), GetIcePath(commonModelIce, binPath));

            //Skybox
            string weatherIce = $"stage/weather/ln_{id:4}_wtr.ice";
            DumpFromIce(binPath, Path.Combine(outFolder, "skybox"), GetIcePath(weatherIce, binPath));

            //Moons
            if (id == 5000 || id == 5001)
            {
                string moonIce = $"object/map_object/ob_5000_0642.ice";
                DumpFromIce(binPath, Path.Combine(outFolder, "skybox"), GetIcePath(moonIce, binPath));
            }
            
        }

        public static string GetIcePath(string unhashedIce, string binPath)
        {
            string iceHash = GetRebootHash(GetFileHash(unhashedIce));
            string icePath = Path.Combine(binPath, iceHash);

            return icePath;
        }


        public static void DumpFromIce(string binPath, string outFolder, string hdModelPath, List<string> ddsList = null, Dictionary<string, LHIData> lhiData = null)
        {
            if (File.Exists(hdModelPath))
            {
                if(lhiData == null)
                {
                    Directory.CreateDirectory(outFolder);
                }
                var file = File.ReadAllBytes(hdModelPath);
                var ice = IceFile.LoadIceFile(new MemoryStream(file));
                List<byte[]> iceFiles = new List<byte[]>();
                iceFiles.AddRange(ice.groupOneFiles);
                iceFiles.AddRange(ice.groupTwoFiles);
                Dictionary<string, AquaObject> models = new Dictionary<string, AquaObject>();
                Dictionary<string, AquaNode> bones = new Dictionary<string, AquaNode>();
                Dictionary<string, List<SetNode>> setObjects = new Dictionary<string, List<SetNode>>();
                string modelName;

                foreach (var data in iceFiles)
                {
                    var fname = IceFile.getFileName(data);
                    var ext = Path.GetExtension(fname);
                    switch (ext)
                    {
                        case ".dds":
                            if (ddsList == null || ddsList.Contains(fname))
                            {
                                var ddsData = AquaGeneralMethods.RemoveIceEnvelope(data);
                                if(lhiData != null)
                                {
                                    if(!lhiData[outFolder].ddsFiles.ContainsKey(fname))
                                    {
                                        lhiData[outFolder].ddsFiles.Add(fname, ddsData);
                                    }
                                    break;
                                }
                                if (pngMode)
                                {
                                    WritePng(outFolder, fname, ddsData);
                                }
                                else
                                {
                                    File.WriteAllBytes(Path.Combine(outFolder, fname + ".dds"), ddsData);
                                }
                            }
                            break;
                        case ".lhi":
                            if(lhiData == null)
                            {
                                break;
                            }
                            var lhi = AquaUtil.LoadLHI(ext, data);
                            var numArr = Path.GetFileNameWithoutExtension(fname).Split('_');
                            var num = numArr[numArr.Length - 1];
                            for (int i = 0; i < lhi.detailInfoList.Count; i++)
                            {
                                var info = lhi.detailInfoList[i];
                                var lhFilename = "object/map_object/" + info.objName + "_l1.ice";
                                var lhColFilename = "object/map_object/" + info.objName + "_col.ice";

                                if(!lhiData.ContainsKey(info.objName))
                                {
                                    lhiData.Add(info.objName, new LHIData());
                                }
                                DumpFromIce(binPath, info.objName, GetIcePath(lhFilename, binPath), null, lhiData);
                                DumpFromIce(binPath, info.objName, GetIcePath(lhColFilename, binPath), null, lhiData);
                                if (!lhiData[info.objName].lhiArrays.ContainsKey(num))
                                {
                                    lhiData[info.objName].lhiArrays.Add(num, WriteMatrixData(lhi.detailInfoList[i]));
                                }
                            }
                            break;
                        case ".set":
                            if (lhiData != null)
                            {
                                break;
                            }
                            SetLayout set;
                            using (Stream stream = (Stream)new MemoryStream(AquaGeneralMethods.RemoveIceEnvelope(data)))
                            using (var streamReader = new BufferedStreamReader(stream, 8192))
                            {
                                set = AquaUtil.LoadSet(fname, stream.Length, streamReader);
                            }
                            for (int st = 0; st < set.setEntities.Count; st++)
                            {
                                var entity = set.setEntities[st];
                                if (entity.variables.ContainsKey("object_name"))
                                {
                                    string name = (string)entity.variables["object_name"];
                                    SetNode node = new SetNode();

                                    if (entity.variables.ContainsKey("position_x"))
                                    {
                                        node.position.X = (float)entity.variables["position_x"];
                                    }
                                    if (entity.variables.ContainsKey("position_y"))
                                    {
                                        node.position.Y = (float)entity.variables["position_y"];
                                    }
                                    if (entity.variables.ContainsKey("position_z"))
                                    {
                                        node.position.Z = (float)entity.variables["position_z"];
                                    }
                                    if (entity.variables.ContainsKey("rotation_x"))
                                    {
                                        node.eulerRotation.X = (float)entity.variables["rotation_x"];
                                    }
                                    if (entity.variables.ContainsKey("rotation_y"))
                                    {
                                        node.eulerRotation.X = (float)entity.variables["rotation_y"];
                                    }
                                    if (entity.variables.ContainsKey("rotation_z"))
                                    {
                                        node.eulerRotation.X = (float)entity.variables["rotation_z"];
                                    }
                                    if (entity.variables.ContainsKey("scale"))
                                    {
                                        float singleScale = (float)entity.variables["scale"];
                                        node.scale = new Vector3(singleScale, singleScale, singleScale);
                                    }
                                    if (entity.variables.ContainsKey("scale_x"))
                                    {
                                        node.scale.X = (float)entity.variables["scale_x"];
                                    }
                                    if (entity.variables.ContainsKey("scale_y"))
                                    {
                                        node.scale.Y = (float)entity.variables["scale_y"];
                                    }
                                    if (entity.variables.ContainsKey("scale_z"))
                                    {
                                        node.scale.Z = (float)entity.variables["scale_z"];
                                    }

                                    if (setObjects.ContainsKey(name))
                                    {
                                        setObjects[name].Add(node);
                                    } else
                                    {
                                        setObjects.Add(name, new List<SetNode>() { node });
                                    }
                                }
                            }
                            break;
                        case ".prm":
                            AquaUtil prmutil = new AquaUtil();
                            prmutil.LoadPRM(data);
                            prmutil.ConvertPRMToAquaObject();
                            models.Add(Path.GetFileNameWithoutExtension(fname) + "_prm", prmutil.aquaModels[0].models[0]);
                            break;
                        case ".tcb":
                            AquaUtil tcbutil = new AquaUtil();
                            tcbutil.ReadCollision(data);
                            tcbutil.ConvertTCBToAquaObject();
                            models.Add(Path.GetFileNameWithoutExtension(fname) + "_tcb", tcbutil.aquaModels[0].models[0]);
                            break;
                        case ".txl":
                            var txl = AquaUtil.LoadTXL(".txl", data);
                            for (int ic = 0; ic < txl.iceList.Count; ic++)
                            {
                                DumpFromIce(binPath, outFolder, GetIcePath(txl.iceList[ic], binPath), txl.texList, lhiData);
                            }
                            break;
                        case ".aqp":
                            modelName = Path.GetFileNameWithoutExtension(fname) + "_aqp";
                            if (!ModelExists(lhiData, outFolder, modelName))
                            {
                                models.Add(modelName, AquaObjectParsingMethods.ReadAQOModel(new BufferedStreamReader(new MemoryStream(data), 8192))[0]);
                            }
                            break;
                        case ".aqn":
                            modelName = Path.GetFileNameWithoutExtension(fname) + "_aqp";
                            if (!ModelExists(lhiData, outFolder, modelName))
                            {
                                bones.Add(modelName, ReadAquaBones(new BufferedStreamReader(new MemoryStream(data), 8192)));
                            }
                            break;
                        case ".trp":
                            modelName = Path.GetFileNameWithoutExtension(fname) + "_trp";
                            if (!ModelExists(lhiData, outFolder, modelName))
                            {
                                models.Add(modelName, AquaObjectParsingMethods.ReadAQOModel(new BufferedStreamReader(new MemoryStream(data), 8192))[0]);
                            }
                            break;
                        case ".trn":
                            modelName = Path.GetFileNameWithoutExtension(fname) + "_trp";
                            if (!ModelExists(lhiData, outFolder, modelName))
                            {
                                bones.Add(modelName, ReadAquaBones(new BufferedStreamReader(new MemoryStream(data), 8192)));
                            }
                            break;
                        case ".trm":
                            //Maybe one day. Usually these do literally nothing anyways, despite their frequency
                            break;
                    }
                }

                foreach (var model in models.Keys)
                {
                    if (models[model].objc.type > 0xC32)
                    {
                        models[model].splitVSETPerMesh();
                    }
                    if (!bones.ContainsKey(model))
                    {
                        bones.Add(model, AquaNode.GenerateBasicAQN());
                    }
                    if(pngMode)
                    {
                        models[model].changeTexExtension("png");
                    }

                    if(lhiData == null)
                    {
                        FbxExporter.ExportToFile(models[model], bones[model], new List<AquaMotion>(), Path.Combine(outFolder, model + ".fbx"), new List<string>(), false);
                    } else
                    {
                        if(!lhiData[outFolder].aqos.ContainsKey(model))
                        {
                            lhiData[outFolder].aqos.Add(model, models[model]);
                            lhiData[outFolder].aqns.Add(model, bones[model]);
                        }
                    }
                }
                foreach (var obj in setObjects)
                {
                    var objFilename = Path.Combine(binPath, "object/map_object", obj.Key + "_l1.ice");
                    var objColFilename = Path.Combine(binPath, "object/map_object", obj.Key + "_col.ice");
                    DumpFromIce(binPath, outFolder, GetIcePath(objFilename, binPath));
                    DumpFromIce(binPath, outFolder, GetIcePath(objColFilename, binPath));

                    List<byte> setData = new List<byte>();
                    setData.AddRange(BitConverter.GetBytes(obj.Value.Count));
                    foreach(var tfm in obj.Value)
                    {
                        setData.AddRange(AquaGeneralMethods.ConvertStruct(tfm.position));
                        setData.AddRange(AquaGeneralMethods.ConvertStruct(tfm.eulerRotation));
                        setData.AddRange(AquaGeneralMethods.ConvertStruct(tfm.scale));
                    }
                    File.WriteAllBytes(Path.Combine(outFolder, obj.Key + "_setData.bin"), setData.ToArray());
                }
            }
        }

        private static bool ModelExists(Dictionary<string, LHIData> lhiData, string lhiObj, string name)
        {
            if(lhiData == null)
            {
                return false;
            }
            return lhiData[lhiObj].aqos.ContainsKey(name);
        }
        private static bool AqnExists(Dictionary<string, LHIData> lhiData, string lhiObj, string name)
        {
            if (lhiData == null)
            {
                return false;
            }
            return lhiData[lhiObj].aqns.ContainsKey(name);
        }

        private static void WritePng(string outFolder, string fname, byte[] ddsData)
        {
            var pngName = Path.Combine(outFolder, fname + ".png");
            using (var image = Pfimage.FromStream(new MemoryStream(ddsData)))
            {
                var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                try
                {
                    var imgdata = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                    var bitmap = new Bitmap(image.Width, image.Height, image.Stride, PixelFormat.Format32bppArgb, imgdata);
                    bitmap.Save(pngName, ImageFormat.Png);
                }
                catch
                {
                    //Delete the failed write
                    if(File.Exists(pngName))
                    {
                        File.Delete(pngName);
                    }
                    File.WriteAllBytes(Path.Combine(outFolder, fname), ddsData);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        public static byte[] WriteMatrixData(DetailInfoObject info)
        {
            List<byte> outBytes = new List<byte>();
            outBytes.AddRange(BitConverter.GetBytes(info.matrices.Count));
            outBytes.AddRange(AquaGeneralMethods.ConvertStruct(info.diStruct.BoundingMin));
            outBytes.AddRange(AquaGeneralMethods.ConvertStruct(info.diStruct.BoundingMax));
            outBytes.AddRange(BitConverter.GetBytes((int)0)); //Alignment

            for(int i = 0; i < info.matrices.Count; i++)
            {
                var mat = info.matrices[i];
                outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M11)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M12)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M13)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M14));
                outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M21)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M22)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M23)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M24));
                outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M31)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M32)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M33)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M34));
                outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M41)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M42)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M43)); outBytes.AddRange(AquaGeneralMethods.ConvertStruct(mat.M44));
            }

            return outBytes.ToArray();
        }
    }
}
