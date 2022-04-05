using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;
using UnityEditor.AI;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float enemyStartHealth;
    [SerializeField] private float lowHealthThreshold;
    [SerializeField] private float healthRestoreRate;

    [SerializeField] private float chasingRange;
    [SerializeField] private float shootingRange;

    [SerializeField] private Transform playerTransform;

    private Tree<EnemyAI> _tree;

    private Material _material;

    private float currentHealth
    {
        get { return currentHealth; }
        set { currentHealth = Mathf.Clamp(value, 0, enemyStartHealth); }
    }

    private void Start()
    {
        currentHealth = enemyStartHealth;
        _material = GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        currentHealth += Time.deltaTime * healthRestoreRate;
        _tree.Update(this);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public void SetColor(Color color)
    {
        _material.color = color;
    }
}

public class EnemyHealth : Node<EnemyAI>
{
    private float _threshold;

    public EnemyHealth(float threshold)
    {
        _threshold = threshold;
    }

    public override bool Update(EnemyAI context)
    {
        return context.GetCurrentHealth() <= _threshold;
    }
}

public class RangeNode : Node<EnemyAI>
{
    private float _range;
    private Transform _target;
    private Transform _origin;

    public RangeNode(float range, Transform target, Transform origin)
    {
        _range = range;
        _target = target;
        _origin = origin;
    }

    public override bool Update(EnemyAI context)
    {
        float distance = Vector3.Distance(_target.position, _origin.position);
        return distance <= _range;
    }
}

public class IsCoveredNode : Node<EnemyAI>
{
    private Transform _target;
    private Transform _origin;

    public IsCoveredNode(Transform target, Transform origin)
    {
        _target = target;
        _origin = origin;
    }

    public override bool Update(EnemyAI context)
    {
        RaycastHit hit;
        if (Physics.Raycast(_origin.position, _target.position - _origin.position, out hit))
        {
            if (hit.collider.transform != _target)
            {
                return true;
            }
        }

        return false;
    }
}

public class ChaseNode : Node<EnemyAI>
{
    private Transform _target;
    private NavMeshAgent _agent;

    public ChaseNode(Transform target, NavMeshAgent agent)
    {
        _target = target;
        _agent = agent;
    }

    public override bool Update(EnemyAI context)
    {
        float distance = Vector3.Distance(_target.position, _agent.transform.position);
        if (distance > 0.2f)
        {
            context.SetColor(Color.yellow);
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
        }
        else
        {
            _agent.isStopped = true;
        }

        return true;
    }
}

public class ShootNode : Node<EnemyAI>
{
    private NavMeshAgent _agent;

    public ShootNode(NavMeshAgent agent)
    {
        _agent = agent;
    }

    public override bool Update(EnemyAI context)
    {
        _agent.isStopped = true;
        context.SetColor(Color.green);
        return true;
    }
}

public class IsCoverAvailableNode : Node<EnemyAI>
{
    private Cover[] _availableCovers;
    private Transform _target;

    public IsCoverAvailableNode(Cover[] availableCovers, Transform target)
    {
        _availableCovers = availableCovers;
        _target = target;
    }

    public override bool Update(EnemyAI context)
    {
        Transform bestSpot = FindBestSpot();
        return bestSpot != null;
    }

    private Transform FindBestSpot()
    {
        float minAngle = 90;
        Transform bestSpot = null;
        return bestSpot;
    }
}