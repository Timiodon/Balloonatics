using System.Collections.Generic;

public interface IRigidConstraints
{
    public bool AddConstraint(RigidBody rb, float stiffness);
    void SolveConstraints(Particle[] xNew, float deltaT);
}
