using System.Collections.Generic;
using System.Data;
using UnityEngine;

public struct MouseFollowConstraint
{
    public MouseFollowConstraint(Particle p, Vector3 mousePos, int idx, float stiffness)
    {
        RestLength = Vector3.Distance(p.X, mousePos);
        Index = idx;
        InvMass = p.W;
        Compliance = 1f / stiffness;
    }

    public float RestLength;
    public int Index;
    public float InvMass;
    public float Compliance;
}

public class MouseFollowConstraints : IConstraints
{
    public Vector3 mousePos;

    private List<MouseFollowConstraint> _constraints = new();
    private float _invMouseMass = 0.1f;

    public bool AddConstraint(Particle[] particles, List<int> indices, float stiffness)
    {
        if (indices.Count != 1)
        {
            Debug.LogError("Mouse follow constraint must have exactly 1 index");
            return false;
        }
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }

        int idx = indices[0];

        if (particles[idx].W == 0f)
        {
            Debug.LogError("Inverse mass of particle must be greater than 0");
            return false;
        }

        _constraints.Add(new MouseFollowConstraint(particles[idx], mousePos, idx, stiffness));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        foreach (var constraint in _constraints)
        {
            var idx = constraint.Index;
            var w1 = constraint.InvMass;
            var alpha = constraint.Compliance / (deltaT * deltaT);

            var lambda = -(Vector3.Distance(xNew[idx].X, mousePos) - constraint.RestLength) / (w1 + _invMouseMass + alpha);
            Vector3 grad1 = (xNew[idx].X - mousePos).normalized;

            xNew[idx].X += lambda * w1 * grad1;
        }
    }

    public void ClearConstraints()
    {
        _constraints.Clear();
    }
}
