using UnityEngine;

public struct Particle
{
    public Particle(Vector3 x, Vector3 v, float m)
    {
        X = x;
        V = v;
        W = 1f / m;
        P = x;
    }

    // Position
    public Vector3 X { get; set; }

    // Velocity
    public Vector3 V { get; set; }

    // Inverse mass
    public float W { get; set; }

    // Previous position
    public Vector3 P { get; set; }
}

public interface ISimulationObject
{
    Particle[] Particles { get; }
    IConstraints[] Constraints { get; }
    Vector3[] Accelerations { get; set; }
    bool CollideWithGround { get; }

    // Initialize positions, velocities, masses, etc.
    void Initialize();

    // Initial guess for next position and velocity
    void Precompute();

    // Correct initial guesses to satisfy constraints
    void SolveConstraints();

    void ResolveGroundCollision();
}
