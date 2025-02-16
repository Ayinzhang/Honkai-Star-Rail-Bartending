using UnityEditor;
using UnityEngine;

public class FlipMeshNormals : Editor
{
    /// <summary> ��ת����ķ��� </summary>
    private static void FlipMeshNormal(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        for (int i = 0, len = triangles.Length; i < len; i += 3)
        {
            // ���������ε���β����
            int t = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = t;
        }
        mesh.triangles = triangles;

    }

    static void FlipMeshNormal(GameObject[] gameObjects)
    {
        for (int i = 0, len = gameObjects.Length; i < len; i++)
        {
            GameObject go = gameObjects[i];

            Mesh mesh = null;
            SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else
            {
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }

            if (mesh)
            {
                string path = AssetDatabase.GetAssetPath(mesh);
                // �Ƿ�Ϊ Assets �ļ����µ���Դ(Assets �ļ����µ���Դ���ܱ༭������༭�� Unity ��������Դ������)
                bool isAssetFolder = path.IndexOf("Assets/") > -1;
                if (isAssetFolder)
                {
                    FlipMeshNormal(mesh);
                }
            }
        }
    }

    /// <summary> ��֤��ѡ�����Ϸ����������ʱ�˵��ſ���(�������Ӽ�) </summary>
    [MenuItem("GameObject/Flip Mesh Normals", true)]
    private static bool ValidateFlipMeshNormalsOnGameObject()
    {
        bool isEnableMenuItem = false;
        GameObject[] gameObjects = Selection.gameObjects;
        for (int i = 0, len = gameObjects.Length; i < len; i++)
        {
            GameObject go = gameObjects[i];
            Mesh mesh = null;
            SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else
            {
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }

            if (mesh)
            {
                string path = AssetDatabase.GetAssetPath(mesh);
                // �Ƿ�Ϊ Assets �ļ����µ���Դ(Assets �ļ����µ���Դ���ܱ༭������༭�� Unity ��������Դ������)
                bool isAssetFolder = path.IndexOf("Assets/") > -1;
                if (isAssetFolder)
                {
                    isEnableMenuItem = true;
                    break;
                }
            }
        }
        return isEnableMenuItem;
    }

    [MenuItem("GameObject/Flip Mesh Normals", false, 11)]
    private static void FlipMeshNormalsOnGameObject()
    {
        FlipMeshNormal(Selection.gameObjects);
    }

}