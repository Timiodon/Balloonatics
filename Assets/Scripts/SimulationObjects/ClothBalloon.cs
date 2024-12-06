using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ClothBalloon : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public List<IClothConstraints> Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }
    public bool UseHelium { get => _useHelium; }
    public bool HandleSelfCollision { get => _handleSelfCollision; }
    public float Friction { get => _friction; }

    private Mesh _mesh;

    // The edges that need to be removed from the mesh this step because they have been torn apart.
    private HashSet<(int, int)> _tornEdges = new();

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private bool _useHelium = true;

    // Do not change this at runtime
    [SerializeField]
    private bool _handleSelfCollision = true;

    [SerializeField]
    private float _friction = 0.0f;

    [Header("Constraint stiffnesses")]
    [SerializeField]
    private float _stretchingStiffness = 0.5f;

    [SerializeField]
    private float _overpressureStiffness = 1f;

    [SerializeField]
    private float _bendingStiffness = 0.5f;

    [SerializeField]
    private float _tearingThreshold = 0.05f;

    private const float MIN_COMPLIANCE_SCALE = 0.01f;
    private const float MAX_COMPLIANCE_SCALE = 200f;

    [Header("Compliance Scales")]
    [SerializeField, Range(MIN_COMPLIANCE_SCALE, MAX_COMPLIANCE_SCALE)]
    private float _stretchingComplianceScale = 1f;

    [SerializeField, Range(MIN_COMPLIANCE_SCALE, MAX_COMPLIANCE_SCALE)]
    private float _bendingComplianceScale = 1f;

    [SerializeField, Range(30f * MIN_COMPLIANCE_SCALE, MAX_COMPLIANCE_SCALE)]
    private float _pressure = 1f;

    private Vector3[] displacedVertices;
    private int[] vertexIdToParticleIdMap;

    private float _mouseDistance;
    private bool _popped = false;

    private OverpressureConstraints _overpressureConstraints;
    private StretchingConstraints _stretchingConstraints;
    private MouseFollowConstraints _mouseFollowConstraints;
    private BendingConstraints _bendingConstraints;

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
        _stretchingConstraints.ShuffleConstraintOrder();
        _stretchingConstraints.tearEdgesCallback = TearEdges;

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
        // Initialize bending constraint, Code adapted from: https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/14-cloth.html#L208
        _bendingConstraints = new();
        int[] neighbours = FindTriangleNeighbours();
        for (int i = 0; i < _mesh.triangles.Length / 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int idx0 = vertexIdToParticleIdMap[_mesh.triangles[3 * i + j]];
                int idx1 = vertexIdToParticleIdMap[_mesh.triangles[3 * i + (j + 1) % 3]];

                int neighbour = neighbours[3 * i + j];
                if (neighbour >= 0)
                {
                    int ni = neighbour / 3;
                    int nj = neighbour % 3;
                    int idx2 = vertexIdToParticleIdMap[_mesh.triangles[3 * i + (j + 2) % 3]];
                    int idx3 = vertexIdToParticleIdMap[_mesh.triangles[3 * ni + (nj + 2) % 3]];
                    // for index order, see Figure 1 https://www.cs.ubc.ca/~rbridson/docs/cloth2003.pdf
                    _bendingConstraints.AddConstraint(Particles, new List<int> { idx2, idx3, idx0, idx1 }, _bendingStiffness);
                }
            }
        }

        Constraints = new List<IClothConstraints> { _stretchingConstraints, _overpressureConstraints, _bendingConstraints };
    }

    public void Precompute(float deltaT, float maxSpeed)
    {
        // TODO: parallelize this
        for (int i = 0; i < Particles.Length; i++)
        {
            // A particle with infinite mass would require an infinite force to be moved. 
            if (Particles[i].W == 0.0f)
                continue;

            // For helium, we assume it exactly cancels gravity at pressure = 1 and scales linearly with pressure
            // Helium is only active if the object did not pop
            float heliumAcc = (UseHelium && !_popped) ? 10 * _overpressureConstraints.Pressure : 0;
            float gravityAcc = UseGravity ? ISimulationObject.GRAVITY : 0;
            Particles[i].V.y += (heliumAcc + gravityAcc) * deltaT;

            // This ensures that we do not miss any collisions
            if (HandleSelfCollision && Particles[i].V.magnitude > maxSpeed)
                Particles[i].V *= maxSpeed / Particles[i].V.magnitude;


            Particles[i].P = Particles[i].X;
            Particles[i].X += Particles[i].V * deltaT;

            // Temporary ground collision inspired by 10 min physics. We might want to replace this with a constraint later
            // This causes the particles to "stick" to the ground somewhat
            if (Particles[i].X.y < 0)
            {
                Particles[i].X = Particles[i].P;
                Particles[i].X.y = 0;
            }
        }
    }

    // Correct initial position guesses to satisfy constraints
    public void SolveConstraints(float deltaT)
    {
        foreach (IClothConstraints constraint in Constraints)
        {
            constraint.SolveConstraints(Particles, deltaT);
        }
    }

    public void CorrectVelocities(float deltaT)
    {
        // TODO: parallelize this
        for (int i = 0; i < Particles.Length; i++)
        {
            if (Particles[i].W == 0.0f)
                continue;

            Particles[i].V = (Particles[i].X - Particles[i].P) / deltaT;
        }
    }

    public void UpdateMesh()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // TODO: we may want to interpolate between timesteps here

            // TODO Transforming to local and back to world every frame is a bit unfortunate. We might want to try to keep everything in global coordinates instead
            displacedVertices[i] = transform.InverseTransformPoint(Particles[vertexIdToParticleIdMap[i]].X);
        }

        _stretchingConstraints.ComplianceScale = _stretchingComplianceScale;
        _stretchingConstraints.TearingThreshold = _tearingThreshold;
        _bendingConstraints.ComplianceScale = _bendingComplianceScale;
        _overpressureConstraints.Pressure = _pressure;

        _mesh.vertices = displacedVertices;

        if (_tornEdges.Count > 0)
        {
            _popped = true;
            var removeMask = new System.Collections.BitArray(_mesh.triangles.Length);
            for (int i = 0; i < _mesh.triangles.Length; i += 3)
            {
                int a = vertexIdToParticleIdMap[_mesh.triangles[i]];
                int b = vertexIdToParticleIdMap[_mesh.triangles[i + 1]];
                int c = vertexIdToParticleIdMap[_mesh.triangles[i + 2]];
                if (_tornEdges.Contains((Mathf.Min(a, b), Mathf.Max(a, b)))
                    || _tornEdges.Contains((Mathf.Min(b, c), Mathf.Max(b, c)))
                    || _tornEdges.Contains((Mathf.Min(a, c), Mathf.Max(a, c))))
                {
                    removeMask.Set(i, true);
                    removeMask.Set(i + 1, true);
                    removeMask.Set(i + 2, true);
                }
            }
            _mesh.triangles = _mesh.triangles.Where((_, index) => !removeMask.Get(index)).ToArray();
            _tornEdges.Clear();
        }

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    void TearEdges(List<(int, int)> edges)
    {
        _tornEdges.UnionWith(edges);

        foreach (IClothConstraints constraint in Constraints)
        {
            constraint.RemoveEdgeConstraints(edges);
        }
    }

    // Code adapted from: https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/14-cloth.html#L208
    private int[] FindTriangleNeighbours()
    {
        // Find the set of all edges with their corresponding global edge number
        // The global edge number of edge j in triangle i is 3*i + j
        List<(int, int, int)> edgeSet = new();
        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            int a = vertexIdToParticleIdMap[_mesh.triangles[i]];
            int b = vertexIdToParticleIdMap[_mesh.triangles[i + 1]];
            int c = vertexIdToParticleIdMap[_mesh.triangles[i + 2]];
            edgeSet.Add((Mathf.Min(a, b), Mathf.Max(a, b), i));
            edgeSet.Add((Mathf.Min(b, c), Mathf.Max(b, c), i + 1));
            edgeSet.Add((Mathf.Min(a, c), Mathf.Max(a, c), i + 2));
        }

        // sort so common edges are consecutive in List
        edgeSet.Sort(delegate ((int, int, int) e1, (int, int, int) e2)
        {
            return e1.Item1.CompareTo(e2.Item1) != 0 ? e1.Item1.CompareTo(e2.Item1) : e1.Item2.CompareTo(e2.Item2);
        });


        // Given the global edge number g of an edge, neighbours[g] returns
        // the global edge number of this edge in the neighbouring triangle or -1 if this edge has no neighbour
        int[] neighbours = Enumerable.Repeat(-1, _mesh.triangles.Length).ToArray();
        int idx = 0;
        while (idx < edgeSet.Count)
        {
            var e0 = edgeSet[idx];
            idx++;
            if (idx < edgeSet.Count)
            {
                var e1 = edgeSet[idx];
                if (e0.Item1 == e1.Item1 && e0.Item2 == e1.Item2)
                {
                    neighbours[e0.Item3] = e1.Item3;
                    // If we pretend the neighbour relation is not symmetric, we don't need to
                    // take care to not add duplicate constraints later
                    // neighbours[e1.Item3] = e0.Item3;
                }
                idx++;
            }
        }

        return neighbours;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Add constraints to follow the mouse
            _mouseFollowConstraints.ClearConstraints();

            int closestVertex = Utils.FindClosestVertex(ray, Particles);
            if (closestVertex != -1)
            {
                _mouseFollowConstraints.mousePos = Particles[closestVertex].X;
                _mouseDistance = Vector3.Distance(Particles[closestVertex].X, ray.origin);

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
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            _mouseFollowConstraints.mousePos = mouseRay.origin + mouseRay.direction * _mouseDistance;
        }

        // Use mouse wheel to adjust mouse distance
        _mouseDistance += Input.mouseScrollDelta.y * 0.1f;
    }

}
