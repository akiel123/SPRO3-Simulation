using UnityEngine;
using System.Collections;

public class AutoPark : MonoBehaviour{
	
	DistanceSensor[] sensors;
	public ControlCar control;

	float rrad = Mathf.PI * 2;
	int checkCount = 0;
	bool doingSubRoutine = false;
	bool performingBasicShift = false;

	void Start()
	{
		StartCoroutine(shiftDistanceBackOffset(4, 45 * Mathf.Deg2Rad));	
		//StartCoroutine(shiftDistanceLimitedSpace(3, 3));
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

		//Drive back without turning
		control.command = new ControlCar.CommandSet(-1, 0, false); 
		while (backPointingRight.getDistance() != -1) yield return null; // until back end of car is aligned with obstacle 1
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		StartCoroutine(turnRight());
		while (doingSubRoutine) yield return null;
		//Drive back while turning right
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
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		StartCoroutine(turnMiddle());
		while (doingSubRoutine) yield return null;
		//Drive back without turning
		control.command = new ControlCar.CommandSet(-1, 0, false); 
		while (frontRightAngular.getDistance() == -1) yield return null; //Until front right corner of car clears obstacle 1
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		StartCoroutine(turnLeft());
		while (doingSubRoutine) yield return null;
		//Drive back while turning left
		control.command = new ControlCar.CommandSet(-1, -1, false);             // until car is aligned with object 2
		while (Mathf.Atan((backMiddlePointingBack1.getDistance() - backMiddlePointingBack2.getDistance()) / backSensorOffset) * Mathf.Rad2Deg > 0 || backMiddlePointingBack1.getDistance() == -1) yield return null; ;
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		StartCoroutine(turnMiddle());
		while (doingSubRoutine) yield return null;

		//Done - Give back control
		control.autoParking = false;
	}
	
