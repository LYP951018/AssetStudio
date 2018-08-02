using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{

    class AssetBundle
    {
        public class AssetInfo
        {
            public int preloadIndex;
            public int preloadSize;
            public PPtr asset;
        }

        public class ContainerData
        {
            public string first;
            public AssetInfo second;
        }


        public List<ContainerData> m_Container = new List<ContainerData>();

        public AssetBundle(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            var sourceFile = preloadData.sourceFile;
            var reader = preloadData.InitReader();

            var m_Name = reader.ReadAlignedString();
            var size = reader.ReadInt32();
            for (int i = 0; i < size; i++)
            {
                sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
            }
            size = reader.ReadInt32();
            for (int i = 0; i < size; i++)
            {
                var temp = new ContainerData();
                temp.first = reader.ReadAlignedString();
                temp.second = new AssetInfo();
                temp.second.preloadIndex = reader.ReadInt32();
                temp.second.preloadSize = reader.ReadInt32();
                temp.second.asset = sourceFile.ReadPPtr(sharedFileIndex, assetsfileList);
                m_Container.Add(temp);
            }
        }
    }
}
