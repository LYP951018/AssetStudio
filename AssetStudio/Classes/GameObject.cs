using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AssetStudio
{
    public class GameObject
    {
        public AssetPreloadData asset;
        public List<PPtr> m_Components;
        public int m_Layer;
        public string m_Name;
        public ushort m_Tag;
        public bool m_IsActive;
        public string Text { get; set; }
        public List<GameObject> Nodes { get; set; } = new List<GameObject>();
        public GameObject Parent { get; set; } 
        public string Name { get; set; }

        public string uniqueID = "0";//this way file and folder TreeNodes will be treated as FBX scene

        public PPtr m_Transform;
        public PPtr m_MeshRenderer;
        public PPtr m_MeshFilter;
        public PPtr m_SkinnedMeshRenderer;
        public PPtr m_Animator;

        public GameObject(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            if (preloadData != null)
            {
                asset = preloadData;
                var sourceFile = preloadData.sourceFile;
                var reader = preloadData.InitReader();

                uniqueID = preloadData.uniqueID;

                int m_Component_size = reader.ReadInt32();
                m_Components = new List<PPtr>(m_Component_size);
                for (int j = 0; j < m_Component_size; j++)
                {
                    if ((sourceFile.version[0] == 5 && sourceFile.version[1] >= 5) || sourceFile.version[0] > 5)//5.5.0 and up
                    {
                        m_Components.Add(sourceFile.ReadPPtr(sharedFileIndex, assetsfileList));
                    }
                    else
                    {
                        int first = reader.ReadInt32();
                        m_Components.Add(sourceFile.ReadPPtr(sharedFileIndex, assetsfileList));
                    }
                }

                m_Layer = reader.ReadInt32();
                m_Name = reader.ReadAlignedString();
                if (m_Name == "") { m_Name = "GameObject #" + uniqueID; }
                m_Tag = reader.ReadUInt16();
                m_IsActive = reader.ReadBoolean();

                Text = m_Name;
                preloadData.FullName = m_Name;
                //name should be unique
                Name = uniqueID;
            }
        }
    }
}
