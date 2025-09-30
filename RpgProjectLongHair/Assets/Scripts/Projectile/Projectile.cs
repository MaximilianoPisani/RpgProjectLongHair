using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Networked] private TickTimer _life { get; set; }

    private Vector3 _direction;
    private float _speed;
    private int _damage;
    private float _maxRange;
    private Vector3 _startPosition;
    private GameObject _owner;
    private LayerMask _targetLayer;

    public void InitProjectile(Vector3 direction, RangedAttackData data, GameObject owner)
    {
        _direction = direction.normalized;
        _speed = data.ProjectileSpeed;
        _damage = data.Damage;
        _maxRange = data.AttackRange;
        _startPosition = transform.position;
        _owner = owner;
        _targetLayer = data.TargetLayer;

        _life = TickTimer.CreateFromSeconds(Runner, 5f);

        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsRunning) return;

        transform.position += _direction * _speed * Runner.DeltaTime;

        Collider[] hits = Physics.OverlapSphere(transform.position, 0.3f, _targetLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == _owner) continue;

            EnemyHealth health = hit.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.RPC_ApplyDamage(_damage);
                Runner.Despawn(Object);
                return;
            }
        }

        if (_life.Expired(Runner) || Vector3.Distance(_startPosition, transform.position) >= _maxRange)
        {
            Runner.Despawn(Object);
        }
    }
}