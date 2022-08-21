using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.PhysicsModule.MeshCollider;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Laser : Agent
{
   public Rigidbody3D laser_head;
   public Rigidbody3D laser_end;
   public int number_of_targets;
   public GameObject[] targets = new GameObject[number_of_targets];
   public GameObject[] terrain;
   public LineRenderer lr;
   public bool isHit;
   public bool isInference;
   public bool ResartEpisode = true;
   public int m_currentReward = 0;
   public bool useVecObs;
   public float r = 20.0f;

   public float laserWidth = 1.0f;
   public float noise = 1.0f;
   public float laserLength = 50.0f;
   public Color color = Color.green;

   EnvironmentParameters m_ResetParams;
   Unity.MLAgents.Policies.BehaviorType BehaviorType;

   void Start ()
   {
    lr = GetComponent<LineRenderer>();
    lineRenderer.SetColors(color);
    laser_head = laser_head.GetComponent<Rigidbody3D>();
    laser_end = laser_end.GetComponent<Rigidbody3D>();
    laser_end.position = new Vector3(0, 0, -0.5);

    isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;

    Debug.Log("IsInference = " + IsInference);
    Debug.Log("behaviourType = " + behaviourType);

    m_ResetParams = Academy.Instance.EnvironmentParameters;

    target = GameObject.FindGameObjectsWithTag("Target");

   }

   public override void OnEpisodeBegin ()
   {
    if (ResartEpisode)
    {
      Debug.Log("Step: " + Academy.Instance.StepCount + " OnEpisodeBegin called");
      SetResetParameters();
      ResartEpisode = false;
      Debug.Log("The target is still there.");
    }
   }

   public override void CollectObeservations (VectorSensor sensor)
   {
    if (useVecObs)
    {
      foreach (GameObject target in targets)
      {
         if (target.activeInhierarchy)
         {
            Debug.Log("Target Position: " + target.transform.position);
            sensor.AddObservation(target.transform.position);
         }
         else
         {
            sensor.AddObservation(Vector3.zero);
            Debug.Log("Target Eliminated");
         }
      }
        
      if (isInference)
      {
         this.RequestDecision();
      }
    }
   }

   public override void OnActionReceived (ActionBuffers actionBuffers)
   {
		Debug.Log("onActionReceived");
		previousAction = actionBuffers.ContinuousActions;
		isPreviousActionSet = true;
		if (isInference)
      {
			MoveAgent(actionBuffers.ContinuousActions);
			StartCoroutine(MoveLaser());
		}
	}

   void Update ()
   {
      lr.SetPosition(laser_head.transform.position, laser_end.transform.position);
      lr.SetWidth(laserWidth, laserWidth);
      GenerateMeshCollider();

      if (!isInference && isPressed)
      {
				RequestDecision();
				if (isPreviousActionSet){MoveAgent(previousAction);}
		}

      if (m_currentReward >= 1 || ResartEpisode){
			Debug.Log("END END END");
			EndEpisode();
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			}

   }

   public void MoveAgent(ActionSegment<float> action){
	   Debug.Log("MoveAgent Step: " + Academy.Instance.StepCount);
		var mousePos = new Vector3(action[0], action[1], action[2]);

		if (mousePos.z >= 0){
         laser_head.position.x = mousePos.x;
         laser_head.position.y = mousePos.y;
         laser_head.position.z = Math.Sqrt(Math.Pow(r, 2) - Math.Pow(mousePos.x, 2) - Math.Pow(mousePos.y, 2));
      }
      else {
         laser_head.position.x = mousePos.x;
         laser_head.position.y = mousePos.y;
         laser_head.position.z = - Math.Sqrt(Math.Pow(r, 2) - Math.Pow(mousePos.x, 2) - Math.Pow(mousePos.y, 2));
      }
	}

   public override void Heuristic (in ActionBuffers actionsOut)
   {
		isTraining = false;

		var continuousActionsOut = actionsOut.ContinuousActions;
		continuousActionsOut[0] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[0];
		continuousActionsOut[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[1];
      continuousActionsOut[2] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[2];
	}

   void OnMouseDown ()
   {
		isPressed = true;
		laser_head.isKinematic = true;
	}

	void OnMouseUp ()
   {
		isPressed = false;
		laser_head.isKinematic = false;
	}

   public void ResetLaser(){
		Debug.Log("reset laser");
	   laser_head.transform.position = new Vector3(0, 0, r);
	}

   void RewardAgent ()
   {
      if (isHit == true) {
         m_currentReward = 1;
      }
      else {
         m_currentReward = 0;
      }
		Debug.Log("Set reward = " + m_currentReward);
		SetReward(m_currentReward); 
	}

   IEnumerator MoveLaser () {
		Academy.Instance.EnvironmentStep();  // evolve the env step

		Debug.Log("targets still alive = "+ Target.TargetsAlive);
		if (Target.TargetsAlive <= 0){
			Debug.Log("LEVEL WON!");
			levelWon = true;
		}
		else {levelWon = false;};

		if (levelWon){
			Debug.Log("End Episode");
			ResartEpisode = true; // is this needed now the control flow is better?
			// EndEpisode(); // Auto starts another Episode
		}
		RewardAgent();
		ResetLaser(); 
	}
   public void SetResetParameters ()
   {
		m_currentReward = 0;
		ResetLaser();
		SetReward(0);

		foreach (GameObject target in targets)
      {
			if (!targets.activeInHierarchy)
         {
				target.GetComponent<targets>().Respawn();
			}
			target.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), Random.Range(-1.4f, -1.0f), 0);
		}

		terrain = GameObject.FindGameObjectsWithTag("Terrain");
		foreach (GameObject block in terrain)
      {
			block.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), Random.Range(-1.4f, -1.0f), 0);
		}

   }

   public void GenerateMeshCollider () {
      MeshCollider collider = GetComponent<MeshCollider>();

      if (collider == null) {
         collider = GameObject.AddComponent<MeshCollider>();
      }

      Mesh mesh = new Mesh();
      lr.BakeMesh(mesh);
      collider.shareMesh = mesh;
   }
}