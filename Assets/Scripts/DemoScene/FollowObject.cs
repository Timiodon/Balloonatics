using System.Collections;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
	[SerializeField]
	Transform followObject;

	Vector3 offset;

	private void Start()
	{
		offset = transform.position - followObject.position;
	}

	// Update is called once per frame
	void Update()
    {
		transform.position = followObject.position + offset;
    }
}