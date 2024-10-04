using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOEDamage : MonoBehaviour {

    public int sourceID = -1;

    public float damage = 10;

    public float radius = 5;

    private float lifeTime;
    public float lifeTimeDuration = 0.1f;

    public List<Transform> damageTargets = new List<Transform>();

    // Use this for initialization
    void Start() {

        lifeTime = Time.time + lifeTimeDuration;

        //Set size of explosion
        transform.GetComponent<SphereCollider>().radius = radius;
    }

    // Update is called once per frame
    void Update() {

        // Explosion finish, damage targets and remove AOE field
        if (Time.time > lifeTime) {
            foreach (Transform target in damageTargets) {
                if (target != null)
                    target.GetComponent<MechSystems>().takeDamage(damage, sourceID);
            }
            Destroy(this.gameObject);
        }
    }


    //Add possible targets to damageTargets list while they exist for duration of explosion
    void OnTriggerEnter(Collider other) {

        if (other.GetComponent<TakeDamage>() != null) {

            //Check if target is not origin object
            if (other.GetComponent<TakeDamage>().ID != sourceID)
                damageTargets.Add(other.gameObject.transform);
        }
    }

}
