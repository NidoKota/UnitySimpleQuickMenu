using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleQuickMenu
{
    /// <summary>
    /// メニューで実行する処理を登録するClass
    /// </summary>
    public class SimpleQuickMenuLine : MonoBehaviour
    {
        public bool back;
        public UnityEvent unityEvent = new UnityEvent();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SimpleQuickMenuLine)), CanEditMultipleObjects]
    public class QuickMenuLineEditor : Editor
    {
        SerializedProperty backProperty;
        SerializedProperty unityEventProperty;

        void OnEnable()
        {
            backProperty = serializedObject.FindProperty("back");
            unityEventProperty = serializedObject.FindProperty("unityEvent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //後ろに戻る設定の時はunityEventを表示しない
            if (backProperty.boolValue)
            {
                EditorGUILayout.HelpBox("上の項目に戻ります", MessageType.Info);
                EditorGUILayout.PropertyField(backProperty);
            }
            else
            //全ての設定を表示
            {
                EditorGUILayout.HelpBox("UnityEventを実行します", MessageType.Info);
                EditorGUILayout.PropertyField(backProperty);
                EditorGUILayout.PropertyField(unityEventProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
