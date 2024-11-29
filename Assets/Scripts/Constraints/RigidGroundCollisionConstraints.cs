using System.Collections.Generic;
using UnityEngine;

public struct RigidGroundCollisionConstraint
{
    public RigidGroundCollisionConstraint(Vector3 localPos) {}
    public Vector3 localPos;
}

// Only for a single body
public class RigidGroundCollisionConstraints : IRigidConstraints
{
    private List<RigidGroundCollisionConstraint> _constraints = new();
    private RigidBody _rb;
    private float _invGroundMass = 0.001f;

    public bool AddConstraint(RigidBody rb, Vector3 localPos)
    {
        if (_rb != null && _rb != rb)
        {
            Debug.LogError("A RigidGroundCollisionConstraints instance only represents a single body");
            return false;
        }
        
        _rb = rb;
        _constraints.Add(new RigidGroundCollisionConstraint(localPos));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        foreach (RigidGroundCollisionConstraint constraint in _constraints)
        {
            Vector3 a2 = _rb.LocalToWorld(r2);
            // a1 is a2 projected onto the ground
            Vector3 a1 = new Vector3(a2.x, 0f, a2.z);
            Vector3 r1 = _rb.WorldToLocal(a1);
            Vector3 n = (a2 - a1).normalized;

            // Check if penetration is happening
            float d = -a2.y; // penetration depth
            if (d > 0f) {
                // Apply delta x = d*n    
                // Compute generalized inverse masses
                // TODO: what should InvI0 be for the ground?
                float w1 = _invGroundMass; // + Vector3.Dot(Vector3.Cross(r1, n), _rb.InvI0.MultiplyVector(Vector3.Cross(r1, n)));
                float w2 = xNew[0].W + Vector3.Dot(Vector3.Cross(r2, n), _rb.InvI0.MultiplyVector(Vector3.Cross(r2, n)));

                // Compute lagrange multiplier
                float lambda = -d / (w1 + w2 + _compliance / (deltaT * deltaT)); // TODO: check if should be d not -d

                // Update states
                xNew[0].X += w2 * lambda * n;
                Vector3 w = 0.5f * lambda * _rb.InvI0.MultiplyVector(Vector3.Cross(r2, n));
                Quaternion dq = new Quaternion(w.x, w.y, w.z, 0) * _rb.q;
                _rb.q.x = _rb.q.x + dq.x;
                _rb.q.y = _rb.q.y + dq.y;
                _rb.q.z = _rb.q.z + dq.z;
                _rb.q.w = _rb.q.w + dq.w;
                _rb.q.Normalize();
            }
        }

    }
}
