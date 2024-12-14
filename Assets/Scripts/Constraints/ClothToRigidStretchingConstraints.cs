using System.Collections.Generic;
using UnityEngine;

public struct ClothToRigidStretchingConstraint
{
    public ClothToRigidStretchingConstraint(Vector3 rbLocalPos, int sbParticleIndex, float restLength)
    {
        RbLocalPos = rbLocalPos;
        RestLength = restLength;
        SbParticleIndex = sbParticleIndex;
    }

    public Vector3 RbLocalPos;
    public float RestLength;
    public int SbParticleIndex;
}

// Only for a single body
public class ClothToRigidStretchingConstraints : IRigidConstraints
{
    private List<ClothToRigidStretchingConstraint> _constraints = new();
    private RigidBody _rb;
    // solve constraints only allows supplying one particles array, so we need to save a reference
    // to the other
    private Particle[] _sbParticles;
    private float _compliance = 0f; // infinite stiffness for all constraints

    public bool AddConstraint(RigidBody rb, float stiffness)
    {
        return false; // not used
    }

    public bool AddConstraint(RigidBody rb, Particle[] sbParticles, int sbParticleIndex, Vector3 rbLocalPos)
    {
        if (_rb != null && _rb != rb)
        {
            Debug.LogError("A ClothToRigidStretchingConstraint instance only represents a single rigid body");
            return false;
        }
        if (_sbParticles != null && _sbParticles != sbParticles)
        {
            Debug.LogError("Softbody particles are not matching");
            return false;
        }

        _rb = rb;
        _sbParticles = sbParticles;
        float restLength = Vector3.Distance(rb.LocalToWorld(rbLocalPos), sbParticles[sbParticleIndex].X);
        _constraints.Add(new ClothToRigidStretchingConstraint(rbLocalPos, sbParticleIndex, restLength));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        foreach (ClothToRigidStretchingConstraint constraint in _constraints)
        {
            // 1 for softbody, 2 for rigidbody
            Vector3 a2 = _rb.LocalToWorld(constraint.RbLocalPos);
            Vector3 a1 = _sbParticles[constraint.SbParticleIndex].X;
            Vector3 n = (a2 - a1).normalized;
            float dist = Vector3.Distance(a2, a1);
            if (dist < constraint.RestLength)
                continue;

            float C = dist - constraint.RestLength;

            // Compute generalized inverse masses
            float w1 = _sbParticles[constraint.SbParticleIndex].W; // no invIO contribution
            float w2 = xNew[0].W + Vector3.Dot(Vector3.Cross(constraint.RbLocalPos, n), _rb.InvI0.MultiplyVector(Vector3.Cross(constraint.RbLocalPos, n)));

            // Compute lagrange multiplier
            float lambda = -C / (w1 + w2 + _compliance / (deltaT * deltaT));

            // Update states
            _sbParticles[constraint.SbParticleIndex].X += w1 * lambda * -n; // TODO: maybe use n instead?
            xNew[0].X += w2 * lambda * n;
            Vector3 w = 0.5f * lambda * _rb.InvI0.MultiplyVector(Vector3.Cross(constraint.RbLocalPos, n));
            Quaternion dq = new Quaternion(w.x, w.y, w.z, 0) * _rb.q;
            _rb.q.x = _rb.q.x + dq.x;
            _rb.q.y = _rb.q.y + dq.y;
            _rb.q.z = _rb.q.z + dq.z;
            _rb.q.w = _rb.q.w + dq.w;
            _rb.q.Normalize();
        }
    }
}
