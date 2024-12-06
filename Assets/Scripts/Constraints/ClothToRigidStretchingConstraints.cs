using System.Collections.Generic;
using UnityEngine;

public struct ClothToRigidStretchingConstraint
{
    public ClothToRigidStretchingConstraint(Vector3 rbLocalPos, int sbParticleIndex, Particle sbParticle, float restLength)
    {
        this.rbLocalPos = rbLocalPos;
        this.RestLength = restLength;
        this.sbParticleIndex = sbParticleIndex;
        this.sbInvMass = sbParticle.W;

    }

    public Vector3 rbLocalPos;
    public float RestLength;
    public int sbParticleIndex;
    public float sbInvMass;
    public float rbInvMass;
    public float Compliance;
}

// Only for a single body
public class ClothToRigidStretchingConstraints : IRigidConstraints
{
    private List<ClothToRigidStretchingConstraint> _constraints = new();
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
            Debug.LogError("A ClothToRigidStretchingConstraint instance only represents a single rigid body");
            return false;
        }

        _rb = rb;
        _constraints.Add(new ClothToRigidStretchingConstraint(localPos));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        foreach (ClothToRigidStretchingConstraint constraint in _constraints)
        {
            Vector3 a2 = _rb.LocalToWorld(constraint.rbLocalPos);
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
                float w2 = xNew[0].W + Vector3.Dot(Vector3.Cross(constraint.rbLocalPos, n), _rb.InvI0.MultiplyVector(Vector3.Cross(constraint.rbLocalPos, n)));

                // Compute lagrange multiplier
                float lambda = -d / (w1 + w2 + _compliance / (deltaT * deltaT)); // TODO: check if should be d not -d

                // Update states
                xNew[0].X += w2 * lambda * n;
                Vector3 w = 0.5f * lambda * _rb.InvI0.MultiplyVector(Vector3.Cross(constraint.rbLocalPos, n));
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
