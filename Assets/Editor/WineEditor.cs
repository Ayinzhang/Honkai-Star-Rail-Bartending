using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Wine))] [CanEditMultipleObjects]
public class WineEditor : Editor
{
    Wine m_target;

    void OnEnable()
    {
        m_target = target as Wine;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Controller", EditorStyles.boldLabel);
        if (GUILayout.Button("Blend") && EditorApplication.isPlaying) m_target.Blend();
        if (GUILayout.Button("Finish") && EditorApplication.isPlaying) m_target.Finish();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        m_target.iceNum = EditorGUILayout.IntField("Ice", m_target.iceNum);
        if (GUILayout.Button("Reset Ice") && EditorApplication.isPlaying)  m_target.ResetIce();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        m_target.color = EditorGUILayout.ColorField("WineColor", m_target.color);
        if (GUILayout.Button("Add Wine") && EditorApplication.isPlaying) m_target.AddWine();
        EditorGUILayout.EndHorizontal();
        if(m_target.colors != null && m_target.colors[0] != Vector4.zero)
        {
            EditorGUILayout.BeginHorizontal();
            for(int i = 0; i < m_target.colors.Length; i++)
                m_target.colors[i] = EditorGUILayout.ColorField(m_target.colors[i]);
            EditorGUILayout.EndHorizontal();
        }
    }
}
