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
    List<IConstraints> Constraints { get; }
    bool UseGravity { get; }

    // Initialize positions, velocities, masses, etc.
    void Initialize();

    // Initial guess for next position and velocity
    void Precompute(float deltaT)
    {
        // TODO: parallelize this
        for (int i = 0; i < Particles.Length; i++)
        {
            Particles[i].V.y += GRAVITY * deltaT;
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
}
