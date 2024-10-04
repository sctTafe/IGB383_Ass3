using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechAIAiming : MonoBehaviour {

    //Aiming variables
    public GameObject aimTarget;
    public GameObject body;
    public Vector3 rotOffSet;
    private Quaternion targetRotation;
    public bool firing = false;
    private float aimTimer;
    private float aimTime = 3.0f;

    //Target Variables
    public GameObject[] targets;
    public List<GameObject> currentTargets = new List<GameObject>();
    public GameObject frustumPointA;
    public GameObject frustumPointB;
    private Vector2 a;
    private Vector2 b;
    private Vector2 c;

    //Raycasting
    public GameObject rayCastTarget;
    public GameObject rayCastPoint;

    public float rotSpeed = 1.0f;

    // Use this for initialization
    void Start () {
        
    }

    // Update is called once per frame
    void Update() {

        targets = GameObject.FindGameObjectsWithTag("Player");

        if (aimTarget) {
            Aim();
        }
    }

    //Allocates the closestTarget if targets are in aiming frustum
    public bool FrustumCheck() {

        //Update Barycentric coords based on current position of frustum and player
        a = new Vector2(frustumPointA.transform.position.x, frustumPointA.transform.position.z);
        b = new Vector2(frustumPointB.transform.position.x, frustumPointB.transform.position.z);
        c = new Vector2(rayCastPoint.transform.position.x, rayCastPoint.transform.position.z);

        //Draw some lines to see in debug
        Debug.DrawLine(frustumPointA.transform.position, frustumPointB.transform.position, Color.magenta);
        Debug.DrawLine(frustumPointB.transform.position, rayCastPoint.transform.position, Color.magenta);
        Debug.DrawLine(rayCastPoint.transform.position, frustumPointA.transform.position, Color.magenta);

        //Check through each target in array
        foreach (GameObject target in targets) {
            //Update vector position of current target
            Vector2 tar = new Vector2(target.transform.position.x, target.transform.position.z);

            //If current target is in frustum, add it as a potential interactable
            if (Barycentric(a, b, c, tar) && !currentTargets.Contains(target) && LineOfSight(target)) {
                currentTargets.Add(target);
            }
            //Otherwise remove it if it is listed as one
            else if (!Barycentric(a, b, c, tar) && currentTargets.Contains(target) && !LineOfSight(target)) {
                currentTargets.Remove(target);
            }
        }

        //Return Logic
        if (currentTargets.Count > 0)
            return true;
        else {
            return false;
        }
    }

    //Vector math for calculating 2D barycentric true/false (point in triangle)
    private bool Barycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 tar) {

        Vector2 v0 = c - a, v1 = b - a, v2 = tar - a;

        // Compute dot products
        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // return if point is in triangle
        return ((u >= 0) && (v >= 0) && (u + v < 1));
    }

    //Method to calculate closest target within LOS within currentTargets (i.e. Frustum)
    public GameObject ClosestTarget(List<GameObject> targetList) {

        if (targetList.Count > 0) {

            float distance = Mathf.Infinity;
            Vector3 position = transform.position;
            GameObject thisTarget = null;

            foreach (GameObject target in targetList) {

                if (target) { //Error check - target may die during check
                    Vector3 diff = target.transform.position - position;
                    float curDistance = diff.sqrMagnitude;

                    //Replace thisTarget as current target if it is closer AND within LOS
                    if (curDistance < distance && LineOfSight(target)) {
                        thisTarget = target;
                        distance = curDistance;
                    }
                }
            }
            return thisTarget;
        }
        else
            return null;
    }

    //Method to determine if object is within LOS of Mech
    public bool LineOfSight(GameObject thisTarget) {

        //Need to correct for wonky pivot point - Mech model pivot at base instead of centre
        Vector3 correction = thisTarget.transform.GetChild(0).gameObject.transform.position;

        RaycastHit hit;
        if (Physics.Raycast(rayCastPoint.transform.position, -(rayCastPoint.transform.position - correction).normalized, out hit, 100.0f)) {

            Debug.DrawLine(transform.position, hit.point, Color.red);

            if (hit.transform.gameObject.tag == "Player" && hit.transform.gameObject != this.gameObject) {
                return true;
            }
            else
                return false;
        }
        else
            return false;
    }

    //Select random target to aim at from passed array
    public void RandomAimTarget(GameObject[] aimTars) {
        if (Time.time > aimTimer) {

            aimTarget = aimTars[Random.Range(0, aimTars.Length - 1)].transform.GetChild(0).gameObject; ;

            aimTimer = Time.time + aimTime + Random.Range(-0.5f, 0.5f);
        }
    }

    private void Aim() {
        //Determine the target rotation. This is the rotation if the transform looks at the target point
        targetRotation = Quaternion.LookRotation(aimTarget.transform.position - body.transform.position);

        //Smoothly rotate towards the target point.
        body.transform.rotation = Quaternion.Slerp(body.transform.rotation, targetRotation * Quaternion.Euler(rotOffSet), rotSpeed * Time.deltaTime);
    }

    //Fire at player - if within LOS and angle from forward vector
    public bool FireAngle(float angle) {
        RaycastHit hit;
        if (Physics.Raycast(rayCastPoint.transform.position, -(rayCastPoint.transform.position - aimTarget.transform.position).normalized, out hit, 100.0f)) {

            //If looking at player and within aiming angle
            if (hit.transform.tag == "Player" &&
                    Vector3.Angle(aimTarget.transform.position - body.transform.position, body.transform.forward) < angle)
                return true;
            else
                return false;
        }
        else
            return false;
    }
}
