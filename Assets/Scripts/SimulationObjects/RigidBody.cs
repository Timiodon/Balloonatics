using System.Collections.Generic;
using UnityEngine;

// Inspiration for this script comes from the following sources:
// - 22 - How to write a rigid body simulator: https://www.youtube.com/watch?v=euypZDssYxE
// - Deriving 3D Rigid Body Physics and implementing it in C/C++ (with intuitions): https://www.youtube.com/watch?v=4r_EvmPKOvY
[RequireComponent(typeof(MeshFilter))]
public class RigidBody : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public List<IConstraints> Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private Vector3 _externalTorque = Vector3.zero;

    public enum Shape
    {
        Cube,
        Sphere,
        Cylinder
    }

    [SerializeField]
    private Shape _shape = Shape.Cube;

    private Quaternion _q = Quaternion.identity; // Current orientation of the rigidbody
    private Quaternion _qPrev; // Previous orientation of the rigidbody
    private Matrix4x4 _invI0; // Inverse of the initial moment of inertia (note that we use a 4x4 matrix since Unity doesn't have 3x3 matrices)
    private Vector3 _w; // Angular velocity, in radians per second

    // TODO: we probably need to handle mouse constraints differently for rigidbodies, since they only have a single particle
    private MouseFollowConstraints _mouseFollowConstraints;

    public void Initialize()
    {
        Constraints = new List<IConstraints>();

        Particles = new Particle[1];
        Particles[0] = new Particle(transform.position, Vector3.zero, _totalMass);
        _mouseFollowConstraints = new MouseFollowConstraints();

        // Calculate initial, inverse moment of inertia for different shapes
        Vector3 size = GetComponent<MeshFilter>().sharedMesh.bounds.size;
        float Ixx = 1;
        float Iyy = 1;
        float Izz = 1;
        // Formulas from https://en.wikipedia.org/wiki/List_of_moments_of_inertia
        switch (_shape)
        {
            case Shape.Cube:
                Ixx = _totalMass / 12 * (size.y * size.y + size.z * size.z);
                Iyy = _totalMass / 12 * (size.x * size.x + size.z * size.z);
                Izz = _totalMass / 12 * (size.x * size.x + size.y * size.y);
                break;
            case Shape.Sphere:
                float r = size.x / 2;
                Ixx = 2 / 5 * _totalMass * r * r;
                Iyy = Ixx;
                Izz = Ixx;
                break;
            case Shape.Cylinder:
                float r1 = size.x / 2;
                float h = size.y;
                Ixx = _totalMass / 12 * (3 * r1 * r1 + h * h);
                Iyy = _totalMass / 2 * r1 * r1;
                Izz = Ixx;
                break;
        }
        _invI0 = Matrix4x4.Scale(new Vector3(1 / Ixx, 1 / Iyy, 1 / Izz));

        // Print _invIO for debugging
        //Debug.Log("_invIO: " + _invI0);
    }


    // Initial guess for next position and velocity
    void ISimulationObject.Precompute(float deltaT)
    {
        if (_useGravity)
            Particles[0].V.y += ISimulationObject.GRAVITY * deltaT;

        Particles[0].P = Particles[0].X;
        Particles[0].X += Particles[0].V * deltaT;

        // Integrate angular velocity and orientation
        _qPrev = _q;
        _w += deltaT * _invI0.MultiplyVector(_externalTorque);

        Quaternion dq = Quaternion.Euler(_w * Mathf.Rad2Deg * 0.5f * deltaT);
        _q = dq * _q;
        _q.Normalize(); // Avoid drift

        /*
        // Matthias Mueller version, which is a bit unclear to me
        Quaternion dq = Quaternion.Euler(_w * Mathf.Rad2Deg * 0.5f * deltaT) * _q;
        _q.x = _q.x + dq.x;
        _q.y = _q.y + dq.y;
        _q.z = _q.z + dq.z;
        _q.w = _q.w + dq.w;
        _q.Normalize();
        */

        // Temporary ground collision inspired by 10 min physics. We might want to replace this with a constraint later
        // TODO: should be handled differently for rigidbodies as they only have a single particle
        if (Particles[0].X.y < 0)
        {
            Particles[0].X = Particles[0].P;
            Particles[0].X.y = 0;
        }
    }

    // Correct velocity to match corrected positions, needs to additionally update angular velocity
    void ISimulationObject.CorrectVelocities(float deltaT)
    {
        Particles[0].V = (Particles[0].X - Particles[0].P) / deltaT;

        // Update angular velocity
        Quaternion dq = _q * Quaternion.Inverse(_qPrev);
        _w = 2 * dq.eulerAngles * Mathf.Deg2Rad / deltaT;

        // Prevent incorrect flips (done by Matthias Mueller, but not sure how well this actually works; seems to cause some constant
        // flipping of _w in experiments)
        //if (dq.w < 0)
        //{
        //    _w = -_w;
        //}
    }

    private Vector3 WorldToLocal(Vector3 worldPos)
    {
        return Quaternion.Inverse(_q) * (worldPos - Particles[0].X); // maybe precompute inverse quaternion
    }

    private Vector3 LocalToWorld(Vector3 localPos)
    {
        return Particles[0].X + _q * localPos;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Add constraints to follow the mouse
            _mouseFollowConstraints.ClearConstraints();

            int closestVertex = Utils.FindClosestVertex(ray, Particles, 2f);
            if (closestVertex != -1)
            {
                _mouseFollowConstraints.mousePos = Particles[closestVertex].X;
                // For now we only add constraint to center of mass particle
                _mouseFollowConstraints.AddConstraint(Particles, new List<int> { 0 }, 100f);
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
            _mouseFollowConstraints.mousePos = Utils.FindClosestPointOnRay(Camera.main.ScreenPointToRay(Input.mousePosition), Particles);
        }

        transform.position = Particles[0].X;
        transform.rotation = _q;

        // Visualize rotation axis
        Debug.DrawRay(Particles[0].X, _w.normalized, Color.cyan);
    }
}
