using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ClothBalloon : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public IConstraints[] Constraints { get; private set; }
    public Vector3[] Accelerations { get; set; }
    public bool CollideWithGround { get; }

    private Mesh _mesh;

    [SerializeField]
    private float _totalMass = 1f;

    public void Initialize()
    {
        _mesh = GetComponent<MeshFilter>().mesh;

        int n = _mesh.vertices.Length;
        Particles = new Particle[n];
        for (int i = 0; i < n; i++)
        {
            Particles[i] = new Particle(_mesh.vertices[i], Vector3.zero, _totalMass / n);
        }

        // Initialize acceleration with a single vector3 for gravity
        // TODO: extract into Environment class
        Accelerations = new Vector3[1];
        Accelerations[0] = new Vector3(0f, -9.81f, 0f);

        // Initialize stretching constraints
        Constraints = new IConstraints[1];
    }

    public void Precompute()
    {
        throw new System.NotImplementedException();
    }

    public void ResolveGroundCollision()
    {
        throw new System.NotImplementedException();
    }

    public void SolveConstraints()
    {
        throw new System.NotImplementedException();
    }
}
