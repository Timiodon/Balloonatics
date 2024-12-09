using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Inspiration for the collision handling comes from https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/15-selfCollision.html
/// </summary>
public class CollisionHandler : MonoBehaviour
{
    [SerializeField]
    private float _particleRadius = 0.01f;
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

    // uniqueID => selfCollisionHashGrid
    private Dictionary<int, SpatialHashGrid> _selfCollisionGrids;
    // uniqueID => originalParticlePositions
    private Dictionary<int, Vector3[]> _originalPositions;



    static readonly ProfilerMarker createGridMarker = new ProfilerMarker("Create Local Grids");
    static readonly ProfilerMarker queryAllMarker = new ProfilerMarker("QueryAll Local Grids");
    static readonly ProfilerMarker selfCollisionsMarker = new ProfilerMarker("Handle Self-Collisions");
    static readonly ProfilerMarker globalCollisionsMarker = new ProfilerMarker("Create and query Global Grid");
    static readonly ProfilerMarker globalCollisionsResolverMarker = new ProfilerMarker("Resolve Global Grid");


    public void Initialize()
    {
        if (Objects == null)
        {
            UnityEngine.Debug.LogError("Objects array is not set.");
            return;
        }

        _selfCollisionGrids = new();
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

            if (obj.HandleSelfCollision)
            {
                // adapt hashcell size to particle density of resting object
                _selfCollisionGrids[i] = new(2 * Mathf.Max(_particleRadius, obj.HashcellSize), obj.Particles.Length);
                Vector3[] tmp = new Vector3[obj.Particles.Length];
                for (int j = 0; j < tmp.Length; j++)
                {
                    tmp[j] = obj.Particles[j].X;
                }
                _originalPositions[i] = tmp;
            }
        }

        _globalGrid = new(2 * _particleRadius, _totalNumberOfParticles);
        _allParticles = new Particle[_totalNumberOfParticles];
    }


    public void CreateGrids(float maxTravelDist)
    {
        if (HandleCols)
        {
            int _currentAllParticleIdx = 0;
            for(int i = 0; i < Objects.Length; i++)
            {
                var obj = Objects[i];
                if (obj.HandleSelfCollision)
                {
                    var grid = _selfCollisionGrids[i];
                    createGridMarker.Begin();
                    grid.Create(obj.Particles);
                    createGridMarker.End();
                    queryAllMarker.Begin();
                    grid.QueryAll(obj.Particles, maxTravelDist);
                    queryAllMarker.End();
                }
                Array.Copy(obj.Particles, 0, _allParticles, _currentAllParticleIdx, obj.Particles.Length);
                _currentAllParticleIdx += obj.Particles.Length;
            }

            globalCollisionsMarker.Begin();
            _globalGrid.Create(_allParticles);
            _globalGrid.QueryAll(_allParticles, maxTravelDist);
            globalCollisionsMarker.End();
        }
    }

    public void HandleCollisions(float deltaT)
    {
        if (HandleCols)
        {
            globalCollisionsResolverMarker.Begin();
            HandleInterObjectCollisions(deltaT);
            globalCollisionsResolverMarker.End();

            for(int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].HandleSelfCollision)
                {
                    selfCollisionsMarker.Begin();
                    HandleSelfCollisions(i);
                    selfCollisionsMarker.End();
                }
            }
        }
    }

    private void HandleSelfCollisions(int objIdx)
    {
        float thickness = 2 * _particleRadius;
        float minDistance2 = thickness * thickness;
        var obj = Objects[objIdx];
        var grid = _selfCollisionGrids[objIdx];
        var originalPos = _originalPositions[objIdx];

        for (int i = 0; i < obj.Particles.Length; i++)
        {
            // Actually, if only one particle has infinite mass, we should still move the other one.
            // However, Mathias does not do this and it might be an unlikely case anyway, because
            // only external points to the soft body will probably be fixed
            if (obj.Particles[i].W == 0.0f)
                continue;

            int first = grid.FirstAdjID[i];
            int last = grid.FirstAdjID[i + 1];

            for (int j = first; j < last; j++)
            {
                int neighbourID = grid.AdjIDs[j];
                if (obj.Particles[neighbourID].W == 0.0f)
                    continue;

                // handle potential duplicates or case i = neighbourID
                Vector3 collisionDir = obj.Particles[neighbourID].X - obj.Particles[i].X;
                float dist2 = collisionDir.sqrMagnitude;
                if (dist2 > minDistance2 || dist2 == 0.0f)
                    continue;

                // To avoid jittering, we cannot enforce minDistance2 if the particles are closer than that in the original 
                // undeformed state
                float restDist2 = (originalPos[i] - originalPos[neighbourID]).sqrMagnitude;
                if (dist2 > restDist2)
                    continue;

                float minDist = thickness;
                if (restDist2 < minDistance2)
                    minDist = (originalPos[i] - originalPos[neighbourID]).magnitude; 

                // position correction
                float dist = collisionDir.magnitude;
                float corrScale = 0.5f * (minDist - dist) / dist;
                obj.Particles[i].X -= corrScale * collisionDir;
                obj.Particles[neighbourID].X += corrScale * collisionDir;

                // If there is friction, make position correction more realistic by having the two particles move
                // away again at a more similar veloctiy. Note that the timestep cancels out in those equations
                Vector3 velI = obj.Particles[i].X - obj.Particles[i].P;
                Vector3 velNeighbour = obj.Particles[neighbourID].X - obj.Particles[neighbourID].P;
                Vector3 avgVel = 0.5f * (velI + velNeighbour);

                obj.Particles[i].X += obj.Friction * velI;
                obj.Particles[neighbourID].X += obj.Friction * velNeighbour;
            }
        }
    }

    private void HandleInterObjectCollisions(float deltaT)
    {
        float minDist = 2.5f * _particleRadius;
        float minDist2 = minDist * minDist;

        for (int i = 0; i < _allParticles.Length; i++)
        {
            var (objIdx, objParticleIdx) = _globalToLocalIndex[i];
            var obj = Objects[objIdx];

            // Actually, if only one particle has infinite mass, we should still move the other one.
            // However, Mathias does not do this and it might be an unlikely case anyway, because
            // only external points to the soft body will probably be fixed
            if (obj.Particles[objParticleIdx].W == 0.0f)
                continue;

            int first = _globalGrid.FirstAdjID[i];
            int last = _globalGrid.FirstAdjID[i + 1];

            for (int j = first; j < last; j++)
            {
                var (neighbourObjIdx, neighbourObjParticleIdx) = _globalToLocalIndex[_globalGrid.AdjIDs[j]];

                // Self-Collisions are handled separately
                if (objIdx == neighbourObjIdx)
                    continue;

                var neighbourObj = Objects[neighbourObjIdx];

                if (neighbourObj.Particles[neighbourObjParticleIdx].W == 0.0f)
                    continue;

                // handle potential duplicates or case i = neighbourID
                Vector3 collisionDir = neighbourObj.Particles[neighbourObjParticleIdx].X - obj.Particles[objParticleIdx].X;
                float dist2 = collisionDir.sqrMagnitude;
                if (dist2 > minDist2 || dist2 == 0.0f)
                    continue;


                // position correction
                float dist = collisionDir.magnitude;
                float corrScale = 0.5f * (minDist - dist) / dist;
                obj.Particles[objParticleIdx].X -= corrScale * collisionDir;
                neighbourObj.Particles[neighbourObjParticleIdx].X += corrScale * collisionDir;
            }
        }
    }
}
