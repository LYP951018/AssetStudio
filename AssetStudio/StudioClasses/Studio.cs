using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AssetStudio
{
    public static class Studio
    {
        //public static  //loaded files
        //public static Dictionary<string, int> sharedFileIndex = new Dictionary<string, int>(); //to improve the loading speed
        //public static Dictionary<string, EndianBinaryReader> resourceFileReaders = new Dictionary<string, EndianBinaryReader>(); //use for read res files
        //public static List<AssetPreloadData> exportableAssets = new List<AssetPreloadData>(); //used to hold all assets while the ListView is filtered
        //private static HashSet<string> assetsNameHash = new HashSet<string>(); //avoid the same name asset
        //public static List<AssetPreloadData> visibleAssets = new List<AssetPreloadData>(); //used to build the ListView from all or filtered assets
        //public static Dictionary<string, SortedDictionary<int, ClassStruct>> AllClassStructures = new Dictionary<string, SortedDictionary<int, ClassStruct>>();
        //public static string path;
        //public static string productName = "";

        //UI
        public static Action<int> SetProgressBarValue = Empty;
        public static Action<int> SetProgressBarMaximum = Empty;
        public static Action ProgressBarPerformStep = Empty;
        public static Action<string> StatusStripUpdate = Empty;
        public static Action<int> ProgressBarMaximumAdd = Empty;

        private static void Empty(int x) { }
        private static void Empty() { }
        private static void Empty(string x) { }

        public enum FileType
        {
            AssetsFile,
            BundleFile,
            WebFile
        }

        public static FileType CheckFileType(Stream stream, out EndianBinaryReader reader)
        {
            reader = new EndianBinaryReader(stream);
            return CheckFileType(reader);
        }

        public static FileType CheckFileType(string fileName, out EndianBinaryReader reader)
        {
            reader = new EndianBinaryReader(File.OpenRead(fileName));
            return CheckFileType(reader);
        }

        private static FileType CheckFileType(EndianBinaryReader reader)
        {
            var signature = reader.ReadStringToNull();
            reader.Position = 0;
            switch (signature)
            {
                case "UnityWeb":
                case "UnityRaw":
                case "\xFA\xFA\xFA\xFA\xFA\xFA\xFA\xFA":
                case "UnityFS":
                    return FileType.BundleFile;
                case "UnityWebData1.0":
                    return FileType.WebFile;
                default:
                    {
                        var magic = reader.ReadBytes(2);
                        reader.Position = 0;
                        if (WebFile.gzipMagic.SequenceEqual(magic))
                        {
                            return FileType.WebFile;
                        }
                        reader.Position = 0x20;
                        magic = reader.ReadBytes(6);
                        reader.Position = 0;
                        if (WebFile.brotliMagic.SequenceEqual(magic))
                        {
                            return FileType.WebFile;
                        }
                        return FileType.AssetsFile;
                    }
            }
        }

        public static void ExtractFile(string[] fileNames)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                int extractedCount = 0;
                foreach (var fileName in fileNames)
                {
                    var type = CheckFileType(fileName, out var reader);
                    if (type == FileType.BundleFile)
                        extractedCount += ExtractBundleFile(fileName, reader);
                    else if (type == FileType.WebFile)
                        extractedCount += ExtractWebDataFile(fileName, reader);
                    else
                        reader.Dispose();
                    ProgressBarPerformStep();
                }
                StatusStripUpdate($"Finished extracting {extractedCount} files.");
            });
        }

        private static int ExtractBundleFile(string bundleFileName, EndianBinaryReader reader)
        {
            StatusStripUpdate($"Decompressing {Path.GetFileName(bundleFileName)} ...");
            var bundleFile = new BundleFile(reader, bundleFileName);
            reader.Dispose();
            if (bundleFile.fileList.Count > 0)
            {
                var extractPath = bundleFileName + "_unpacked\\";
                Directory.CreateDirectory(extractPath);
                return ExtractStreamFile(extractPath, bundleFile.fileList);
            }
            return 0;
        }

        private static int ExtractWebDataFile(string webFileName, EndianBinaryReader reader)
        {
            StatusStripUpdate($"Decompressing {Path.GetFileName(webFileName)} ...");
            var webFile = new WebFile(reader);
            reader.Dispose();
            if (webFile.fileList.Count > 0)
            {
                var extractPath = webFileName + "_unpacked\\";
                Directory.CreateDirectory(extractPath);
                return ExtractStreamFile(extractPath, webFile.fileList);
            }
            return 0;
        }

        private static int ExtractStreamFile(string extractPath, List<StreamFile> fileList)
        {
            int extractedCount = 0;
            foreach (var file in fileList)
            {
                var filePath = extractPath + file.fileName;
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }
                if (!File.Exists(filePath) && file.stream is MemoryStream stream)
                {
                    File.WriteAllBytes(filePath, stream.ToArray());
                    extractedCount += 1;
                }
                file.stream.Dispose();
            }
            return extractedCount;
        }

        public static List<AssetPreloadData> BuildAssetStructures(bool loadAssets, bool displayAll, bool buildHierarchy, 
            bool buildClassStructures, bool displayOriginalName, out List<GameObject> fileNodes, Dictionary<string, int> sharedFileIndex, List<AssetsFile> assetsfileList)
        {
            fileNodes = null;
            List<AssetPreloadData> exportableAssets = new List<AssetPreloadData>();
            List<AssetPreloadData> visibleAssets = new List<AssetPreloadData>();
            HashSet<string> assetsNameHash = new HashSet<string>();
            string productName;
            Dictionary<string, EndianBinaryReader> resourceFileReaders = new Dictionary<string, EndianBinaryReader>();
            #region first loop - read asset data & create list
            if (loadAssets)
            {
                SetProgressBarValue(0);
                SetProgressBarMaximum(assetsfileList.Sum(x => x.preloadTable.Values.Count));
                StatusStripUpdate("Building asset list...");

                string fileIDfmt = "D" + assetsfileList.Count.ToString().Length;

                for (var i = 0; i < assetsfileList.Count; i++)
                {
                    var assetsFile = assetsfileList[i];

                    string fileID = i.ToString(fileIDfmt);
                    AssetBundle ab = null;
                    foreach (var asset in assetsFile.preloadTable.Values)
                    {
                        asset.uniqueID = fileID + asset.uniqueID;
                        var exportable = false;
                        switch (asset.Type)
                        {
                            case ClassIDReference.GameObject:
                                {
                                    GameObject m_GameObject = new GameObject(asset, sharedFileIndex, assetsfileList);
                                    assetsFile.GameObjectList.Add(asset.m_PathID, m_GameObject);
                                    break;
                                }
                            case ClassIDReference.Transform:
                                {
                                    Transform m_Transform = new Transform(asset, sharedFileIndex, assetsfileList);
                                    assetsFile.TransformList.Add(asset.m_PathID, m_Transform);
                                    break;
                                }
                            case ClassIDReference.RectTransform:
                                {
                                    RectTransform m_Rect = new RectTransform(asset, sharedFileIndex, assetsfileList);
                                    assetsFile.TransformList.Add(asset.m_PathID, m_Rect.m_Transform);
                                    break;
                                }
                            case ClassIDReference.Texture2D:
                                {
                                    Texture2D m_Texture2D = new Texture2D(asset, false, resourceFileReaders);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.Shader:
                                {
                                    Shader m_Shader = new Shader(asset, false);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.TextAsset:
                                {
                                    TextAsset m_TextAsset = new TextAsset(asset, false);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.AudioClip:
                                {
                                    AudioClip m_AudioClip = new AudioClip(asset, false, resourceFileReaders);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.MonoBehaviour:
                                {
                                    var m_MonoBehaviour = new MonoBehaviour(asset, false, sharedFileIndex, assetsfileList);
                                    if (asset.Type1 != asset.Type2 && assetsFile.ClassStructures.ContainsKey(asset.Type1))
                                        exportable = true;
                                    break;
                                }
                            case ClassIDReference.Font:
                                {
                                    UFont m_Font = new UFont(asset, false, sharedFileIndex, assetsfileList);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.PlayerSettings:
                                {
                                    var plSet = new PlayerSettings(asset);
                                    productName = plSet.productName;
                                    break;
                                }
                            case ClassIDReference.Mesh:
                                {
                                    Mesh m_Mesh = new Mesh(asset, false, sharedFileIndex, assetsfileList);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.AssetBundle:
                                {
                                    ab = new AssetBundle(asset, sharedFileIndex, assetsfileList);
                                    break;
                                }
                            case ClassIDReference.VideoClip:
                                {
                                    var m_VideoClip = new VideoClip(asset, false, resourceFileReaders);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.MovieTexture:
                                {
                                    var m_MovieTexture = new MovieTexture(asset, false, sharedFileIndex, assetsfileList);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.Sprite:
                                {
                                    var m_Sprite = new Sprite(asset, false, sharedFileIndex, assetsfileList);
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.Animator:
                                {
                                    exportable = true;
                                    break;
                                }
                            case ClassIDReference.AnimationClip:
                                {
                                    exportable = true;
                                    var reader = asset.sourceFile.reader;
                                    reader.Position = asset.Offset;
                                    asset.FullName = reader.ReadAlignedString();
                                    break;
                                }
                        }
                        if (string.IsNullOrEmpty(asset.FullName))
                        {
                            asset.FullName = asset.TypeString + " #" + asset.uniqueID;
                        }
                        asset.SubItems.AddRange(new[] { asset.TypeString, asset.fullSize.ToString() });
                        //处理同名文件
                        if (!assetsNameHash.Add((asset.TypeString + asset.FullName).ToUpper()))
                        {
                            asset.FullName += " #" + asset.uniqueID;
                        }
                        //处理非法文件名
                        asset.FullName = FixFileName(asset.FullName);
                        if (displayAll)
                        {
                            exportable = true;
                        }
                        if (exportable)
                        {
                            assetsFile.exportableAssets.Add(asset);
                        }
                        ProgressBarPerformStep();
                    }
                    if (displayOriginalName)
                    {
                        foreach (var asset in assetsFile.exportableAssets)
                        {
                            var replacename = ab?.m_Container.Find(y => y.second.asset.m_PathID == asset.m_PathID)?.first;
                            if (!string.IsNullOrEmpty(replacename))
                            {
                                var ex = Path.GetExtension(replacename);
                                asset.FullName = !string.IsNullOrEmpty(ex) ? replacename.Replace(ex, "") : replacename;
                            }
                        }
                    }
                    exportableAssets.AddRange(assetsFile.exportableAssets);
                }

                visibleAssets = exportableAssets;
                assetsNameHash.Clear();
            }
            #endregion

            #region second loop - build tree structure
            if (buildHierarchy)
            {
                fileNodes = new List<GameObject>();
                var gameObjectCount = assetsfileList.Sum(x => x.GameObjectList.Values.Count);
                if (gameObjectCount > 0)
                {
                    SetProgressBarValue(0);
                    SetProgressBarMaximum(gameObjectCount);
                    StatusStripUpdate("Building tree structure...");

                    foreach (var assetsFile in assetsfileList)
                    {
                        GameObject fileNode = new GameObject(null, sharedFileIndex, assetsfileList);
                        fileNode.Text = Path.GetFileName(assetsFile.filePath);
                        fileNode.m_Name = "RootNode";

                        foreach (var m_GameObject in assetsFile.GameObjectList.Values)
                        {
                            foreach (var m_Component in m_GameObject.m_Components)
                            {
                                if (m_Component.m_FileID >= 0 && m_Component.m_FileID < assetsfileList.Count)
                                {
                                    var sourceFile = assetsfileList[m_Component.m_FileID];
                                    if (sourceFile.preloadTable.TryGetValue(m_Component.m_PathID, out var asset))
                                    {
                                        switch (asset.Type)
                                        {
                                            case ClassIDReference.Transform:
                                                {
                                                    m_GameObject.m_Transform = m_Component;
                                                    break;
                                                }
                                            case ClassIDReference.MeshRenderer:
                                                {
                                                    m_GameObject.m_MeshRenderer = m_Component;
                                                    break;
                                                }
                                            case ClassIDReference.MeshFilter:
                                                {
                                                    m_GameObject.m_MeshFilter = m_Component;
                                                    if (assetsfileList.TryGetPD(m_Component, out var assetPreloadData))
                                                    {
                                                        var m_MeshFilter = new MeshFilter(assetPreloadData, sharedFileIndex, assetsfileList);
                                                        if (assetsfileList.TryGetPD(m_MeshFilter.m_Mesh, out assetPreloadData))
                                                        {
                                                            assetPreloadData.gameObject = m_GameObject;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case ClassIDReference.SkinnedMeshRenderer:
                                                {
                                                    m_GameObject.m_SkinnedMeshRenderer = m_Component;
                                                    if (assetsfileList.TryGetPD(m_Component, out var assetPreloadData))
                                                    {
                                                        var m_SkinnedMeshRenderer = new SkinnedMeshRenderer(assetPreloadData, sharedFileIndex, assetsfileList);
                                                        if (assetsfileList.TryGetPD(m_SkinnedMeshRenderer.m_Mesh, out assetPreloadData))
                                                        {
                                                            assetPreloadData.gameObject = m_GameObject;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case ClassIDReference.Animator:
                                                {
                                                    m_GameObject.m_Animator = m_Component;
                                                    asset.FullName = m_GameObject.asset.FullName;
                                                    break;
                                                }
                                        }
                                    }
                                }
                            }

                            var parentNode = fileNode;

                            if (assetsfileList.TryGetTransform(m_GameObject.m_Transform, out var m_Transform))
                            {
                                if (assetsfileList.TryGetTransform(m_Transform.m_Father, out var m_Father))
                                {
                                    if (assetsfileList.TryGetGameObject(m_Father.m_GameObject, out parentNode))
                                    {
                                    }
                                }
                            }

                            parentNode.Nodes.Add(m_GameObject);
                            ProgressBarPerformStep();
                        }

                        if (fileNode.Nodes.Count > 0)
                        {
                            fileNodes.Add(fileNode);
                        }
                    }
                }
            }
            #endregion

            #region build list of class strucutres
            if (buildClassStructures)
            {
                Dictionary<string, SortedDictionary<int, ClassStruct>> AllClassStructures = new Dictionary<string, SortedDictionary<int, ClassStruct>>();
                //group class structures by versionv
                foreach (var assetsFile in assetsfileList)
                {
                    if (AllClassStructures.TryGetValue(assetsFile.m_Version, out var curVer))
                    {
                        foreach (var uClass in assetsFile.ClassStructures)
                        {
                            curVer[uClass.Key] = uClass.Value;
                        }
                    }
                    else
                    {
                        AllClassStructures.Add(assetsFile.m_Version, assetsFile.ClassStructures);
                    }
                }
            }
            #endregion
            return visibleAssets.ToList();

        }

        public static string FixFileName(string str)
        {
            if (str.Length >= 260) return Path.GetRandomFileName();
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }

        public static string[] ProcessingSplitFiles(List<string> selectFile)
        {
            var splitFiles = selectFile.Where(x => x.Contains(".split"))
                .Select(x => Path.GetDirectoryName(x) + "\\" + Path.GetFileNameWithoutExtension(x))
                .Distinct()
                .ToList();
            selectFile.RemoveAll(x => x.Contains(".split"));
            foreach (var file in splitFiles)
            {
                if (File.Exists(file))
                {
                    selectFile.Add(file);
                }
            }
            return selectFile.Distinct().ToArray();
        }

        public static List<AssetPreloadData> AnalysisResourceFolder(string path)
        {
            Importer.MergeSplitAssets(path);
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList();
            var readFile = ProcessingSplitFiles(files);
            var importFiles = new List<string>();
            var importFilesHash = new HashSet<string>();
            List<AssetsFile> assetsfileList = new List<AssetsFile>();
            Dictionary<string, EndianBinaryReader> resourceFileReaders = new Dictionary<string, EndianBinaryReader>();
            foreach (var i in readFile)
            {
                importFiles.Add(i);
                importFilesHash.Add(Path.GetFileName(i));
            }
            SetProgressBarValue(0);
            SetProgressBarMaximum(importFiles.Count);
            //use a for loop because list size can change
            for (int f = 0; f < importFiles.Count; f++)
            {
                Importer.LoadFile(importFiles[f], resourceFileReaders, assetsfileList);
                ProgressBarPerformStep();
            }
            importFilesHash.Clear();
            Dictionary<string, int> sharedFileIndex = new Dictionary<string, int>();
            return Studio.BuildAssetStructures(true, displayAll: true, buildHierarchy: false, 
                buildClassStructures: false, true, out var nodes, sharedFileIndex, assetsfileList);
        }
    }
}
