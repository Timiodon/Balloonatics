using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ClothBalloon : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public List<IConstraints> Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }

    private Mesh _mesh;

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private float _stretchingStiffness = 0.5f;

    [SerializeField]
    private float _overpressureStiffness = 1f;

    [SerializeField]
    private float _pressure = 1f;

    private Vector3[] displacedVertices;
    private int[] vertexIdToParticleIdMap;

    private OverpressureConstraints _overpressureConstraints;
    private StretchingConstraints _stretchingConstraints;
    private MouseFollowConstraints _mouseFollowConstraints;

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
        _stretchingConstraints = new StretchingConstraints();
        foreach ((int, int) edge in edgeSet)
        {
            _stretchingConstraints.AddConstraint(Particles, new List<int> { edge.Item1, edge.Item2 }, _stretchingStiffness);
        }

        // Initialize overpressure constraint
        _overpressureConstraints = new OverpressureConstraints();
        Dictionary<int, int[]> triangleToParticleIndices = new();

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            int a = vertexIdToParticleIdMap[_mesh.triangles[i]];
            int b = vertexIdToParticleIdMap[_mesh.triangles[i + 1]];
            int c = vertexIdToParticleIdMap[_mesh.triangles[i + 2]];
            triangleToParticleIndices[i / 3] = new int[] { a, b, c };
        }
        _overpressureConstraints.TriangleToParticleIndices = triangleToParticleIndices;
        _overpressureConstraints.Pressure = _pressure;
        _overpressureConstraints.AddConstraint(Particles, new List<int>(), _overpressureStiffness);

        // Initialize mouse follow constraint
        _mouseFollowConstraints = new MouseFollowConstraints();

        Constraints = new List<IConstraints> { _stretchingConstraints, _overpressureConstraints };
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Add constraints to follow the mouse
            _mouseFollowConstraints.ClearConstraints();

            int closestVertex = FindClosestVertex(ray, Particles);
            if (closestVertex != -1)
            {
                _mouseFollowConstraints.mousePos = Particles[closestVertex].X;

                for (int i = 0; i < _mesh.vertices.Length; i++)
                {
                    _mouseFollowConstraints.AddConstraint(Particles, new List<int> { vertexIdToParticleIdMap[i] }, 1f);
                }

                Constraints.Add(_mouseFollowConstraints);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (Constraints.Contains(_mouseFollowConstraints))
            {
                Constraints.Remove(_mouseFollowConstraints);
            }
        }
        if (Input.GetMouseButton(0))
        {
            _mouseFollowConstraints.mousePos = FindClosestPointOnRay(Camera.main.ScreenPointToRay(Input.mousePosition), Particles);
        }

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // TODO: we may want to interpolate between timesteps here

            // TODO Transforming to local and back to world every frame is a bit unfortunate. We might want to try to keep everything in global coordinates instead
            displacedVertices[i] = transform.InverseTransformPoint(Particles[vertexIdToParticleIdMap[i]].X);
        }

        _overpressureConstraints.Pressure = _pressure;

        _mesh.vertices = displacedVertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    // Finds closest vertex index to ray that is at most 0.2 units away, otherwise returns -1
    private int FindClosestVertex(Ray ray, Particle[] particles)
    {
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < particles.Length; i++)
        {
            float distance = Vector3.Cross(ray.direction, particles[i].X - ray.origin).magnitude;

            if (distance < closestDistance && distance < 0.2f)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private Vector3 FindClosestPointOnRay(Ray ray, Particle[] particles)
    {
        float closestDistance = Mathf.Infinity;
        Vector3 closestPointOnRay = Vector3.zero;

        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 originToVertex = particles[i].X - ray.origin;
            float projectionLength = Vector3.Dot(originToVertex, ray.direction.normalized);
            Vector3 pointOnRay = ray.origin + ray.direction.normalized * projectionLength;
            float distance = Vector3.Distance(pointOnRay, particles[i].X);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPointOnRay = pointOnRay;
            }
        }

        return closestPointOnRay;
    }
}
