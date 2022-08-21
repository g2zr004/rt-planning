using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public GameObject deathEffect;
	public static int TargetsAlive = 0;
	
	public static int RewardGiven = 0;

	void Start ()
	{
		TargetsAlive = 1;
		Debug.Log("start target " + TargetsAlive);
	}

	void OnCollisionEnter3D (Collision3D colInfo)
	{
		Die();
	}

	void Die ()
	{
		Instantiate(deathEffect, transform.position, Quaternion.identity);

		TargetsAlive = 0;
		RewardGiven = 1;
		Destroy(gameObject);
		gameObject.SetActive(false);
	}

	public void Respawn () 
	{
		Destroy(deathEffect);
		deathEffect.SetActive(false);
		TargetsAlive = 1; 
		gameObject.SetActive(true);

	}
}
