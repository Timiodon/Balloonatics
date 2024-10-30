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
        // TODO: Maybe we need to scale this to do smaller steps
        float deltaT = Time.fixedDeltaTime;

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.Precompute(deltaT);
        }

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.SolveConstraints(deltaT);
        }

        // Object-object collision handling would be here

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.CorrectVelocities(deltaT);
        }    
    }
}
