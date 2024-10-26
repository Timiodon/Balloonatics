using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField]
    private ISimulationObject[] _simulationObjects;

    void Start()
    {
        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.Initialize();
        }
    }

    void FixedUpdate()
    {
        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.Precompute();
        }

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.SolveConstraints();

            if (simulationObject.CollideWithGround)
            {
                simulationObject.ResolveGroundCollision();
            }
        }

        // Object-object collision handling would be here
    }
}
