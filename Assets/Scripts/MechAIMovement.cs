using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class MechAIMovement : MonoBehaviour {

    Animator anim;

    //Movement variables
    public NavMeshAgent agent;
    public float rotSpeed = 1.0f;
    public GameObject moveTarget;

	// Use this for initialization
	void Start () {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {

	}

    public void Movement(Vector3 position, float stopDistance) {

        //If target is not near and is within 90 degrees of forward vector, move towards
        if (Vector3.Distance (transform.position, position) >= stopDistance+2) {
            // && Vector3.Angle(transform.forward, target.transform.position) < 90) {
            agent.SetDestination(position);
            agent.stoppingDistance = stopDistance;
            anim.SetBool("Walking", true);
        }
        //Rotate towards target
        else { 
            transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, position - transform.position, rotSpeed * Time.deltaTime, 0.0f));
            anim.SetBool("Walking", false);
        }
    }

}
