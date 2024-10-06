using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;

public class Movement : MonoBehaviour
{
    public GameObject target;
    public float moveSpeed = 5.0f;
    public GameObject[] patrolPoints;
    int patrolIndex = 0;


    [Task]
    void Patrol()
    {
        transform.position = Vector3.MoveTowards(transform.position, patrolPoints[patrolIndex].transform.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) < 1)
        {
            patrolIndex++;

            if (patrolIndex >= patrolPoints.Length)
                patrolIndex = 0;
        }
    }

    [Task]
    bool IsTargetNear()
    {

        if (Vector3.Distance(transform.position, target.transform.position) < 10.0f)
            return true;
        else
            return false;
    }

    [Task]
    void Attack()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 2 * moveSpeed * Time.deltaTime);
    }


}
