using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ObjectWeightScale
{
    public static class ObjectWeightScale
    {
        [MenuItem("GameObject/WeightScale")]
        public static void Run()
        {
            AssetDatabase.Refresh();

            foreach (var target in Selection.gameObjects)
            {
                var dependencies = EditorUtility
                    .CollectDependencies(new Object[] { target })
                    .Select(x => Tuple.Create(x, AssetDatabase.GetAssetPath(x)))
                    .GroupBy(x => x.Item2)
                    .Select(x => x.First())
                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2));

                var parsed = new List<SizeData>();

                foreach (var (asset, assetPath) in dependencies)
                {
                    if (assetPath == "Resources/unity_builtin_extra") continue;

                    var size = GetCompressedFileSize(assetPath);
                    if (size == null)
                    {
                        Debug.LogWarning($"{assetPath} is not found");
                        continue;
                    }

                    parsed.Add(new SizeData(asset, size.Value));
                }

                var sizeData = parsed.OrderByDescending(x => x.Size).ToArray();
                var text = string.Join("\n", sizeData.Select(x =>
                {
                    var path = AssetDatabase.GetAssetPath(x.Source);
                    return $"{ReadableFileSize(x.Size)} {path}";
                }));
                var total = ReadableFileSize(parsed.Sum(x => x.Size));

                Debug.Log($"WeightScale {target}\nTotal: {total}\n{text}");

                var window = EditorWindow.GetWindow<ResultWindow>("WeightScale");
                window.Refresh(target, sizeData);
            }
        }

        private static double? GetCompressedFileSize(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var p = Path.GetFullPath(Application.dataPath + "../../Library/metadata/" + guid.Substring(0, 2) + "/" + guid);
            if (File.Exists(p))
            {
                var file = new FileInfo(p);
                return file.Length;
            }

            return null;
        }

        public static string ReadableFileSize(double size, int unit = 0)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return $"{size:0.00} {units[unit]}";
        }
    }

    public class SizeData
    {
        public SizeData(Object source, double size)
        {
            Source = source;
            Size = size;
        }

        public Object Source { get; }
        public double Size { get; }
    }
}
