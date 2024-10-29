using Panda;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

//public partial class MechAIDecisions2
//{
//    #region Velocity Logic
//    [Header("Vectore Stuff")]
//    private Vector3[] _myPreviousPositions = new Vector3[10];
//    private int _myVelocityCurrentIndex = 0;
//    [SerializeField] private Vector3 _myCurrentAvgVelocity;

//    private Vector3[] _targetPreviousPositions = new Vector3[10];
//    private int _targetVelocityCurrentIndex = 0;
//    [SerializeField] private GameObject _targetVelocityGO;                   // the game object of the current velocity tracking object 
//    [SerializeField] private Vector3 _targetCurrentAvgVelocity;

//    float _currentVelocityOfTargetTowardsMe;

//    private void Start_MyInitializeVelocityLogic()
//    {
//        for (int i = 0; i < _myPreviousPositions.Length; i++)
//            _myPreviousPositions[i] = transform.position;
//    }

//    private void InitalizeTargetVelocity()
//    {
//        if (_targetVelocityGO)
//        {
//            _targetVelocityCurrentIndex = 0;
//            for (int i = 0; i < _targetPreviousPositions.Length; i++)
//                _targetPreviousPositions[i] = _targetVelocityGO.transform.position;
//        }
//    }
//    private void Update_TargetVelocityLogic()
//    {
//        // Guards
//        if (_targetVelocityGO == null)
//        {
//            _targetVelocityGO = attackTarget;
//            InitalizeTargetVelocity();
//            return;
//        }
//        if (attackTarget != _targetVelocityGO)
//        {
//            _targetVelocityGO = attackTarget;
//            InitalizeTargetVelocity();
//            return;
//        }

//        // Shift the positions: oldest (index 0) to newest (index 9)
//        for (int i = 0; i < _targetPreviousPositions.Length - 1; i++)
//        {
//            _targetPreviousPositions[i] = _targetPreviousPositions[i + 1];
//        }
//        // Store the current position as the newest point
//        _targetPreviousPositions[_targetPreviousPositions.Length - 1] = _targetVelocityGO.transform.position;

//        // Calculate velocity between consecutive points
//        Vector3 totalVelocity = Vector3.zero;
//        for (int i = 0; i < _targetPreviousPositions.Length - 1; i++)
//        {
//            totalVelocity += (_targetPreviousPositions[i + 1] - _targetPreviousPositions[i]) / Time.deltaTime;
//        }
//        // Average the total velocity over the last 9 intervals
//        _targetCurrentAvgVelocity = totalVelocity / (_targetPreviousPositions.Length - 1);
//    }
//    private void Update_MyVelocityLogic()
//    {

//        // Shift the positions: oldest (index 0) to newest (index 9)
//        for (int i = 0; i < _myPreviousPositions.Length - 1; i++)
//        {
//            _myPreviousPositions[i] = _myPreviousPositions[i + 1];
//        }
//        // Store the current position as the newest point
//        _myPreviousPositions[_myPreviousPositions.Length - 1] = transform.position;

//        // Calculate velocity between consecutive points
//        Vector3 totalVelocity = Vector3.zero;
//        for (int i = 0; i < _myPreviousPositions.Length - 1; i++)
//        {
//            totalVelocity += (_myPreviousPositions[i + 1] - _myPreviousPositions[i]) / Time.deltaTime;
//        }
//        // Average the total velocity over the last 9 intervals
//        _myCurrentAvgVelocity = totalVelocity / (_myPreviousPositions.Length - 1);

//    }

//    private bool Velocity_IsAttackTargetMovingTowardsMe()
//    {
//        Vector3 directionFromTargetToMe = this.transform.position - attackTarget.transform.position;
//        _currentVelocityOfTargetTowardsMe = Vector3.Dot(_targetCurrentAvgVelocity, directionFromTargetToMe.normalized);
//        return _currentVelocityOfTargetTowardsMe > 0;
//    }


//    #endregion

//    #region ResourcePoints

