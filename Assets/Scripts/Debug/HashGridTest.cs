using System.Collections.Generic;
using UnityEngine;

public class HashGridTest : MonoBehaviour
{
    [SerializeField]
    private GameObject _queryPrefab;
    [SerializeField]
    private Vector3 _queryPosition;
    [SerializeField] 
    private float _queryRadius;

    private Particle[] _particles;
    private SpatialHashGrid _spatialHashGrid;
    private float _particleRadius = 0.05f;
    private List<GameObject> _createdInstances;

    void Start()
    {
        var _mesh = GetComponent<MeshFilter>().mesh;

        // Vertices are usually duplicated in meshes so each quad can have it's own set of verts. We don't want duplicate particles so we have to do this mapping.
        int n = _mesh.vertices.Length;
        Vector3[] displacedVertices = new Vector3[n];
        int[] vertexIdToParticleIdMap = new int[n];
        // Use a hashtable to do duplication detection of the positions
        Dictionary<Vector3, int> positionToParticleMap = new();
        for (int i = 0; i < n; i++)
        {
            displacedVertices[i] = _mesh.vertices[i];
            if (positionToParticleMap.TryGetValue(_mesh.vertices[i], out int particleIndex))
            {
                vertexIdToParticleIdMap[i] = particleIndex;
            }
            else
            {
                int nextIndex = positionToParticleMap.Count;
                positionToParticleMap[_mesh.vertices[i]] = nextIndex;
                vertexIdToParticleIdMap[i] = nextIndex;
            }
        }

        _particles = new Particle[positionToParticleMap.Count];
        foreach (KeyValuePair<Vector3, int> particlesAndIndices in positionToParticleMap)
        {
            _particles[particlesAndIndices.Value] = new Particle(transform.TransformPoint(particlesAndIndices.Key), Vector3.zero, 0.0f);
        }

        _spatialHashGrid = new(2 * _particleRadius, _particles.Length);
        _spatialHashGrid.Create(_particles);
        _createdInstances = new();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                Debug.DrawRay(hit.point, -ray.direction, Color.red, 10.0f);
                _queryPosition = hit.point;

                foreach (var obj in _createdInstances)
                {
                    Destroy(obj);
                }

                _spatialHashGrid.Query(_queryPosition, _queryRadius);

                foreach (var index in _spatialHashGrid.Neighbours)
                {
                    _createdInstances.Add(Instantiate(_queryPrefab, _particles[index].X, Quaternion.identity));
                }
            }
        }

    }
}
