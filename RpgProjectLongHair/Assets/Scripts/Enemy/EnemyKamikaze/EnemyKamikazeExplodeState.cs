using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.UI.Image;
using Fusion;

public class EnemyKamikazeExplodeState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;
    private bool _exploded = false;
    private bool _animationTriggered = false;

    public EnemyKamikazeExplodeState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Detener movimiento inmediatamente
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.transform.position);

        // Trigger animación de explosión
        TriggerExplodeAnimation();
    }

    public void ExitState() { }

    public void UpdateState() { }

    private void TriggerExplodeAnimation()
    {
        if (_animationTriggered) return;
        _animationTriggered = true;

        // Llamar a la animación de explosión
        var animController = _enemy.GetComponent<EnemyAnimationController>();
        if (animController != null)
        {
            // Puedes pasar un VFXConfig si lo tienes configurado
            // AttackVFXConfig vfxConfig = ...;
            animController.PlayExplode(null);
        }

        // Usar el delay configurado en KamikazeAttackData
        float explosionDelay = _enemy.KamikazeData != null
            ? _enemy.KamikazeData.ExplosionDelay
            : 0.3f;

        DelayedExplode(explosionDelay);
    }

    private void DelayedExplode(float delay)
    {
        // Usar MonoBehaviour para el delay
        _enemy.StartCoroutine(ExplodeAfterDelay(delay));
    }

    private System.Collections.IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        int damage = _enemy.KamikazeData.Damage;
        float radius = _enemy.KamikazeData.HitRadius;
        Vector3 origin = _enemy.transform.position;

        Collider[] hits = Physics.OverlapSphere(
            _enemy.transform.position,
            radius,
            _enemy.PlayerLayer
        );

        var alreadyHit = new HashSet<PlayerHealth>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            var ph = hit.GetComponent<PlayerHealth>()
                   ?? hit.GetComponentInParent<PlayerHealth>();

            if (ph == null) continue;
            if (ph.IsDead) continue;
            if (!alreadyHit.Add(ph)) continue;

            ph.TakeDamage(damage, _enemy.transform.position);
        }

        var hitEnemies = new HashSet<EnemyHealth>();
        foreach (var hit in Physics.OverlapSphere(origin, radius, _enemy.EnemyLayer))
        {
            // Ignorarse a sí mismo
            if (hit.transform.IsChildOf(_enemy.transform) || hit.gameObject == _enemy.gameObject)
                continue;

            var eh = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (eh == null || eh.IsDead || !hitEnemies.Add(eh)) continue;

            // Usar el mismo flujo de red que usa el resto del juego
            eh.ApplyDamageServer(damage, PlayerRef.None);
        }

        SpawnVFX();

        if (_enemy.Runner != null && _enemy.Object.IsValid)
            _enemy.Runner.Despawn(_enemy.Object);
    }

    private void SpawnVFX()
    {
        var prefab = _enemy.KamikazeData.ExplosionVFXPrefab;
        if (prefab == null) return;

        GameObject vfx = GameObject.Instantiate(
            prefab,
            _enemy.transform.position,
            Quaternion.identity
        );

        GameObject.Destroy(vfx, 3f);
    }
}