//    // Assumed Knowledge
//    private PickupSpawner[] _allResourcePickupPoints;
//    [SerializeField] private List<GameObject> _ResourcePoints = new List<GameObject>();
//    [SerializeField]
//    private List<Tuple<GameObject, ResourceRiskTeir>> _RankedResourcePointsList = new List<Tuple<GameObject, ResourceRiskTeir>>();
//    enum ResourceRiskTeir
//    {
//        low,
//        med,
//        high,
//    }

//    private void Start_GetResourcePoints()
//    {
//        // Get Assumed Knowledge
//        _allResourcePickupPoints = UnityEngine.Object.FindObjectsOfType<PickupSpawner>();
//        foreach (var rp in _allResourcePickupPoints)
//        {
//            if (rp.enabled == true)
//                _ResourcePoints.Add(rp.gameObject);
//        }
//        RankResourcePoints();

//        void RankResourcePoints()
//        {
//            Vector3 center = Vector3.zero;
//            foreach (var rp in _ResourcePoints)
//            {
//                center += rp.transform.position;
//            }
//            center = center / _ResourcePoints.Count;

//            // Sort by distance to center
//            _ResourcePoints.Sort((a, b) =>
//                Vector3.Distance(a.transform.position, center)
//                    .CompareTo(Vector3.Distance(b.transform.position, center)));

//            // Assign a risk tier based on distance, with high risk nodes at the center
//            int pointCount = _ResourcePoints.Count;
//            for (int i = 0; i < pointCount; i++)
//            {
//                GameObject resourcePoint = _ResourcePoints[i];
//                float distance = Vector3.Distance(resourcePoint.transform.position, center);

//                ResourceRiskTeir riskTier;
//                if (i < pointCount * 0.18f)
//                    riskTier = ResourceRiskTeir.high;
//                else if (i < pointCount * 0.55f)
//                    riskTier = ResourceRiskTeir.med;
//                else
//                    riskTier = ResourceRiskTeir.low;

//                // Add to the ranked list
//                _RankedResourcePointsList.Add(new Tuple<GameObject, ResourceRiskTeir>(resourcePoint, riskTier));
//            }
//        }
//    }


//    private GameObject GetNextClosestTeiredResourcePoint(ResourceRiskTeir teir)
//    {
//        float currentDis = float.MaxValue;
//        float toDis;
//        GameObject targetResourcePoint = null;
//        foreach (var rp in _RankedResourcePointsList)
//        {
//            if (rp.Item2 == teir)
//            {
//                toDis = Vector3.Distance(rp.Item1.transform.position, this.transform.position);
//                if (toDis < currentDis && toDis > 1f) // NOTE: Ignore if next to it
//                {
//                    targetResourcePoint = rp.Item1;
//                    currentDis = toDis;
//                }
//            }
//        }

//        return targetResourcePoint;
//    }


//    #endregion

//    #region ObservationPoints
//    // Artificial Observation Points
//    public GameObject[] _observationPoints;
//    public int _currentObservationPointIndex;
//    public float _nextObservationPointTimer;
//    public bool _isLookingLeft;

//    void Start_CreateObservationPoints()
//    {
//        _observationPoints = new GameObject[6];
//        _observationPoints[0] = CreateEmptyChild("FrontObservationPoint", Vector3.forward * 5f);
//        _observationPoints[1] = CreateEmptyChild("LeftObservationPoint", Vector3.left * 5f);
//        _observationPoints[2] = CreateEmptyChild("BackObservationPoint", Vector3.back * 5f);
//        _observationPoints[3] = CreateEmptyChild("RightObservationPoint", Vector3.right * 5f);
//        _observationPoints[4] = CreateEmptyChild("FLObservationPoint", new Vector3(-3, 0, 3));
//        _observationPoints[5] = CreateEmptyChild("FRbservationPoint", new Vector3(3, 0, 3));
//    }
//    GameObject CreateEmptyChild(string name, Vector3 localPosition)
//    {
//        Vector3 verticalOffset = new Vector3(0f, 3.35f, 0f);
//        localPosition = localPosition + verticalOffset;
//        GameObject newObject = new GameObject(name);
//        newObject.transform.parent = this.transform;
//        newObject.transform.localPosition = localPosition;
//        return newObject;
//    }
//    #endregion

