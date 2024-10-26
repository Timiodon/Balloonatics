using System.Collections.Generic;
using UnityEngine;

public struct StretchingConstraint
{
    public StretchingConstraint(Particle p1, Particle p2, int idx1, int idx2, float stiffness)
    {
        RestLength = Vector3.Distance(p1.X, p2.X);
        Indices = (idx1, idx2);
        InvMasses = (p1.W, p2.W);
        Compliance = 1f / stiffness;
    }

    public float RestLength;
    public (int, int) Indices;
    public (float, float) InvMasses;
    public float Compliance;
}

public class StretchingConstraints : IConstraints
{
    private List<StretchingConstraint> _constraints;

    public bool AddConstraint(List<Particle> particles, List<int> indices, float stiffness)
    {
        if (particles.Count != 2)
        {
            Debug.LogError("Stretching constraint must have 2 particles");
            return false;
        }
        if (indices.Count != 2)
        {
            Debug.LogError("Stretching constraint must have 2 indices");
            return false;
        }
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }

        Particle p1 = particles[0];
        Particle p2 = particles[1];

        if (p1.W + p2.W == 0f)
        {
            Debug.LogError("Cumulative inverse mass of particles must be greater than 0");
            return false;
        }

        _constraints.Add(new StretchingConstraint(p1, p2, indices[0], indices[1], stiffness));

        return true;
    }

    public void SolveConstraints()
    {
        throw new System.NotImplementedException();
    }
}
