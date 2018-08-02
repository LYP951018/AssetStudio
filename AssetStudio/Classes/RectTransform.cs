using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class RectTransform
    {
        public Transform m_Transform;

        public RectTransform(AssetPreloadData preloadData, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            m_Transform = new Transform(preloadData, sharedFileIndex, assetsfileList);

            //var sourceFile = preloadData.sourceFile;
            //var a_Stream = preloadData.sourceFile.a_Stream;

            /*
            float[2] AnchorsMin
            float[2] AnchorsMax
            float[2] Pivod
            float Width
            float Height
            float[2] ?
            */

        }
    }
}
