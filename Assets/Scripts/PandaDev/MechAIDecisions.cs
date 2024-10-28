using System.Collections.Generic;
using UnityEngine;
using Panda;
using System.Linq;
using System.Net.NetworkInformation;

public partial class MechAIDecisions : MechAI {

    public string botName = "Scotts MurderBot";

    // NOTE: Not sure if this is permissible for the tournament, it felt resonable to be able to shoot at already engaged enemies, 
    // but it can be disabled if needed. It only affects firing at known targets already assigned as the 'attackTarget'. 
    // TLDR: Allows the bot to shoot at fleeing enemies or enemies that have shot at it beyond 100f distance.
    bool setupCondition_LOSPlus100 = true;

    // Turn this off, when not needed
    bool isUpdatingDebugginFunction = true;


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

    

    private float _health_MaxValue = 2000;

    private float _laser_EnergyMaxValue = 750;
    private float _laser_HalfEnergyMaxValue = 375;
    private float _laser_BaseActivationDistance = 175f;     //NOTE: 'In Frustum' Check limited to 100f; if setupCondition_LOSPlus100 true, can hit fleeing enemies
    private float _laser_BaseFireAngle = 35f;
    private float _laser_ActivationDistance;
    private float _laser_FireAngle;

    private float _cannon_AmmoMaxValue = 30;
    private float _cannon_HalfAmmoMaxValue = 15;
    private float _cannon_BaseActivationDistance = 150;     //NOTE: 'In Frustum' Check limited to 100f, Cannon Ammo More Valuble then Energy, so reduced to 150
    private float _cannon_BaseFireAngle = 30f;
    private float _cannon_ActivationDistance;
    private float _cannon_FireAngle;

    private float _missiles_AmmoMaxValue = 54;
    private float _missiles_HalfAmmoMaxValue = 27;
    private float _missile_BaseActivationDistance = 50f;
    private float _missile_BaseFireAngle = 60f;
    private float _missile_ActivationDistance;
    private float _missile_FireAngle;
    private bool _HasMissileLock;

    private float _beam_HalfAmmoMaxValue = 375;
    private float _beam_BaseActivationDistanceMin = 10f;
    private float _beam_BaseActivationDistanceMax = 50f;
    private float _beam_BaseFireAngle = 15f;
    private float _beam_ActivationDistanceMin;
    private float _beam_ActivationDistanceMax;
    private float _beam_FireAngle;

    private float _weapons_ConservativeReductionCoefficent = 0.75f;



    private float shells_temp;


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


