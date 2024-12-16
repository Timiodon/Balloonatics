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

	public bool enabled = true;

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
		if (!enabled)
			return;

        foreach (ClothToRigidStretchingConstraint constraint in _constraints)
        {
            // 1 for softbody, 2 for rigidbody
            Vector3 a2 = _rb.LocalToWorld(constraint.RbLocalPos);
            Vector3 a1 = _sbParticles[constraint.SbParticleIndex].X;
            Vector3 n = (a2 - a1).normalized;
            float dist = Vector3.Distance(a2, a1);
            float C = dist - constraint.RestLength;

            _rb.ApplyCorrection(_compliance, -C * n, a2, deltaT, _sbParticles, constraint.SbParticleIndex);
        }
    }
}
