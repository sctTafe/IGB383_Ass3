using System.Collections.Generic;
using UnityEngine;
using Panda;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using UnityEditor;

public partial class MechAIDecision_ScottBarley : MechAI {

    public string botName = "ScottsBot";

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

    //Attack Variables
    public GameObject[] aimTargets;
    private GameObject attackTarget;

    //Pursue Variables
    public GameObject pursueTarget;
    private Vector3 pursuePoint;
    Vector3 _strafePostion;

    //Flee Variables
    public GameObject fleeTarget;

    //Known Values
    private float _health_MaxValue = 2000;

    #region Unity Native Function
    void Start () {
        //Collect Mech and AI Systems
        mechSystem = GetComponent<MechSystems>();
        mechAIMovement = GetComponent<MechAIMovement>();
        mechAIAiming = GetComponent<MechAIAiming>();
        mechAIWeapons = GetComponent<MechAIWeapons>();

        Start_GetResourcePoints();      
        Start_CreateObservationPoints();
        Start_MyInitializeVelocityLogic();
        Start_SetAllBaseWeaponValues();
    }

    void Update()
    {
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
    #endregion // END Unity Native Function

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
    public float dpsOnMe;
    public GameObject atkTarget;

    private void Update_DebuggingValues()
    {
        distanceToAtkTarget = Utility_DistanceToAtackTarget();
        isOnResourcePoint = Utility_IsCurrentlyOnAResorucePoint();
        targetRelativeVelocity = _currentVelocityOfTargetTowardsMe;
        dpsOnMe = _currenthpLossRate;
        atkTarget = attackTarget;
    }

    #endregion // END Debugging Values

    #region Utility Functions
    /// <summary>
    ///  Get the best attackTarget In LOS, from known current targets
    /// </summary>
    void Utility_GetClosestVisableKnownAttackTarget()
    {
        List<GameObject> inLOS = new List<GameObject>();
        for (int i = 0; i < mechAIAiming.currentTargets.Count; i++)
        {
            // get
            GameObject tempAttackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
            if (tempAttackTarget)
                if (mechAIAiming.LineOfSight(tempAttackTarget))
                    inLOS.Add(tempAttackTarget);
        }
        attackTarget = mechAIAiming.ClosestTarget(inLOS);
    }

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
            {
                isOnAResorucePoint = true;
                _riskTierOfRPAt = _RankedResourcePointsList.FirstOrDefault(tuple => tuple.Item1 == rp).Item2;
            }
                
        }
        return isOnAResorucePoint;
    }
  
    private bool Utiltiy_IsNearAResroucePoint_Within10f()
    {
        bool isnear = false;
        foreach (var rp in _ResourcePoints)
        {
            if (Vector3.Distance(rp.transform.position, this.transform.position) < 10.5f)
            {
                isnear = true;
            }
        }
        return isnear;
    }


    #endregion //END Utility Functions

    #region Velocity Logic
    [Header("Vectore Stuff")]
    private Vector3[] _myPreviousPositions = new Vector3[10];
    private int _myVelocityCurrentIndex = 0;
    [SerializeField] private Vector3 _myCurrentAvgVelocity;

    private Vector3[] _targetPreviousPositions = new Vector3[10];
    private int _targetVelocityCurrentIndex = 0;
    [SerializeField] private GameObject _targetVelocityGO;                   // the game object of the current velocity tracking object 
    [SerializeField] private Vector3 _targetCurrentAvgVelocity;

    float _currentVelocityOfTargetTowardsMe;

    private void Start_MyInitializeVelocityLogic()
    {
        for (int i = 0; i < _myPreviousPositions.Length; i++)
            _myPreviousPositions[i] = transform.position;
    }

    private void InitalizeTargetVelocity()
    {
        if (_targetVelocityGO)
        {
            _targetVelocityCurrentIndex = 0;
            for (int i = 0; i < _targetPreviousPositions.Length; i++)
                _targetPreviousPositions[i] = _targetVelocityGO.transform.position;
        }
    }
    private void Update_TargetVelocityLogic()
    {
        // Guards
        if (_targetVelocityGO == null)
        {
            _targetVelocityGO = attackTarget;
            InitalizeTargetVelocity();
            return;
        }
        if (attackTarget != _targetVelocityGO)
        {
            _targetVelocityGO = attackTarget;
            InitalizeTargetVelocity();
            return;
        }

        // Shift the positions: oldest (index 0) to newest (index 9)
        for (int i = 0; i < _targetPreviousPositions.Length - 1; i++)
        {
            _targetPreviousPositions[i] = _targetPreviousPositions[i + 1];
        }
        // Store the current position as the newest point
        _targetPreviousPositions[_targetPreviousPositions.Length - 1] = _targetVelocityGO.transform.position;

        // Calculate velocity between consecutive points
        Vector3 totalVelocity = Vector3.zero;
        for (int i = 0; i < _targetPreviousPositions.Length - 1; i++)
        {
            totalVelocity += (_targetPreviousPositions[i + 1] - _targetPreviousPositions[i]) / Time.deltaTime;
        }
        // Average the total velocity over the last 9 intervals
        _targetCurrentAvgVelocity = totalVelocity / (_targetPreviousPositions.Length - 1);
    }
    private void Update_MyVelocityLogic()
    {

        // Shift the positions: oldest (index 0) to newest (index 9)
        for (int i = 0; i < _myPreviousPositions.Length - 1; i++)
        {
            _myPreviousPositions[i] = _myPreviousPositions[i + 1];
        }
        // Store the current position as the newest point
        _myPreviousPositions[_myPreviousPositions.Length - 1] = transform.position;

        // Calculate velocity between consecutive points
        Vector3 totalVelocity = Vector3.zero;
        for (int i = 0; i < _myPreviousPositions.Length - 1; i++)
        {
            totalVelocity += (_myPreviousPositions[i + 1] - _myPreviousPositions[i]) / Time.deltaTime;
        }
        // Average the total velocity over the last 9 intervals
        _myCurrentAvgVelocity = totalVelocity / (_myPreviousPositions.Length - 1);

    }

    private bool Velocity_IsAttackTargetMovingTowardsMe()
    {
        Vector3 directionFromTargetToMe = this.transform.position - attackTarget.transform.position;
        _currentVelocityOfTargetTowardsMe = Vector3.Dot(_targetCurrentAvgVelocity, directionFromTargetToMe.normalized);
        return _currentVelocityOfTargetTowardsMe > 0;
    }


    #endregion

    #region ResourcePoints

    // Assumed Knowledge
    private PickupSpawner[] _allResourcePickupPoints;
    [SerializeField] private List<GameObject> _ResourcePoints = new List<GameObject>();
    [SerializeField] private List<Tuple<GameObject, ResourceRiskTeir>> _RankedResourcePointsList = new List<Tuple<GameObject, ResourceRiskTeir>>();
    GameObject _currentResorucePointTarget;
    ResourceRiskTeir _currentRP_RiskTeir;
    ResourceRiskTeir _riskTierOfRPAt;
    enum ResourceRiskTeir
    {
        low,
        med,
        high,
    }

    private void Start_GetResourcePoints()
    {
        // Get Assumed Knowledge
        _allResourcePickupPoints = UnityEngine.Object.FindObjectsOfType<PickupSpawner>();
        foreach (var rp in _allResourcePickupPoints)
        {
            if (rp.enabled == true)
                _ResourcePoints.Add(rp.gameObject);
        }
        RankResourcePoints();

        void RankResourcePoints()
        {
            Vector3 center = Vector3.zero;
            foreach (var rp in _ResourcePoints)
            {
                center += rp.transform.position;
            }
            center = center / _ResourcePoints.Count;

            // Sort by distance to center
            _ResourcePoints.Sort((a, b) =>
                Vector3.Distance(a.transform.position, center)
                    .CompareTo(Vector3.Distance(b.transform.position, center)));

            // Assign a risk tier based on distance, with high risk nodes at the center
            int pointCount = _ResourcePoints.Count;
            for (int i = 0; i < pointCount; i++)
            {
                GameObject resourcePoint = _ResourcePoints[i];
                float distance = Vector3.Distance(resourcePoint.transform.position, center);

                ResourceRiskTeir riskTier;
                if (i < pointCount * 0.18f)
                    riskTier = ResourceRiskTeir.high;
                else if (i < pointCount * 0.55f)
                    riskTier = ResourceRiskTeir.med;
                else
                    riskTier = ResourceRiskTeir.low;

                // Add to the ranked list
                _RankedResourcePointsList.Add(new Tuple<GameObject, ResourceRiskTeir>(resourcePoint, riskTier));
            }
        }
    }


    private GameObject GetNextClosestTeiredResourcePoint(ResourceRiskTeir teir)
    {
        float currentDis = float.MaxValue;
        float toDis;
        GameObject targetResourcePoint = null;
        foreach (var rp in _RankedResourcePointsList)
        {
            if (rp.Item2 == teir)
            {
                toDis = Vector3.Distance(rp.Item1.transform.position, this.transform.position);
                if (toDis < currentDis && toDis > 1f) // NOTE: Ignore if next to it
                {
                    targetResourcePoint = rp.Item1;
                    currentDis = toDis;
                }
            }
        }

        return targetResourcePoint;
    }

    public GameObject GetClosestResourcePoint()
    {
        GameObject closestRp = null;
        float dis = Mathf.Infinity;

        foreach (GameObject rp in _ResourcePoints)
        {   
            float distance = Vector3.Distance(this.transform.position, rp.transform.position);

            if (distance < dis)
            {
                dis = distance;
                closestRp = rp;
            }           
        }

        return closestRp;
    }


    #endregion

    #region ObservationPoints
    // Artificial Observation Points
    public GameObject[] _observationPoints;
    public int _currentObservationPointIndex;
    public float _nextObservationPointTimer;
    public bool _isLookingLeft;

    void Start_CreateObservationPoints()
    {
        _observationPoints = new GameObject[6];
        _observationPoints[0] = CreateEmptyChild("FrontObservationPoint", Vector3.forward * 5f);
        _observationPoints[1] = CreateEmptyChild("LeftObservationPoint", Vector3.left * 5f);
        _observationPoints[2] = CreateEmptyChild("BackObservationPoint", Vector3.back * 5f);
        _observationPoints[3] = CreateEmptyChild("RightObservationPoint", Vector3.right * 5f);
        _observationPoints[4] = CreateEmptyChild("FLObservationPoint", new Vector3(-3, 0, 3));
        _observationPoints[5] = CreateEmptyChild("FRbservationPoint", new Vector3(3, 0, 3));
    }
    GameObject CreateEmptyChild(string name, Vector3 localPosition)
    {
        Vector3 verticalOffset = new Vector3(0f, 3.35f, 0f);
        localPosition = localPosition + verticalOffset;
        GameObject newObject = new GameObject(name);
        newObject.transform.parent = this.transform;
        newObject.transform.localPosition = localPosition;
        return newObject;
    }
    #endregion

    #region CurrentCombatStats
    private void Update_CheckForResorucePointTrigger()
    {

    }

    private Queue<float> _healthSamples = new Queue<float>();
    private const int _hpSampleFrameCount = 20;
    private float _currenthpLossRate = 0f;

    private void Update_HPInfo()
    {
        _healthSamples.Enqueue(mechSystem.health);
        if (_healthSamples.Count > _hpSampleFrameCount)
        {
            _healthSamples.Dequeue();
        }
        _currenthpLossRate = GetAverageDamagePerSecond();
    }

    float GetAverageDamagePerSecond()
    {
        if (_healthSamples.Count < 10)
            return 0f;
        
        // Calculate the total damage taken over the sample period
        float oldestHealth = _healthSamples.Peek();
        float newestHealth = mechSystem.health;
        float totalDamage = oldestHealth - newestHealth;

        // Convert frame count to time in seconds
        float timeSpan = _hpSampleFrameCount * Time.deltaTime;

        // Avoid dividing by zero in case timeSpan is very small
        return timeSpan > 0 ? totalDamage / timeSpan : 0f;
    }

    [Task]
    bool Stats_HPLossRate_High()
    {
        if(_currenthpLossRate > 50)
           return true;
        return false;
    }
    [Task]
    bool Stats_HPLossRate_VeryHigh()
    {
        if (_currenthpLossRate > 200)
            return true;
        return false;
    }

    #endregion

    #region View
    [Task]
    void View_CuriouseLookAround()
    {
        if (Time.time > _nextObservationPointTimer)
            NextViewPoint();
        else
            mechAIAiming.aimTarget = _observationPoints[_currentObservationPointIndex];

        void NextViewPoint()
        {
            _nextObservationPointTimer = Time.time + 0.5f;

            if (_isLookingLeft)
            {
                // Try Look Right
                if (_currentObservationPointIndex == 0)
                    _currentObservationPointIndex = 5; //Set look right
                else
                {
                    _isLookingLeft = false;
                    _currentObservationPointIndex = 0; // Set look Foward
                }
            }
            else
            {
                // Try Look Left
                if (_currentObservationPointIndex == 0)
                    _currentObservationPointIndex = 4; //Set look right
                else
                {
                    _isLookingLeft = true;
                    _currentObservationPointIndex = 0;
                }
            }
        }
        Task.current.Succeed();
    }

    /// <summary>
    /// Rapidly Spins the View in a circle
    /// </summary>
    [Task]
    void View_Spin()
    {
        if (Time.time > _nextObservationPointTimer)
            NextViewPoint();
        else
            mechAIAiming.aimTarget = _observationPoints[_currentObservationPointIndex];

        void NextViewPoint()
        {
            _nextObservationPointTimer = Time.time + 0.2f;
            _currentObservationPointIndex = (_currentObservationPointIndex + 1) % 4;
        }
        Task.current.Succeed();
    }
    [Task]
    void View_LookAtAtackTarget()
    {
        if (mechAIAiming.aimTarget)
        {
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
        Task.current.Succeed();
    }
    // mechAIAiming.aimTarget = _observationPoints[0]; // Look Fowards
    #endregion // END View

    #region Movement


    /// <summary>
    /// Check if the nav agent for this bot is stopped
    /// </summary>
    [Task]
    private bool Movement_Conditional_IsAgentMovementStopped()
    {
        return mechAIMovement.agent.isStopped;
    }

    [Task]
    private void Movement_Action_MoveToClosestTeir2ResourcePoint()
    {
        if (_currentResorucePointTarget == null 
            || Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) < 2f
            || _currentRP_RiskTeir != ResourceRiskTeir.med
            )
        {
            _currentResorucePointTarget = GetNextClosestTeiredResourcePoint(ResourceRiskTeir.med);
        }

        // Move to Resoruce Point
        if (_currentResorucePointTarget != null)
        {
            if (Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f)
            {
                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
                mechAIMovement.agent.isStopped = false;
            }
            else
                _currentResorucePointTarget = null;
        }
        _currentRP_RiskTeir = ResourceRiskTeir.med;
        Task.current.Succeed();
    }

    [Task]
    private void Movement_Action_MoveToClosestTeir3ResourcePoint()
    {
        if (_currentResorucePointTarget == null 
            || Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) < 2f
            || _currentRP_RiskTeir != ResourceRiskTeir.low
            )
        {
            _currentResorucePointTarget = GetNextClosestTeiredResourcePoint(ResourceRiskTeir.low);
        }

        // Move to Resoruce Point
        if (_currentResorucePointTarget != null)
        {
            if (Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f)
            {
                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
                mechAIMovement.agent.isStopped = false;
            }
            else
                _currentResorucePointTarget = null;
        }

        _currentRP_RiskTeir = ResourceRiskTeir.low;
        Task.current.Succeed();
    }



    [Task]
    private void Movement_Action_StandStill()
    {
        //Debug.Log(this.gameObject.name + "Movement_Action_StandStill - Stop Moving");       
        mechAIMovement.Movement(this.transform.position, 0); //Set to current postion      
        mechAIMovement.agent.isStopped = true;

        Task.current.Succeed();
    }

    [Task]
    private void Movement_Action_MoveTowardsAttackTarget_Close()
    {
        //Debug.Log(this.gameObject.name + ": Movement_Action_MoveTowardsAttackTarget");
        pursuePoint = attackTarget.transform.position;
        if (Vector3.Distance(transform.position, pursuePoint) > 4.5f)
        {
            mechAIMovement.Movement(pursuePoint, 4); // Set to postion of attackTarget + 5f (stop close, but not to close)
            mechAIMovement.agent.isStopped = false;
        }
        else
        {
            mechAIMovement.agent.isStopped = true;
        }
        Task.current.Succeed();
    }

    [Task]
    private void Movement_Action_MoveTowardsAttachTarget_ToOptimalAttackRange()
    {
        //Debug.Log(this.gameObject.name + ": Movement_Action_MoveTowardsAttackTarget");
        pursuePoint = attackTarget.transform.position;

        //NOTE: <50 is optimal, but the is momentem / delay, that will carry the bot over the line, so the
        //optimal strategy it to stop befor any other bot
        if (Vector3.Distance(transform.position, pursuePoint) > 51f)
        {
            mechAIMovement.Movement(pursuePoint, 0);
            mechAIMovement.agent.isStopped = false;
        }
        else
        {
            mechAIMovement.agent.isStopped = true;
        }
        Task.current.Succeed();
    }

    [Task]
    private void Movement_StrafeAroundClosestResourcePoint()
    {

        if (_strafePostion == null || Vector3.Distance(_strafePostion,this.transform.position) > 50f)
        {
            GenerateNewStrafePoint();
        }

        if (Vector3.Distance(transform.position, _strafePostion) > 1f)
        {
            mechAIMovement.Movement(_strafePostion, 0);
            mechAIMovement.agent.isStopped = false;
        }
        else
        {
            GenerateNewStrafePoint();
            mechAIMovement.agent.isStopped = true;
        }
        Task.current.Succeed();

        void GenerateNewStrafePoint()
        {
            Vector3 closestRP = GetClosestResourcePoint().transform.position;
            closestRP.y = this.transform.position.y;
            _strafePostion = GetRandomPositionInRadius(closestRP, 8);
        }
    }

    Vector3 GetRandomPositionInRadius(Vector3 center, float radius)
    {
        Vector3 randPos = Random.insideUnitSphere * radius;
        randPos.y = this.transform.position.y;
        return center + randPos;
    }


    [Task]
    private void Movement_Flee()
    {
        //// Chose a position that is away from enemies
        //if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        //    patrolIndex = UnityEngine.Random.Range(0, patrolPoints.Length - 1);
        //else
        //    mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
    }
    #endregion // END MOVEMENT

    #region Engagement Heuristics

    public CombatFitnessTier combatFitness;
    float sitRepTimer;
    GameObject currentCFGO;
    public enum CombatFitnessTier
    {
        max,    // Max - Recharge Till This FLag
        good,   // Good - Fully Combat Ready - Can be Aggressive 
        ok,     // OK - Withdrawing Combat - Defensive Stance
        bad     // Bad - Prioritise Survival at this flag     
    }

    /// <summary>
    /// DOES: Carries out The targeting logic, finds targets and gets closest
    /// Task - Always Succeed
    /// </summary>
    [Task]
    private void Engagement_Update_TargetingLogic()
    {
        //Acquire valid attack target: perform frustum and LOS checks and determine closest target
        mechAIAiming.FrustumCheck();
        
        if(attackTarget != null)
            if(Vector3.Distance(attackTarget.transform.position,this.transform.position) > 120f)
                 attackTarget = null;

        if (!attackTarget || attackTarget == null)
        {
            Utility_GetClosestVisableKnownAttackTarget();
            //attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
        }
        if (attackTarget)
        {
            Update_MyVelocityLogic();
            Update_TargetVelocityLogic();
        }
        Update_HPInfo();
        Task.current.Succeed();
    }


    /// <summary>
    /// Ensures enough time has passed for the Velocity to be calculated correctly
    /// </summary>
    [Task]
    private void Engagement_Update_HoldAndFireValue()
    {
        if (currentCFGO == null || currentCFGO != attackTarget)
        {
            currentCFGO = attackTarget;
            sitRepTimer = 0;
        }

        sitRepTimer += 1f * Time.deltaTime;
        //Debug.Log("Engagement_Update_HoldAndFireValue: Value = " + sitRepTimer);
        if (sitRepTimer > 1f)
        {
            Task.current.Fail();
        }
        else
        {
            Task.current.Succeed();
        }
    }

    [Task]
    private bool Enguagement_Conditional_IsTargetMovingTowardsMe()
    {
        return Velocity_IsAttackTargetMovingTowardsMe();
    }


    [Task]
    private void Engagement_Update_CombatFitnessHeuristics()
    {
        float ammoRemianing_pct =
            (mechSystem.energy / _laser_EnergyMaxValue) * 0.33f +
            (mechSystem.shells / _cannon_AmmoMaxValue) * 0.33f +
            (mechSystem.missiles / _missiles_AmmoMaxValue) * 0.33f;

        float healthRemaining_pct =
            mechSystem.health / _health_MaxValue;

        // Max - Recharge Till This FLag
        if (healthRemaining_pct > 0.90f && ammoRemianing_pct > 0.80f)
            combatFitness = CombatFitnessTier.max;

        // Good - Fully Combat Ready - Can be Aggressive 
        else if (healthRemaining_pct > 0.90f && ammoRemianing_pct > 0.60f
            && Cannon_Conditional_OverHalfAmmoRemaining()
            && Missile_Conditional_OverHalfAmmoRemaining()
            && Beam_Conditional_OverHalfAmmoRemaining())
            combatFitness = CombatFitnessTier.good;


        // OK - Withdrawing Combat - Defensive Stance
        // Note: All Weapons Still Firable
        else if (healthRemaining_pct > 0.70f && ammoRemianing_pct > 0.30f
            && Cannons_Conditional_SufficientResources()
            && Missile_Conditional_SufficientResources()
            && Beam_Conditional_SufficientResources())
            combatFitness = CombatFitnessTier.ok;

        // Bad - Prioritise Survival at this flag
        else
            combatFitness = CombatFitnessTier.bad;

        Task.current.Succeed();
    }
    [Task]
    private bool Enguagement_Conditional_IsCombatFitness_Max() => combatFitness == CombatFitnessTier.max;
    [Task]
    private bool Enguagement_Conditional_IsCombatFitness_Good() => combatFitness == CombatFitnessTier.good;
    [Task]
    private bool Enguagement_Conditional_IsCombatFitness_OK() => combatFitness == CombatFitnessTier.ok;
    [Task]
    private bool Enguagement_Conditional_IsCombatFitness_Bad() => combatFitness == CombatFitnessTier.bad;



    [Task]
    private bool Enguagement_Conditional_IsOnResourcePoint()
    {
        return Utility_IsCurrentlyOnAResorucePoint();
    }

    [Task]
    private bool Enguagement_Conditional_IsNearResourcePoint()
    {
        return Utiltiy_IsNearAResroucePoint_Within10f();
    }

    [Task]
    private bool Enguagement_Conditional_IsOnLowRiskResourcePoint()
    {
        return _riskTierOfRPAt == ResourceRiskTeir.low;
    }

    #endregion // Engagement END

    #region Required 'leaf nodes' (Version 5.1)

    [Task]
    private void EndLeafV5_CombatPlanA_HoldPositon()
    {
        Movement_Action_StandStill();
        View_LookAtAtackTarget();

        Task.current.Succeed();
    }

    [Task]
    private void EndLeafV5_CombatPlanA_AdvanceOnTarget()
    {
        Movement_Action_MoveTowardsAttachTarget_ToOptimalAttackRange();
        View_LookAtAtackTarget();

        Task.current.Succeed();
    }

    [Task]
    private void EndLeafV5_HuntForTarget()
    {
        Movement_Action_MoveToClosestTeir2ResourcePoint();
        View_CuriouseLookAround();

        Task.current.Succeed();
    }

    [Task]
    private void EndLeafV5_FightingWithdrawal()
    {
        Movement_Action_MoveToClosestTeir2ResourcePoint();
        View_LookAtAtackTarget();

        Task.current.Succeed();
    }

    [Task]
    private void EndLeafV5_VigilantlyHoldPosition()
    {
        Movement_Action_MoveToClosestTeir2ResourcePoint();
        View_Spin();

        Task.current.Succeed();
    }

    #endregion

    #region Targeting & Weapons
    private float _laser_EnergyMaxValue = 750;
    private float _laser_HalfEnergyMaxValue = 375;
    private float _laser_BaseActivationDistance = 120f;     //NOTE: 'In Frustum' Check limited to 100f; if setupCondition_LOSPlus100 true, can hit fleeing enemies
    private float _laser_BaseFireAngle = 20f;
    private float _laser_ActivationDistance;
    private float _laser_FireAngle;

    private float _cannon_AmmoMaxValue = 25;
    private float _cannon_HalfAmmoMaxValue = 15;
    private float _cannon_BaseActivationDistance = 100;     //NOTE: 'In Frustum' Check limited to 100f, Cannon Ammo More Valuble then Energy, so reduced to 150
    private float _cannon_BaseFireAngle = 20f;
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
    private float _beam_BaseFireAngle = 20f;
    private float _beam_ActivationDistanceMin;
    private float _beam_ActivationDistanceMax;
    private float _beam_FireAngle;

    private float _weapons_ConservativeReductionCoefficent = 0.75f;

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
        ////Move towards random patrol point
        //if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
        //    patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        //} else {
        //    mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        //}
        ////Look at random patrol points
        //mechAIAiming.RandomAimTarget(patrolPoints);
    }

    //FSM Behaviour: Attack 
    [Task]
    private void Attack() {
         
        ////If there is a target, set it as the aimTarget 
        //if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {

        //    //Child object correction - wonky pivot point
        //    mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;

        //    //Move Towards attack Target
        //    if (Vector3.Distance(transform.position, attackTarget.transform.position) >= 45.0f) {
        //        mechAIMovement.Movement(attackTarget.transform.position, 45);
        //    }
        //    //Otherwise "strafe" - move towards random patrol points at intervals
        //    else if (Vector3.Distance(transform.position, attackTarget.transform.position) < 45.0f && Time.time > attackTimer) {
        //        patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        //        mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 2);
        //        attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);
        //    }

        //    //Track position of current target to pursue if lost
        //    pursuePoint = attackTarget.transform.position;
        //}
    }

    //FSM Behaviour: Pursue
    [Task]
    void Pursue() {

        ////Move towards last known position of attackTarget
        //if (Vector3.Distance(transform.position, pursuePoint) > 3.0f) {
        //    mechAIMovement.Movement(pursuePoint, 1);
        //    mechAIAiming.RandomAimTarget(patrolPoints);
        //}
        ////Otherwise if reached and have not re-engaged, reset attackTarget and Roam
        //else {
        //    attackTarget = null;
        //    patrolIndex = UnityEngine.Random.Range(0, patrolPoints.Length - 1);
        //    mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        //    mechState = MechStates.Roam;
        //}
    }

    //FSM Behaviour: Flee
    [Task]
    void Flee() {

        ////If there is an attack target, set it as the aimTarget 
        //if (attackTarget && mechAIAiming.LineOfSight(attackTarget)) {
        //    //Child object correction - wonky pivot point
        //    mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
        //} else {
        //    //Look at random patrol points
        //    mechAIAiming.RandomAimTarget(patrolPoints);
        //}

        ////Move towards random patrol points <<< This could be drastically improved!
        //if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f) {
        //    patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        //} else {
        //    mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        //}
    }
    #endregion
}
