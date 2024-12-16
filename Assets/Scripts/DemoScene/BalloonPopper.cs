using System.Collections;
using UnityEngine;

public class BalloonPopper : MonoBehaviour
{
	[SerializeField]
	ClothBalloon[] balloons;

	[SerializeField]
	float popPressure;

	int i = 0;

	public void Pop()
	{
		if (i < balloons.Length)
		{
			StartCoroutine(BallonPopSequence(i));
			i++;
		}
	}

	IEnumerator BallonPopSequence(int i)
	{
		// Detaching the balloon before popping it makes it pop in a nicer way because otherwise the vertex that it is attached to pops first because it's streched the most
		balloons[i].Detached = true;
		yield return new WaitForSeconds(0.1f);
		balloons[i].Pressure = popPressure;
	}
}