using Panda;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MechAIDecisions
{
    #region Velocity Logic
    [Header("Vectore Stuff")]
    private Vector3[] _myPreviousPositions = new Vector3[10];
    private int _myVelocityCurrentIndex = 0;
    [SerializeField] private Vector3 _myCurrentAvgVelocity;

    private Vector3[] _targetPreviousPositions = new Vector3[10]; 
    private int _targetVelocityCurrentIndex = 0;
    [SerializeField] private GameObject _targetVelocityGO;                   // the game object of the current velocity tracking object 
    [SerializeField] private Vector3 _targetCurrentAvgVelocity;
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
        if(_targetVelocityGO == null)
        {
            _targetVelocityGO = attackTarget;
            InitalizeTargetVelocity();
            return;
        }
        if(attackTarget != _targetVelocityGO)
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



    private Vector3 CalculateAverageVelocity(ref Vector3[] prePos)
    {
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < prePos.Length; i++)
        {
            int nextIndex = (i + 1) % prePos.Length;
            // Velocity between two consecutive positions
            Vector3 velocity = (prePos[nextIndex] - prePos[i]) / Time.deltaTime;
            sum += velocity;
        }

        sum.y = 0;

        // Return the average velocity over 3 frames
        return sum / prePos.Length;
    }



    #endregion

    #region ResourcePoints

    // Assumed Knowledge
    private PickupSpawner[] _allResourcePickupPoints;
    [SerializeField] private List<GameObject> _ResourcePoints = new List<GameObject>();
    [SerializeField]
    private List<Tuple<GameObject,ResourceRiskTeir>> _RankedResourcePointsList = new List<Tuple<GameObject, ResourceRiskTeir>>();
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
            center = center/_ResourcePoints.Count;

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
                if (i < pointCount * 0.21f)
                    riskTier = ResourceRiskTeir.high;
                else if (i < pointCount * 0.42f)
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
    }
    [Task]
    void View_LookAtAtackTarget()
    {
        mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
    }
    // mechAIAiming.aimTarget = _observationPoints[0]; // Look Fowards
    #endregion // END View


    #region Movement


    [Task]
    private void Movement_Action_MoveToClosestTeir2ResourcePoint()
    {
        if (_currentResorucePointTarget == null || Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) < 1f)
        {
            _currentResorucePointTarget = GetNextClosestTeiredResourcePoint(ResourceRiskTeir.med); 
        }

        // Move to Resoruce Point
        if (_currentResorucePointTarget != null)
        {
            if (Vector3.Distance(this.transform.position, _currentResorucePointTarget.transform.position) > 2f)
                mechAIMovement.Movement(_currentResorucePointTarget.transform.position, 1);
            else
                _currentResorucePointTarget = null;
        }
    }
    
    [Task]
    private void Movement_Action_StandStill()
    {
        Debug.Log(this.gameObject.name + "Movement_Action_StandStill - Stop Moving");
        mechAIMovement.Movement(this.transform.position, 0); //Set to current postion
    }

    [Task]
    private void Movement_Action_MoveTowardsAttackTarget()
    {
        Debug.Log(this.gameObject.name + ": Movement_Action_MoveTowardsAttackTarget");
        pursuePoint = attackTarget.transform.position;
        if (Vector3.Distance(transform.position, pursuePoint) > 3.0f)
        {
            mechAIMovement.Movement(pursuePoint, 2); // Set to postion of attackTarget

        }

    }

    [Task]
    private void Movement_Flee()
    {
        // Chose a position that is away from enemies
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
            patrolIndex = UnityEngine.Random.Range(0, patrolPoints.Length - 1);
        else
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
    }
    #endregion // END MOVEMENT

    #region Engagement Heuristics
    float holdAndFire;
    
    [Task]
    private void Engagement_Update_HoldAndFireValue()
    {
        holdAndFire += 0.1f * Time.deltaTime;
    }

    #endregion // Engagement END
}
