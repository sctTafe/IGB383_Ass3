using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour {

    //Generic be all end all pickup for Mechs
    //Contains health, energy, shells and missiles
    //Currently setup to randomize slightly on spawn

    public float healthRestore = 250;
    public float energyRestore = 250;
    public float shellsRestore = 15;
    public float missilesRestore = 36;

    //Effects
    public GameObject pickupSound;

    // Use this for initialization
    void Start() {
        healthRestore = healthRestore + Random.Range(-100, 100);
        energyRestore = energyRestore + Random.Range(-100, 100);
        shellsRestore = shellsRestore + Random.Range(-5, 5);
        missilesRestore = missilesRestore + Random.Range(-12, 12);
    }

    // Update is called once per frame
    void Update() {

    }

    void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player") {
            other.GetComponent<MechSystems>().health += healthRestore;
            other.GetComponent<MechSystems>().energy += energyRestore;
            other.GetComponent<MechSystems>().shells += shellsRestore;
            other.GetComponent<MechSystems>().missiles += missilesRestore;
            Instantiate(pickupSound, transform.position, transform.rotation);
            Destroy(this.gameObject);
        }
    }
}