        Start_GetResourcePoints();      
        Start_CreateObservationPoints();
        Start_MyInitializeVelocityLogic();
        Start_SetAllBaseWeaponValues();
    }



    // Update is called once per frame
    void Update()
    {
        //Acquire valid attack target: perform frustum and LOS checks and determine closest target
        mechAIAiming.FrustumCheck();

        //OLD_WeaponsSystem();
        //OLD_FSMStateSwitching();


        if (!attackTarget)
        {
            GetClosestVisableKnownAttackTarget();
            //attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
        }

        if (attackTarget)
        {
            Update_MyVelocityLogic();
            Update_TargetVelocityLogic();
        }

        // Only For Debugging
        if(isUpdatingDebugginFunction)
            Update_DebuggingValues();

    }

    // Visual Debugging
    private void OnDrawGizmosSelected()
    {
        // Draw Resource Points
        foreach (var point in _RankedResourcePointsList)
        {
            if (point.Item1 != null)
            {
                Gizmos.color = GetColorByTier(point.Item2);
                Gizmos.DrawWireSphere(point.Item1.transform.position, 5.0f); 
            }
        }

        Color GetColorByTier(ResourceRiskTeir tier)
        {
            switch (tier)
            {
                case ResourceRiskTeir.low: return Color.green;
                case ResourceRiskTeir.med: return Color.yellow;
                case ResourceRiskTeir.high: return Color.red;
                default: return Color.white;
            }
        }
    }


    //Method allowing AI Mech to acquire target after taking damage from enemy
    public override void TakingFire(int origin)
    {

        //If not own damage and no current attack target, find attack target
        if (origin != mechSystem.ID && !attackTarget)
        {
            foreach (GameObject target in mechAIAiming.targets)
            {
                if (target)
                {
                    if (origin == target.GetComponent<MechSystems>().ID)
                    {
                        attackTarget = target;
                        mechAIAiming.aimTarget = target;
                    }
                }
            }
        }
    }




    #region Debugging Values
    [Header("Debugging Values")]
    public float distanceToAtkTarget;
    public bool isOnResourcePoint;
    public float targetRelativeVelocity;

    private void Update_DebuggingValues()
    {
        distanceToAtkTarget = Utility_DistanceToAtackTarget();
        isOnResourcePoint = Utility_IsCurrentlyOnAResorucePoint();
        targetRelativeVelocity = _currentVelocityOfTargetTowardsMe;

    }

    #endregion // END Debugging Values

    #region Utility Functions

    private float Utility_DistanceToAtackTarget()
    {
        if (!attackTarget)
            return float.MaxValue;
        else
            return Vector3.Distance(this.transform.position, attackTarget.transform.position);
    }
    /// <summary>
    /// Checks if an object(GameObject) is in LOS of this bot
    /// </summary>
    private bool Utility_IsGameObjectInLOS(GameObject Go)
    {
        if (setupCondition_LOSPlus100) 
        {
            return LineOfSight(Go);
        }
        else
        {
            return mechAIAiming.LineOfSight(Go);
        }
            
        bool LineOfSight(GameObject thisTarget)
        {

            //Need to correct for wonky pivot point - Mech model pivot at base instead of centre
            Vector3 correction = thisTarget.transform.GetChild(0).gameObject.transform.position;
            Vector3 pointAboveMech = this.transform.position + Vector3.up * 5f + Vector3.forward * 5f;
            RaycastHit hit;
            if (Physics.Raycast(pointAboveMech, -(pointAboveMech - correction).normalized, out hit, 200f))
            {

                Debug.DrawLine(transform.position, hit.point, Color.magenta);

                if (hit.transform.gameObject.tag == "Player" && hit.transform.gameObject != this.gameObject)
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
    }

    
    private bool Utility_IsCurrentlyOnAResorucePoint()
    {
        bool isOnAResorucePoint = false;
        foreach (var rp in _ResourcePoints)
        {
            if(Vector3.Distance(rp.transform.position, this.transform.position) < 1)
                isOnAResorucePoint = true;
        }
        return isOnAResorucePoint;
    }


    
    #endregion //END Utility Functions

    // Preditive Firing

    /// <summary>
    ///  Get the best attackTarget In LOS, from known current targets
    /// </summary>
    void GetClosestVisableKnownAttackTarget()
    {
        List<GameObject> inLOS = new List<GameObject>();
        for (int i = 0; i < mechAIAiming.currentTargets.Count; i++)
        {
            // get
            GameObject tempAttackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
            if(tempAttackTarget)
                if (mechAIAiming.LineOfSight(tempAttackTarget))
                    inLOS.Add(tempAttackTarget);
        }
        attackTarget = mechAIAiming.ClosestTarget(inLOS);
    }



    #region Targeting & Weapons

    private void Start_SetAllBaseWeaponValues()
    {
        _laser_ActivationDistance = _laser_BaseActivationDistance;
        _laser_FireAngle = _laser_BaseFireAngle;
        _cannon_ActivationDistance = _cannon_BaseActivationDistance;
        _cannon_FireAngle = _cannon_BaseFireAngle;
        _missile_ActivationDistance = _missile_BaseActivationDistance;
        _missile_FireAngle = _missile_BaseFireAngle;
        _beam_ActivationDistanceMin = _beam_BaseActivationDistanceMin;
        _beam_ActivationDistanceMax = _beam_BaseActivationDistanceMax;
        _beam_FireAngle = _beam_BaseFireAngle;
    }

    /// <summary>
    /// Sets All Weapon Firing Values to Base Values
    /// </summary>
    [Task]
    void Weapons_SetAllBaseWeaponValues() {
        Laser_Action_SetBaseWeaponValues();
        Cannon_Action_SetBaseWeaponValues();
        Missile_Action_SetBaseWeaponValues();
        Beam_Action_SetBaseWeaponValues();
    }

    /// <summary>
    /// Sets all weapons to conservative firing values
    /// </summary>
    [Task]
    void Weapons_SetAllToConservativeFiringValues() {
        Laser_Action_SetConservativeFiringValues();
        Cannon_Action_SetConservativeFiringValues();
        Missile_Action_SetConservativeFiringValues();
        Beam_Action_SetConservativeFiringValues();
    }


    #region Targeting
    [Task]
    bool Targeting_Conditional_AttackTargetOver100FDistance()
    {
        if(Utility_DistanceToAtackTarget() > 99f)
            return true;
        return false;
    }

    [Task]
    bool Targeting_Conditional_AttackTargetInLOS()
    {
        if(!attackTarget) 
            return false;
        return Utility_IsGameObjectInLOS(attackTarget);
    }

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
        {
            //Debug.Log("Targeting_Conditional_HasAttackTarget: TRUE");
            return true;
        }
        else
        {
            //Debug.Log("Targeting_Conditional_HasAttackTarget: FALLSE");
            return false;
        }
            
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
        float dis = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (dis < _laser_ActivationDistance)
        {
            //Debug.Log("Laser_Conditional_InRange: TRUE, Dis = " + dis);
            return true;
        }
        else
        {
            //Debug.Log("Laser_Conditional_InRange: FALSE, Dis = " + dis);
            return false;
        }

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
        Task.current.Succeed();
    }

    [Task]
    bool Laser_Conditional_OverHalfAmmoRemaining() {
        
        if (mechSystem.energy > _laser_HalfEnergyMaxValue)
        {
            //Debug.Log("Laser_Conditional_OverHalfAmmoRemaining: TRUE");
            return true;
        }         
        else
        {
            //Debug.Log("Laser_Conditional_OverHalfAmmoRemaining: FALSE");
            return false;
        }
            
    }

    [Task]
    void Laser_Action_SetBaseWeaponValues() {
        _laser_ActivationDistance = _laser_BaseActivationDistance;
        _laser_FireAngle = _laser_BaseFireAngle;
        Task.current.Succeed();
    }

    [Task]
    void Laser_Action_SetConservativeFiringValues() {
        _laser_ActivationDistance = _laser_BaseActivationDistance * _weapons_ConservativeReductionCoefficent;
        _laser_FireAngle = _laser_BaseFireAngle * _weapons_ConservativeReductionCoefficent;
        Task.current.Succeed();
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
        Task.current.Succeed();
    }
    [Task]
    bool Cannon_Conditional_OverHalfAmmoRemaining() {

        
        if (mechSystem.shells > _cannon_HalfAmmoMaxValue)
        {
            //Debug.Log("Cannon_Conditional_OverHalfAmmoRemaining - mechSystem.shells = " + mechSystem.shells + " Return TRUE");
            return true;
        }
        else
        {
            //Debug.Log("Cannon_Conditional_OverHalfAmmoRemaining - mechSystem.shells = " + mechSystem.shells + " Return FALSE");
            return false;
        }
            
    }
    [Task]
    void Cannon_Action_SetBaseWeaponValues() {
        //Debug.Log("Cannon_Action_SetBaseWeaponValues");
        _cannon_ActivationDistance = _cannon_BaseActivationDistance;
        _cannon_FireAngle = _cannon_BaseFireAngle;
        Task.current.Succeed();
    }

    [Task]
    void Cannon_Action_SetConservativeFiringValues() {
        //Debug.Log("Cannon_Action_SetBaseWeaponValues");
        _cannon_ActivationDistance = _cannon_BaseActivationDistance * _weapons_ConservativeReductionCoefficent;
        _cannon_FireAngle = _cannon_BaseFireAngle * _weapons_ConservativeReductionCoefficent;
        Task.current.Succeed();
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
        return mechSystem.missiles >= 18;
    }
    [Task]
    void Missile_Action_Fire()
    {
        mechAIWeapons.MissileArray();
        Task.current.Succeed();
    }
    [Task]
    bool Missile_Conditional_OverHalfAmmoRemaining() {
        if (mechSystem.missiles > _missiles_HalfAmmoMaxValue)
            return true;
        else
            return false;
    }
    [Task]
    bool Missile_Action_SetBaseWeaponValues() {
        _missile_ActivationDistance = _missile_BaseActivationDistance;
        _missile_FireAngle = _missile_BaseFireAngle;
        return true; 
    }

    [Task]
    bool Missile_Action_SetConservativeFiringValues() {
        _missile_ActivationDistance = _missile_BaseActivationDistance * _weapons_ConservativeReductionCoefficent;
        _missile_FireAngle = _missile_BaseFireAngle * _weapons_ConservativeReductionCoefficent;
        return true; 
    }

    #endregion // Missile End
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
    bool Beam_Conditional_OverHalfAmmoRemaining() {
        if (mechSystem.energy > _beam_HalfAmmoMaxValue)
            return true;
        else
            return false;
    }
    [Task]
    void Beam_Action_Activate()
    {
        mechAIWeapons.laserBeamAI = true;
        Task.current.Succeed();
    }
    [Task]
    void Beam_Action_DeactivateBeam()
    {
        // only deactivate it if its already On
        if(mechAIWeapons.laserBeamAI == true)
            mechAIWeapons.laserBeamAI = false;
        
        Task.current.Succeed();
    }
    [Task]
    void Beam_Action_SetBaseWeaponValues() {
        _beam_ActivationDistanceMin = _beam_BaseActivationDistanceMin;
        _beam_ActivationDistanceMax = _beam_BaseActivationDistanceMax;
        _beam_FireAngle = _beam_BaseFireAngle;
        Task.current.Succeed();
    }

    [Task]
    void Beam_Action_SetConservativeFiringValues() {
        _beam_ActivationDistanceMin = _beam_BaseActivationDistanceMin * _weapons_ConservativeReductionCoefficent;
        _beam_ActivationDistanceMax = _beam_BaseActivationDistanceMax * _weapons_ConservativeReductionCoefficent;
        _beam_FireAngle = _beam_BaseFireAngle * _weapons_ConservativeReductionCoefficent;
        Task.current.Succeed();
    }
    #endregion // Beam End
    #endregion // Targeting & Weapons End









    #region OLD LOGIC - Primary Behavior Actions
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
    GameObject _currentResorucePointTarget;


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

        View_Spin();
        //View_CuriouseLookAround();

        // Generate New target point
        if (_currentResorucePointTarget == null) 
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

    #endregion //OLD LOGIC - Early Behaviro Tree

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
