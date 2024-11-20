using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

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

    private Quaternion _q = Quaternion.identity; // Current orientation of the rigidbody
    private Quaternion _qPrev; // Previous orientation of the rigidbody
    private Vector3 _invI0; // Inverse of the initial moment of inertia (note that this has to be inserted on the diagonal of a 3x3 matrix)
    private Vector3 _L; // Angular momentum
    private Vector3 _w; // Angular velocity

    // TODO: we probably need to handle mouse constraints differently for rigidbodies, since they only have a single particle
    private MouseFollowConstraints _mouseFollowConstraints;

    public void Initialize()
    {
        Constraints = new List<IConstraints>();
    }


    // Initial guess for next position and velocity
    void ISimulationObject.Precompute(float deltaT)
    {
        Particles[0].V.y += ISimulationObject.GRAVITY * deltaT;
        Particles[0].P = Particles[0].X;
        Particles[0].X += Particles[0].V * deltaT;

        // TODO: integrate angular velocity and orientation
        //_qPrev = _q;

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
        // TODO: update angular velocity
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

            int closestVertex = Utils.FindClosestVertex(ray, Particles);
            if (closestVertex != -1)
            {
                _mouseFollowConstraints.mousePos = Particles[closestVertex].X;
                // For now we only add constraint to center of mass particle
                _mouseFollowConstraints.AddConstraint(Particles, new List<int> { 0 }, 1f);
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
    }
}
