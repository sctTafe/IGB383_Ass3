using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;
using UnityEditor.Experimental.GraphView;
using System.Linq;

public class MechAIDecisions : MechAI {

    public string botName = "Test Bot";

    //Links to Mech AI Systems
    MechSystems mechSystem;
    MechAIMovement mechAIMovement;
    MechAIAiming mechAIAiming;
    MechAIWeapons mechAIWeapons;

    //FSM State Implementation
    public enum MechStates {
        Roam,
        Attack,
        Pursue,
        Flee
    }
    public MechStates mechState;

    //Roam Variables
    public GameObject[] patrolPoints;
    private int patrolIndex = 0;
    public GameObject[] aimTargets;

    //Attack Variables
    [SerializeField] private GameObject attackTarget;
    private float attackTime = 3.5f;
    private float attackTimer;

    //Pursue Variables
    public GameObject pursueTarget;
    private Vector3 pursuePoint;

    //Flee Variables
    public GameObject fleeTarget;

    private float _laser_ActivationDistance = 50f;
    private float _laser_FireAngle = 20f;

    private float _cannon_ActivationDistance = 100f;
    private float _cannon_FireAngle = 15f; // Probably to high?

    private float _missile_ActivationDistance = 200f;
    private float _missile_FireAngle = 40f;

    private float _beam_ActivationDistanceMin = 20f;
    private float _beam_ActivationDistanceMax = 50f;
    private float _beam_FireAngle = 20f;

    // Assumed Knowledge
    private PickupSpawner[] _allResorucePickupPoints;
    [SerializeField] private List<GameObject> _ResourcePoints = new List<GameObject>();



    // Use this for initialization
    void Start () {
        //Collect Mech and AI Systems
        mechSystem = GetComponent<MechSystems>();
        mechAIMovement = GetComponent<MechAIMovement>();
        mechAIAiming = GetComponent<MechAIAiming>();
        mechAIWeapons = GetComponent<MechAIWeapons>();

        //Roam State Startup Declarations
        patrolPoints = GameObject.FindGameObjectsWithTag("Patrol Point");
        patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);

