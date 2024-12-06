using System;
using UnityEngine;

public class BalloonHouse : RigidBody
{
    [SerializeField]
    private List<ClothBalloon> _balloons;

    private List<Vector3> _houseVertices;
    private ClothToRigidStretchingConstraints _clothToRigidStretchingConstraints;
    
    override void Initialize() {
        base.Initialize();

        _house = GetComponent<RigidBody>();
        _clothToRigidStretchingConstraints = new ClothToRigidStretchingConstraints();

        _houseVertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        // Attach the balloons to the house
        for (int i = 0; i < _balloons.Count; i++) {
            // Just randomly pick a particle of the balloon for now
            _clothToRigidStretchingConstraints.AddConstraint(this, _balloons[i].Particles, 0, houseVertices[i++]);
        }

        Constraints.Add(_clothToRigidStretchingConstraints);
    }

    override void Update() {
        base.Update();

        for (int i = 0; i < _balloons.Count; i++) {
            Debug.DrawLine(_houseVertices[i], _balloons[i].Particles[0].X, Color.white);
        }
    }
}
