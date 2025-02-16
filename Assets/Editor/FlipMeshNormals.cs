using UnityEditor;
using UnityEngine;

public class FlipMeshNormals : Editor
{
    /// <summary> 翻转网格的法线 </summary>
    private static void FlipMeshNormal(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        for (int i = 0, len = triangles.Length; i < len; i += 3)
        {
            // 交换三角形的首尾索引
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
                // 是否为 Assets 文件夹下的资源(Assets 文件夹下的资源才能编辑，避免编辑到 Unity 的内置资源的网格)
                bool isAssetFolder = path.IndexOf("Assets/") > -1;
                if (isAssetFolder)
                {
                    FlipMeshNormal(mesh);
                }
            }
        }
    }

    /// <summary> 验证所选择的游戏对象有网格时菜单才可用(不计算子级) </summary>
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
                // 是否为 Assets 文件夹下的资源(Assets 文件夹下的资源才能编辑，避免编辑到 Unity 的内置资源的网格)
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