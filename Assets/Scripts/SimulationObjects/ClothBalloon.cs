using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ClothBalloon : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public IConstraints[] Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }

    private Mesh _mesh;

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private float _stiffness = 0.5f;

    private Vector3[] displacedVertices;
    private int[] vertexIdToParticleIdMap;


    public void Initialize()
    {
        _mesh = GetComponent<MeshFilter>().mesh;

        // Vertices are usually duplicated in meshes so each quad can have it's own set of verts. We don't want duplicate particles so we have to do this mapping.
        int n = _mesh.vertices.Length;
        displacedVertices = new Vector3[n];
        vertexIdToParticleIdMap = new int[n];
        // Use a hashtable to do duplication detection of the positions
        Dictionary<Vector3, int> positionToParticleMap = new();
        for (int i = 0; i < n; i++)
        {
            displacedVertices[i] = _mesh.vertices[i];
            if (positionToParticleMap.TryGetValue(_mesh.vertices[i], out int particleIndex))
            {
                vertexIdToParticleIdMap[i] = particleIndex;
            }
            else
            {
                int nextIndex = positionToParticleMap.Count;
                positionToParticleMap[_mesh.vertices[i]] = nextIndex;
                vertexIdToParticleIdMap[i] = nextIndex;
            }
        }

        Particles = new Particle[positionToParticleMap.Count];
        foreach (KeyValuePair<Vector3, int> particlesAndIndices in positionToParticleMap)
        {
            Particles[particlesAndIndices.Value] = new Particle(transform.TransformPoint(particlesAndIndices.Key), Vector3.zero, _totalMass / n);
        }

        // Find the set of all unique edges for the stretching constraints
        HashSet<(int, int)> edgeSet = new();
        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            int a = vertexIdToParticleIdMap[_mesh.triangles[i]];
            int b = vertexIdToParticleIdMap[_mesh.triangles[i + 1]];
            int c = vertexIdToParticleIdMap[_mesh.triangles[i + 2]];
            edgeSet.Add((Mathf.Max(a, b), Mathf.Min(a, b)));
            edgeSet.Add((Mathf.Max(a, c), Mathf.Min(a, c)));
            edgeSet.Add((Mathf.Max(b, c), Mathf.Min(b, c)));
        }

        // Initialize stretching constraints
        StretchingConstraints constraint = new StretchingConstraints();
        foreach ((int, int) edge in edgeSet)
        {
            constraint.AddConstraint(Particles, new List<int> { edge.Item1, edge.Item2 }, _stiffness);
        }

        Constraints = new IConstraints[] { constraint };
    }

    void Update()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // TODO: we may want to interpolate between timesteps here

            // TODO Transforming to local and back to world every frame is a bit unfortunate. We might want to try to keep everything in global coordinates instead
            displacedVertices[i] = transform.InverseTransformPoint(Particles[vertexIdToParticleIdMap[i]].X);
        }

        _mesh.vertices = displacedVertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}
