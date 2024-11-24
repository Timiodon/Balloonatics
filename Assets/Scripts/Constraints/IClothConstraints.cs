using System.Collections.Generic;

public interface IClothConstraints
{
    bool AddConstraint(Particle[] particles, List<int> indices, float stiffness);
    void SolveConstraints(Particle[] xNew, float deltaT);
}
