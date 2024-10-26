using System.Collections;
using System.Collections.Generic;

public interface IConstraints
{
    bool AddConstraint(List<Particle> particles, List<int> indices, float stiffness);
    void SolveConstraints(Particle[] xNew, float deltaT);
}
