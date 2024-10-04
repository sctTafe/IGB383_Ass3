using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechSounds : MonoBehaviour {

    public bool AIMech = false;

    public MechControls mechControls;

    public GameObject footstep;
    public GameObject footstepx;    //Running

    //Engine Audio
    public AudioSource engine;

    //TorsoTwist Audio
    public AudioSource torso;
    public GameObject rayCastPoint;
    private Transform target;

	// Use this for initialization
	void Start () {

        if (!AIMech) {
            //Acquire Camera aim target
            if (!target) {
                target = GameObject.FindGameObjectWithTag("MainCamera").transform.GetChild(0);
            }
        }

	}
	
	// Update is called once per frame
	void Update () {

        //Engine Sound
        if (!AIMech) {

            engine.pitch = Mathf.Clamp(mechControls.moveSpeed / 5, 0.5f, 1.5f);

            //TorsoTwist Audio
            if (Vector3.Angle(rayCastPoint.transform.position - target.transform.position, rayCastPoint.transform.forward) > 2.5f) {
                torso.enabled = true;
                torso.pitch = Vector3.Angle(rayCastPoint.transform.forward, target.transform.forward) / 35;
            }
            else
                torso.enabled = false;
        }
    }

    public void MechFootStep() {
        Instantiate(footstep, transform.position, transform.rotation);
    }

    public void MechFootStepX() {
        Instantiate(footstepx, transform.position, transform.rotation);
    }
}