//    #region CurrentCombatStats
//    private void Update_CheckForResorucePointTrigger()
//    {

//    }

//    #endregion

//    #region View
//    [Task]
//    void View_CuriouseLookAround()
//    {
//        if (Time.time > _nextObservationPointTimer)
//            NextViewPoint();
//        else
//            mechAIAiming.aimTarget = _observationPoints[_currentObservationPointIndex];

//        void NextViewPoint()
//        {
//            _nextObservationPointTimer = Time.time + 0.5f;

//            if (_isLookingLeft)
//            {
//                // Try Look Right
//                if (_currentObservationPointIndex == 0)
//                    _currentObservationPointIndex = 5; //Set look right
//                else
//                {
//                    _isLookingLeft = false;
//                    _currentObservationPointIndex = 0; // Set look Foward
//                }
//            }
//            else
//            {
//                // Try Look Left
//                if (_currentObservationPointIndex == 0)
//                    _currentObservationPointIndex = 4; //Set look right
//                else
//                {
//                    _isLookingLeft = true;
//                    _currentObservationPointIndex = 0;
//                }
//            }
//        }
//        Task.current.Succeed();
//    }

//    /// <summary>
//    /// Rapidly Spins the View in a circle
//    /// </summary>
//    [Task]
//    void View_Spin()
//    {
//        if (Time.time > _nextObservationPointTimer)
//            NextViewPoint();
//        else
//            mechAIAiming.aimTarget = _observationPoints[_currentObservationPointIndex];

//        void NextViewPoint()
//        {
//            _nextObservationPointTimer = Time.time + 0.2f;
//            _currentObservationPointIndex = (_currentObservationPointIndex + 1) % 4;
//        }
//        Task.current.Succeed();
//    }
//    [Task]
//    void View_LookAtAtackTarget()
//    {
//        if (mechAIAiming.aimTarget)
//        {
//            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
//            Task.current.Succeed();
//        }
//        else
//        {
//            Task.current.Fail();
//        }
//        Task.current.Succeed();
//    }
//    // mechAIAiming.aimTarget = _observationPoints[0]; // Look Fowards
//    #endregion // END View

//    #region Movement
//    /// <summary>
//    /// Check if the nav agent for this bot is stopped
//    /// </summary>
//    [Task]
//    private bool Movement_Conditional_IsAgentMovementStopped()
//    {
//        return mechAIMovement.agent.isStopped;
//    }

//    [Task]
//    private void Movement_Action_MoveToClosestTeir2ResourcePoint()
//    {
//        if (_currentResorucePointTarget == null || Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) < 1f)
//        {
//            _currentResorucePointTarget = GetNextClosestTeiredResourcePoint(ResourceRiskTeir.med);
//        }

//        // Move to Resoruce Point
//        if (_currentResorucePointTarget != null)
//        {
//            if (Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f)
//            {
//                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
//                mechAIMovement.agent.isStopped = false;
//            }
//            else
//                _currentResorucePointTarget = null;
//        }
//        Task.current.Succeed();
//    }

//    [Task]
//    private void Movement_Action_MoveToClosestTeir3ResourcePoint()
//    {
//        if (_currentResorucePointTarget == null || Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) < 1f)
//        {
//            _currentResorucePointTarget = GetNextClosestTeiredResourcePoint(ResourceRiskTeir.low);
//        }

//        // Move to Resoruce Point
//        if (_currentResorucePointTarget != null)
//        {
//            if (Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f)
//            {
//                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
//                mechAIMovement.agent.isStopped = false;
//            }
//            else
//                _currentResorucePointTarget = null;
//        }
//        Task.current.Succeed();
//    }

