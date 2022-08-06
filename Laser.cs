using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public class Laser : Agent
{
   public Rigidbody3D laser_head;
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

   public float laserWidth = 1.0f;
   public float noise = 1.0f;
   public float laserLength = 50.0f;
   public Color color = Color.green;
   Vecrtor3 Offset = new Vector3.zero;

   EnvironmentParameters m_ResetParams;
   Unity.MLAgents.Policies.BehaviorType BehaviorType;

   void Start ()
   {
    lr = GetComponent<LineRenderer>();
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
			StartCoroutine(Release());
		}
	}

   void Update ()
   {
      lr.SetPosition(laser_head.position, )
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
      if (Input.GetKey(KeyCode.RightArrow))
      {
           transform.Rotate(new Vector3(0, 1, 0) * Time.deltaTime * Speed, Space.World);
      }

      if (Input.GetKey(KeyCode.LeftArrow))
      {
          transform.Rotate(new Vector3(0, -1, 0) * Time.deltaTime * Speed, Space.World);
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
		Debug.Log("reward func:: n Enemies = " + m_numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);
		m_currentReward = ((float)m_numberofEnemies - (float)Enemy.EnemiesAlive)/(float)m_numberofEnemies; 
		Debug.Log("Set reward = " + m_currentReward);
		SetReward(m_currentReward); 
	}

   public void SetResetParameters ()
   {
		m_currentReward = 0;
		m_throwsRemaining = m_numberofThrows; 
		ResetBall();
		SetReward(0);

		foreach (GameObject enemyGO in m_cachedEnemiesGO)
      {
			if (!enemyGO.activeInHierarchy)
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