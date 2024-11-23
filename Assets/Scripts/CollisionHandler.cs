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
                    tmp[i] = obj.Particles[j].X;
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
            // Handle inter-object collisions here


            for(int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].HandleSelfCollision)
                    HandleSelfCollision(i, deltaT);
            }
        }
    }

    private void HandleSelfCollision(int objIdx, float deltaT)
    {

    }
}
