using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;
using UnityEngine.SceneManagement;

public class Solver : MonoBehaviour
{
    // GameObject because interfaces can't be serialized
    [SerializeField]
    private GameObject[] _simulationGameObjects;
    private ISimulationObject[] _simulationObjects;

    [SerializeField]
    private int _simulationLoopSubsteps = 1;

    [SerializeField]
    private CollisionHandler _collisionHandler;
    [SerializeField]
    private bool _handleCollisions = true;

    static readonly ProfilerMarker createGridMarker = new ProfilerMarker("Create Grid");
    static readonly ProfilerMarker subStepMarker = new ProfilerMarker("Substeps");
    static readonly ProfilerMarker precomputeMarker = new ProfilerMarker("Precompute");
    static readonly ProfilerMarker solveConstraintMarker = new ProfilerMarker("Solve Constraints");
    static readonly ProfilerMarker handleCollisionsMarker = new ProfilerMarker("Handle Collisions");
    static readonly ProfilerMarker correctVelocitiesMarker = new ProfilerMarker("Velocity Correction");
    static readonly ProfilerMarker updateMeshMarker = new ProfilerMarker("Update Mesh");


    void Start()
    {
        _simulationObjects = _simulationGameObjects
            .Select(go => go.GetComponent<ISimulationObject>())
            .ToArray();

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.Initialize();
        }

        _collisionHandler.Objects = _simulationObjects;
        _collisionHandler.Initialize();
    }

    void FixedUpdate()
    {
        float deltaT = Time.fixedDeltaTime;
        float scaledDeltaT = deltaT / _simulationLoopSubsteps;
        float maxSpeed = 0.2f * _collisionHandler.ParticleRadius / scaledDeltaT;

        _collisionHandler.HandleCols = _handleCollisions;
        createGridMarker.Begin();
        // If we query the grids directly after creating them with 2 times the max travelling distance in the whole FixedUpdate loop
        // we get all possible collision candidates but do not have to do this expensive call in the substep loop
        _collisionHandler.CreateGrids(2f * maxSpeed * deltaT);
        createGridMarker.End();

        subStepMarker.Begin();
        for (int i = 0; i < _simulationLoopSubsteps; i++)
        {   
            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                precomputeMarker.Begin();
                simulationObject.Precompute(scaledDeltaT, maxSpeed);
                precomputeMarker.End();
            }

            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                solveConstraintMarker.Begin();
                simulationObject.SolveConstraints(scaledDeltaT);
                solveConstraintMarker.End();
            }

            handleCollisionsMarker.Begin();
            _collisionHandler.HandleCollisions(scaledDeltaT);
            handleCollisionsMarker.End();

            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                correctVelocitiesMarker.Begin();
                simulationObject.CorrectVelocities(scaledDeltaT);
                correctVelocitiesMarker.End();
            }
        }
        subStepMarker.End();

        foreach(ISimulationObject simulationObject in _simulationObjects)
        {
            updateMeshMarker.Begin();
            simulationObject.UpdateMesh();
            updateMeshMarker.End();
        }
    }

    void Update()
    {
        // Reset scene with R
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