//    [Task]
//    private void Movement_Action_StandStill()
//    {
//        //Debug.Log(this.gameObject.name + "Movement_Action_StandStill - Stop Moving");       
//        mechAIMovement.Movement(this.transform.position, 0); //Set to current postion      
//        mechAIMovement.agent.isStopped = true;

//        Task.current.Succeed();
//    }

//    [Task]
//    private void Movement_Action_MoveTowardsAttackTarget_Close()
//    {
//        //Debug.Log(this.gameObject.name + ": Movement_Action_MoveTowardsAttackTarget");
//        pursuePoint = attackTarget.transform.position;
//        if (Vector3.Distance(transform.position, pursuePoint) > 4.5f)
//        {
//            mechAIMovement.Movement(pursuePoint, 4); // Set to postion of attackTarget + 5f (stop close, but not to close)
//            mechAIMovement.agent.isStopped = false;
//        }
//        else
//        {
//            mechAIMovement.agent.isStopped = true;
//        }
//        Task.current.Succeed();
//    }

//    [Task]
//    private void Movement_Action_MoveTowardsAttachTarget_ToOptimalAttackRange()
//    {
//        //Debug.Log(this.gameObject.name + ": Movement_Action_MoveTowardsAttackTarget");
//        pursuePoint = attackTarget.transform.position;

//        //NOTE: <50 is optimal, but the is momentem / delay, that will carry the bot over the line, so the
//        //optimal strategy it to stop befor any other bot
//        if (Vector3.Distance(transform.position, pursuePoint) > 51f)
//        {
//            mechAIMovement.Movement(pursuePoint, 0);
//            mechAIMovement.agent.isStopped = false;
//        }
//        else
//        {
//            mechAIMovement.agent.isStopped = true;
//        }
//        Task.current.Succeed();
//    }

//    [Task]
//    private void Movement_Flee()
//    {
//        // Chose a position that is away from enemies
//        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
//            patrolIndex = UnityEngine.Random.Range(0, patrolPoints.Length - 1);
//        else
//            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
//    }
//    #endregion // END MOVEMENT

//    #region Engagement Heuristics

//    public CombatFitnessTier combatFitness;
//    float sitRepTimer;
//    GameObject currentCFGO;
//    public enum CombatFitnessTier
//    {
//        max,    // Max - Recharge Till This FLag
//        good,   // Good - Fully Combat Ready - Can be Aggressive 
//        ok,     // OK - Withdrawing Combat - Defensive Stance
//        bad     // Bad - Prioritise Survival at this flag     
//    }

//    /// <summary>
//    /// DOES: Carries out The targeting logic, finds targets and gets closest
//    /// Task - Always Succeed
//    /// </summary>
//    [Task]
//    private void Engagement_Update_TargetingLogic()
//    {
//        //Acquire valid attack target: perform frustum and LOS checks and determine closest target
//        mechAIAiming.FrustumCheck();
//        if (!attackTarget)
//        {
//            GetClosestVisableKnownAttackTarget();
//            //attackTarget = mechAIAiming.ClosestTarget(mechAIAiming.currentTargets);
//        }
//        if (attackTarget)
//        {
//            Update_MyVelocityLogic();
//            Update_TargetVelocityLogic();
//        }
//        Task.current.Succeed();
//    }


//    /// <summary>
//    /// Ensures enough time has passed for the Velocity to be calculated correctly
//    /// </summary>
//    [Task]
//    private void Engagement_Update_HoldAndFireValue()
//    {
//        if (currentCFGO == null || currentCFGO != attackTarget)
//        {
//            currentCFGO = attackTarget;
//            sitRepTimer = 0;
//        }

//        sitRepTimer += 1f * Time.deltaTime;
//        //Debug.Log("Engagement_Update_HoldAndFireValue: Value = " + sitRepTimer);
//        if (sitRepTimer > 1f)
//        {
//            Task.current.Fail();
//        }
//        else
//        {
//            Task.current.Succeed();
//        }
//    }

