using UnityEngine;
using System.Collections;

public class AutoPark : MonoBehaviour{
	
	DistanceSensor[] sensors;
	public ControlCar control;

	int checkCount = 0;

	void Start()
	{
		StartCoroutine("shiftDistance");
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
		//shiftDistance(5);
	}
	private IEnumerator shiftDistance()
	{
		float distance = 2;
		float angle = Mathf.Acos(1-(distance/(control.ib60TurnR+control.ob60TurnR)));
		float startAngle = control.getBodyAngleR();
		Debug.Log("Angle: " + angle + "   Start Angle: " + startAngle);

		control.autoParking = true; //Start

		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to the right
		while (!(control.getWheelAngle() > control.maxRotation - 1 && control.getWheelAngle() < control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned

		float a1 = (startAngle - angle) % (Mathf.PI * 2) + 0.1f;
		float a2 = (startAngle - angle) % (Mathf.PI * 2) - 0.1f;
		if ((startAngle - angle) % (Mathf.PI * 2) < 0.1f) a2 = 0;
		if ((startAngle - angle) % (Mathf.PI * 2) > Mathf.PI * 2 - 0.1f) a1 = Mathf.PI * 2;
		Debug.Log((startAngle - angle) % (Mathf.PI * 2));
		Debug.Log("a1: " + a1 + "   a2: " + a2);

		control.command = new ControlCar.CommandSet(-1, 1, false); //Back while turning
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
		Debug.Log("angle is: " + control.getBodyAngleD());
		check();
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, -1, true); //Turn to the left
		while (control.getWheelAngle() > (360 - control.maxRotation - 1) && control.getWheelAngle() < (360 - control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
		control.command = new ControlCar.CommandSet(-1, -1, false); //Drive back while turning

		a1 = startAngle + 0.1f;
		a2 = startAngle - 0.1f;
		if (startAngle < 0.1f) a2 = 0;
		if (startAngle > Mathf.PI * 2 - 0.1f) a1 = Mathf.PI * 2;

		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (control.getWheelAngle() > 100 && control.getWheelAngle() < 356) yield return null; // until done
		Debug.Log("Done shifting");

		control.autoParking = false; //Done

	}




}
