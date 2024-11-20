using System.Collections.Generic;

public interface IConstraints
{
    bool AddConstraint(Particle[] particles, List<int> indices, float stiffness);
    // TODO: constraints also need to update orientation for rigidbodies, maybe can return delta quaternion?
    void SolveConstraints(Particle[] xNew, float deltaT);
}
