using UnityEngine;

public class CombineMeshes : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get all MeshFilters in children (include inactive = true)
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Material sharedMaterial = null;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i].transform == transform) continue; // skip parent

            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

            // Use the material of the first found cube
            if (sharedMaterial == null)
                sharedMaterial = meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial;

            // Optional: disable original cube
            meshFilters[i].gameObject.SetActive(false);
        }

        // Create new combined mesh
        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // allow >65k vertices
        combinedMesh.CombineMeshes(combine);

        // Add final mesh to parent
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = combinedMesh;

        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = sharedMaterial;

        Debug.Log("Meshes combined successfully!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
