using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour {

    public GameObject pickup;
    private GameObject activePickup;
    public GameObject pickupSpawnPos;

    public float respawnTime = 5.0f;
    private float respawnTimer;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
        if(!activePickup && Time.time > respawnTimer) {
            activePickup = Instantiate(pickup, pickupSpawnPos.transform.position, pickupSpawnPos.transform.rotation);
            respawnTimer = Time.time + respawnTime;
        } 
	}
}
