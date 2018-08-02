using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    class Animation
    {
        public PPtr m_GameObject;
        public List<PPtr> m_Animations;

        public Animation(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            var sourceFile = preloadData.sourceFile;
            var reader = preloadData.InitReader();
            reader.Position = preloadData.Offset;

            m_GameObject = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            var m_Enabled = reader.ReadByte();
            reader.AlignStream(4);
            var m_Animation = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            int numAnimations = reader.ReadInt32();
            m_Animations = new List<PPtr>(numAnimations);
            for (int i = 0; i < numAnimations; i++)
            {
                m_Animations.Add(sourceFile.ReadPPtr(sharedFileIndex, assetsfileList));
            }
        }
    }
}
