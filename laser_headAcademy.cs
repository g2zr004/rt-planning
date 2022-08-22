using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Laser : Agent
{
   public Rigidbody laser_head;
   public Rigidbody laser_end;
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
   public float laserLength = 50.0f;
   public Color color = Color.green;

   EnvironmentParameters m_ResetParams;
   Unity.MLAgents.Policies.BehaviorType BehaviorType;

   void Start ()
   {
    lr = GetComponent<LineRenderer>();
    lr.startColor = color;
    lr.endColor = color;
    lr.startWidth = laserWidth;
    lr.endWidth = laserWidth;
    laser_head = laser_head.GetComponent<Rigidbody>();
    laser_end = laser_end.GetComponent<Rigidbody>();
    laser_end.position = new Vector3(0, -0.5f, 0);

    isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;

    Debug.Log("IsInference = " + IsInference);
    Debug.Log("behaviourType = " + behaviourType);

    m_ResetParams = Academy.Instance.EnvironmentParameters;

    targets = GameObject.FindGameObjectsWithTag("Target");

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

   public override void CollectObservations (VectorSensor sensor)
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

		 if (mousePos.y >= 0){
            rb.transform.position = new Vector3(mousePos.x, Convert.ToSingle(Math.Sqrt(Math.Pow(r, 2) - Math.Pow(mousePos.x, 2) - Math.Pow(mousePos.z, 2))), mousePos.z);
        }
        else {
            rb.transform.position = new Vector3(mousePos.x, - Convert.ToSingle(Math.Sqrt(Math.Pow(r, 2) - Math.Pow(mousePos.x, 2) - Math.Pow(mousePos.z, 2))), mousePos.z);
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
	   laser_head.transform.position = new Vector3(0, r, 0);
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