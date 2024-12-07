using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class BalloonHouse : RigidBody
{
    [SerializeField]
    private List<ClothBalloon> _balloons;

    private ClothToRigidStretchingConstraints _clothToRigidStretchingConstraints;

    private Dictionary<ClothBalloon, int> _attachementParticleIndex;
    private Vector3 _rbLocalPos;

    public override void Initialize()
    {
        base.Initialize();

        _clothToRigidStretchingConstraints = new ClothToRigidStretchingConstraints();

        _attachementParticleIndex = new Dictionary<ClothBalloon, int>();
        // Attach the balloons to the house
        for (int i = 0; i < _balloons.Count; i++)
        {
            // Find particle that has the lowest Y value
            Particle lowestParticle = _balloons[i].Particles[0];
            for (int j = 1; j < _balloons[i].Particles.Length; j++)
            {
                if (_balloons[i].Particles[j].X.y < lowestParticle.X.y)
                {
                    lowestParticle = _balloons[i].Particles[j];
                }
            }
            _attachementParticleIndex.Add(_balloons[i], Array.IndexOf(_balloons[i].Particles, lowestParticle));

            // Use the sharedMesh bounds to retrieve the max Y value of the house
            Vector3 size = GetComponent<MeshFilter>().sharedMesh.bounds.size;
            _rbLocalPos = new Vector3(0, size.y / 2, 0);

            // Just randomly pick a particle of the balloon for now
            _clothToRigidStretchingConstraints.AddConstraint(this, _balloons[i].Particles, 0, _rbLocalPos);
        }

        Constraints.Add(_clothToRigidStretchingConstraints);
    }

    protected override void Update()
    {
        base.Update();

        foreach (ClothBalloon balloon in _balloons)
        {
            Debug.DrawLine(balloon.Particles[_attachementParticleIndex[balloon]].X, LocalToWorld(_rbLocalPos), Color.white);
        }
    }
}
