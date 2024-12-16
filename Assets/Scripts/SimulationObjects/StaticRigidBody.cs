using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Just do nothing in all the simulation methods
/// </summary>
public class StaticRigidBody : RigidBody
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Precompute(float deltaT, float maxSpeed)
    {
        return;
    }

    public override void CorrectVelocities(float deltaT)
    {
        return;
    }

    public override void SolveConstraints(float deltaT)
    {
        return;
    }

    public override void UpdateMesh()
    {
        return;
    }

    protected override void Update()
    {
        return;
    }
}
