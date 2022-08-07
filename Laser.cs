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
   public Rigidbody3D laser_head;
   public Rigidbody3D laser_end;
   public int number_of_targets;
   public GameObject[] targets = new GameObject[number_of_targets];
   public GameObject[] terrain;
   private LineRenderer lb;
   public bool isTrigger;
   public bool isInference;
   public bool ResartEpisode = true;
   public float current_rewards = 0f;
   public bool useVecObs;
   public float Speed = 3.0f;
   public float rot_Speed = 5.0f;

   public float laserWidth = 1.0f;
   public float noise = 1.0f;
   public float laserLength = 50.0f;
   public Color color = Color.green;
   Vecrtor3 Offset = new Vector3.zero;
   Angle rot_angle = new Angle(0, Angle.Type.Degrees);

   EnvironmentParameters m_ResetParams;
   Unity.MLAgents.Policies.BehaviorType BehaviorType;

   void Start ()
   {
    lr = GetComponent<LineRenderer>();
    lineRenderer.SetColors(color);
    laser_head.transform.position = new Vector3.zero;
    laser_end.transform.position = new Vector3(0, 0, -laserLength);

    isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;

    Debug.Log("IsInference = " + IsInference);
    Debug.Log("behaviourType = " + behaviourType);

    m_ResetParams = Academy.Instance.EnvironmentParameters;
   }

   public override void OnEpisodeBegin ()
   {
    if (ResartEpisode)
    {
      SetResetParameters();
      ResartEpisode = false;
    }
   }

   public override void CollectObeservations (VectorSensor VectorSensor)
   {
    if (useVecObs)
    {
        foreach (GameObject target in targets)
        {
         if (target.activeInhierarchy)
         {
            sensor.AddObservation(target.transform.position);
         }
         else
         {
            sensor.AddObservation(Vector3.zero);
         }
        }
    }

    if (isInference)
    {
      this.RequestDecision();
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
			StartCoroutine();
		}
	}

   public override void StartCoroutine ()
   {
      
   }

   void Update ()
   {
      lr.SetPosition(laser_head.transform.position, laser_end.transform.position);
      lineRenderer.SetWidth(laserWidth, laserWidth);

      if (!isInference && isPressed)
      {
				RequestDecision();
				if (isPreviousActionSet){MoveAgent(previousAction);}
		}

   }

   void Move (Vector3 movementVector)
   {
      laser_head.transform.Translate(movementVector);
   }

   Vector3 CalculateMovement ()
   {
      Vector3 totalMovement = new Vector3(Input.GetAxis("Horizontal") * Speed * Time.deltaTime, 0, 0);
      Vector3 movementIfApplied = totalMovement + transform.position;

      if (movementIfApplied.x > 50.0f || movementIfApplied.x < 0f)
      {
         totalMovement = Vector3.zero;
      }
   }

   void Rotate ()
   {
      if (Input.GetKey(KeyCode.A))
      {
         rot_angle = -rot_Speed * Time.deltaTime;
      }
      if (Input.GetKey(KeyCode.D))
      {
         rot_angle = rot_Speed * Time.deltaTime;
      }

      laser_end.transform.Translate(horizontal_distance, vertical_distance);
      if (rot_angle < new Angle(0, Angle.Type.Degrees))
      {
         horizontal_distance = -Math.Sin(rot_angle) * laserLength;
         vertical_distance = = -Math.Cos(rot_angle) * laserLength;
      }
      if (rot_angle >= new Angle(0, Angle.Type.Degrees))
      {
         horizontal_distance = Math.Sin(rot_angle) * laserLength;
         vertical_distance = = -Math.Cos(rot_angle) * laserLength;
      }
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

		StartCoroutine(Release());
	}

   public override void Heuristic (in ActionBuffers actionsOut)
   {
		isTraining = false;

		var continuousActionsOut = actionsOut.ContinuousActions;
		continuousActionsOut[0] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[0];
		continuousActionsOut[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[1];
	}

   void RewardAgent ()
   {
		Debug.Log("Time used to elimate the block: " + );
		m_currentReward = ((float)m_numberofEnemies - (float)Enemy.EnemiesAlive)/(float)m_numberofEnemies; 
		Debug.Log("Set reward = " + m_currentReward);
		SetReward(m_currentReward); 
	}

   public void SetResetParameters ()
   {
		m_currentReward = 0;
		ResetTarget();
		SetReward(0);

		foreach (GameObject target in targets)
      {
			if (!targets.activeInHierarchy)
         {
				Debug.Log("RESPAWN = " + enemyGO.name);
				enemyGO.GetComponent<Enemy>().Respawn();
			}
			enemyGO.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
			Random.Range(-1.4f, -1.0f), 0);
		}

		terrain = GameObject.FindGameObjectsWithTag("Terrain");
		foreach (GameObject wood in terrain)
      {
			wood.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
			Random.Range(-1.4f, -1.0f), 0);
		}

   }
}