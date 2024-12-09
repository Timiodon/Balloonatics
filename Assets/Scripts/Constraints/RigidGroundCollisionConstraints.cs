using System.Collections.Generic;
using UnityEngine;

public struct RigidGroundCollisionConstraint
{
    public RigidGroundCollisionConstraint(Vector3 localPos, int vertexId)
    {
        this.localPos = localPos;
        previousDepth = 0f;
        this.vertexId = vertexId;
    }
    public Vector3 localPos;
    public float previousDepth;
    public int vertexId;
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

    public bool AddConstraint(RigidBody rb, Vector3 localPos, int vertexId)
    {
        if (_rb != null && _rb != rb)
        {
            Debug.LogError("A RigidGroundCollisionConstraints instance only represents a single body");
            return false;
        }

        _rb = rb;
        _constraints.Add(new RigidGroundCollisionConstraint(localPos, vertexId));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        for (int i = 0; i < _constraints.Count; i++)
        {
            RigidGroundCollisionConstraint constraint = _constraints[i];
            Vector3 a2 = _rb.LocalToWorld(constraint.localPos);
            // a1 is a2 projected onto the ground

            // Check if penetration is happening
            float d = -a2.y; // penetration depth
            if (d > 0f)
            {
                Debug.Log("Vertex " + constraint.vertexId + " penetrates with depth " + d);
                if (d > constraint.previousDepth && constraint.previousDepth > 0)
                {
                    Debug.LogWarning("Penetration depth increased");
                    //d = constraint.previousDepth;
                }
                _rb.ApplyCorrection(_compliance, d * Vector3.up, a2, deltaT);
            }
            //constraint.previousDepth = d;
        }

    }
}
