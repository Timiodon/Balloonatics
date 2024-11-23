using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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
    

    void Start()
    {
        _simulationObjects = _simulationGameObjects
            .Select(go => go.GetComponent<ISimulationObject>())
            .ToArray();

        _collisionHandler.Objects = _simulationObjects;

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.Initialize();
        }
    }

    void FixedUpdate()
    {
        float deltaT = Time.fixedDeltaTime;
        float scaledDeltaT = deltaT / _simulationLoopSubsteps;
        float maxSpeed = 0.2f * _collisionHandler.ParticleRadius / scaledDeltaT;

        _collisionHandler.HandleCols = _handleCollisions;
        // If we query the grids directly after creating them with 2 times the max travelling distance in the whole FixedUpdate loop
        // we get all possible collision candidates but do not have to do this expensive call in the substep loop
        _collisionHandler.CreateGrids(2f * maxSpeed * deltaT);
        
        for (int i = 0; i < _simulationLoopSubsteps; i++)
        {
            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                simulationObject.Precompute(scaledDeltaT, maxSpeed);
            }

            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                simulationObject.SolveConstraints(scaledDeltaT);
            }

            _collisionHandler.HandleCollisions(scaledDeltaT);

            foreach (ISimulationObject simulationObject in _simulationObjects)
            {
                simulationObject.CorrectVelocities(scaledDeltaT);
            }
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
