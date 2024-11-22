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

    void Start()
    {
        _simulationObjects = _simulationGameObjects
            .Select(go => go.GetComponent<ISimulationObject>())
            .ToArray();

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

            if (simulationObject.GetType() == typeof(RigidBody))
            {
                RigidBody rigidBody = (RigidBody)simulationObject;
                rigidBody.SolveRigidBodyConstraints(deltaT);
            }
        }

        // Object-object collision handling would be here

        foreach (ISimulationObject simulationObject in _simulationObjects)
        {
            simulationObject.CorrectVelocities(deltaT);
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
