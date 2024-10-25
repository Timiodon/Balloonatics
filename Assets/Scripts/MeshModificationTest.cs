using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshModificationTest : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;

    public float waveHeight = 0.2f;
    public float waveSpeed = 2f;
    public float waveFrequency = 2f;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 original = originalVertices[i];
            float wave = Mathf.Sin(Time.time * waveSpeed + original.x * waveFrequency + original.z * waveFrequency);
            displacedVertices[i] = original + Vector3.up * wave * waveHeight;
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
