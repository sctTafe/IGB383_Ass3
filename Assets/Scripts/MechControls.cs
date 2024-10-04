using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechControls : MonoBehaviour {

    private Transform target;

    Animator anim;

    public Transform body;
    public Vector3 rotOffSet;
    public float rotSpeed = 1.0f;
    private Quaternion targetRotation;
    public GameObject rayCastPoint;

    public float moveSpeed;
    public float maxMoveSpeed;
    public float accDecSpeed;

    // Use this for initialization
    void Start () {

        anim = GetComponent<Animator>();

        //Acquire Camera aim target
        if (!target) {
            target = GameObject.FindGameObjectWithTag("MainCamera").transform.GetChild(0);
        }
	}

    private void Update() {

        MovementAndAnimations();

    }

    // Update is called once per frame
    void LateUpdate () {

        //Determine the target rotation. This is the rotation if the transform looks at the target point
        targetRotation = Quaternion.LookRotation(target.transform.position - body.transform.position);

        //Smoothly rotate towards the target point.
        body.rotation = Quaternion.Slerp(body.rotation, targetRotation * Quaternion.Euler(rotOffSet), rotSpeed * Time.deltaTime) ;

	}

    void MovementAndAnimations() {
        //Forward Movement
        if (Input.GetKey("w")) {

            //Running
            if (Input.GetKey("w") && Input.GetKey("left shift")) {
                Throttle(15);
                anim.SetBool("Running", true);
            }
            //Walking
            else {
                Throttle(5);
                anim.SetBool("Running", false);
            }

            anim.SetBool("Walking", true);
        }
        //Backward Movement
        else if (Input.GetKey("s")) {

            Throttle(-5);
            anim.SetBool("Backpedalling", true);
        }
        //Reset primitive movement
        else {
            Throttle(0);
            anim.SetBool("Walking", false);
            anim.SetBool("Backpedalling", false);
        }

        //Left and Right Rotation
        if (Input.GetKey("a")) {
            transform.Rotate(0, -rotSpeed * 15 * Time.deltaTime, 0, Space.World);
            anim.SetBool("Turning", true);
        }
        else if (Input.GetKey("d")) {
            transform.Rotate(0, rotSpeed * 15 * Time.deltaTime, 0, Space.World);
            anim.SetBool("Turning", true);
        }else {
            anim.SetBool("Turning", false);
        }
    }

    //Accelerate or Decelerate
    void Throttle(float maxSpeed) {
        
        if (maxSpeed > moveSpeed) {
            moveSpeed += Time.deltaTime * accDecSpeed;
        }
        else if (maxSpeed < moveSpeed) {
            moveSpeed -= Time.deltaTime * accDecSpeed;
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}
