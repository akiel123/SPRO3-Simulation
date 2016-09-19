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

		//Enable automatic control
		control.autoParking = true;

		//Setup sensors
		sensors = control.sensors;
		DistanceSensor backPointingRight = sensors[0];
		DistanceSensor backMiddlePointingBack1 = sensors[2];
		DistanceSensor backMiddlePointingBack2 = sensors[3];
		DistanceSensor frontRightAngular = sensors[1];

		//Setup other variables
		float lastDistance1 = 0;
		float lastDistance2 = 0;
		float backSensorOffset = 0.1f;
		float angle = 0;

		//Drive back
		control.command = new ControlCar.CommandSet(-1, 0, false); 
		while (backPointingRight.getDistance() != -1) yield return null; // until back end of car is aligned with obstacle 1
		breaK();
		turnRight();

		//Drive back while turning
		control.command = new ControlCar.CommandSet(-1, 1, false); 
		while (backMiddlePointingBack1.getDistance() == -1) yield return null; //until obstacle two is visible;
		while (backMiddlePointingBack1.getDistance() != -1) { //and then until it isn't visible again, while keeping track of the last measured value
			lastDistance1 = backMiddlePointingBack1.getDistance();
			lastDistance2 = backMiddlePointingBack2.getDistance();
			yield return null;
		}

		// Calculate angle between alignent of obstacle2(and supposedly object1) and the alignment of the car
		angle = Mathf.Atan((lastDistance1 - lastDistance2) / backSensorOffset) * Mathf.Rad2Deg;
		//Set the rotation of the angluar distance sensor to the difference in alignment
		frontRightAngular.transform.localEulerAngles = new Vector3(0, 180 + angle, 0); 

		breaK();
		turnMiddle();

		//Drive back
		control.command = new ControlCar.CommandSet(-1, 0, false); 
		while (frontRightAngular.getDistance() == -1) yield return null; //Until front right corner of car clears obstacle 1

		breaK();
		turnLeft();

		//Drive back while turning
		control.command = new ControlCar.CommandSet(-1, -1, false);             // until car is aligned with object 2
		while (Mathf.Atan((backMiddlePointingBack1.getDistance() - backMiddlePointingBack2.getDistance()) / backSensorOffset) * Mathf.Rad2Deg > 0 || backMiddlePointingBack1.getDistance() == -1) yield return null; ;
																	
		breaK();
		turnMiddle();

		//Done - Give back control
		control.autoParking = false;
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

		//Enable automatic control
		control.autoParking = true; 

		//Prepare variables
		bool turnRadiusExceeded = false;
		float extraDistance = 0;
		float angle = 0;
		float startAngle = control.getBodyAngleR();

		//Check if straight backing up is needed and prepare variables
		if (distance > control.ib60TurnR + control.ob60TurnR){ 
			angle = 90 * Mathf.Deg2Rad;
			turnRadiusExceeded = true;
			extraDistance = distance - (control.ib60TurnR + control.ob60TurnR);
		}
		else angle = Mathf.Acos(1 - (distance / (control.ib60TurnR + control.ob60TurnR)));

		breaK();
		turnRight();

		//Back while turning
		control.command = new ControlCar.CommandSet(-1, 1, false); 
		float a1 = (startAngle - angle + rrad) % rrad + 0.01f;
		float a2 = (startAngle - angle + rrad) % rrad - 0.01f;
		if ((startAngle - angle + rrad) % rrad < 0.01f) a2 = 0;
		if ((startAngle - angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
		breaK();
		
		//If staight backing is necessary, do it now
		if (turnRadiusExceeded){ //If staight backing is necessary, do it now
			backUpDistance(extraDistance);
			breaK();
		}
		turnLeft();

		//Drive back while turning
		control.command = new ControlCar.CommandSet(-1, -1, false); 
		a1 = startAngle + 0.01f;
		a2 = startAngle - 0.01f;
		if (startAngle < 0.01f) a2 = 0;
		if (startAngle > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle

		breaK();
		turnMiddle();
		
		//Done - Give back control
		control.autoParking = false; 

	}

	private IEnumerable breaK()
	{
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
	}
	private IEnumerable turnLeft()
	{
		control.command = new ControlCar.CommandSet(0, -1, false); //Turn to the left
		while (!(control.getWheelAngle() > (360 - control.maxRotation - 1) && control.getWheelAngle() < (360 - control.maxRotation + 1))) yield return null; // until wheel is allmost fully turned
	}
	private IEnumerable turnMiddle()
	{
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (control.getWheelAngle() > 100 && control.getWheelAngle() < 356) yield return null; // until done
	}
	private IEnumerable turnRight()
	{
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to the right
		while (!(control.getWheelAngle() > control.maxRotation - 1 && control.getWheelAngle() < control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
	}
	private IEnumerable backUpDistance(float distance)
	{
		Vector3 pos = control.getPosition();
		turnMiddle();
		control.command = new ControlCar.CommandSet(-1, 0, false); //Back up
		while (control.distanceToPoint(pos) < distance) yield return null; // until an appropriate distance from starting point
	}



}
