using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : Projectile {

    public int sourceID;

    public GameObject AOEDamage;

    //Ignition
    public float ignitionTime;
    private bool ignited = false;

    //Movement
    public float acceleration = 50.0f;
    public float maxSpeed = 150.0f;

    //Rotation variables
    private Quaternion targetRotation;
    private float adjRotSpeed;
    public float rotSpeed = 10.0f;

    //Missile Lock
    private float lockTime;
    private bool locked = true;
    public Vector3 lockPosition;
    public GameObject lockTarget;

    //Effects
    public GameObject explosion;
    public GameObject flames;
    public GameObject explosionSound;

    // Use this for initialization
    void Start() {
        Destroy(this.gameObject, lifeTime);

        //Randomize accel
        acceleration = Random.Range(acceleration - 5, acceleration + 5);

        //Calculate locktime
        lockTime = Time.time + Random.Range(1.5f, 2.0f);
    }

    // Update is called once per frame
    void Update() {

        //If locked target, lockPosition is active lockTarget's position
        if (lockTarget)
            lockPosition = lockTarget.transform.position;

        if(Time.time > ignitionTime && ignitionTime > 0) {
            Movement();
            transform.parent = null;
        }
    }
            

    public override void Movement() {

        //Move forward
        transform.position += transform.forward * speed * Time.deltaTime;

        //Increase movement speed over time
        speed += Time.deltaTime * acceleration;
        if (speed > maxSpeed)
            speed = maxSpeed;

        //Missile lock
        if(Time.time > lockTime && locked) {
            targetRotation = Quaternion.LookRotation(lockPosition - transform.position);
            adjRotSpeed = Mathf.Min(rotSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
        }

        //Unlock if near target
        if (Vector3.Distance(transform.position, lockPosition) < 5)
            locked = false;
    }

    public override void OnTriggerEnter(Collider other) {

        //Check if surface is environment or capable of taking damage
        if (other.tag == "Environment") {
            Explode();
        }
        else if (other.GetComponent<TakeDamage>()) {
            if (other.GetComponent<TakeDamage>().ID != sourceID)
                Explode();
        }
    }

    void Explode() {
        Instantiate(explosion, transform.position, transform.rotation);
        Instantiate(flames, transform.position, transform.rotation);
        GameObject thisAOEDmg = Instantiate(AOEDamage, transform.position, transform.rotation) as GameObject;
        thisAOEDmg.GetComponent<AOEDamage>().sourceID = sourceID;
        GameObject expSound = Instantiate(explosionSound, transform.position, transform.rotation) as GameObject;
        expSound.GetComponent<AudioSource>().pitch = Random.Range(0.5f, 1.25f);
        Destroy(this.gameObject);
    }

}
