﻿using System.Collections.Generic;
using System;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
	private Inputs inputs;

	[Header("Components")]


	public GameObject hook_detector;

	// Objects that will interact with the rope
	public GameObject hookObject;
	private Transform objectHanging;

	// Spring joint prefab
	public GameObject springjoint_rb_pref;


	public int timeLever;

	public bool isGrappling;


	//public float speed = 10;

	[Header("Rope")]


	public float lengthRopeMax = 50;
	private float currentLengthRopeMax;
	public float lengthRopeMin = 2;


	[Header("Variables")]

	public LayerMask surfaces; 

	// Line renderer
	private LineRenderer LR;

	private SpringJoint spring;

	private GameObject springJointRB;

	// List with all rope sections
	private List<Vector3> distToHitPoints = new List<Vector3>();
	private List<Transform> ropePositions = new List<Transform>();

	// Hit point between the grapplin joint and the character
	private RaycastHit hit;

	// Hit point where the grapplin is attached
	private RaycastHit hitAttachedToGrapplin;

	public KeyCode keyGrapplin;
	private int countGrapplin;


	private bool hasChangedRope = false;
	private bool changeHook = false;
	private bool detachHook = false;
	private bool attachHook = false;
	private bool moveUpAndDown = false;


	//Rope data
	private float ropeLength;

	//Beginning of the min length before the rope becomes a straight line (ropelengthmin = ropelength)
	private float beginLengthMin = 1f;

	//Mass of what the rope is carrying
	private float loadMass = 7f;

	private float dist_objects;

	// Main Character variables
	private Rigidbody body;
	private GameObject mainChar;
	public Animator myAnimator; //Animator
	private Transform leftHarm;
	private Movement movements; //Movement script
	private MoveBox moveBox;

	//How fast we can add more/less rope
	float winchSpeed = 3f;


	private CheckLenghtSound checkLenghtSound; 
	private void Awake()
	{
		inputs = new Inputs();
	}

	private void OnEnable()
	{
		inputs.Enable();
	}
	private void OnDisable()
	{
		inputs.Disable();
	}


	public bool DetachHook
	{
		get { return detachHook; }   // get method
		set { detachHook = value; }  // set method
	}


	void Start()
	{
		mainChar = gameObject;

		//Init the line renderer we use to display the rope
		LR = GetComponent<LineRenderer>();

		// Get the object transform
		// objectHanging = GameObject.FindGameObjectWithTag("GrapplinHand").transform ;
		// if (objectHanging == null)
		objectHanging = transform;

		//Init the Rigidbody
		body = GetComponent<Rigidbody>();

		//Get rigidbodyCharacter component
		movements = GetComponent<Movement>();

		//Get MoveBox component
		moveBox = GetComponent<MoveBox>();

		// Get the animator 
		myAnimator = GetComponentInChildren<Animator>();
		//leftHarm = myAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);

		// Get the key to grapple
		//inputs.Uni.Grapple.performed += ctx => AttachHook();
  //      inputs.Uni.Detach.performed += ctx => DttachHook();

        checkLenghtSound = GetComponent<CheckLenghtSound>(); 
	}


	void Update()
	{

        if (Convert.ToBoolean(inputs.Uni.Grapple.ReadValue<float>()))
            attachHook = true;

        if (isGrappling)
		{

			// Get the angle to rotate uni in Movement
			if (Vector3.Angle(Vector3.down, objectHanging.position - hookObject.transform.position) - 10 > 0.5f)
				Movement.angleHook = (Vector3.Angle(Vector3.down, objectHanging.position - hookObject.transform.position) - 10) * Math.Sign((hookObject.transform.position - objectHanging.position).z);
			else
				Movement.angleHook = 0;

			//leftHarm.LookAt(hookObject.transform.position);

			// Vecteur unitaire
			Vector3 u_dir = (hookObject.transform.position - objectHanging.position) / dist_objects;

			// Get the distance to the hook on y for the Movement
			Movement.distToHook = (hookObject.transform.position.y - objectHanging.position.y ) / ropeLength ;



			if (distToHitPoints.Count >= 3)
			{

				//TODO Toujours un pb avec les coins de rectangles ca se colle un peu, a regler a cause du *1.00001f qui est fait pour les surfaces incurvees type sphere
				for (int ropeId = 2; ropeId < ropePositions.Count; ropeId++)
				{
					if (!TheLineTouch(ropePositions[ropeId].position + distToHitPoints[ropeId] * 1.0001f, ropePositions[ropeId - 2].position + distToHitPoints[ropeId - 2], ropePositions[ropeId - 2]))
					{
						if (hit.transform != hookObject.transform && hit.transform != objectHanging)
						{
							//Debug.Log(ropePositions[ropePositions.Count - 3]);
							DeleteRopeJoint(ropeId);
						}
					}
				}

			}
            else
            {
				currentLengthRopeMax = lengthRopeMax;
			}


			if (TheLineTouch(ropePositions[ropePositions.Count - 1].position + distToHitPoints[distToHitPoints.Count - 1], ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], ropePositions[ropePositions.Count - 2]))
			{
				//TODO: add a method to update the joints if they are moving with an object (take the Transform instead of a list of vector)

				if (hit.transform != hookObject.transform && hit.transform != objectHanging && Vector3.Distance(hit.point, ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2]) > 0.2f)
					AddRopeJoint();
				 
			}
			/*            else
						{
							float ray_obj = Vector3.Distance(hook.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.max, hook.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.min) / 2;
							raycastHits = Physics.Raycast(player, dir, out hitAttachedToGrapplin, dir.magnitude - ray_obj * 1.5f);
						}
			*/


			if (hookObject.CompareTag("hook"))
				springJointRB.transform.position = ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2];


			DisplayRope();

            if (Convert.ToBoolean(inputs.Uni.Detach.ReadValue<float>()))
                detachHook = true;

        }


		//Comportements quand il y a un crochet détecté
		if (hookObject != null)
		{


			//When you grab the hook, the first behaviour of the rope is not a rigid line, only when you reach the end of the rope
			if (!hookObject.CompareTag("lever") && isGrappling)
			{
				// Quand la corde est tendue on peut la retracter
				if ( countGrapplin > 15 /*&& beginLengthMin < 0.5f*/)
					moveUpAndDown = true;

				if (Movement.isGrounded || Movement.distToHook < 0.3)
                {
					beginLengthMin = ropeLength - Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], objectHanging.position) + 1;
					hasChangedRope = true;
				}
				else if ( countGrapplin > 5 && Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], objectHanging.position) > spring.minDistance && Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], objectHanging.position) < spring.maxDistance)
				{
					if (hookObject.CompareTag("hook"))
					{
						beginLengthMin = ropeLength - Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], objectHanging.position);
						hasChangedRope = true;
					}
				}
			}


			// Retrait du grappin au bout d'un certain temps sur le levier
			if (hookObject.CompareTag("lever") && countGrapplin > timeLever && isGrappling == true)
			{
				CutRope();
			}
		}


		//The rope lenght changed
		if (hasChangedRope)
		{
			ropeLength = Mathf.Clamp(ropeLength, 1, currentLengthRopeMax);

			//Need to recalculate the k-value because it depends on the length of the rope
			UpdateRopePositions();
		}

	}

    private void FixedUpdate()
    {

		// Send Grapplin
		if ((attachHook || changeHook) && !isGrappling && countGrapplin>10 && !moveBox.canGrab )
		{

			attachHook = false;
			hookObject = hook_detector.GetComponent<hook_detector>().nearest_hook;


			if (hookObject && GetComponent<Movement>().enabled == true)
			{
				if (!Physics.Raycast( mainChar.GetComponent<Collider>().bounds.center , (hookObject.transform.position - mainChar.GetComponent<Collider>().bounds.center).normalized, out hit,
					Vector3.Distance(hookObject.transform.position, mainChar.GetComponent<Collider>().bounds.center)) &&
					!Physics.Raycast(hookObject.transform.position, (mainChar.GetComponent<Collider>().bounds.center - hookObject.transform.position).normalized, out hit,
					Vector3.Distance(hookObject.transform.position, mainChar.GetComponent<Collider>().bounds.center)))
				{
					Grapple();
					countGrapplin = 0;
					dist_objects = Vector3.Distance(hookObject.transform.position, objectHanging.position);
				}

			}

		}

		changeHook = false;

		// Retrait du grappin
		if ( detachHook && isGrappling == true)
		{
			CutRope();
			movements.JumpAfterGrapplin();
			detachHook = false;
		}


		// Retrait du grappin si on veut changer de crochet
		if (  attachHook && hookObject != hook_detector.GetComponent<hook_detector>().nearest_hook && isGrappling == true )  
		{
			attachHook = false;
			changeHook = true;
			CutRope();
            movements.JumpAfterGrapplin();
        }






		//Less rope
		if (isGrappling && moveUpAndDown
			&& (inputs.Uni.Grapple_Vert.ReadValue<float>() == 1 && (ropeLength > lengthRopeMin || ( hookObject.CompareTag("movable_hook") || hookObject.CompareTag("lever")) ))
			&& ropeLength >= lengthRopeMin)
		{

			MoveUp();
			hasChangedRope = true;
		}

		//More rope
		else if (isGrappling && moveUpAndDown
			&& (inputs.Uni.Grapple_Vert.ReadValue<float>() == -1 && ropeLength < lengthRopeMax && (Movement.isGrounded == false || (hookObject.CompareTag("movable_hook") || hookObject.CompareTag("lever")) ))
			&& ropeLength <= lengthRopeMax)
		{
			MoveDown();
			hasChangedRope = true;
		}

		countGrapplin += 1;
		attachHook = false;

	}

	void AttachHook()
	{
		attachHook = true;
	}

	void DttachHook()
	{
		detachHook = true;
	}


	// Envois du grappin
	public void Grapple()
	{


		beginLengthMin = 2f;
		currentLengthRopeMax = lengthRopeMax;

		if (hookObject.CompareTag("hook"))
		{
			Movement.isGrapplin = true;
			// Animation of the grapplin
			myAnimator.Play("GroundGrapplin" , 1);
			myAnimator.Play("GroundGrapplin", 2);
		}
		else if (hookObject.CompareTag("lever") || hookObject.CompareTag("movable_hook"))
        {
			// Animation of the grapplin lanched
			myAnimator.Play("GroundGrapplinLever" , 1);
			myAnimator.Play("GroundGrapplinLever", 2);
		}

		body.mass = loadMass;

		//rigidbodyCharacter.Grappling = true;

		//The first rope length is the distance between the two objects
		ropeLength = Vector3.Distance(hookObject.transform.position, objectHanging.position);


		//Add the weight to what the rope is carrying
		//GetComponent<Rigidbody>().mass = loadMass;


		// Add the Transforms to the list of rope positions
		ropePositions.Add(hookObject.transform);
		ropePositions.Add(objectHanging.transform);

		// Add the distances from the rope nodes to the hit points
		distToHitPoints.Add((objectHanging.transform.position - hookObject.transform.position).normalized * 0.2f /*whatTheRopeIsConnectedTo.GetComponent<SphereCollider>().radius*/ );
        //distToHitPoints.Add(Vector3.zero);

		distToHitPoints.Add(Vector3.zero);


		if (hookObject.CompareTag("hook"))
			// Add the first spring joint
			AddSpringJoint();

		if (hookObject.CompareTag("movable_hook"))
			// Add the first spring joint
			AddMovableSpringJoint();


		if (hookObject.CompareTag("lever"))
			hookObject.GetComponent<Lever>().Unlock();

		//Init the spring we use to approximate the rope from point a to b
		UpdateRopePositions();


		//Display the rope
		DisplayRope();

		LR.enabled = true;


		isGrappling = true;




	}

	// Add a spring joint
	public void AddSpringJoint()
	{
		//Add the spring joint component
		mainChar.AddComponent<SpringJoint>();
		spring = GetComponent<SpringJoint>();

		springJointRB = Instantiate(springjoint_rb_pref, ropePositions[0].position , Quaternion.identity);

		spring.connectedBody = springJointRB.GetComponent<Rigidbody>();
		spring.autoConfigureConnectedAnchor = false;
		spring.anchor = Vector3.zero;
		spring.connectedAnchor = Vector3.zero;

		//Add the value to the spring
		//spring.tolerance = 0.01f;
		spring.spring = 1000000000000f;
		spring.damper = 70f;

		spring.enableCollision = false;
	}

	public void AddMovableSpringJoint()
	{
		//Add the spring joint component
		mainChar.AddComponent<SpringJoint>();
		spring = GetComponent<SpringJoint>();

		spring.connectedBody = hookObject.GetComponent<Rigidbody>();
		spring.autoConfigureConnectedAnchor = false;
		spring.anchor = Vector3.zero;
		spring.connectedAnchor = Vector3.zero;

		//Add the value to the spring
		//spring.tolerance = 0.01f;
		spring.spring = 1000f;
		spring.damper = 70f;

		spring.enableCollision = true;
	}

	// Deplacement du joueur vers le point touche par le grappin
	public void MoveDown()
	{
		ropeLength += winchSpeed * Time.deltaTime;
		beginLengthMin = 1;
		bool isSoundFinished = checkLenghtSound.IsEventPlayingOnGameObject("Hook_ralonge_event", gameObject);
		if (!isSoundFinished)
			AkSoundEngine.PostEvent("Hook_ralonge_event", gameObject);
	}

	// DÃ©placement du joueur vers le point touchÃ© par le grappin
	public void MoveUp()
	{
		ropeLength -= winchSpeed * Time.deltaTime;
		
		bool isSoundFinished = checkLenghtSound.IsEventPlayingOnGameObject("Hook_retracte_event", gameObject);
		if (!isSoundFinished)
			AkSoundEngine.PostEvent("Hook_retracte_event", gameObject); 

		//AkSoundEngine.Event
	}

	// Décrochage
	public void CutRope()
	{
		isGrappling = false;
		moveUpAndDown = false;

		Destroy(spring);
		if (hookObject.CompareTag("hook"))
			Destroy(springJointRB);
		distToHitPoints.Clear();
		ropePositions.Clear();
		// Clear the line Renderer
		LR.positionCount = 0;
		LR.enabled = false;

		if (hookObject.CompareTag("hook"))
        {
			spring.minDistance = 0;
			if (!changeHook /*&& body.velocity.y<1*/ && countGrapplin > 10 && (!Movement.isGrounded && !Movement.isJumping) )
				body.AddForce(new Vector3(0, movements.jump_force, 0), ForceMode.Impulse);
			Movement.isGrapplin = false;
		}

/*
		if (hookObject.tag == "lever")
			hookObject.transform.Rotate(90, 0, 0);
*/

		hookObject = null;


	}


	//Update the spring constant and the length of the spring
	private void UpdateRopePositions()
	{

		if (hookObject.CompareTag("hook"))
		{
			//Update length of the rope
			spring.maxDistance = ropeLength;
			spring.minDistance = ropeLength - beginLengthMin;
		}

		if (hookObject.CompareTag("movable_hook"))
		{
			//Update length of the rope
			spring.maxDistance = ropeLength;
			spring.minDistance = 1f;
		}

		//The rope changed
		hasChangedRope = false;


	}

	//Display the rope with a line renderer
	private void DisplayRope()
	{
		/*			//This is not the actual width, but the width use so we can see the rope
					float ropeWidth = 0.2f;

					LR.startWidth = ropeWidth;
					LR.endWidth = ropeWidth;


					//Update the list with rope sections by approximating the rope with a bezier curve
					//A Bezier curve needs 4 control points
					Vector3 A = whatTheRopeIsConnectedTo.position;
					Vector3 D = whatIsHangingFromTheRope.position;

					//Upper control point
					//To get a little curve at the top than at the bottom
					Vector3 B = A + whatTheRopeIsConnectedTo.up * (-(A - D).magnitude * 0.1f);
					//B = A;

					//Lower control point
					Vector3 C = D + whatIsHangingFromTheRope.up * ((A - D).magnitude * 0.5f);

					//Get the positions
					BezierCurve.GetBezierCurve(A, B, C, D, ropePositions);


					//An array with all rope section positions
					Vector3[] positions = new Vector3[ropePositions.Count];

					for (int i = 0; i < ropePositions.Count; i++)
					{
						positions[i] = ropePositions[i];
					}*/



		ropePositions[ropePositions.Count - 1] = objectHanging.transform;
		distToHitPoints[distToHitPoints.Count - 1] = Vector3.zero;

		//Just add a line between the start and end position for testing purposes
		Vector3[] positions = new Vector3[distToHitPoints.Count];

		//positions[0] = whatTheRopeIsConnectedTo.transform.position;
		//positions[1] = whatIsHangingFromTheRope.transform.position;

		for (int i = 0; i < distToHitPoints.Count; i++)
		{
			positions[i] = ropePositions[i].position + distToHitPoints[i];
		}
		

		//Add the positions to the line renderer
		LR.positionCount = positions.Length;


		positions[distToHitPoints.Count - 1] = GameObject.FindGameObjectWithTag("GrapplinHand").transform.position ;
		// positions[distToHitPoints.Count - 1] += new Vector3(0f, 1f, 0f);

		LR.SetPositions(positions);
	}

	// Add a new rope joint when the line touch a rigidbody
	private void AddRopeJoint()
	{


		// Add the transform of the object touched by the raycast
		ropePositions.RemoveAt(ropePositions.Count - 1);
		ropePositions.Add(hit.transform);
		ropePositions.Add(objectHanging.transform);

		// Place a hit distance between the transform of the object and the hit point
		distToHitPoints.RemoveAt(distToHitPoints.Count - 1);
		distToHitPoints.Add(hit.point - hit.transform.position);
		distToHitPoints.Add(Vector3.zero);


		ropeLength -= Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], ropePositions[ropePositions.Count - 3].position + distToHitPoints[distToHitPoints.Count - 3]);
		currentLengthRopeMax -= Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], ropePositions[ropePositions.Count - 3].position + distToHitPoints[distToHitPoints.Count - 3]);

		//The new joint manage the rigidbody
		//spring.connectedBody = hit.rigidbody;


		ropeLength = Vector3.Distance(ropePositions[ropePositions.Count - 1].position + distToHitPoints[distToHitPoints.Count - 1], ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2]);

		UpdateRopePositions();


	}


	//Display the rope with a line renderer
	private void DeleteRopeJoint( int ropeId )
	{

		//ropeLength += Vector3.Distance(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], ropePositions[ropePositions.Count - 3].position + distToHitPoints[distToHitPoints.Count - 3]) ;

		//Remove the joint created before and add again the main character joint
		ropePositions.RemoveAt(ropeId-1);

		//Remove the joint created before and add again the main character joint
		distToHitPoints.RemoveAt(ropeId - 1);

		// TODO: here the spring is connecting to the first attach point, change it when it's possible with the list of Objects instead of a list of vec3
		// spring.connectedBody = ropePositions[ropePositions.Count - 2].gameObject.GetComponent<Rigidbody>();

		//springJointRB.transform.position = ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2];


		ropeLength = Vector3.Distance( ropePositions[ropePositions.Count - 1].position + distToHitPoints[distToHitPoints.Count - 1], ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2]);
		currentLengthRopeMax = Vector3.Distance(ropePositions[ropePositions.Count - 1].position + distToHitPoints[distToHitPoints.Count - 1], ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2]);

		UpdateRopePositions();


	}


	private bool TheLineTouch(Vector3 player, Vector3 hook_pos , Transform hook)
	{
		
		bool raycastHits = false;

		

		//Raycast( whatIsHangingFromTheRope.position , Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);

		Vector3 dir = hook_pos - player;

		float ray_obj = Vector3.Distance(hook.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.max, hook.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.min) / 2;
		raycastHits = Physics.Raycast(player, dir, out hit, dir.magnitude  /*- ray_obj * 1.5f*/);


		return raycastHits;
	}




	
	void OnDrawGizmos()
    {
		Gizmos.color = Color.yellow;

		if (distToHitPoints.Count >= 3)
			Gizmos.DrawSphere(ropePositions[ropePositions.Count - 3].position + distToHitPoints[distToHitPoints.Count - 3], 0.2f);

		if (distToHitPoints.Count >= 2)
			Gizmos.DrawSphere(ropePositions[ropePositions.Count - 2].position + distToHitPoints[distToHitPoints.Count - 2], 0.2f ) 
			/*Vector3.Distance(ropePositions[ropePositions.Count - 2].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.max, ropePositions[ropePositions.Count - 2].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.min) / 2)*/;

		Gizmos.color = Color.red;
		if (springJointRB!= null)
			Gizmos.DrawSphere(springJointRB.transform.position, 0.2f);
	}
	

}
