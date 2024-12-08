using System.Collections.Generic;
using System.Data;
using UnityEngine;
using System.Linq;
using Unity.Profiling;
public struct StretchingConstraint
{
    public StretchingConstraint(Particle p1, Particle p2, int idx1, int idx2, float stiffness)
    {
        RestLength = Vector3.Distance(p1.X, p2.X);
        Indices = (idx1, idx2);
        InvMasses = (p1.W, p2.W);
        Compliance = 1f / stiffness;
    }

    public float RestLength;
    public (int, int) Indices;
    public (float, float) InvMasses;
    public float Compliance;

    public bool ContainsEdge((int, int) edge)
    {
        return System.Math.Min(Indices.Item1, Indices.Item2) == System.Math.Min(edge.Item1, edge.Item2)
            && System.Math.Max(Indices.Item1, Indices.Item2) == System.Math.Max(edge.Item1, edge.Item2);
    }
}


public class StretchingConstraints : IClothConstraints
{
    private List<StretchingConstraint> _constraints = new();
    public float ComplianceScale = 1f;
    public float TearingThreshold = 0.05f;

    public System.Action<List<(int, int)>> tearEdgesCallback;

    static readonly ProfilerMarker solveMarker = new ProfilerMarker("Solve Stretching Constraint");

    public bool AddConstraint(Particle[] particles, List<int> indices, float stiffness)
    {
        if (indices.Count != 2)
        {
            Debug.LogError("Stretching constraint must have 2 indices");
            return false;
        }
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }

        int idx0 = indices[0];
        int idx1 = indices[1];

        if (particles[idx0].W + particles[idx1].W == 0f)
        {
            Debug.LogError("Cumulative inverse mass of particles must be greater than 0");
            return false;
        }

        _constraints.Add(new StretchingConstraint(particles[idx0], particles[idx1], idx0, idx1, stiffness));

        return true;
    }

    public void ShuffleConstraintOrder()
    {
        // Shuffling the constrains helps mitigate the rotation bias when inflated
        _constraints = _constraints.OrderBy(_ => Random.Range(0f, 1f)).ToList();
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        List<(int, int)> tornEdges = null;

        solveMarker.Begin();
        foreach (var constraint in _constraints)
        {
            var (idx1, idx2) = constraint.Indices;
            var (w1, w2) = constraint.InvMasses;
            var alpha = constraint.Compliance * ComplianceScale / (deltaT * deltaT);

            float C = Vector3.Distance(xNew[idx1].X, xNew[idx2].X) - constraint.RestLength;
            // Solve constraints only if change in volume is non-zero
            if (C != 0)
            {
                var lambda = -C / (w1 + w2 + alpha);
                
                Vector3 grad1 = (xNew[idx1].X - xNew[idx2].X).normalized;

                float forceNorm = Mathf.Abs(C * constraint.Compliance * ComplianceScale);

                if (false/*forceNorm > TearingThreshold*/)
                {
                    if (tornEdges is null)
                        tornEdges = new();

                    tornEdges.Add((Mathf.Min(constraint.Indices.Item1, constraint.Indices.Item2), Mathf.Max(constraint.Indices.Item1, constraint.Indices.Item2)));
                }
                else
                {
                    xNew[idx1].X += lambda * w1 * grad1;
                    xNew[idx2].X += lambda * w1 * -grad1;
                }
            }
        }

        if (tornEdges is not null)
        {
            tearEdgesCallback(tornEdges);
        }
        solveMarker.End();
    }

    public void RemoveEdgeConstraints(List<(int, int)> edges)
    {
        _constraints.RemoveAll(constraint => edges.Any(edge => constraint.ContainsEdge(edge)));
    }

    public void ClearConstraints()
    {
        _constraints.Clear();
    }
}
