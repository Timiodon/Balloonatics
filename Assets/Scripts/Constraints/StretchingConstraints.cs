using System.Collections.Generic;
using System.Data;
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
    private List<StretchingConstraint> _constraints = new();

    public bool AddConstraint(Particle[] particles, List<int> indices, float stiffness)
    {
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

        int idx0 = indices[0];
        int idx1 = indices[1];

        if (particles[idx0].W + particles[idx1].W == 0f)
        {
            Debug.LogError("Cumulative inverse mass of particles must be greater than 0");
            return false;
        }

        _constraints.Add(new StretchingConstraint(particles[idx0], particles[idx1], idx0, idx1, stiffness));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        foreach (var constraint in _constraints)
        {
            var (idx1, idx2) = constraint.Indices;
            var (w1, w2) = constraint.InvMasses;
            var alpha = constraint.Compliance / (deltaT * deltaT);

            var lambda = -(Vector3.Distance(xNew[idx1].X, xNew[idx2].X) - constraint.RestLength) / (w1 + w2 + alpha);
            Vector3 grad1 = (xNew[idx1].X - xNew[idx2].X).normalized;

            xNew[idx1].X += lambda * w1 * grad1;
            xNew[idx2].X += lambda * w1 * -grad1;
        }
    }

    public void ClearConstraints()
    {
        _constraints.Clear();
    }
}
