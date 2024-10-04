using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : Projectile {

    public int sourceID;

    public GameObject explosion;
    public GameObject AOEDamage;

    public GameObject explosionSound;

    public override void OnTriggerEnter(Collider other) {

        //Check if surface is environment or capable of taking damage
        if (other.tag == "Environment") {
            Explode();
        } else if (other.GetComponent<TakeDamage>()) {
            if (other.GetComponent<TakeDamage>().ID != sourceID)
                Explode();
        }
    }


    void Explode() {
        Instantiate(explosion, transform.position, transform.rotation);
        GameObject thisAOEDmg = Instantiate(AOEDamage, transform.position, transform.rotation) as GameObject;
        thisAOEDmg.GetComponent<AOEDamage>().sourceID = sourceID;
        Instantiate(explosionSound, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
