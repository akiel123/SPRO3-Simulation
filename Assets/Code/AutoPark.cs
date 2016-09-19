using UnityEngine;
using System.Collections;

public class AutoPark : MonoBehaviour{
	
	DistanceSensor[] sensors;
	public ControlCar control;

	float rrad = Mathf.PI * 2;

	int checkCount = 0;

	void Start()
	{
		doShiftDistance(5);
	}

	public void runAutoPark()
	{
		StartCoroutine("doPark");
	}
	private IEnumerator doPark()
	{
		sensors = control.sensors;

		DistanceSensor backPointingRight = sensors[0];
		DistanceSensor backMiddlePointingBack1 = sensors[2];
		DistanceSensor backMiddlePointingBack2 = sensors[3];
		DistanceSensor frontRightAngular = sensors[1];

		
		float lastDistance1 = 0;
		float lastDistance2 = 0;
		float backSensorOffset = 0.1f;
		float angle = 0;

		control.autoParking = true; //Start

		control.command = new ControlCar.CommandSet(-1, 0, false); //Drive back 
		while (backPointingRight.getDistance() != -1) yield return null; // until back end of car is aligned with obstacle 1
		control.command = new ControlCar.CommandSet(0, 0, true); //Break 
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to the right
		while (!(control.getWheelAngle() > control.maxRotation - 1 && control.getWheelAngle() < control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
		control.command = new ControlCar.CommandSet(-1, 1, false); //Drive back while turning
		while (backMiddlePointingBack1.getDistance() == -1) yield return null; //until obstacle two is visible;
		check();
		while (backMiddlePointingBack1.getDistance() != -1) //and then until it isn't visible again, while keeping the last measured value
		{
			lastDistance1 = backMiddlePointingBack1.getDistance();
			lastDistance2 = backMiddlePointingBack2.getDistance();
			yield return null;
		}
		angle = Mathf.Atan((lastDistance1 - lastDistance2) / backSensorOffset) * Mathf.Rad2Deg; //Calculate angle between alignent of obstacle2 (and supposedly object1) and the alignment of the car
		frontRightAngular.transform.localEulerAngles = new Vector3(0, 180 + angle, 0); //Set the rotation of the angluar distance sensor to the difference in alignment
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, -1, false); //Turn to middle position
		while (!((control.getWheelAngle() < 4 || control.getWheelAngle() > 300))) yield return null; // until done
		control.command = new ControlCar.CommandSet(-1, 0, false); //Drive back
		while (frontRightAngular.getDistance() == -1) yield return null; //Until front right corner of car clears obstacle 1
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, -1, true); //Turn to the left
		while (control.getWheelAngle() > (360 - control.maxRotation - 1) && control.getWheelAngle() < (360 - control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
		control.command = new ControlCar.CommandSet(-1, -1, false); //Drive back while turning
		while (Mathf.Atan((backMiddlePointingBack1.getDistance() - backMiddlePointingBack2.getDistance()) / backSensorOffset) * Mathf.Rad2Deg > 0 || backMiddlePointingBack1.getDistance() == -1) yield return null; ; // until car is aligned with object 2
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (control.getWheelAngle() > 100 && control.getWheelAngle() < 356) yield return null; // until done

		control.autoParking = false; //Done

		Debug.Log("Done parking");
	}

	void check()
	{
		Debug.Log("check" + checkCount);
		checkCount++;
	}

	public void doShiftDistance(float distance)
	{
		StartCoroutine( shiftDistance(5));
	}
	private IEnumerator shiftDistance(float distance)
	{

		bool turnRadiusExceeded = false;
		float extraDistance = 0;

		Debug.Log("Check: " + control.ib60TurnR + ", " +  control.ob60TurnR);


		float angle = 0;
		if (distance > control.ib60TurnR + control.ob60TurnR){
			angle = 90 * Mathf.Deg2Rad;
			turnRadiusExceeded = true;
			extraDistance = distance - (control.ib60TurnR + control.ob60TurnR);
		}
		else angle = Mathf.Acos(1 - (distance / (control.ib60TurnR + control.ob60TurnR)));

		float startAngle = control.getBodyAngleR();

		control.autoParking = true; //Start

		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to the right
		while (!(control.getWheelAngle() > control.maxRotation - 1 && control.getWheelAngle() < control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
		float a1 = (startAngle - angle + rrad) % rrad + 0.01f;
		float a2 = (startAngle - angle + rrad) % rrad - 0.01f;
		if ((startAngle - angle + rrad) % rrad < 0.01f) a2 = 0;
		if ((startAngle - angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;

		control.command = new ControlCar.CommandSet(-1, 1, false); //Back while turning
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
		check();
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill

		if (turnRadiusExceeded){
			Vector3 pos = control.getPosition();
			control.command = new ControlCar.CommandSet(0, -1, false); //Turn to middle position
			while (!((control.getWheelAngle() < 4 || control.getWheelAngle() > 300))) yield return null; // until done
			control.command = new ControlCar.CommandSet(-1, 0, false); //Back up
			while (control.distanceToPoint(pos) < extraDistance) yield return null; // until an appropriate distance from starting point
			control.command = new ControlCar.CommandSet(0, 0, true); //Break
			while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill

		}

		control.command = new ControlCar.CommandSet(0, -1, false); //Turn to the left
		while (!(control.getWheelAngle() > (360 - control.maxRotation - 1) && control.getWheelAngle() < (360 - control.maxRotation + 1))) yield return null; // until wheel is allmost fully turned
		Debug.Log("Wheel angle: " + control.getWheelAngle());

		control.command = new ControlCar.CommandSet(-1, -1, false); //Drive back while turning
		
		a1 = startAngle + 0.01f;
		a2 = startAngle - 0.01f;
		if (startAngle < 0.01f) a2 = 0;
		if (startAngle > rrad - 0.01f) a1 = rrad;

		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (control.getWheelAngle() > 100 && control.getWheelAngle() < 356) yield return null; // until done
		
		control.autoParking = false; //Done

	}




}
