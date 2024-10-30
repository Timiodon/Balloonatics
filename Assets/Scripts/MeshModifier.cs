using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(ISimulationObject))]
public class MeshModifier : MonoBehaviour
{
    // Assumes that the simulation object uses the mesh vertices as particles.
    private ISimulationObject simulationObject;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        simulationObject = GetComponent<ISimulationObject>();

        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // TODO: we may want to interpolate between timesteps here

            // TODO Transforming to local and back to world every frame is a bit unfortunate. We might want to try to keep everything in global coordinates instead
            displacedVertices[i] = transform.InverseTransformPoint(simulationObject.Particles[i].X);
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}