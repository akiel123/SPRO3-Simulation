﻿using UnityEngine;
using System.Collections;

public class ControlCar : MonoBehaviour
{
	

	public Rigidbody wheelLeft;
	public Rigidbody wheelRight;
	public Rigidbody wheelIntLeft;
	public Rigidbody wheelIntRight;
	public Rigidbody body;
	public Rigidbody backLeft;
	public Rigidbody backRight;
	public Transform measurePoint;

	public DistanceSensor[] sensors;
	public bool enableAutoPark = false;

	public struct CommandSet{
		public int forward;
		public int right;
		public bool breaking;

		public CommandSet(int forward, int right, bool breaking)
		{
			this.forward = forward;
			this.right = right;
			this.breaking = breaking;
		}
	}

	public AutoPark autoParker;
	public bool autoParking = false;
	public CommandSet command;

	[SerializeField]
	private float motorPower = 6;
	[SerializeField]
	private float turnPower = 10;
	[SerializeField]
	public float maxRotation = 60;

	private float ib60TurnRV = Mathf.Sqrt(Mathf.Pow(1.2f, 2) + Mathf.Pow(0, 2));
	private float ob60TurnRV = Mathf.Sqrt(Mathf.Pow(5.2f, 2) + Mathf.Pow(0, 2));
	private float if60TurnRV = Mathf.Sqrt(Mathf.Pow(1.2f, 2) + Mathf.Pow(5.8f, 2));
	private float of60TurnRV= Mathf.Sqrt(Mathf.Pow(5.2f, 2) + Mathf.Pow(5.8f, 2));

	public float ib60TurnR { get { return ib60TurnRV; } }
	public float ob60TurnR { get { return ob60TurnRV; } }
	public float if60TurnR { get { return if60TurnRV; } }
	public float of60TurnR { get { return of60TurnRV; } }

	Vector3[] originalPosition;
	Quaternion[] originalRotation;
	GameObject[] objects;

	// Use this for initialization
	void Start()
	{
		objects = new GameObject[5];
		objects[0] = gameObject;
		objects[1] = wheelIntLeft.gameObject;
		objects[2] = wheelIntRight.gameObject;
		objects[3] = wheelLeft.gameObject;
		objects[4] = wheelRight.gameObject;
		originalPosition = new Vector3[objects.Length];
		originalRotation = new Quaternion[objects.Length];
		for (int i = 0; i < objects.Length; i++)
		{
			originalPosition[i] = objects[i].transform.position;
			originalRotation[i] = objects[i].transform.rotation;
		}
		if(enableAutoPark) autoParker.runAutoPark();
		
	}

	// Update is called once per frame
	void Update()
	{
		float angle = wheelIntLeft.transform.localEulerAngles.y;
		if (angle > maxRotation && angle < 360 - maxRotation)
		{
			if (Mathf.Abs(angle - maxRotation) < Mathf.Abs(angle - (360 - maxRotation)))
			{
				wheelIntLeft.transform.localEulerAngles = new Vector3(wheelIntLeft.transform.localEulerAngles.x, maxRotation, wheelIntLeft.transform.localEulerAngles.z);
				wheelIntRight.transform.localEulerAngles = new Vector3(wheelIntRight.transform.localEulerAngles.x, maxRotation, wheelIntRight.transform.localEulerAngles.z);
			}
			else
			{
				wheelIntLeft.transform.localEulerAngles = new Vector3(wheelIntLeft.transform.localEulerAngles.x, -maxRotation, wheelIntLeft.transform.localEulerAngles.z);
				wheelIntRight.transform.localEulerAngles = new Vector3(wheelIntRight.transform.localEulerAngles.x, -maxRotation, wheelIntRight.transform.localEulerAngles.z);
			}
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Reset();
		}

		//Debug.Log(command.forward + "," + command.right + "," + command.breaking + "   " + Time.time);
	}

	void FixedUpdate()
	{
		if (autoParking)
		{
			Break(command.breaking);
			Accelerate(command.forward);
			Turn(command.right);
		}
		else
		{
			if (Input.GetKey(KeyCode.Space)) Break();

			if (Input.GetKey(KeyCode.W)) Accelerate(1);
			else if (Input.GetKey(KeyCode.S)) Accelerate(-1);

			if (Input.GetKey(KeyCode.A)) Turn(-1);
			else if (Input.GetKey(KeyCode.D)) Turn(1);
			else Turn(0);
		}
	}

	void Reset()
	{
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].transform.rotation = originalRotation[i];
			objects[i].transform.position = originalPosition[i];
		}
		wheelIntRight.velocity = Vector3.zero;
		wheelIntRight.angularVelocity = Vector3.zero;
		wheelIntLeft.velocity = Vector3.zero;
		wheelIntLeft.angularVelocity = Vector3.zero;
		wheelRight.velocity = Vector3.zero;
		wheelRight.angularVelocity = Vector3.zero;
		wheelLeft.velocity = Vector3.zero;
		wheelLeft.angularVelocity = Vector3.zero;
		body.velocity = Vector3.zero;
		body.angularVelocity = Vector3.zero;
		backLeft.velocity = Vector3.zero;
		backLeft.angularVelocity = Vector3.zero;
		backRight.velocity = Vector3.zero;
		backRight.angularVelocity = Vector3.zero;
	}

	void Accelerate(int direction)
	{
		if(direction != 0) direction = direction / Mathf.Abs(direction);
		wheelLeft.AddTorque(direction * wheelLeft.transform.up * motorPower);
		wheelRight.AddTorque(direction * wheelRight.transform.up * motorPower);
	}
	void Break()
	{
		wheelIntRight.velocity = Vector3.zero;
		wheelIntRight.angularVelocity = Vector3.zero;
		wheelIntLeft.velocity = Vector3.zero;
		wheelIntLeft.angularVelocity = Vector3.zero;
		wheelRight.velocity = Vector3.zero;
		wheelRight.angularVelocity = Vector3.zero;
		wheelLeft.velocity = Vector3.zero;
		wheelLeft.angularVelocity = Vector3.zero;
		body.velocity = Vector3.zero;
		body.angularVelocity = Vector3.zero;
		backLeft.velocity = Vector3.zero;
		backLeft.angularVelocity = Vector3.zero;
		backRight.velocity = Vector3.zero;
		backRight.angularVelocity = Vector3.zero;
	}
	void Break(bool b)
	{
		if (b)
		{
			Break();
		}
	}
	void Turn(int direction)
	{
		if (direction != 0) direction = direction / Mathf.Abs(direction);
		else
		{
			wheelIntLeft.angularVelocity *= 0.3f;
			wheelIntRight.angularVelocity *= 0.3f;
			return;
		}
		wheelIntLeft.AddTorque(direction * wheelIntLeft.transform.right * turnPower);
		wheelIntRight.AddTorque(direction * wheelIntRight.transform.right * turnPower);
		
	}

	public float getVelocity()
	{
		return wheelLeft.angularVelocity.magnitude;
	}
	public float getWheelAngle()
	{
		return wheelIntLeft.transform.localEulerAngles.y;
	}
	public float getBodyAngleD()
	{
		return measurePoint.eulerAngles.y % 360;
	}
	public float getBodyAngleR()
	{
		return measurePoint.eulerAngles.y * Mathf.Deg2Rad % (2 * Mathf.PI);
	}
	public float distanceToPoint(Vector3 point)
	{
		return (measurePoint.transform.position - point).magnitude;
	}
	public Vector3 getPosition()
	{
		return measurePoint.transform.position;
	}
}
