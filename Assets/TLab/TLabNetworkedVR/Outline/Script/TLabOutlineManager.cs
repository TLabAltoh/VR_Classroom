using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.XR.VRGrabber.VFX
{
#if UNITY_EDITOR
    public class TLabOutlineManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_outlineTargets;
        [SerializeField] private Shader m_outline;

        [Header("Save Path")]
        [SerializeField] private string m_savePathMesh;
        [SerializeField] private string m_savePathMaterial;

        const float error = 1e-8f;

        // https://blog.syn-sophia.co.jp/articles/2022/10/17/outline_rendering_01

        public void SaveMesh(Mesh mesh, MeshFilter meshFilter)
        {
            string path = m_savePathMesh + "/" + mesh.name + ".asset";
            Mesh copyMesh = GameObject.Instantiate(mesh);
            string copyMeshName = copyMesh.name.ToString();
            copyMesh.name = copyMeshName.Substring(0, copyMeshName.Length - "(Clone)".Length);
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (asset != null)
            {
                EditorUtility.CopySerialized(copyMesh, asset);
                meshFilter.sharedMesh = asset;
            }
            else
            {
                AssetDatabase.CreateAsset(copyMesh, path);
                meshFilter.sharedMesh = copyMesh;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void SaveManaterial(Material outline, ref Material[] newMaterials, MeshRenderer meshRenderer)
        {
            string path = m_savePathMaterial + "/" + outline.name + ".mat";
            Material prevMat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (prevMat != null)
            {
                EditorUtility.CopySerialized(outline, prevMat);
                newMaterials[newMaterials.Length - 1] = prevMat;
                meshRenderer.sharedMaterials = newMaterials;
            }
            else
                AssetDatabase.CreateAsset(outline, path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void ProcessMesh(GameObject obj)
        {
            var meshFilters = obj.GetComponents<MeshFilter>();

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
                        if (v.sqrMagnitude < error) softEdge += normals[j];
                    }

                    softEdge.Normalize();
                    softEdges[i] = new Color(softEdge.x, softEdge.y, softEdge.z, 0);
                }

                mesh.colors = softEdges;
                meshFilter.sharedMesh = mesh;
                EditorUtility.SetDirty(meshFilter);

                SaveMesh(mesh, meshFilter);
            }

            EditorUtility.SetDirty(obj);
        }

        private void AddOutlineMaterial(GameObject obj)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material[] prevMaterials = meshRenderer.sharedMaterials;

                List<Material> newMaterialList = new List<Material>();
                for (int i = 0; i < prevMaterials.Length; i++)
                    if (prevMaterials[i] != null && prevMaterials[i].shader != m_outline)
                        newMaterialList.Add(prevMaterials[i]);

                Material outline = new Material(m_outline);
                outline.name = obj.name + "_Outline";
                newMaterialList.Add(outline);

                Material[] newMaterials = newMaterialList.ToArray();
                meshRenderer.sharedMaterials = newMaterials;

                EditorUtility.SetDirty(meshRenderer);

                SaveManaterial(outline, ref newMaterials, meshRenderer);
            }
        }

        private void AddOutlineSelectable(GameObject obj)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                TLabOutlineSelectable selectable = obj.GetComponent<TLabOutlineSelectable>();
                if (selectable == null) selectable = obj.AddComponent<TLabOutlineSelectable>();

                int materialsLength = meshRenderer.sharedMaterials.Length;
                selectable.OutlineMat = meshRenderer.sharedMaterials[materialsLength - 1];

                EditorUtility.SetDirty(selectable);
            }
        }

        public void CreateOutline(GameObject obj)
        {
            AddOutlineMaterial(obj);
            AddOutlineSelectable(obj);

            EditorUtility.SetDirty(obj);
        }

        public void ProcessMesh()
        {
            foreach (GameObject outlineTarget in m_outlineTargets)
                ProcessMesh(outlineTarget);
        }

        public void CreateOutline()
        {
            foreach (GameObject outlineTarget in m_outlineTargets)
                CreateOutline(outlineTarget);
        }

        private string GetDiskPath(string assetPath)
        {
            int startIndex = "Assets".Length - 1;
            string currentDir = Directory.GetCurrentDirectory();
            string assetDir = assetPath.Substring(startIndex, assetPath.Length - startIndex);
            return currentDir + assetDir;
        }

        private bool FileExists(string assetPath)
        {
            if (assetPath.Length < "Assets".Length - 1) return false;

            return File.Exists(GetDiskPath(assetPath));
        }

        public bool SelectMeshSavePath()
        {
            string initialPath = m_savePathMesh != null && FileExists(m_savePathMesh) ? m_savePathMesh : "Assets";
            string path = EditorUtility.SaveFolderPanel("Save Path", initialPath, "");
            if (path == "") return false;
            string fullPath = Directory.GetCurrentDirectory();
            m_savePathMesh = path.Remove(0, fullPath.Length + 1);
            return true;
        }

        public bool SelectMaterialSavePath()
        {
            string initialPath = m_savePathMaterial != null && FileExists(m_savePathMesh) ? m_savePathMaterial : "Assets";
            string path = EditorUtility.SaveFolderPanel("Save Path", initialPath, "");
            if (path == "") return false;
            string fullPath = Directory.GetCurrentDirectory();
            m_savePathMaterial = path.Remove(0, fullPath.Length + 1);
            return true;
        }
    }

    [CustomEditor(typeof(TLabOutlineManager))]
    public class TLabOutlineManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            TLabOutlineManager manager = target as TLabOutlineManager;

            if (GUILayout.Button("Process Mesh"))
            {
                if (manager.SelectMeshSavePath()) manager.ProcessMesh();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Create Outline"))
            {
                if (manager.SelectMaterialSavePath()) manager.CreateOutline();
                EditorUtility.SetDirty(manager);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
