using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMech : MonoBehaviour {

    public GameObject[] aimTargets;

    public GameObject aimTarget;
    public GameObject body;
    public Vector3 rotOffSet;
    public float rotSpeed = 0.5f;
    private Quaternion targetRotation;

    public float aimTimer;
    public float aimTime = 3.0f;

    // Use this for initialization
    void Start () {
        Time.timeScale = 1;
	}
	
	// Update is called once per frame
	void Update () {

        if(Time.time > aimTimer) {

            aimTarget = aimTargets[Random.Range(0, aimTargets.Length - 1)];
            aimTimer = Time.time + aimTime;
        }


        //Determine the target rotation. This is the rotation if the transform looks at the target point
        targetRotation = Quaternion.LookRotation(aimTarget.transform.position - body.transform.position);

        //Smoothly rotate towards the target point.
        body.transform.rotation = Quaternion.Lerp(body.transform.rotation, targetRotation * Quaternion.Euler(rotOffSet), rotSpeed * Time.deltaTime);


    }
}
