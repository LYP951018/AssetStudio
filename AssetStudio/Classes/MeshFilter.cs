using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class MeshFilter
    {
        public long preloadIndex;
        public PPtr m_GameObject = new PPtr();
        public PPtr m_Mesh = new PPtr();

        public MeshFilter(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            var sourceFile = preloadData.sourceFile;
            var reader = preloadData.InitReader();

            m_GameObject = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            m_Mesh = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
        }
    }
}
