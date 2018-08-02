﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class SkinnedMeshRenderer : MeshRenderer
    {
        public PPtr m_Mesh;
        public PPtr[] m_Bones;
        public List<float> m_BlendShapeWeights;

        public SkinnedMeshRenderer(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            var sourceFile = preloadData.sourceFile;
            var version = sourceFile.version;
            var reader = preloadData.InitReader();

            m_GameObject = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            if (version[0] < 5)
            {
                var m_Enabled = reader.ReadBoolean();
                var m_CastShadows = reader.ReadByte();
                var m_ReceiveShadows = reader.ReadBoolean();
                var m_LightmapIndex = reader.ReadByte();
            }
            else
            {
                var m_Enabled = reader.ReadBoolean();
                reader.AlignStream(4);
                var m_CastShadows = reader.ReadByte();
                var m_ReceiveShadows = reader.ReadBoolean();
                reader.AlignStream(4);
                if (version[0] >= 2018)//2018 and up
                {
                    var m_RenderingLayerMask = reader.ReadUInt32();
                }
                var m_LightmapIndex = reader.ReadUInt16();
                var m_LightmapIndexDynamic = reader.ReadUInt16();
            }

            if (version[0] >= 3)
            {
                reader.Position += 16;//Vector4f m_LightmapTilingOffset
            }

            if (version[0] >= 5)
            {
                reader.Position += 16;//Vector4f m_LightmapTilingOffsetDynamic
            }

            m_Materials = new PPtr[reader.ReadInt32()];
            for (int m = 0; m < m_Materials.Length; m++)
            {
                m_Materials[m] = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            }

            if (version[0] < 3)
            {
                reader.Position += 16;//m_LightmapTilingOffset vector4d
            }
            else
            {
                if ((sourceFile.version[0] == 5 && sourceFile.version[1] >= 5) || sourceFile.version[0] > 5)//5.5.0 and up
                {
                    m_StaticBatchInfo = new StaticBatchInfo
                    {
                        firstSubMesh = reader.ReadUInt16(),
                        subMeshCount = reader.ReadUInt16()
                    };
                }
                else
                {
                    int numSubsetIndices = reader.ReadInt32();
                    m_SubsetIndices = reader.ReadUInt32Array(numSubsetIndices);
                }

                var m_StaticBatchRoot = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);

                if ((sourceFile.version[0] == 5 && sourceFile.version[1] >= 4) || sourceFile.version[0] > 5)//5.4.0 and up
                {
                    var m_ProbeAnchor = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
                    var m_LightProbeVolumeOverride = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
                }
                else if (version[0] >= 4 || (version[0] == 3 && version[1] >= 5))//3.5 - 5.3
                {
                    var m_UseLightProbes = reader.ReadBoolean();
                    reader.AlignStream(4);
                    if (version[0] == 5)//5.0 and up
                    {
                        int m_ReflectionProbeUsage = reader.ReadInt32();
                    }
                    var m_LightProbeAnchor = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
                }

                if (version[0] >= 5 || (version[0] == 4 && version[1] >= 3))//4.3 and up
                {
                    if (version[0] == 4 && version[1] == 3)//4.3
                    {
                        int m_SortingLayer = reader.ReadInt16();
                    }
                    else
                    {
                        int m_SortingLayerID = reader.ReadInt32();
                        //SInt16 m_SortingOrder 5.6 and up
                    }

                    int m_SortingOrder = reader.ReadInt16();
                    reader.AlignStream(4);
                }
            }

            int m_Quality = reader.ReadInt32();
            var m_UpdateWhenOffscreen = reader.ReadBoolean();
            var m_SkinNormals = reader.ReadBoolean(); //3.1.0 and below
            reader.AlignStream(4);

            if (version[0] == 2 && version[1] < 6)//2.6 down
            {
                var m_DisableAnimationWhenOffscreen = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            }

            m_Mesh = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);

            m_Bones = new PPtr[reader.ReadInt32()];
            for (int b = 0; b < m_Bones.Length; b++)
            {
                m_Bones[b] = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            }

            if (version[0] < 3)
            {
                int m_BindPose = reader.ReadInt32();
                reader.Position += m_BindPose * 16 * 4;//Matrix4x4f
            }
            else
            {
                if (version[0] > 4 || (version[0] == 4 && version[1] >= 3))//4.3 and up
                {
                    int numBSWeights = reader.ReadInt32();
                    m_BlendShapeWeights = new List<float>(numBSWeights);
                    for (int i = 0; i < numBSWeights; i++)
                    {
                        m_BlendShapeWeights.Add(reader.ReadSingle());
                    }
                }
            }
        }
    }
}
