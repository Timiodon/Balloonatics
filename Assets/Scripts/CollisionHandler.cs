using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Inspiration for the collision handling comes from https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/15-selfCollision.html
/// </summary>
public class CollisionHandler : MonoBehaviour
{
    [SerializeField]
    private float _particleRadius = 0.01f;
    [SerializeField]
    private float _selfCollisionDistanceScale = 2.0f;
    [SerializeField]
    private float _interObjecCollisiontDistanceScale = 2.5f;
    public float ParticleRadius { get => _particleRadius; }
    // Take care to first call Create after updating this property
    public bool HandleCols { get; set; }

    // Grid used for collision handling between different objects
    private SpatialHashGrid _globalGrid;
    private int _totalNumberOfParticles = 0;
    private Particle[] _allParticles;
    // allParticlesIndex => (ObjectsIndex, ObjectParticlesIndex)
    private Dictionary<int, (int, int)> _globalToLocalIndex;

    public ISimulationObject[] Objects { get; set; }

    // ObjectsIndex => originalParticlePositions
    private Dictionary<int, Vector3[]> _originalPositions;



    static readonly ProfilerMarker createGridMarker = new ProfilerMarker("Create Spatialhashgrid");
    static readonly ProfilerMarker queryAllMarker = new ProfilerMarker("QueryAll Spatialhashgrid");
    static readonly ProfilerMarker collisionsMarker = new ProfilerMarker("Resolve Collisions");


    public void Initialize()
    {
        if (Objects == null)
        {
            UnityEngine.Debug.LogError("Objects array is not set.");
            return;
        }

        _originalPositions = new();
        _globalToLocalIndex = new();
        for (int i = 0; i < Objects.Length; i++)
        {
            var obj = Objects[i];
            for (int j = 0; j < obj.Particles.Length; j++)
            {
                _globalToLocalIndex[_totalNumberOfParticles + j] = (i, j);
            }
            _totalNumberOfParticles += obj.Particles.Length;

            Vector3[] tmp = new Vector3[obj.Particles.Length];
            for (int j = 0; j < tmp.Length; j++)
            {
                tmp[j] = obj.Particles[j].X;
            }
            _originalPositions[i] = tmp;

        }

        _globalGrid = new(2 * _particleRadius, _totalNumberOfParticles);
        _allParticles = new Particle[_totalNumberOfParticles];
    }


    public void CreateGrids(float maxTravelDist)
    {
        if (HandleCols)
        {
            createGridMarker.Begin();
            int _currentAllParticleIdx = 0;
            for(int i = 0; i < Objects.Length; i++)
            {
                var obj = Objects[i];
                Array.Copy(obj.Particles, 0, _allParticles, _currentAllParticleIdx, obj.Particles.Length);
                _currentAllParticleIdx += obj.Particles.Length;
            }

            _globalGrid.Create(_allParticles);
            createGridMarker.End();
            queryAllMarker.Begin();
            _globalGrid.QueryAll(_allParticles, maxTravelDist);
            queryAllMarker.End();
        }
    }

    public void HandleCollisions(float deltaT)
    {
        if (HandleCols)
        {
            collisionsMarker.Begin();
            for (int i = 0; i < _allParticles.Length; i++)
            {
                var (objIdx, objParticleIdx) = _globalToLocalIndex[i];
                var obj = Objects[objIdx];

                // Actually, if only one particle has infinite mass, we should still move the other one.
                // However, Mathias does not do this and it might be an unlikely case anyway, because
                // soft bodies are probably not fixed (that would be rigidbodies, which we don't move anyway)
                if (obj.Particles[objParticleIdx].W == 0.0f)
                    continue;

                int first = _globalGrid.FirstAdjID[i];
                int last = _globalGrid.FirstAdjID[i + 1];

                for (int j = first; j < last; j++)
                {
                    var (neighbourObjIdx, neighbourObjParticleIdx) = _globalToLocalIndex[_globalGrid.AdjIDs[j]];

                    // Self-Collisions are handled separately
                    if (objIdx == neighbourObjIdx)
                    {
                        if (obj.HandleSelfCollision)
                            HandleSelfCollision(obj, _originalPositions[objIdx], objParticleIdx, neighbourObjParticleIdx);
                    }
                    else
                    {
                        if (obj.HandleInterObjectCollisions || Objects[neighbourObjIdx].HandleInterObjectCollisions)
                            HandleInterObjectCollision(obj, Objects[neighbourObjIdx], objParticleIdx, neighbourObjParticleIdx);
                    }
                }
            }
            collisionsMarker.End();
        }
    }

