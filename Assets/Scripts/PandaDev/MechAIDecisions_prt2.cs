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
}
