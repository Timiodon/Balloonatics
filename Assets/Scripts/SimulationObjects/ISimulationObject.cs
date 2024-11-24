using System.Collections.Generic;
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
    public const float GRAVITY = -10f;

    Particle[] Particles { get; }
    bool UseGravity { get; }
    bool HandleSelfCollision { get; }
    float Friction { get; }

    // Initialize positions, velocities, masses, etc.
    void Initialize();

    // Initial guess for next position and velocity
    void Precompute(float deltaT, float maxSpeed);

    void UpdateMesh();

    // Correct initial position guesses to satisfy constraints
    void SolveConstraints(float deltaT);

    // Correct velocity to match corrected positions
    void CorrectVelocities(float deltaT);
}
