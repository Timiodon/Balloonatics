using UnityEngine;

public class Solver : MonoBehaviour
{
    [SerializeField]
    private ISimulationObject[] simulationObjects;

    void Start()
    {
        foreach (ISimulationObject simulationObject in simulationObjects)
        {
            simulationObject.Initialize();
        }
    }

    void FixedUpdate()
    {
        foreach (ISimulationObject simulationObject in simulationObjects)
        {
            simulationObject.Precompute();
        }

        foreach (ISimulationObject simulationObject in simulationObjects)
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
