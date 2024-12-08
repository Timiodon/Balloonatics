using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Unity.Profiling;


public class OverpressureConstraints : IClothConstraints
{
    public Dictionary<int, int[]> TriangleToParticleIndices;
    public float Pressure = 1f;

    private Vector3[] _gradients;
    private float _compliance;
    private float _V0 = 0f; // Initial volume

    private bool _popped = false;

    static readonly ProfilerMarker solveMarker = new ProfilerMarker("Solve Overpressure constraint");

    private float ComputeVolume(Particle[] particles)
    {
        float volume = 0f;
        foreach (KeyValuePair<int, int[]> pair in TriangleToParticleIndices)
        {
            int[] indices = pair.Value;
            volume += Vector3.Dot(Vector3.Cross(particles[indices[0]].X, particles[indices[1]].X), particles[indices[2]].X);
        }
        return volume;
    }

    public bool AddConstraint(Particle[] particles, List<int> _, float stiffness)
    {
        if (_gradients != null)
        {
            Debug.LogError("Overpressure constraint already exists");
            return false;
        }
        if (stiffness <= 0f)
        {
            Debug.LogError("Stiffness must be greater than 0");
            return false;
        }
        if (TriangleToParticleIndices == null)
        {
            Debug.LogError("Triangle to particle indices dict is uninitialized");
            return false;
        }

        _compliance = 1f / stiffness;
        _gradients = new Vector3[particles.Length];

        _V0 = ComputeVolume(particles);

        return true;
    }

    public void SolveConstraints(Particle[] xNew, float deltaT)
    {
        if (_popped)
            return;

        solveMarker.Begin();
        float V = ComputeVolume(xNew);
        float C = V - Pressure * _V0;

        // Solve constraints only if change in volume is non-zero
        if (C != 0)
        {
            for (int i = 0; i < xNew.Length; i++)
            {
                _gradients[i] = Vector3.zero;
            }

            foreach (KeyValuePair<int, int[]> pair in TriangleToParticleIndices)
            {
                int[] indices = pair.Value;

                _gradients[indices[0]] += Vector3.Cross(xNew[indices[1]].X, xNew[indices[2]].X);
                _gradients[indices[1]] += Vector3.Cross(xNew[indices[2]].X, xNew[indices[0]].X);
                _gradients[indices[2]] += Vector3.Cross(xNew[indices[0]].X, xNew[indices[1]].X);
            }

            float alpha = _compliance / (deltaT * deltaT);
            float lambda_denom = alpha;
            for (int i = 0; i < xNew.Length; i++)
            {
                lambda_denom += xNew[i].W * Vector3.Dot(_gradients[i], _gradients[i]);
            }

            if (lambda_denom != 0)
            {
                float deltaLambda = -C / lambda_denom; // We don't need - alpha * lambda term since we only do one iteration, right?
                for (int i = 0; i < xNew.Length; i++)
                {
                    xNew[i].X += deltaLambda * xNew[i].W * _gradients[i];
                }
            }
        }
        solveMarker.End();
    }

    public void RemoveEdgeConstraints(List<(int, int)> edges)
    {
		// I'm not sure what to do with the pressure after the balloon popped.
		_popped = true;
	}
}
