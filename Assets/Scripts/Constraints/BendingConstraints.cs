using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Profiling;

public struct BendingConstraint
{
    public BendingConstraint(Particle[] particles, List<int> indices, float restAngle, float stiffness)
    {
        RestAngle = restAngle;
        Indices = (indices[0], indices[1], indices[2], indices[3]);
        InvMasses = (particles[indices[0]].W, particles[indices[1]].W, particles[indices[2]].W, particles[indices[3]].W);
        Compliance = 1f / stiffness;
    }

    public float RestAngle;
    public (int, int, int, int) Indices;
    public (float, float, float, float) InvMasses;
    public float Compliance;

    private bool EdgeEquals((int, int) edge1, (int, int) edge2)
    {
        return System.Math.Min(edge1.Item1, edge1.Item2) == System.Math.Min(edge2.Item1, edge2.Item2)
            && System.Math.Max(edge1.Item1, edge1.Item2) == System.Math.Max(edge2.Item1, edge2.Item2);
    }

    public bool ContainsEdge((int, int) edge)
    {
        return EdgeEquals(edge, (Indices.Item3, Indices.Item4))
            || EdgeEquals(edge, (Indices.Item1, Indices.Item3))
            || EdgeEquals(edge, (Indices.Item1, Indices.Item4))
            || EdgeEquals(edge, (Indices.Item2, Indices.Item3))
            || EdgeEquals(edge, (Indices.Item2, Indices.Item4));
    }
}

public class BendingConstraints : IClothConstraints
{
    private List<BendingConstraint> _constraints = new();
    public float ComplianceScale = 1f;
    public bool Enabled = true;

    static readonly ProfilerMarker solveMarker = new ProfilerMarker("Solve Bending Constraint");

    public bool AddConstraint(Particle[] particles, List<int> indices, float stiffness)
    {
        if (indices.Count != 4)
        {
            Debug.LogError("Bending constraint must have 4 indices");
            return false;
        }
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }

        Particle p0 = particles[indices[0]];
        Particle p1 = particles[indices[1]];
        Particle p2 = particles[indices[2]];
        Particle p3 = particles[indices[3]];
        Vector3 e = p3.X - p2.X;
        if (e.magnitude < 1e-6)
        {
            Debug.LogError("Triangles are degenerate. Shared edge length is: " + e.magnitude);
            return false;
        }

        Vector3 n1 = Vector3.Cross(p2.X - p0.X, p3.X - p0.X).normalized;
        Vector3 n2 = Vector3.Cross(p3.X - p1.X, p2.X - p1.X).normalized;
        float cosPhi = Vector3.Dot(n1, n2);

        cosPhi = Mathf.Clamp(cosPhi, -1.0f, 1.0f);
        float phi = Mathf.Acos(cosPhi);

        _constraints.Add(new BendingConstraint(particles, indices, phi, stiffness));

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        if (!Enabled)
            return;

        solveMarker.Begin();
        foreach (var constraint in _constraints)
        {
            // Code adapted from PBS Ex. 4
            var (idx0, idx1, idx2, idx3) = constraint.Indices;
            var (w0, w1, w2, w3) = constraint.InvMasses;
            var alpha = constraint.Compliance * ComplianceScale / (deltaT * deltaT);
            Particle p0 = xNew[idx0], p1 = xNew[idx1], p2 = xNew[idx2], p3 = xNew[idx3];

            Vector3 e =  p3.X - p2.X;
            float elen = e.magnitude;
            // Case triangle is degenerate
            if (elen < 1e-6)
            {
                solveMarker.End();
                return;
            }
            float invElen = 1.0f / elen;

            // Normal computation
            Vector3 n1 = Vector3.Cross(p2.X - p0.X, p3.X - p0.X);
            n1 /= n1.sqrMagnitude;
            Vector3 n2 = Vector3.Cross(p3.X - p1.X, p2.X - p1.X);
            n2 /= n2.sqrMagnitude;

            // gradient computation
            Vector3 u0 = elen * n1;
            Vector3 u1 = elen * n2;
            Vector3 u2 = Vector3.Dot((p0.X - p3.X), e) * invElen * n1 + Vector3.Dot((p1.X - p3.X), e) * invElen * n2;
            Vector3 u3 = Vector3.Dot((p2.X - p0.X), e) * invElen * n1 + Vector3.Dot((p2.X - p1.X), e) * invElen * n2;

            // Angle computation
            n1.Normalize();
            n2.Normalize();
            float cosPhi = Vector3.Dot(n1, n2);
            cosPhi = Mathf.Clamp(cosPhi, -1.0f, 1.0f);
            float phi = Mathf.Acos(cosPhi);

            // Real phi = (-0.6981317 * dot * dot - 0.8726646) * dot + 1.570796;	// fast approximation

            float lambda =
                w0 * u0.sqrMagnitude +
                w1 * u1.sqrMagnitude +
                w2 * u2.sqrMagnitude +
                w3 * u3.sqrMagnitude +
                alpha;

            if (lambda == 0.0)
            {
                solveMarker.End();
                return;
            }    

            // stability
            // 1.5 is the largest magic number I found to be stable in all cases :-)
            //if (stiffness > 0.5 && fabs(phi - b.restAngle) > 1.5)		
            //	stiffness = 0.5;

            lambda = -(phi - constraint.RestAngle) / lambda;
            if (lambda == 0.0)
            {
                solveMarker.End();
                return;
            }

            if (Vector3.Dot(Vector3.Cross(n1, n2), e) > 0.0)
                lambda = -lambda;

            xNew[idx0].X += lambda * w0 * u0;
            xNew[idx1].X += lambda * w1 * u1;
            xNew[idx2].X += lambda * w2 * u2;
            xNew[idx3].X += lambda * w3 * u3;
        }
        solveMarker.End();
    }

    public void RemoveEdgeConstraints(List<(int, int)> edges)
    {
        _constraints.RemoveAll(constraint => edges.Any(edge => constraint.ContainsEdge(edge)));
    }
}
