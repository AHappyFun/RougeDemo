using UnityEditor;
using UnityEngine;

namespace Rouge.EditorTools
{
    [CustomEditor(typeof(SkillDebugPanel))]
    public class SkillDebugPanelEditor : Editor
    {
        private string[] typeNames;
        private int selectedIndex;

        private void OnEnable()
        {
            typeNames = System.Enum.GetNames(typeof(BulletManager.BulletType));
            selectedIndex = (int)((SkillDebugPanel)target).selectedType;
        }

        public override void OnInspectorGUI()
        {
            var panel = (SkillDebugPanel)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎯 技能调试面板", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // ── 类型选择下拉 ──
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("选择子弹", selectedIndex, typeNames);
            if (EditorGUI.EndChangeCheck())
            {
                panel.selectedType = (BulletManager.BulletType)selectedIndex;
                EditorUtility.SetDirty(panel);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("══════ 最终属性 ══════", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // ── 显示数据 ──
            if (!string.IsNullOrEmpty(panel.displayText))
            {
                // 解析并分行显示
                var parts = panel.displayText.Split("  ");
                foreach (var part in parts)
                {
                    if (part.Contains(":"))
                    {
                        var kv = part.Split(':');
                        if (kv.Length == 2)
                            DrawField(kv[0].Trim(), kv[1].Trim());
                        else
                            DrawField(kv[0].Trim(), string.Join(":", kv[1..]).Trim());
                    }
                    else
                    {
                        EditorGUILayout.LabelField(part, EditorStyles.miniLabel);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("等待运行时数据...", MessageType.Info);
            }

            // ── 原始数据显示（备选） ──
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("原始数据:", EditorStyles.miniLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(panel.displayText, GUILayout.Height(60));
            EditorGUI.EndDisabledGroup();
        }

        private void DrawField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(80));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
