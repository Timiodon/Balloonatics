using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class BalloonHouse : RigidBody
{
    public List<ClothBalloon> BalloonList { get => _balloons; }
    [SerializeField]
    private List<ClothBalloon> _balloons;

    [SerializeField]
    private Material _ropeMaterial;

    [SerializeField]
    float _attachPosRadius;

    private int[] _attachementParticleIndex;
    private LineRenderer[] _ropes;
    private Vector3[] _rbLocalPoses;

	private ClothToRigidStretchingConstraints[] _constraints;

    public override void Initialize()
    {
        base.Initialize();


        _attachementParticleIndex = new int[_balloons.Count];
        _ropes = new LineRenderer[_balloons.Count];
        _rbLocalPoses = new Vector3[_balloons.Count];
		_constraints = new ClothToRigidStretchingConstraints[_balloons.Count];
		// Attach the balloons to the house
		for (int i = 0; i < _balloons.Count; i++)
        {
			_constraints[i] = new ClothToRigidStretchingConstraints();
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
            float attachPosAngle = Mathf.PI * 2 * i / _balloons.Count;
            _rbLocalPoses[i] = new Vector3(Mathf.Cos(attachPosAngle) * _attachPosRadius, size.y / 2, Mathf.Sin(attachPosAngle) * _attachPosRadius);

            // Create a rope between the balloon and the house
            GameObject newRope = new GameObject($"Rope {i}");
            newRope.transform.parent = transform;

            _ropes[i] = newRope.AddComponent<LineRenderer>();
            _ropes[i].material = _ropeMaterial;
            _ropes[i].startWidth = 0.015f;
            _ropes[i].endWidth = 0.015f;
            _ropes[i].positionCount = 2;
            _ropes[i].SetPosition(0, _balloons[i].Particles[lowestParticleIndex].X);
            _ropes[i].SetPosition(1, LocalToWorld(_rbLocalPoses[i]));

			// Attach balloon to highest, middle y value of house and lowest particle of balloon
			_constraints[i].AddConstraint(this, _balloons[i].Particles, lowestParticleIndex, _rbLocalPoses[i]);
            Constraints.Add(_constraints[i]);
        }

    }

    protected override void Update()
    {
        base.Update();

        for (int i = 0; i < _balloons.Count; i++)
        {
            if (_balloons[i].Popped || _balloons[i].Detached)
            {
                _ropes[i].enabled = false;
				_constraints[i].enabled = false;
                continue;
            }
            //Debug.DrawLine(_balloons[i].Particles[_attachementParticleIndex[i]].X, LocalToWorld(_rbLocalPos), Color.white);
            _ropes[i].SetPosition(0, _balloons[i].Particles[_attachementParticleIndex[i]].X);
            _ropes[i].SetPosition(1, LocalToWorld(_rbLocalPoses[i]));
        }
    }
}
