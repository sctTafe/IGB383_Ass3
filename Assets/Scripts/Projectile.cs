using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    public float speed = 100;

    public float lifeTime = 3.0f;

    // Use this for initialization
    void Start () {
        Destroy(this.gameObject, lifeTime);
    }
	
	// Update is called once per frame
	void Update () {
        Movement();
    }

    public virtual void Movement() {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public virtual void OnTriggerEnter(Collider other) {

        if (other.gameObject.tag == "Environment") {
            Destroy(this.gameObject);
        }
    }
}
