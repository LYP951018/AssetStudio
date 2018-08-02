using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    class Animator
    {
        public PPtr m_GameObject;
        public PPtr m_Avatar;
        public PPtr m_Controller;

        public Animator(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            var sourceFile = preloadData.sourceFile;
            var reader = preloadData.InitReader();
            reader.Position = preloadData.Offset;

            m_GameObject = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            var m_Enabled = reader.ReadByte();
            reader.AlignStream(4);
            m_Avatar = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            m_Controller = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
        }
    }
}
