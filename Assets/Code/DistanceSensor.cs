using UnityEngine;
using System.Collections;

public class DistanceSensor : MonoBehaviour {

	public GameObject visualiser;
	bool visual = true;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (visual)
		{
			UpdateVisualiser();
		}
	}

	public float getDistance()
	{
		RaycastHit hit;

		if (Physics.Raycast(transform.position, transform.forward, out hit))
		{
			return (float)hit.distance;
		}
		
		
		return -1;
	}
	public void UpdateVisualiser()
	{
		RaycastHit hit;

		if (Physics.Raycast(transform.position, transform.forward, out hit))
		{
			visualiser.transform.position = hit.point;
		}
		else
		{
			visualiser.transform.position = transform.position;
		}
	}
}