//    [Task]
//    private bool Enguagement_Conditional_IsTargetMovingTowardsMe()
//    {
//        return Velocity_IsAttackTargetMovingTowardsMe();
//    }
        

//    [Task]
//    private void Engagement_Update_CombatFitnessHeuristics()
//    {
//        float ammoRemianing_pct =
//            (mechSystem.energy / _laser_EnergyMaxValue) * 0.33f +
//            (mechSystem.shells / _cannon_AmmoMaxValue) * 0.33f +
//            (mechSystem.missiles / _missiles_AmmoMaxValue) * 0.33f;

//        float healthRemaining_pct =
//            mechSystem.health / _health_MaxValue;

//        // Max - Recharge Till This FLag
//        if (healthRemaining_pct > 0.90f && ammoRemianing_pct > 0.80f)
//            combatFitness = CombatFitnessTier.max;

//        // Good - Fully Combat Ready - Can be Aggressive 
//        else if (healthRemaining_pct > 0.90f && ammoRemianing_pct > 0.80f
//            && Cannon_Conditional_OverHalfAmmoRemaining()
//            && Missile_Conditional_OverHalfAmmoRemaining()
//            && Beam_Conditional_OverHalfAmmoRemaining())
//            combatFitness = CombatFitnessTier.good;


//        // OK - Withdrawing Combat - Defensive Stance
//        // Note: All Weapons Still Firable
//        else if (healthRemaining_pct > 0.70f && ammoRemianing_pct > 0.40f
//            && Cannons_Conditional_SufficientResources()
//            && Missile_Conditional_SufficientResources()
//            && Beam_Conditional_SufficientResources())
//            combatFitness = CombatFitnessTier.ok;

//        // Bad - Prioritise Survival at this flag
//        else
//            combatFitness = CombatFitnessTier.bad;

//        Task.current.Succeed();
//    }
//    [Task]
//    private bool Enguagement_Conditional_IsCombatFitness_Max() => combatFitness == CombatFitnessTier.max;
//    [Task]
//    private bool Enguagement_Conditional_IsCombatFitness_Good() => combatFitness == CombatFitnessTier.good;
//    [Task]
//    private bool Enguagement_Conditional_IsCombatFitness_OK() => combatFitness == CombatFitnessTier.ok;
//    [Task]
//    private bool Enguagement_Conditional_IsCombatFitness_Bad() => combatFitness == CombatFitnessTier.bad;



//    [Task]
//    private bool Enguagement_Conditional_IsOnResourcePoint()
//    {
//        return Utility_IsCurrentlyOnAResorucePoint();
//    }


//    #endregion // Engagement END

//    #region Required 'leaf nodes' (Version 5.1)

//    [Task]
//    private void EndLeafV5_CombatPlanA_HoldPositon()
//    {
//        Movement_Action_StandStill();
//        View_LookAtAtackTarget();

//        Task.current.Succeed();
//    }

//    [Task]
//    private void EndLeafV5_CombatPlanA_AdvanceOnTarget()
//    {
//        Movement_Action_MoveTowardsAttachTarget_ToOptimalAttackRange();
//        View_LookAtAtackTarget();

//        Task.current.Succeed();
//    }

//    [Task]
//    private void EndLeafV5_HuntForTarget()
//    {
//        Movement_Action_MoveToClosestTeir2ResourcePoint();
//        View_CuriouseLookAround();

//        Task.current.Succeed();
//    }

//    [Task]
//    private void EndLeafV5_FightingWithdrawal()
//    {
//        Movement_Action_MoveToClosestTeir2ResourcePoint();
//        View_LookAtAtackTarget();

//        Task.current.Succeed();
//    }

//    [Task]
//    private void EndLeafV5_VigilantlyHoldPosition()
//    {
//        Movement_Action_MoveToClosestTeir2ResourcePoint();
//        View_LookAtAtackTarget();

//        Task.current.Succeed();
//    }

//    #endregion

//}