        //Get Assumed Knowledge
        _allResorucePickupPoints = Object.FindObjectsOfType<PickupSpawner>();
        foreach (var rp in _allResorucePickupPoints) {
            if (rp.enabled == true)
                _ResourcePoints.Add(rp.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Acquire valid attack target: perform frustum and LOS checks and determine closest target
        mechAIAiming.FrustumCheck();

        //OLD_WeaponsSystem();
        //OLD_FSMStateSwitching();


        // TODO: Circile back to this to remove it
        if (!attackTarget)
        {
            attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
            mechAIWeapons.laserBeamAI = false;  //Hard disable on laserBeam
        }


        

    }

    //private GameObject GetClosestResroucePoint()
    //{
    //    float closestDis = float.MaxValue;
    //    GameObject currentClosest = null;
    //    float distToRp;
    //    foreach (var rp in _ResourcePoints)
    //    {
    //        distToRp = Vector3.Distance(this.transform.position, rp.transform.position);
    //        if (distToRp < closestDis)
    //        {
    //            closestDis = distToRp;
    //            currentClosest = rp;
    //        }
    //    }
    //    return currentClosest;
    //}



    #region Targeting
    [Task]
    bool Targeting_Action_TryGetClosestTarget()
    {
        attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
        
        if (attackTarget == null)
            return false;
        else return true;
    }

    [Task]
    bool Targeting_Conditional_HasAttackTarget()
    {
        if (attackTarget != null)
            return true;
        else
            return false;
    }

    [Task]
    bool Targeting_Conditional_isTargetInLOS()
    {
        if (mechAIAiming.LineOfSight(attackTarget))
            return true;
        else
            return false;
    }
    #endregion

    #region Laser
    [Task]
    bool Laser_Conditional_InRange()
    {
        return Vector3.Distance(transform.position, attackTarget.transform.position) < _laser_ActivationDistance;
    }
    [Task]
    bool Laser_Conditional_InFireAngle()
    {
        return mechAIAiming.FireAngle(_laser_FireAngle);
    }
    [Task]
    bool Laser_Conditional_SufficientResources()
    {    
        return mechSystem.energy > 4; 
    }
    [Task]
    void Laser_Action_Fire()
    {
        mechAIWeapons.Lasers();
    }
    #endregion
    #region Cannons
    [Task]
    bool Cannons_Conditional_InRange()
    {
        return Vector3.Distance(transform.position, attackTarget.transform.position) < _cannon_ActivationDistance;
    }
    [Task]
    bool Cannons_Conditional_InFireAngle()
    {
        return mechAIAiming.FireAngle(_cannon_FireAngle);
    }
    [Task]
    bool Cannons_Conditional_SufficientResources() 
    {
        return mechSystem.shells > 2;
    }
    [Task]
    void Cannon_Action_Fire()
    {
        mechAIWeapons.Cannons();
    }
    #endregion
    #region Missiles
    [Task]
    bool Missile_Conditional_InRange()
    {
        return Vector3.Distance(transform.position, attackTarget.transform.position) < _missile_ActivationDistance;
    }
    [Task]
    bool Missile_Conditional_InFireAngle()
    {
        return mechAIAiming.FireAngle(_missile_FireAngle);
    }
    [Task]
    bool Missile_Conditional_SufficientResources()
    {
        return mechSystem.shells > 2;
    }
    [Task]
    void Missile_Action_Fire()
    {
        mechAIWeapons.MissileArray();
    }
    #endregion
    #region Beam
    [Task]
    bool Beam_Conditional_InRange()
    {
        if(Vector3.Distance(transform.position, attackTarget.transform.position) > _beam_ActivationDistanceMin &&
            Vector3.Distance(transform.position, attackTarget.transform.position) < _beam_ActivationDistanceMax)
            return true;
        else 
            return false;
    }
    [Task]
    bool Beam_Conditional_InFireAngle()
    {
        return mechAIAiming.FireAngle(_beam_FireAngle);
    }
    [Task]
    bool Beam_Conditional_SufficientResources()
    {
        return mechSystem.energy >= 300;
    }
    [Task]
    void Beam_Action_Activate()
    {
        mechAIWeapons.laserBeamAI = true;
    }

    [Task]
    bool Beam_DeactivateBeam()
    {
        mechAIWeapons.laserBeamAI = false;
        return false; // NOTE: Returns false to exit fallback the node
    }
    #endregion

           
    #region Primary Behavior Actions
    //FSM Behaviour: Roam - Roam between random patrol points
    [Task]
    private void Roam() {
        //Move towards random patrol point
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        } else {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
        //Look at random patrol points
        mechAIAiming.RandomAimTarget(patrolPoints);
    }

    //FSM Behaviour: Attack 
    [Task]
    private void Attack() {
         
        //If there is a target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {

            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;

            //Move Towards attack Target
            if (Vector3.Distance(transform.position, attackTarget.transform.position) >= 45.0f) {
                mechAIMovement.Movement(attackTarget.transform.position, 45);
            }
            //Otherwise "strafe" - move towards random patrol points at intervals
            else if (Vector3.Distance(transform.position, attackTarget.transform.position) < 45.0f && Time.time > attackTimer) {
                patrolIndex = Random.Range(0, patrolPoints.Length - 1);
                mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 2);
                attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);
            }

