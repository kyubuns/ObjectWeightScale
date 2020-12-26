using System;
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
        }

        public void OnGUI()
        {
            if (_size == null) return;

            GUILayout.Label($"{_target.name} Total: {_total}");

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
                        SetSearchFilter($"ref:{size.Item1}");
                    }

                    GUILayout.Label($"{size.Item2} {size.Item1}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public static void SetSearchFilter(string filter)
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
    }
}