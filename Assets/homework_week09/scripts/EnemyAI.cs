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
    [SerializeField] private Cover[] availableCovers;

    private Tree<EnemyAI> _tree;

    private Material _material;
    private Transform _bestCoverSpot;
    private NavMeshAgent _agent;

    //private Node<EnemyAI> topNode;

    private float _currentHealth;
    public float currentHealth
    {
        get { return _currentHealth; }
        set { _currentHealth = Mathf.Clamp(value, 0, enemyStartHealth); }
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _material = GetComponent<MeshRenderer>().material;
    }

    private void Start()
    {
        currentHealth = enemyStartHealth;
        ConstructBehaviorTree();
    }

    private void ConstructBehaviorTree()
    {
        EnemyHealth enemyHealth = new EnemyHealth(lowHealthThreshold);
        IsCoveredNode coveredNode = new IsCoveredNode(playerTransform, transform);
        IsCoverAvailableNode coverAvailableNode = new IsCoverAvailableNode(availableCovers, playerTransform);
        GoToCoverNode goToCoverNode = new GoToCoverNode();
        RangeNode shootingRangeNode = new RangeNode(shootingRange, playerTransform, transform);
        ShootNode shootNode = new ShootNode();
        RangeNode chasingRangeNode = new RangeNode(chasingRange, playerTransform, transform);
        ChaseNode chaseNode = new ChaseNode(playerTransform);

        var chaseSequence = new Sequence<EnemyAI>(chasingRangeNode, chaseNode);
        
        var shootSequence = new Sequence<EnemyAI>(shootingRangeNode, shootNode);
        
        var gotToCoverSequence = new Sequence<EnemyAI>(coverAvailableNode, goToCoverNode);
        var findCoverSelector = new Selector<EnemyAI>(gotToCoverSequence, chaseSequence);
        var tryToTakeCoverSelector = new Selector<EnemyAI>(coveredNode, findCoverSelector);
        var coverSequence = new Sequence<EnemyAI>(enemyHealth, tryToTakeCoverSelector);
        
        var topNodeSelector = new Selector<EnemyAI>(coverSequence, shootSequence, chaseSequence);

        _tree = new Tree<EnemyAI>(topNodeSelector);
    }

    private void Update()
    {
        currentHealth += Time.deltaTime * healthRestoreRate;
        _tree.Update(this);
    }

    private void OnMouseDown()
    {
        currentHealth -= 10f;
    }

    public void SetColor(Color color)
    {
        _material.color = color;
    }

    public void SetBestCoverSpot(Transform bestSpot)
    {
        _bestCoverSpot = bestSpot;
    }

    public Transform GetBestCoverSpot()
    {
        return _bestCoverSpot;
    }

    public NavMeshAgent GetNavMeshAgent()
    {
        return _agent;
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
        return context.currentHealth <= _threshold;
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

    public ChaseNode(Transform target)
    {
        _target = target;
    }

    public override bool Update(EnemyAI context)
    {
        NavMeshAgent agent = context.GetNavMeshAgent();
        float distance = Vector3.Distance(_target.position, agent.transform.position);
        context.SetColor(Color.yellow);
        if (distance > 0.2f)
        {
            agent.isStopped = false;
            agent.SetDestination(_target.position);
        }
        else
        {
            agent.isStopped = true;
        }

        return true;
    }
}

public class GoToCoverNode : Node<EnemyAI>
{
    public override bool Update(EnemyAI context)
    {
        NavMeshAgent agent = context.GetNavMeshAgent();
        Transform cover = context.GetBestCoverSpot();
        if (cover == null)
        {
            return false;
        }
        context.SetColor(Color.blue);
        float distance = Vector3.Distance(cover.position, agent.transform.position);
        if (distance > 0.2f)
        {
            agent.isStopped = false;
            agent.SetDestination(cover.position);
        }
        else
        {
            agent.isStopped = true;
        }

        return true;
    }
}

public class ShootNode : Node<EnemyAI>
{
    public override bool Update(EnemyAI context)
    {
        NavMeshAgent agent = context.GetNavMeshAgent();
        agent.isStopped = true;
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
        Transform bestSpot = FindBestSpot(context);
        context.SetBestCoverSpot(bestSpot);
        return bestSpot != null;
    }

    private Transform FindBestSpot(EnemyAI context)
    {
        if (context.GetBestCoverSpot() != null)
        {
            if (CheckIfSpotIsValid(context.GetBestCoverSpot()))
            {
                return context.GetBestCoverSpot();
            }
        }
        float minAngle = 90;
        Transform bestSpot = null;
        for (int i = 0; i < _availableCovers.Length; i++)
        {
            Transform bestSpotInCover = FindBestSpotInCover(_availableCovers[i], ref minAngle);
            if (bestSpotInCover != null)
            {
                bestSpot = bestSpotInCover;
            }
        }

        return bestSpot;
    }

    private Transform FindBestSpotInCover(Cover cover, ref float minAngle)
    {
        Transform[] availableSpots = cover.GetCoverSpots();
        Transform bestSpot = null;
        for (int i = 0; i < availableSpots.Length; i++)
        {
            Vector3 direction = _target.position - availableSpots[i].position;
            if (CheckIfSpotIsValid(availableSpots[i]))
            {
                float angle = Vector3.Angle(availableSpots[i].forward, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestSpot = availableSpots[i];
                }
            }
        }

        return bestSpot;
    }

    private bool CheckIfSpotIsValid(Transform spot)
    {
        RaycastHit hit;
        Vector3 direction = _target.position - spot.position;
        if (Physics.Raycast(spot.position, direction, out hit))
        {
            if (hit.collider.transform != _target)
            {
                return true;
            }
        }

        return false;
    }
}