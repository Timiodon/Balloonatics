using System.Collections.Generic;
using UnityEngine;

// Inspiration for this script comes from the following sources:
// - 22 - How to write a rigid body simulator: https://www.youtube.com/watch?v=euypZDssYxE
// - Deriving 3D Rigid Body Physics and implementing it in C/C++ (with intuitions): https://www.youtube.com/watch?v=4r_EvmPKOvY
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Collider))]
public class RigidBody : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public List<IRigidConstraints> Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }
    public bool HandleSelfCollision { get => _handleSelfCollision; }
    public bool HandleInterObjectCollisions { get => _handleInterObjectCollisions; }
    public float HashcellSize { get => _hashcellSize; }
    public float Friction { get => _friction; }

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private Vector3 _externalTorque = Vector3.zero;

    [SerializeField]
    private bool _handleSelfCollision = false;
    [SerializeField]
    private bool _handleInterObjectCollisions = true;

    private float _hashcellSize = 0.01f;

    [SerializeField]
    private float _friction = 0.0f;

    public enum Shape
    {
        Cube,
        Sphere,
        Cylinder
    }

    public Quaternion q = Quaternion.identity; // Current orientation of the rigidbody
    public Matrix4x4 InvI0; // Inverse of the initial moment of inertia (note that we use a 4x4 matrix since Unity doesn't have 3x3 matrices)

    [SerializeField]
    private Shape _shape = Shape.Cube;
    public Quaternion qPrev; // Previous orientation of the rigidbody
    private Vector3 _w; // Angular velocity, in radians per second

    private float _mouseDistance;
    private Vector3[] _localParticlePositions;

    private RigidMouseFollowConstraints _mouseFollowConstraints;
    private RigidGroundCollisionConstraints _groundCollisionConstraints;

    public virtual void Initialize()
    {
        Constraints = new List<IRigidConstraints>();
        q = transform.rotation;

        // Calculate initial, inverse moment of inertia for different shapes
        Vector3 size = GetComponent<MeshFilter>().sharedMesh.bounds.size;
        // Account for Unity's scaling of the mesh
        size = size.CwiseProduct(transform.localScale);
        float Ixx = 1;
        float Iyy = 1;
        float Izz = 1;
        // Formulas from https://en.wikipedia.org/wiki/List_of_moments_of_inertia
        switch (_shape)
        {
            case Shape.Cube:
                Ixx = _totalMass / 12f * (size.y * size.y + size.z * size.z);
                Iyy = _totalMass / 12f * (size.x * size.x + size.z * size.z);
                Izz = _totalMass / 12f * (size.x * size.x + size.y * size.y);
                break;
            case Shape.Sphere:
                float r = size.x / 2;
                Ixx = 2f / 5f * _totalMass * r * r;
                Iyy = Ixx;
                Izz = Ixx;
                break;
            case Shape.Cylinder:
                float r1 = size.x / 2f;
                float h = size.y;
                Ixx = _totalMass / 12f * (3f * r1 * r1 + h * h);
                Iyy = _totalMass / 2f * r1 * r1;
                Izz = Ixx;
                break;
        }
        InvI0 = Matrix4x4.Scale(new Vector3(1f / Ixx, 1f / Iyy, 1f / Izz));

        // Print _invIO for debugging
        //Debug.Log("_invIO: " + _invI0);

        _mouseFollowConstraints = new RigidMouseFollowConstraints();
        _mouseFollowConstraints.AddConstraint(this, 1f);

        // Add a ground collision constraint for every vertex of the rigidbody
        // First perform de-duplication
        // Vertices are usually duplicated in meshes so each quad can have it's own set of verts. We don't want duplicate particles so we have to do this mapping.
        Mesh _mesh = GetComponent<MeshFilter>().sharedMesh;
        int n = _mesh.vertices.Length;
        Vector3[] displacedVertices = new Vector3[n];
        int[] vertexIdToParticleIdMap; vertexIdToParticleIdMap = new int[n];
        // Use a hashtable to do duplication detection of the positions
        Dictionary<Vector3, int> positionToParticleMap = new();
        for (int i = 0; i < n; i++)
        {
            displacedVertices[i] = _mesh.vertices[i].CwiseProduct(transform.localScale);
            if (positionToParticleMap.TryGetValue(_mesh.vertices[i].CwiseProduct(transform.localScale), out int particleIndex))
            {
                vertexIdToParticleIdMap[i] = particleIndex;
            }
            else
            {
                int nextIndex = positionToParticleMap.Count;
                positionToParticleMap[_mesh.vertices[i].CwiseProduct(transform.localScale)] = nextIndex;
                vertexIdToParticleIdMap[i] = nextIndex;
            }
        }

        Particles = new Particle[positionToParticleMap.Count + 1];
        _localParticlePositions = new Vector3[positionToParticleMap.Count];
        // Initialize particles, only first particle is used for rigid body calculations, others are solely used for collisions
        Particles[0] = new Particle(transform.position, Vector3.zero, _totalMass);

        _groundCollisionConstraints = new RigidGroundCollisionConstraints();
        int idx = 0;
        foreach (KeyValuePair<Vector3, int> particlesAndIndices in positionToParticleMap)
        {
            _groundCollisionConstraints.AddConstraint(this, particlesAndIndices.Key, idx++);
            _localParticlePositions[particlesAndIndices.Value] = particlesAndIndices.Key;
            Particles[particlesAndIndices.Value + 1] = new Particle(LocalToWorld(particlesAndIndices.Key), Vector3.zero, _totalMass);
        }
        Constraints.Add(_groundCollisionConstraints);
    }


    // Initial guess for next position and velocity
    public void Precompute(float deltaT, float maxSpeed)
    {
        if (_useGravity)
            Particles[0].V.y += ISimulationObject.GRAVITY * deltaT;

        Particles[0].P = Particles[0].X;
        Particles[0].X += Particles[0].V * deltaT;

        // Integrate angular velocity and orientation
        qPrev = q;
        _w += deltaT * InvI0.MultiplyVector(_externalTorque);

        Quaternion dq = new Quaternion(_w.x, _w.y, _w.z, 0);
        dq *= q;
        q.x += 0.5f * deltaT * dq.x;
        q.y += 0.5f * deltaT * dq.y;
        q.z += 0.5f * deltaT * dq.z;
        q.w += 0.5f * deltaT * dq.w;
        q.Normalize();
    }

    // Correct velocity to match corrected positions, needs to additionally update angular velocity
    public void CorrectVelocities(float deltaT)
    {
        Particles[0].V = (Particles[0].X - Particles[0].P) / deltaT;

        // Matthias Müller version
        Quaternion dq = q * Quaternion.Inverse(qPrev);
        //dq.Normalize();
        _w = new Vector3(dq.x, dq.y, dq.z) * (2 / deltaT);

        // Prevent incorrect flips (done by Matthias Mueller, but not sure how well this actually works; seems to cause some constant
        // flipping of _w in experiments)
        //if (dq.w < 0)
        //{
        //    _w = -_w;
        //}
    }

    public Vector3 WorldToLocal(Vector3 worldPos)
    {
        return Quaternion.Inverse(q) * (worldPos - Particles[0].X); // maybe precompute inverse quaternion
    }

    public Vector3 LocalToWorld(Vector3 localPos)
    {
        return Particles[0].X + q * localPos;
    }

    public void SolveConstraints(float deltaT)
    {
        foreach (IRigidConstraints constraint in Constraints)
        {
            constraint.SolveConstraints(Particles, deltaT);
        }
    }

    public void UpdateMesh()
    {
        transform.SetPositionAndRotation(Particles[0].X, q);
        for (int i = 0; i < _localParticlePositions.Length; i++)
        {
            Particles[i + 1].X = LocalToWorld(_localParticlePositions[i]);
        }
    }

    public float GetInverseMass(Vector3 n, Vector3 worldPos)
    {
        if (Mathf.Approximately(Particles[0].W, 0))
            return 0;

        return Particles[0].W + Vector3.Dot(Vector3.Cross(WorldToLocal(worldPos), n), InvI0.MultiplyVector(Vector3.Cross(WorldToLocal(worldPos), n)));
    }

    // Apply correction at localPos to the rigid body, assuming other object has a generalized inverse mass of zero
    public void ApplyCorrection(float compliance, Vector3 correction, Vector3 worldPos, float deltaT)
    {
        if (Mathf.Approximately(correction.sqrMagnitude, 0f))
            return;

        float C = correction.magnitude;
        Vector3 n = correction.normalized;

        // Compute generalized inverse mass
        float w = GetInverseMass(n, worldPos);

        // Note we don't have to add w2 since it's zero

        if (Mathf.Approximately(w, 0))
            return;

        float alpha = compliance / deltaT / deltaT;
        float lambda = -C / (w + alpha);

        //Debug.Log("Constraint force: " + lambda * n / (deltaT * deltaT));
        ApplyCorrection(-lambda, n, worldPos);
    }

    public void ApplyCorrection(float lambda, Vector3 n, Vector3 worldPos)
    {
        // Linear correction
        Particles[0].X += Particles[0].W * lambda * n;

        // Angular correction v1
        Vector3 w = 0.5f * lambda * InvI0.MultiplyVector(Vector3.Cross(WorldToLocal(worldPos), n));
        Quaternion dq = new Quaternion(w.x, w.y, w.z, 0) * q;
        q.x += dq.x;
        q.y += dq.y;
        q.z += dq.z;
        q.w += dq.w;
        q.Normalize();

        // Angular correction v2
        /*
        Quaternion invQ = Quaternion.Inverse(q);
        Vector3 dw = Vector3.Cross(WorldToLocal(worldPos), correction);
        dw = invQ * dw;
        dw = InvI0.MultiplyVector(dw);
        dw = q * dw;
        Quaternion dq = new Quaternion(dw.x, dw.y, dw.z, 0) * q;
        q.x += 0.5f * dq.x;
        q.y += 0.5f * dq.y;
        q.z += 0.5f * dq.z;
        q.w += 0.5f * dq.w;
        q.Normalize();
        */
    }

    protected virtual void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                Constraints.Add(_mouseFollowConstraints);

                _mouseFollowConstraints.r2 = WorldToLocal(hit.point);
                _mouseFollowConstraints.mousePos = hit.point;
                _mouseDistance = hit.distance;
            }
        }
        if (Input.GetMouseButton(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            _mouseFollowConstraints.mousePos = mouseRay.origin + mouseRay.direction * _mouseDistance;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (Constraints.Contains(_mouseFollowConstraints))
            {
                Constraints.Remove(_mouseFollowConstraints);
            }
        }

        // Use mouse wheel to adjust mouse distance
        _mouseDistance += Input.mouseScrollDelta.y * 0.1f;

        // Test saving grabbed point
        //Debug.DrawLine(Particles[0].X, LocalToWorld(r2), Color.red);

        // Visualize rotation axis
        Debug.DrawRay(Particles[0].X, _w.normalized, Color.cyan);
    }

    private void OnDrawGizmos()
    {
        if (Constraints != null && (Constraints.Contains(_mouseFollowConstraints)))
            Gizmos.DrawSphere(_mouseFollowConstraints.mousePos, 0.05f);

        if (Particles != null)
            foreach (Particle p in Particles)
            {
                Gizmos.DrawSphere(p.X, 0.05f);
            }
    }
}
