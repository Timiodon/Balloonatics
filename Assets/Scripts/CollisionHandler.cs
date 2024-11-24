using System.Collections.Generic;
using UnityEngine;

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
    // allParticlesIndex => (ObjectsIndex, ObjectParticlesIndex)
    private Dictionary<int, (int, int)> _globalToLocalIndex;

    public ISimulationObject[] Objects { get; set; }

    // uniqueID => selfCollisionHashGrid
    private Dictionary<int, SpatialHashGrid> _selfCollisionGrids;
    // uniqueID => originalParticlePositions
    private Dictionary<int, Vector3[]> _originalPositions;


    public void Initialize()
    {
        if (Objects == null)
        {
            Debug.LogError("Objects array is not set.");
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
                _selfCollisionGrids[i] = new(2 * _particleRadius, obj.Particles.Length);
                Vector3[] tmp = new Vector3[obj.Particles.Length];
                for (int j = 0; j < tmp.Length; j++)
                {
                    tmp[j] = obj.Particles[j].X;
                }
                _originalPositions[i] = tmp;
            }
        }

        _globalGrid = new(2 * _particleRadius, _totalNumberOfParticles);
    }


    public void CreateGrids(float maxTravelDist)
    {
        if (HandleCols)
        {
            List<Particle> allParticles = new();
            foreach(KeyValuePair<int, SpatialHashGrid> pair in _selfCollisionGrids)
            {
                pair.Value.Create(Objects[pair.Key].Particles);
                pair.Value.QueryAll(Objects[pair.Key].Particles, maxTravelDist);
                allParticles.AddRange(Objects[pair.Key].Particles);
            }

            var allParticlesArr = allParticles.ToArray();
            _globalGrid.Create(allParticlesArr);
            _globalGrid.QueryAll(allParticlesArr, maxTravelDist);
        }
    }

    public void HandleCollisions(float deltaT)
    {
        if (HandleCols)
        {
            HandleInterObjectCollisions(deltaT);

            for(int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].HandleSelfCollision)
                    HandleSelfCollisions(i);
            }
        }
    }

    private void HandleSelfCollisions(int objIdx)
    {
        float minDistance2 = (2 * _particleRadius) * (2 * _particleRadius);
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
                float dist = collisionDir.magnitude;
                if (dist2 > minDistance2 || dist2 == 0.0f)
                    continue;

                // To avoid jittering, we cannot enforce minDistance2 if the particles are closer than that in the original 
                // undeformed state
                float restDist2 = (originalPos[i] - originalPos[neighbourID]).sqrMagnitude;
                float restDist = (originalPos[i] - originalPos[neighbourID]).magnitude;
                float minDist = 2 * _particleRadius;
                if (dist2 > restDist2)
                    continue;
                if (restDist2 < minDistance2)
                    minDist = restDist;

                // position correction
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

    }
}