	private IEnumerator shiftDistanceBack(float distance)
	{
		performingBasicShift = true;

		//Prepare variables
		float extraDistance = 0;
		float angle = 0;

		//Check if straight backing up is needed and prepare variables
		if (distance > control.ib60TurnR + control.ob60TurnR){ 
			angle = 90 * Mathf.Deg2Rad;
			extraDistance = distance - (control.ib60TurnR + control.ob60TurnR);
		}
		else angle = Mathf.Acos(1 - (distance / (control.ib60TurnR + control.ob60TurnR)));

		//Start the turning based on calculated variables
		StartCoroutine(shiftDistanceBack(angle, extraDistance));
		yield return null;
	}
	private IEnumerator shiftDistanceBack(float angle, float extraDistance)
	{
		performingBasicShift = true;
		//Enable automatic control
		control.autoParking = true;

		//Prepare variables
		bool turnRadiusExceeded = extraDistance > 0;
		float startAngle = control.getBodyAngleR();
		
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		StartCoroutine(turnRight());
		while (doingSubRoutine) yield return null;
		//Back while turning right
		control.command = new ControlCar.CommandSet(-1, 1, false);
		float a1 = (startAngle - angle + rrad) % rrad + 0.01f;
		float a2 = (startAngle - angle + rrad) % rrad - 0.01f;
		if ((startAngle - angle + rrad) % rrad < 0.01f) a2 = 0;
		if ((startAngle - angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		//If staight backing is necessary, do it now
		if (turnRadiusExceeded)
		{ //If staight backing is necessary, do it now
			StartCoroutine(backUpDistance(extraDistance));
			while (doingSubRoutine) yield return null;
			StartCoroutine(breaK());
			while (doingSubRoutine) yield return null;
		}

		StartCoroutine(turnLeft());
		while (doingSubRoutine) yield return null;
		//Drive back while turning left
		control.command = new ControlCar.CommandSet(-1, -1, false);
		a1 = startAngle + 0.01f;
		a2 = startAngle - 0.01f;
		if (startAngle < 0.01f) a2 = 0;
		if (startAngle > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		StartCoroutine(turnMiddle());
		while (doingSubRoutine) yield return null;

		//Done - Give back control
		control.autoParking = false;
		performingBasicShift = false;
	}

	private IEnumerator shiftDistanceBackOffset(float distance, float offsetAngle) //Shifts backwards with an offset angle
	{
		//ASSUMING THAT THE BACK END OF THE CAR IS CLOSER TO THE GOAL
		performingBasicShift = true;
		offsetAngle = (offsetAngle + 2 * Mathf.PI) % Mathf.PI * 2;
		float offAngDeg = offsetAngle * Mathf.Rad2Deg;

		bool approachForwards = true; //Defines whether the front is pointing towards the goal (or the back)
		approachForwards = (offAngDeg > 180 && distance > 0) || (offsetAngle < Mathf.PI && distance < 0);
		int approachingDirection = System.Convert.ToInt32(approachForwards);

		//Prepare variables
		float extraDistance = 0;
		float angle = 0;
		float anglePhi = 0;
		float startAngle = control.getBodyAngleR();

		float minimum1PointDistance = (1 - Mathf.Cos(offsetAngle)) * (control.ob60TurnR); //The distance moved along y-axis if the car directly aligns
																						  //itself with the desired axis
		float maxCurveDistance = control.ob60TurnR - (1 - Mathf.Sin(offsetAngle)) * control.ib60TurnR + control.ib60TurnR; //The distance if car turns
																						//to an angle perpendicular to the desired angle, and then directly
																						//to a parallel angle. Maximum distance before emplying straight
																						//driving
		int initialDirection = 1;


		if (maxCurveDistance < distance) //If straight driving is to be employed, calculate distance of this.
		{
			anglePhi = 0.5f * Mathf.PI - offsetAngle; //Angle to turn to go perpendicular
			extraDistance = distance - maxCurveDistance;
		}
		else if (distance > minimum1PointDistance) 
		{	//Angle it has to turn towards wall, getting a total displacement larger than the minimum1PointDistance
			anglePhi = Mathf.Abs(Mathf.Acos((control.ib60TurnR * Mathf.Cos(offsetAngle) + control.ob60TurnR - distance) / (control.ib60TurnR + control.ob60TurnR)) - offsetAngle);
		}
		else
		{	//Angle it has to turn away from wall, getting a smaller total displacement than the minimum1PointDistance
			anglePhi = -1 * Mathf.Abs(Mathf.Acos((control.ib60TurnR * Mathf.Cos(offsetAngle) + control.ob60TurnR - distance) / (control.ib60TurnR + control.ob60TurnR)) + offsetAngle);
			initialDirection = -1; //Start by moving away from wall
		}

		int initialTurnDirection = 1;
		//Initial turning direction. Depending on which corner angle-wise the car is in, and reversed if distance is negative
		if ((((offAngDeg < 90) || (180 < offAngDeg && offAngDeg < 270)) && distance > 0)
			|| (((90 < offAngDeg && offAngDeg < 180) || (270 < offAngDeg)) && distance < 0)) initialTurnDirection = -1;



		//Debug.Log("angle: " + angle + ",   anglePhi: " + anglePhi + ",   extraDistance: " + extraDistance + ",   offsetAngle: " + offsetAngle
		//	+  ",   initialDirection: " + initialDirection);

		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		//Debug.Log("Done Breaking");

		if(initialTurnDirection == -1) StartCoroutine(turnLeft());
		else StartCoroutine(turnRight());
		while (doingSubRoutine) yield return null;
		//Debug.Log("Done turning right");

		//Back while turning
		control.command = new ControlCar.CommandSet(initialDirection, 1, false);
		int mod = -1; //Controls the direction
		if (offAngDeg < 90 || 180 < offAngDeg && offAngDeg < 270) mod = 1;
		if (distance < 0) mod *= -1;


		float a1 = (startAngle + anglePhi * mod + rrad) % rrad + 0.01f;
		float a2 = (startAngle + anglePhi * mod + rrad) % rrad - 0.01f;
		if ((startAngle + anglePhi * mod + rrad) % rrad < 0.01f) a2 = 0;
		if ((startAngle + anglePhi * mod + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //Until angle is more than required angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		Debug.Log("Done turning right");


		if (extraDistance != 0)
		{//If staight backing is necessary, do it now
			StartCoroutine(backUpDistance(extraDistance));
			while (doingSubRoutine) yield return null;
			StartCoroutine(breaK());
			while (doingSubRoutine) yield return null;
		}

		StartCoroutine(turnLeft());
		while (doingSubRoutine) yield return null;
		
		//Drive back while turning left
		control.command = new ControlCar.CommandSet(-1, -1, false);
		if (90 < offAngDeg || offAngDeg > 270) //if the offset is less than 90 or more than 270 degree go for a relative angle of 0
		{
			a1 = (startAngle + offsetAngle + rrad) % rrad + 0.01f;
			a2 = (startAngle + offsetAngle + rrad) % rrad - 0.01f;
			if ((startAngle + offsetAngle + rrad) % rrad < 0.01f) a2 = 0;
			if ((startAngle + offsetAngle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		}
		else //else go for a relative angle of 180
		{
			a1 = (startAngle + (Mathf.PI - offsetAngle) + rrad) % rrad + 0.01f;
			a2 = (startAngle + (Mathf.PI - offsetAngle) + rrad) % rrad - 0.01f;
			if ((startAngle + (Mathf.PI - offsetAngle) + rrad) % rrad < 0.01f) a2 = 0;
			if ((startAngle + (Mathf.PI - offsetAngle) + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		}
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at no offset angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		StartCoroutine(turnMiddle());
		while (doingSubRoutine) yield return null;

	}

	private IEnumerator shiftDistanceFront(float distance)
	{
		performingBasicShift = false;

		//Prepare variables
		float extraDistance = 0;
		float angle = 0;

		//Check if straight driving is needed and prepare variables
		if (distance > (control.ib60TurnR + control.ob60TurnR) * 2)
		{
			Debug.Log("Straight needed");
			angle = 90 * Mathf.Deg2Rad;
			extraDistance = distance - (control.ib60TurnR + control.ob60TurnR) * 2;
		}
		else angle = Mathf.Acos(1 - (distance / ((control.ib60TurnR + control.ob60TurnR) * 2)));
		
		//Start the turning based on calculated variables
		StartCoroutine(shiftDistanceFront(angle, extraDistance));
		yield return null;
	}
	private IEnumerator shiftDistanceFront(float angle, float extraDistance)
	{
		performingBasicShift = true;
		//Enable automatic control
		control.autoParking = true;

		//Prepare variables
		bool turnRadiusExceeded = extraDistance > 0;
		float startAngle = control.getBodyAngleR();

		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		StartCoroutine(turnRight());
		//Drive forward while turning right
		control.command = new ControlCar.CommandSet(1, 1, false);
		float a1 = (startAngle + angle + rrad) % rrad + 0.01f;
		float a2 = (startAngle + angle + rrad) % rrad - 0.01f;
		if ((startAngle + angle + rrad) % rrad < 0.01f) a2 = 0;
		if ((startAngle + angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;

		//If staight driving is necessary, do it now
		if (turnRadiusExceeded)
		{
			Debug.Log("Driving straight");
			StartCoroutine(driveDistance(extraDistance));
			while (doingSubRoutine) yield return null;
			StartCoroutine(breaK());
			while (doingSubRoutine) yield return null;
		}

		StartCoroutine(turnLeft());
		while (doingSubRoutine) yield return null;
		//Drive forward while turning left
		control.command = new ControlCar.CommandSet(1, -1, false);
		a1 = startAngle + 0.01f;
		a2 = startAngle - 0.01f;
		if (startAngle < 0.01f) a2 = 0;
		if (startAngle > rrad - 0.01f) a1 = rrad;
		while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
		performingBasicShift = false;
	}

	private IEnumerator shiftDistanceBackFront(float distance)
	{
		StartCoroutine (shiftDistanceBack(distance * 0.5f));
		while (performingBasicShift) yield return null;
		StartCoroutine (shiftDistanceFront(distance * 0.5f));
		while (performingBasicShift) yield return null;
		turnMiddle();
		while (doingSubRoutine) yield return null;
	}
	private IEnumerator shiftDistanceFrontBack(float distance)
	{
		StartCoroutine(shiftDistanceFront(distance * 0.5f));
		while (performingBasicShift) yield return null;
		StartCoroutine(shiftDistanceBack(distance * 0.5f));
		while (performingBasicShift) yield return null;
		turnMiddle();
		while (doingSubRoutine) yield return null;
	}
	private IEnumerator shiftDistanceLimitedSpace(float distance, float spaceLimit)
	{
		//Prepare variables
		float extraDistance = 0;
		float angle = 0;
		//float startAngle = control.getBodyAngleR();   not used yet

		//Check if limit comforming is necessary
		if (spaceLimit > (control.ib60TurnR + control.ob60TurnR))
		{
			Debug.Log("Straight needed");
			angle = 90 * Mathf.Deg2Rad;
			extraDistance = spaceLimit - (control.ib60TurnR + control.ob60TurnR) * 0.5f;
		}
		else angle = Mathf.Asin(spaceLimit / ((control.ib60TurnR + control.ob60TurnR)));

		float distanceShifted = 0;
		float stepLength = (1 - Mathf.Cos(angle)) * (control.ib60TurnR + control.ob60TurnR) + extraDistance;

		int maxSteps = 10; //for debugging purposes
		int i = 0;
		Debug.Log("Check 1 - Distance: " + (distance - distanceShifted) + "   StepLength: " + stepLength);
		while (i < maxSteps) //Shift back and front at the maximum calculated angle until goal is reachable in next step.
		{
			if (distance - distanceShifted < stepLength * 2) break;
			StartCoroutine(shiftDistanceBack(angle, extraDistance));
			while (performingBasicShift) yield return null;
			distanceShifted += stepLength;
			
		Debug.Log("Check 3: " + distanceShifted);
			StartCoroutine(shiftDistanceFront(angle, extraDistance));
			while (performingBasicShift) yield return null;
			distanceShifted += stepLength;
		Debug.Log("Check 4: " + distanceShifted);
			i++;
		}
		Debug.Log("Check 2: " + ((distance - distanceShifted) / 2));
		StartCoroutine(shiftDistanceBack((distance - distanceShifted) / 2));
		while (performingBasicShift) yield return null;
		StartCoroutine(shiftDistanceFront((distance - distanceShifted) / 2));
		while (performingBasicShift) yield return null;
		yield return null;
	}


	private IEnumerator breaK()	
	{
		doingSubRoutine = true;
		control.command = new ControlCar.CommandSet(0, 0, true); //Break
		while (control.getVelocity() > 0.5f) yield return null; // until allmost at standstill
		doingSubRoutine = false;
	}
	private IEnumerator turnLeft()
	{
		doingSubRoutine = true;
		control.command = new ControlCar.CommandSet(0, -1, false); //Turn to the left
		while (!(control.getWheelAngle() > (360 - control.maxRotation - 1) && control.getWheelAngle() < (360 - control.maxRotation + 1))) yield return null; // until wheel is allmost fully turned
		doingSubRoutine = false;
	}
	private IEnumerator turnMiddle()
	{
		doingSubRoutine = true;
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (!(control.getWheelAngle() < 3 || control.getWheelAngle() > 357)) yield return null; // until done
		doingSubRoutine = false;
	}
	private IEnumerator turnRight()
	{
		//Debug.Log("Starting turning right");
		doingSubRoutine = true;
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to the right
		//Debug.Log("in progress");
		while (!(control.getWheelAngle() > control.maxRotation - 1 && control.getWheelAngle() < control.maxRotation + 1)) yield return null; // until wheel is allmost fully turned
		//Debug.Log("done progress");
		doingSubRoutine = false;
		//Debug.Log("ACtually done turning right");
	}
	private IEnumerator backUpDistance(float distance)
	{
		doingSubRoutine = true;
		Vector3 pos = control.getPosition();
		control.command = new ControlCar.CommandSet(0, 1, false); //Turn to middle position
		while (control.getWheelAngle() > 100 && control.getWheelAngle() < 356) yield return null; // until done
		control.command = new ControlCar.CommandSet(-1, 0, false); //Back up
		while (control.distanceToPoint(pos) < distance) yield return null; // until an appropriate distance from starting point
		doingSubRoutine = false;
	}
	private IEnumerator driveDistance(float distance)
	{
		doingSubRoutine = true;
		Vector3 pos = control.getPosition();
		control.command = new ControlCar.CommandSet(0, -1, false); //Turn to middle position
		while (!(control.getWheelAngle() < 3 || control.getWheelAngle() > 357)) yield return null; // until done
		control.command = new ControlCar.CommandSet(1, 0, false); //Drive ahead
		while (control.distanceToPoint(pos) < distance) yield return null; // until an appropriate distance from starting point
		doingSubRoutine = false;
	}



}
/* NOT SURE IF NEEDED, DONT DELETE YET
private IEnumerator shiftDistanceFrontBack(float distance)
{
	performingBasicShift = true;
	//Enable automatic control
	control.autoParking = true;

	//Prepare variables
	bool turnRadiusExceeded = false;
	float extraDistance = 0;
	float angle = 0;
	float startAngle = control.getBodyAngleR();

	//Check if straight driving is needed and prepare variables
	if (distance > (control.ib60TurnR + control.ob60TurnR) * 2)
	{
		Debug.Log("Straight needed");
		angle = 90 * Mathf.Deg2Rad;
		turnRadiusExceeded = true;
		extraDistance = distance - (control.ib60TurnR + control.ob60TurnR) * 2;
	}
	else angle = Mathf.Acos(1 - (distance / ((control.ib60TurnR + control.ob60TurnR) * 2)));
	StartCoroutine(breaK());
	while (doingSubRoutine) yield return null;


	StartCoroutine(turnRight());
	//Drive forward while turning right
	control.command = new ControlCar.CommandSet(1, 1, false);
	float a1 = (startAngle + angle + rrad) % rrad + 0.01f;
	float a2 = (startAngle + angle + rrad) % rrad - 0.01f;
	if ((startAngle + angle + rrad) % rrad < 0.01f) a2 = 0;
	if ((startAngle + angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
	while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
	StartCoroutine(breaK());
	while (doingSubRoutine) yield return null;

	//If staight driving is necessary, do it now
	if (turnRadiusExceeded)
	{
		Debug.Log("Driving straight");
		StartCoroutine(driveDistance(extraDistance));
		while (doingSubRoutine) yield return null;
		StartCoroutine(breaK());
		while (doingSubRoutine) yield return null;
	}

	StartCoroutine(turnLeft());
	while (doingSubRoutine) yield return null;
	//Drive forward while turning left
	control.command = new ControlCar.CommandSet(1, -1, false);
	a1 = startAngle + 0.01f;
	a2 = startAngle - 0.01f;
	if (startAngle < 0.01f) a2 = 0;
	if (startAngle > rrad - 0.01f) a1 = rrad;
	while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle
	StartCoroutine(breaK());
	while (doingSubRoutine) yield return null;

	StartCoroutine(turnRight());
	while (doingSubRoutine) yield return null;
	//Back while turning right
	control.command = new ControlCar.CommandSet(-1, 1, false);
	a1 = (startAngle - angle + rrad) % rrad + 0.01f;
	a2 = (startAngle - angle + rrad) % rrad - 0.01f;
	if ((startAngle - angle + rrad) % rrad < 0.01f) a2 = 0;
	if ((startAngle - angle + rrad) % rrad > rrad - 0.01f) a1 = rrad;
	while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2) yield return null; //Until angle is more than required angle
	StartCoroutine(breaK());
	while (doingSubRoutine) yield return null;

	StartCoroutine(turnLeft());
	while (doingSubRoutine) yield return null;
	//Drive back while turning left
	control.command = new ControlCar.CommandSet(-1, -1, false);
	a1 = startAngle + 0.01f;
	a2 = startAngle - 0.01f;
	if (startAngle < 0.01f) a2 = 0;
	if (startAngle > rrad - 0.01f) a1 = rrad;
	while (control.getBodyAngleR() > a1 || control.getBodyAngleR() < a2 % (2 * Mathf.PI)) yield return null; //until back at start angle

	StartCoroutine(breaK());
	while (doingSubRoutine) yield return null;
	StartCoroutine(turnMiddle());
	while (doingSubRoutine) yield return null;

	//Done - Give back control
	control.autoParking = false;
	performingBasicShift = false;
}*/