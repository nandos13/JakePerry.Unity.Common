using System.Collections.Generic;
using UnityEditor;
using static UnityEngine.Networking.UnityWebRequest;

namespace JakePerry.Unity.FPM
{
    [CustomEditor(typeof(KeyTableBase))]
    public sealed class KeyTableDrawer : Editor
    {
        private const string kFoldoutPref = "JakePerry.Unity.FPM.DesignTime.SymbolSelector.Foldout.";

        private static readonly List<(string, SerializeGuid)> _cachedList = new List<(string, SerializeGuid)>();

        private static string GetFoldoutKey(KeyTableBase table)
        {
            var path = AssetDatabase.GetAssetPath(table);
            var guid = AssetDatabase.AssetPathToGUID(path);
            return kFoldoutPref + guid;
        }

        private static void Bar(SerializedObject sObj)
        {
            var symbolsProp = sObj.FindProperty("m_symbols");

            EditorGUILayout.Space(LineHeight + Spacing * 2, false);

            EditorGUILayout.BeginVertical();
            {
                var symResult = DrawSymbols(symbolsProp, table, ref dirty, true, false);

                if (symResult.action == SymbolInteractionDat.kClick)
                {
                    result = (true, symResult.guid);
                }
                if (symResult.action == SymbolInteractionDat.kDelete)
                {
                    symbolsProp.DeleteArrayElementAtIndex(symResult.symbolIndex);
                    dirty = true;
                    Repaint();
                }

                if (m_searchWords.Count == 0)
                {
                    EditorGUILayout.LabelField("Actions:", EditorStyles.miniBoldLabel);
                    DrawAddSymbolButton(table, symbolsProp, ref dirty);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void Foo(KeyTableBase table, )
        {
            var sObj = new SerializedObject(table);

            bool dirty = false;
            EditorGUILayout.BeginHorizontal();
            {
                
            }
            EditorGUILayout.EndHorizontal();

            if (dirty)
                sObj.ApplyModifiedProperties();
        }

        private static void Foo(KeyTableBase table)
        {
            try
            {
                _cachedList.Clear();
                table.GetDefinitions(_cachedList);
                if (!CheckTableSymbolsAgainstSearchFilter())
                    return;

                if (i++ > 0)
                    EditorGUILayout.Space();

                var iconContent = EditorGUIUtility.IconContent("d_ScriptableObject Icon");
                if (DrawHeader(table.name, GetFoldoutKey(table), iconContent, table))
                {
                    var sObj = new SerializedObject(table);
                    var symbolsProp = sObj.FindProperty("m_symbols");

                    bool dirty = false;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space(LineHeight + Spacing * 2, false);

                        EditorGUILayout.BeginVertical();
                        {
                            var symResult = DrawSymbols(symbolsProp, table, ref dirty, true, false);

                            if (symResult.action == SymbolInteractionDat.kClick)
                            {
                                result = (true, symResult.guid);
                            }
                            if (symResult.action == SymbolInteractionDat.kDelete)
                            {
                                symbolsProp.DeleteArrayElementAtIndex(symResult.symbolIndex);
                                dirty = true;
                                Repaint();
                            }

                            if (m_searchWords.Count == 0)
                            {
                                EditorGUILayout.LabelField("Actions:", EditorStyles.miniBoldLabel);
                                DrawAddSymbolButton(table, symbolsProp, ref dirty);
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (dirty)
                        sObj.ApplyModifiedProperties();
                }
            }
            finally { _cachedList.Clear(); }
        }

        public override void OnInspectorGUI()
        {

        }
    }
}
