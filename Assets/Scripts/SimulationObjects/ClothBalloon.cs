using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ClothBalloon : MonoBehaviour, ISimulationObject
{
    public Particle[] Particles { get; set; }
    public IConstraints[] Constraints { get; private set; }
    public bool UseGravity { get => _useGravity; }

    private Mesh _mesh;

    [SerializeField]
    private float _totalMass = 1f;

    [SerializeField]
    private bool _useGravity = true;

    public void Initialize()
    {
        _mesh = GetComponent<MeshFilter>().mesh;

        int n = _mesh.vertices.Length;
        Particles = new Particle[n];
        for (int i = 0; i < n; i++)
        {
            Particles[i] = new Particle(transform.TransformPoint(_mesh.vertices[i]), Vector3.zero, _totalMass / n);
        }

        // Initialize stretching constraints
        // we should be able to use eulers formula to precompute the number of edges
        Constraints = new IConstraints[0];
    }
}
