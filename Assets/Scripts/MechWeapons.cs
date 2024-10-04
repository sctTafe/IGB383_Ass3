using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechWeapons : MonoBehaviour {

    Animator anim;

    public MechSystems mechSystem;

    //Raycasting
    public GameObject rayCastTarget;
    public GameObject rayCastPoint;

    //Laser variables
    public GameObject laser;
    public GameObject[] laserMuzzles;
    private float laserFireTime;
    private float laserFireRate = 0.1f;
    public GameObject laserMuzzleFlash;
    public GameObject laserSound;

    //Hellfire Cannon variables
    public GameObject shell;
    public GameObject[] CannonMuzzles;
    private bool CannonAlternate = true;
    private float CannonFireTime;
    private float CannonFireRate = 0.75f;
    public GameObject cannonMuzzleFlash;
    public GameObject cannonFireSound;

    //Laser Beam Cannon variables
    public LineRenderer[] beamRenders;
    private float beamTimer;
    private bool beamsActive = false;
    private float beamDamage = 10;
    private float beamFireRate = 6.0f;
    private float beamCooldown = 3.5f;
    private float beamHitTimer;
    private float beamHitTime = 0.05f;
    public Light beamLightFlare;
    public GameObject beamHitEffect;
    public AudioSource beamAudio;

    //Missile Array
    public GameObject missile;
    public GameObject[] MissileSlots;
    private float missileFireTime;
    private float missileRechargeTime = 5.0f;
    private bool missileFiring = false;
    private GameObject lockedTarget;
    private float lockTimer;

	// Use this for initialization
	void Start () {
        anim = GetComponent<Animator>();

        mechSystem = GetComponent<MechSystems>();
    }
	
	// Update is called once per frame
	void Update () {

        Lasers();

        Cannons();

        LaserBeam();

        MissileArray();
	}


    public void Lasers() {

        if(Input.GetMouseButton(0) && mechSystem.energy >= 4){

            anim.SetBool("Lasers", true);

            if (Time.time > laserFireTime) {
                //Fire from first 4 weapon muzzles
                for (int i = 0; i < 4; i++) {
                    GameObject thisLaser = Instantiate(laser, laserMuzzles[i].transform.position, laserMuzzles[i].transform.rotation) as GameObject;
                    thisLaser.GetComponent<Laser>().sourceID = mechSystem.ID;
                    Instantiate(laserMuzzleFlash, laserMuzzles[i].transform.position, laserMuzzles[i].transform.rotation);
                }
                mechSystem.energy -= 4;
                Instantiate(laserSound, transform.position, transform.rotation);
                laserFireTime = Time.time + laserFireRate;
            }
        } else
            anim.SetBool("Lasers", false);
    }

    //Hellfire Cannons
    void Cannons() {

        if (Input.GetMouseButton(1) && Time.time > CannonFireTime && mechSystem.shells >= 2) {

            //Alternate between cannon array firing
            if (CannonAlternate) {
                for (int i = 0; i < 2; i++) {
                    GameObject thisShell = Instantiate(shell, CannonMuzzles[i].transform.position, CannonMuzzles[i].transform.rotation) as GameObject;
                    thisShell.GetComponent<Shell>().sourceID = mechSystem.ID;
                    Instantiate(cannonMuzzleFlash, laserMuzzles[i].transform.position, CannonMuzzles[i].transform.rotation);
                }
                CannonAlternate = false;
                anim.SetBool("CannonsA", true);
                anim.SetBool("CannonsB", false);
            } else if (!CannonAlternate) {
                for (int i = 2; i < 4; i++) {
                    GameObject thisShell = Instantiate(shell, CannonMuzzles[i].transform.position, CannonMuzzles[i].transform.rotation) as GameObject;
                    thisShell.GetComponent<Shell>().sourceID = mechSystem.ID;
                    Instantiate(cannonMuzzleFlash, CannonMuzzles[i].transform.position, CannonMuzzles[i].transform.rotation);
                }
                CannonAlternate = true;
                anim.SetBool("CannonsA", false);
                anim.SetBool("CannonsB", true);
            }
            mechSystem.shells -= 2;
            Instantiate(cannonFireSound, transform.position, transform.rotation);
            CannonFireTime = Time.time + CannonFireRate;
        } else {
            anim.SetBool("CannonsA", false);
            anim.SetBool("CannonsB", false);
        }
    }

    void LaserBeam() {

        //Laser firing with automatic cooldown
        if (Input.GetKey("f") && Time.time > beamTimer && mechSystem.energy >= 250) {
            beamsActive = true;
            beamTimer = Time.time + beamFireRate;
            beamAudio.enabled = true;
        } else if (Time.time > beamTimer - beamCooldown) {
            beamsActive = false;
            beamAudio.enabled = false;
        }

        //While firing...
        if (beamsActive) {

            beamLightFlare.enabled = true;
            mechSystem.energy -= 100 * Time.deltaTime;

            for (int i = 0; i < 2; i++) {
                beamRenders[i].enabled = true;
                beamRenders[i].SetPosition(0, beamRenders[i].transform.position);
                beamRenders[i].SetPosition(1, rayCastTarget.transform.position);

                beamRenders[i].material.SetTextureOffset("_MainTex", new Vector2(Time.deltaTime * 10.0f, 0.0f));

                RaycastHit hit;
                if (Physics.Raycast(beamRenders[i].transform.position, -(beamRenders[i].transform.position - rayCastTarget.transform.position).normalized, out hit, 50.0f)) {

                    //Set draw for destination of beam. If enemy intercepting, draw at hit point, else draw at position
                    if (hit.transform.tag == "Environment" || hit.transform.GetComponent<TakeDamage>() != null) {

                        beamRenders[i].SetPosition(1, hit.point);

                        //Damage & Hit Effects
                        if(Time.time > beamHitTimer) {

                            //Damage
                            if (hit.transform.GetComponent<TakeDamage>())
                                hit.transform.GetComponent<TakeDamage>().takeDamage(beamDamage, mechSystem.ID);

                            //Effects
                            GameObject thisBeamHit = Instantiate(beamHitEffect, hit.point, transform.rotation) as GameObject;
                            thisBeamHit.transform.parent = hit.transform;

                            beamHitTimer = Time.time + beamHitTime;
                        }
                    }
                }          
            }
        }
        else {  // Turn off all beams
            for (int i = 0; i < 2; i++) {
                beamRenders[i].enabled = false;
            }
            beamLightFlare.enabled = false;
        }
    }

    //Missile Array - Scattered Ground to Ground Missile system
    void MissileArray() {

        //Live Target Acquisition: Enemy Mech - 3 Second Lock System
        RaycastHit hit;
        if (Physics.Raycast(rayCastPoint.transform.position, -(rayCastPoint.transform.position - rayCastTarget.transform.position).normalized, out hit, 50.0f)) {

            if (hit.transform.GetComponent<TakeDamage>()) {
                lockedTarget = hit.transform.gameObject;
                lockTimer = Time.time + 3.0f;
            }
            else if (lockTimer < Time.time)
                lockedTarget = null;
        }

        //Firing System
        if (Input.GetKey("space") && Time.time > missileFireTime && mechSystem.missiles >= 18) {

            //Repeat 3 times...
            for (int j = 0; j < 3; j++) {
                //For each missile slot
                for (int i = 0; i < 6; i++) {

                    GameObject thisMissile = Instantiate(missile, MissileSlots[i].transform.position, MissileSlots[i].transform.rotation) as GameObject;
                    thisMissile.GetComponent<Missile>().sourceID = mechSystem.ID;
                    thisMissile.GetComponent<Missile>().ignitionTime = Time.time + Random.Range(0.2f, 0.8f);
                    thisMissile.gameObject.transform.parent = MissileSlots[i].transform;

                    //If recently lockedTarget
                    if (lockedTarget && Time.time < lockTimer)
                        thisMissile.GetComponent<Missile>().lockTarget = lockedTarget;
                    //Otherwise target part of the environment
                    else if (Physics.Raycast(rayCastPoint.transform.position, -(rayCastPoint.transform.position - rayCastTarget.transform.position).normalized, out hit, 50.0f)) {
                        if (hit.transform.tag == "Environment")
                            thisMissile.GetComponent<Missile>().lockPosition = scatteredLock(hit.point, 5);
                    }
                    //Otherwise just lock onto random position near the ray cast target position
                    else
                        thisMissile.GetComponent<Missile>().lockPosition = scatteredLock(rayCastTarget.transform.position, 5);
                }
            }
            mechSystem.missiles -= 18;
            missileFireTime = Time.time + missileRechargeTime;
        }
    }


    private Vector3 scatteredLock(Vector3 lockPos, float spread) {

        lockPos.x += Random.Range(-spread, spread);
        lockPos.y += Random.Range(-spread, spread);
        lockPos.z += Random.Range(-spread, spread);

        return lockPos;
    }

}
