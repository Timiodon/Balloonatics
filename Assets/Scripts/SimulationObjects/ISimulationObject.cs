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
    public Vector3 X;

    // Velocity
    public Vector3 V;

    // Inverse mass
    public float W;

    // Previous position
    public Vector3 P;
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
    void Precompute(float deltaT);

    // Correct initial position guesses to satisfy constraints
    void SolveConstraints(float deltaT)
    {
        foreach (IConstraints constraint in Constraints)
        {
            constraint.SolveConstraints(Particles, deltaT);
        }
    }

    // Correct velocity to match corrected positions
    void CorrectVelocities(float deltaT)
    {
        // TODO: parallelize this
        for (int i = 0; i < Particles.Length; i++)
        {
            Particles[i].V = (Particles[i].X - Particles[i].P) / deltaT;
        }
    }

    void ResolveGroundCollision();
}