    private void HandleSelfCollision(ISimulationObject obj, Vector3[] originalPos, int particleIdx, int neighbourParticleIdx)
    {
        float thickness = _selfCollisionDistanceScale * _particleRadius;
        float minDistance2 = thickness * thickness;

        if (obj.Particles[neighbourParticleIdx].W == 0.0f)
            return;

        // handle potential duplicates or case i = neighbourID
        Vector3 collisionDir = obj.Particles[neighbourParticleIdx].X - obj.Particles[particleIdx].X;
        float dist2 = collisionDir.sqrMagnitude;
        if (dist2 > minDistance2 || dist2 == 0.0f)
            return;

        // To avoid jittering, we cannot enforce minDistance2 if the particles are closer than that in the original 
        // undeformed state
        float restDist2 = (originalPos[particleIdx] - originalPos[neighbourParticleIdx]).sqrMagnitude;
        if (dist2 > restDist2)
            return;


        float minDist = thickness;
        if (restDist2 < minDistance2)
            minDist = (originalPos[particleIdx] - originalPos[neighbourParticleIdx]).magnitude;

        // position correction
        float dist = collisionDir.magnitude;
        float corrScale = 0.5f * (minDist - dist) / dist;
        obj.Particles[particleIdx].X -= corrScale * collisionDir;
        obj.Particles[neighbourParticleIdx].X += corrScale * collisionDir;

        if (obj.Friction > 0.0f)
        {
            // If there is friction, make position correction more realistic by having the two particles move
            // away again at a more similar veloctiy. Note that the timestep cancels out in those equations
            Vector3 velI = obj.Particles[particleIdx].X - obj.Particles[particleIdx].P;
            Vector3 velNeighbour = obj.Particles[neighbourParticleIdx].X - obj.Particles[neighbourParticleIdx].P;
            Vector3 avgVel = 0.5f * (velI + velNeighbour);

            obj.Particles[particleIdx].X += obj.Friction * (avgVel - velI);
            obj.Particles[neighbourParticleIdx].X += obj.Friction * (avgVel - velNeighbour);
        }
    }

    private void HandleInterObjectCollision(ISimulationObject obj, ISimulationObject neighbourObj, int objParticleIdx, int neighbourObjParticleIdx)
    {
        float minDist = _interObjecCollisiontDistanceScale * _particleRadius;
        float minDist2 = minDist * minDist;

        if (neighbourObj.Particles[neighbourObjParticleIdx].W == 0.0f)
            return;

        // handle potential false positives
        Vector3 collisionDir = neighbourObj.Particles[neighbourObjParticleIdx].X - obj.Particles[objParticleIdx].X;
        float dist2 = collisionDir.sqrMagnitude;
        if (dist2 > minDist2 || dist2 == 0.0f)
            return;

        // position correction
        float dist = collisionDir.magnitude;
        float corrScale = 0.5f * (minDist - dist) / dist;
        float massObj = obj.Particles[objParticleIdx].M;
        float massNeighbour = neighbourObj.Particles[neighbourObjParticleIdx].M;
        float totalMass = massObj + massNeighbour;
        if (obj.HandleInterObjectCollisions)
        {
            if (obj is RigidBody body)
            {
                float weight = 1f - (massObj / totalMass);
                body.ApplyCorrection(-corrScale * weight, collisionDir, obj.Particles[objParticleIdx].X);
            }
            else
                obj.Particles[objParticleIdx].X -= corrScale * collisionDir;
        }

        if (neighbourObj.HandleInterObjectCollisions)
        {
            if (neighbourObj is RigidBody neighbourBody)
            {
                float weight = 1f - (massNeighbour / totalMass);
                neighbourBody.ApplyCorrection(corrScale * weight, collisionDir, neighbourObj.Particles[neighbourObjParticleIdx].X);
            }
            else
                neighbourObj.Particles[neighbourObjParticleIdx].X += corrScale * collisionDir;
        }

        // apply momentum conservation
        obj.Particles[objParticleIdx].V = 
            ((massObj - massNeighbour) / totalMass) * obj.Particles[objParticleIdx].V + (2f * massNeighbour / totalMass) * neighbourObj.Particles[neighbourObjParticleIdx].V;
        neighbourObj.Particles[neighbourObjParticleIdx].V =
            ((massNeighbour - massObj) / totalMass) * neighbourObj.Particles[neighbourObjParticleIdx].V + (2f * massObj / totalMass) * obj.Particles[objParticleIdx].V;
    }
}
