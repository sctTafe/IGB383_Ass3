using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : Projectile {

    public int sourceID;

    public float damage = 10.0f;

    public GameObject hitEffect;

    public override void OnTriggerEnter(Collider other) {

        //Check if target can take damage
        if (other.tag == "Environment") {
            Explode();
        } else if (other.GetComponent<TakeDamage>()) {
            if (other.GetComponent<TakeDamage>().ID != sourceID) {
                other.GetComponent<TakeDamage>().takeDamage(damage, sourceID);
                Explode();
            }
        }
    }
   

    void Explode() {
        Instantiate(hitEffect, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
