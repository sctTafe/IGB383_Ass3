using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechSystems : TakeDamage {

    //Primitive Class for controlling destroyable objects health and death effects

    GameManager gameManager;

    public bool bot = false;
    private bool dead = false;

    public float health = 2000.0f;

    //Ammunition
    public float energy = 750;
    public float shells = 30;
    public float missiles = 54;

    //Effects
    public GameObject spawnEffect;
    public GameObject spawnSound;
    public GameObject explosion;
    public GameObject explosionSound;

    private void Start() {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        Instantiate(spawnEffect, transform.position, transform.rotation);
        Instantiate(spawnSound, transform.position, transform.rotation);
    }


    private void Update() {

        regenEnergy();

        //Clamp resources
        health = Mathf.Clamp(health, 0, 2000);
        energy = Mathf.Clamp(energy, 0, 750);
        shells = Mathf.Clamp(shells, 0, 30);
        missiles = Mathf.Clamp(missiles, 0, 54);
    }

    //Auto regeneration of Energy - Current cap of 1000
    private void regenEnergy() {
        if (energy < 750)
            energy += 10 * Time.deltaTime;
    }

    public override void takeDamage(float damage, int origin) {
        health -= damage;

        if (health <= 0 && !dead) {
            Instantiate(explosion, transform.position, transform.rotation);
            Instantiate(explosionSound, transform.position, transform.rotation);
            gameManager.playerScores[origin] += 1;
            gameManager.playerDeaths[ID] += 1;
            dead = true;
            if (bot) {
                gameManager.StartCoroutine(gameManager.RespawnBot(ID));
            }
            Destroy(this.gameObject);
        }

        if (bot)
            GetComponent<MechAIDecisions>().TakingFire(origin);
    }
}
