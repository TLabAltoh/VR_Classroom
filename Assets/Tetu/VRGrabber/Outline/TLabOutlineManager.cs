using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class TLabOutlineManager : MonoBehaviour
{
    [SerializeField] private GameObject[] m_outlineTarget;
    [SerializeField] private string m_savePath;

    const float error = 1e-8f;

    // https://blog.syn-sophia.co.jp/articles/2022/10/17/outline_rendering_01

    public void BakeNormal(GameObject obj)
    {
        var meshFilters = obj.GetComponentsInChildren<MeshFilter>();

        foreach (var meshFilter in meshFilters)
        {

            var mesh = meshFilter.sharedMesh;

            var normals = mesh.normals;
            var vertices = mesh.vertices;
            var vertexCount = mesh.vertexCount;

            Color[] softEdges = new Color[normals.Length];

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 softEdge = Vector3.zero;

                for (int j = 0; j < vertexCount; j++)
                {
                    var v = vertices[i] - vertices[j];

                    if (v.sqrMagnitude < error)
                    {
                        softEdge += normals[j];
                    }
                }

                softEdge.Normalize();

                softEdges[i] = new Color(softEdge.x, softEdge.y, softEdge.z, 0);
            }
            mesh.colors = softEdges;

            string path = m_savePath + "/" + mesh.name + ".asset";
            Mesh copyMesh = GameObject.Instantiate(mesh);
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset != null)
            {
                EditorUtility.CopySerialized(asset, copyMesh);
            }
            else
            {
                AssetDatabase.CreateAsset(copyMesh, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            meshFilter.sharedMesh = copyMesh;
        }
    }

    public void BakeVertexColor()
    {
        for (int i = 0; i < m_outlineTarget.Length; i++)
        {
            BakeNormal(m_outlineTarget[i]);
        }
    }

    public void SelectSavePath()
    {
        string path = EditorUtility.SaveFolderPanel("Save Path", "Assets", "");

        if (path == null)
        {
            return;
        }

        string fullPath = System.IO.Directory.GetCurrentDirectory();

        m_savePath = path.Remove(0, fullPath.Length + 1);
    }
}
#endif
