using System.Collections.Generic;
using UnityEngine;

public struct RigidGroundCollisionConstraint
{
    public RigidGroundCollisionConstraint(Vector3 localPos)
    {
        this.localPos = localPos;
    }
    public Vector3 localPos;
}

// Only for a single body
public class RigidGroundCollisionConstraints : IRigidConstraints
{
    private List<RigidGroundCollisionConstraint> _constraints = new();
    private RigidBody _rb;
    private float _compliance = 0f; // infinite stiffness for all collision constraints

    public bool AddConstraint(RigidBody rb, float stiffness)
    {
        return false; // not used
    }

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
            Vector3 a2 = _rb.LocalToWorld(constraint.localPos);
            // a1 is a2 projected onto the ground
            Vector3 a1 = new Vector3(a2.x, 0f, a2.z);
            Vector3 n = (a2 - a1).normalized;

            // Check if penetration is happening
            float d = -a2.y; // penetration depth
            if (d > 0f)
            {
                // Apply delta x = d*n
                // Compute generalized inverse masses
                float w1 = 0; // ground has infinite mass and cannot be rotated
                float w2 = xNew[0].W + Vector3.Dot(Vector3.Cross(constraint.localPos, n), _rb.InvI0.MultiplyVector(Vector3.Cross(constraint.localPos, n)));

                // Compute lagrange multiplier
                float lambda = -d / (w1 + w2 + _compliance / (deltaT * deltaT));

                // Update states
                xNew[0].X += w2 * lambda * n;
                Quaternion invQ = Quaternion.Inverse(_rb.q);
                Vector3 dw = Vector3.Cross(constraint.localPos, n);
                dw = invQ * dw;
                dw = _rb.InvI0.MultiplyVector(dw);
                dw = _rb.q * dw;
                Quaternion dq = new Quaternion(dw.x, dw.y, dw.z, 0) * _rb.q;
                _rb.q.x += 0.5f * lambda * dq.x;
                _rb.q.y += 0.5f * lambda * dq.y;
                _rb.q.z += 0.5f * lambda * dq.z;
                _rb.q.w += 0.5f * lambda * dq.w;
                _rb.q.Normalize();
            }
        }

    }
}