            //Track position of current target to pursue if lost
            pursuePoint = attackTarget.transform.position;
        }
    }

    //FSM Behaviour: Pursue
    [Task]
    void Pursue() {

        //Move towards last known position of attackTarget
        if (Vector3.Distance(transform.position, pursuePoint) > 3.0f) {
            mechAIMovement.Movement(pursuePoint, 1);
            mechAIAiming.RandomAimTarget(patrolPoints);
        }
        //Otherwise if reached and have not re-engaged, reset attackTarget and Roam
        else {
            attackTarget = null;
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
            mechState = MechStates.Roam;
        }
    }

    //FSM Behaviour: Flee
    [Task]
    void Flee() {

        //If there is an attack target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {
            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
        } else {
            //Look at random patrol points
            mechAIAiming.RandomAimTarget(patrolPoints);
        }

        //Move towards random patrol points <<< This could be drastically improved!
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        } else {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
    }
    #endregion

    //Method allowing AI Mech to acquire target after taking damage from enemy
    public override void TakingFire(int origin) {

        //If not own damage and no current attack target, find attack target
        if (origin != mechSystem.ID && !attackTarget) {
            foreach (GameObject target in mechAIAiming.targets) {
                if (target) {
                    if (origin == target.GetComponent<MechSystems>().ID) {
                        attackTarget = target;
                        mechAIAiming.aimTarget = target;
                    }
                }
            }
        }
    }

    //Method for checking heuristic status of Mech to determine if Fleeing is necessary
    [Task]
    private bool StatusCheck() {

        float status = mechSystem.health + mechSystem.energy + (mechSystem.shells * 7) + (mechSystem.missiles * 10);

        if (status > 1500)
            return false;
        else
            return true;
    }


    bool _isWalkingToResourcePoint;
    
    public GameObject _currentResorucePointTarget;


    [Task]
    private bool HasReachedDestinationResourcePoint() {
            return !_isWalkingToResourcePoint;    
    }
    [Task]
    private bool Temp() {
        return true;
    }

    /// Consideration: if next to a resrouce point should pick another 
    /// If best is less then 5f away and not present, chose the next to run to
    /// IDEA: use a physics check to check if theres any bots that are closer to the point than you
    [Task] 
    private void GoToNearestActiveResourcePoint() {

        // Generate New target point
        if(_currentResorucePointTarget == null) 
        {
            // Order Resorucde Points by distance to
            _ResourcePoints = _ResourcePoints.OrderBy(obj => Vector3.Distance(this.transform.position, obj.transform.position)).ToList();

            // DOES: If close to point, check if pickup is there,
            // TODO / NOTE: maybe a LOS check to make it realistic or atleast need to be a close distance near the thing to check if its present


            // Check Point for if there is a pickup at it
            Vector3 posToCheck = _ResourcePoints[0].transform.position;
            Collider[] hitColliders = Physics.OverlapSphere(posToCheck, 5f);
            bool isResorucePackPresent = false;
            foreach (var hit in hitColliders) {
                if (hit.TryGetComponent<Pickup>(out Pickup p))
                    isResorucePackPresent = true;
            }

            // iF the is a resource package at first point of there, else head to next closest
            if (isResorucePackPresent)
                _currentResorucePointTarget = patrolPoints[patrolIndex];
            else
                _currentResorucePointTarget = _ResourcePoints[1];
        }

        // Move to Resrouce Point
        if(_currentResorucePointTarget != null) {
            if(Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f) {

                //Move towards it not there yet
                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
            }
            else {
                // Clear target if there
                _currentResorucePointTarget = null;
            }          
        }

        //Look at random patrol points - Just look around randomly
        mechAIAiming.RandomAimTarget(patrolPoints);
    }

    #region OLD LOGIC - Pre Behavior Tree Logic
    private void OLD_WeaponsSystem()
    {
        if (!attackTarget)
        {
            attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
            mechAIWeapons.laserBeamAI = false;  //Hard disable on laserBeam
        }
        else
            FiringSystem();
    }
    
    //Method controlling logic of firing of weapons: Consider minimum ammunition, appropriate range, firing angle etc
    private void FiringSystem()
    {

        //Lasers - Enough energy and within a generous firing angle
        if (mechSystem.energy > 10 && mechAIAiming.FireAngle(20))
            mechAIWeapons.Lasers();

        //Cannons - Moderate distance, enough shells and tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 25
            && mechSystem.shells > 4 && mechAIAiming.FireAngle(15))
            mechAIWeapons.Cannons();

        //Missile Array - Long Range, enough ammo, very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 50
            && mechSystem.missiles >= 18 && mechAIAiming.FireAngle(5))
            mechAIWeapons.MissileArray();

        //Laser Beam - Strict range, plenty of energy and very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 20
            && Vector3.Distance(transform.position, attackTarget.transform.position) < 50
            && mechSystem.energy >= 300 && mechAIAiming.FireAngle(10))
            mechAIWeapons.laserBeamAI = true;
        else
            mechAIWeapons.laserBeamAI = false;
    }
    private void OLD_FSMStateSwitching()
    {
        //FSM - Behaviour Selection
        switch (mechState)
        {
            case (MechStates.Roam):
                Roam();
                break;
            case (MechStates.Attack):
                Attack();
                break;
            case (MechStates.Pursue):
                Pursue();
                break;
            case (MechStates.Flee):
                Flee();
                break;
        }

        //FSM Transition Logic - Replace this with Decision Tree implementation!
        if (attackTarget && !mechAIAiming.LineOfSight(attackTarget) && !StatusCheck())
            mechState = MechStates.Pursue;
        else if (attackTarget && mechAIAiming.LineOfSight(attackTarget) && !StatusCheck())
            mechState = MechStates.Attack;
        else if (StatusCheck())
            mechState = MechStates.Flee;
        else
            mechState = MechStates.Roam;
    }

    #endregion
}
