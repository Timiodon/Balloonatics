using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class BalloonHouse : RigidBody
{
    [SerializeField]
    private List<ClothBalloon> _balloons;

    [SerializeField]
    private Material _ropeMaterial;

    private ClothToRigidStretchingConstraints _clothToRigidStretchingConstraints;


    private int[] _attachementParticleIndex;
    private LineRenderer[] _ropes;
    private Vector3 _rbLocalPos;

    public override void Initialize()
    {
        base.Initialize();

        _clothToRigidStretchingConstraints = new ClothToRigidStretchingConstraints();

        _attachementParticleIndex = new int[_balloons.Count];
        _ropes = new LineRenderer[_balloons.Count];
        // Attach the balloons to the house
        for (int i = 0; i < _balloons.Count; i++)
        {
            // Find particle that has the lowest Y value
            Particle lowestParticle = _balloons[i].Particles[0];
            int lowestParticleIndex = 0;
            for (int j = 1; j < _balloons[i].Particles.Length; j++)
            {
                if (_balloons[i].Particles[j].X.y < lowestParticle.X.y)
                {
                    lowestParticle = _balloons[i].Particles[j];
                    lowestParticleIndex = j;
                }
            }
            _attachementParticleIndex[i] = lowestParticleIndex;

            // Use the sharedMesh bounds to retrieve the max Y value of the house
            Vector3 size = GetComponent<MeshFilter>().sharedMesh.bounds.size;
            _rbLocalPos = new Vector3(0, size.y / 2, 0);

            // Create a rope between the balloon and the house
            GameObject newRope = new GameObject($"Rope {i}");
            newRope.transform.parent = transform;

            _ropes[i] = newRope.AddComponent<LineRenderer>();
            _ropes[i].material = _ropeMaterial;
            _ropes[i].startWidth = 0.015f;
            _ropes[i].endWidth = 0.015f;
            _ropes[i].positionCount = 2;
            _ropes[i].SetPosition(0, _balloons[i].Particles[lowestParticleIndex].X);
            _ropes[i].SetPosition(1, LocalToWorld(_rbLocalPos));

            // Attach balloon to highest, middle y value of house and lowest particle of balloon
            _clothToRigidStretchingConstraints.AddConstraint(this, _balloons[i].Particles, lowestParticleIndex, _rbLocalPos);
        }

        Constraints.Add(_clothToRigidStretchingConstraints);
    }

    protected override void Update()
    {
        base.Update();

        for (int i = 0; i < _balloons.Count; i++)
        {
            //Debug.DrawLine(_balloons[i].Particles[_attachementParticleIndex[i]].X, LocalToWorld(_rbLocalPos), Color.white);
            _ropes[i].SetPosition(0, _balloons[i].Particles[_attachementParticleIndex[i]].X);
            _ropes[i].SetPosition(1, LocalToWorld(_rbLocalPos));
        }
    }
}
