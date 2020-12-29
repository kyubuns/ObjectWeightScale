using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ObjectWeightScale
{
    public class ResultWindow : EditorWindow
    {
        private GameObject _target;
        private string _total;
        private Tuple<string, string, Object>[] _size;
        private Vector2 _scrollPosition = Vector2.zero;
        private string _assetBundleSize = null;
        private static SearchableEditorWindow _hierarchy;

        public void Refresh(GameObject target, SizeData[] sizeList)
        {
            _target = target;
            _total = ObjectWeightScale.ReadableFileSize(sizeList.Sum(x => x.Size));
            _size = sizeList.Select(x =>
            {
                var assetPath = AssetDatabase.GetAssetPath(x.Source);
                if (assetPath.StartsWith("Assets/")) assetPath = assetPath.Substring(7, assetPath.Length - 7);
                return Tuple.Create(
                    assetPath,
                    ObjectWeightScale.ReadableFileSize(x.Size),
                    x.Source
                );
            }).ToArray();
            _assetBundleSize = null;
        }

        public void OnGUI()
        {
            if (_size == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{_target.name}");
                GUILayout.Label($"Total Size: {_total}");
                GUILayout.FlexibleSpace();
                if (_assetBundleSize == null)
                {
                    if (GUILayout.Button("Calc AssetBundle Size", GUILayout.Width(180)))
                    {
                        _assetBundleSize = CalcAssetBundleSize(_target);
                    }
                }
                else
                {
                    GUILayout.Label($"AssetBundle Size: {_assetBundleSize}");
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var size in _size)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(size.Item3);
                    }

                    if (GUILayout.Button("References", GUILayout.Width(80)))
                    {
                        SetSearchFilter($"ref:\"{size.Item1}\"");
                    }

                    GUILayout.Label($"{size.Item2} {size.Item1}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void SetSearchFilter(string filter)
        {
            var windows = (SearchableEditorWindow[]) Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));

            foreach (var window in windows)
            {
                if (window.GetType().ToString() != "UnityEditor.SceneHierarchyWindow") continue;
                _hierarchy = window;
                break;
            }

            if (_hierarchy == null) return;

            var setSearchType = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { filter, 0, false, true };

            setSearchType?.Invoke(_hierarchy, parameters);
        }

        private static string CalcAssetBundleSize(GameObject target)
        {
            var prefabTempPath = "Assets/WeightScaleTemp.prefab";

            File.Delete(prefabTempPath);
            File.Delete(prefabTempPath + ".meta");

            PrefabUtility.SaveAsPrefabAsset(target, prefabTempPath);

            var assetBundleBuild = new AssetBundleBuild
            {
                assetNames = new[]{ prefabTempPath },
                assetBundleName = "WeightScaleTemp.unity3d"
            };

            File.Delete(prefabTempPath);
            File.Delete(prefabTempPath + ".meta");

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new[]
            {
                assetBundleBuild
            }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            var assetBundle = new FileInfo($"{Application.temporaryCachePath}/WeightScaleTemp.unity3d");

            AssetDatabase.Refresh();

            return ObjectWeightScale.ReadableFileSize(assetBundle.Length);
        }
    }
}