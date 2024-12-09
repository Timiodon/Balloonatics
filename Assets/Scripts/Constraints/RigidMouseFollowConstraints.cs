using System.Collections.Generic;
using UnityEngine;

public class RigidMouseFollowConstraints : IRigidConstraints
{
    public Vector3 mousePos;
    public Vector3 r2;

    private RigidBody _rb;
    private float _restLength = 0f; // the way this is set up, rest length is always 0
    private float _compliance;

    public bool AddConstraint(RigidBody rb, float stiffness)
    {
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }

        if (_rb != null)
        {
            Debug.LogError("Mouse follow constraint already exists");
            return false;
        }

        _rb = rb;
        _compliance = 1f / stiffness;

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        // a1 is mousePos, a2 is r2 in world space
        Vector3 a1 = mousePos;
        Vector3 a2 = _rb.LocalToWorld(r2);
        Vector3 n = (a2 - a1).normalized;
        float C = Vector3.Distance(a2, mousePos) - _restLength;

        _rb.ApplyCorrection(_compliance, -C * n, a2, deltaT);

        // Visualize mouse follow constraint
        Debug.DrawLine(a1, a2, Color.red);
    }
}
