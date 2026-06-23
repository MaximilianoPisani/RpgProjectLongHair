using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class EnemyKamikazeExplodeState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;
    private bool _exploded = false;
    private bool _animationTriggered = false;
    private EnemyNetworkSync _networkSync;
    public EnemyKamikazeExplodeState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Detener movimiento inmediatamente
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.transform.position);

        _networkSync = _enemy.GetComponent<EnemyNetworkSync>();

        // Trigger animación de explosión
        TriggerExplodeAnimation();
    }

    public void ExitState() { }

    public void UpdateState() { }

    private void TriggerExplodeAnimation()
    {
        if (_animationTriggered) return;
        _animationTriggered = true;

        var data = _enemy.KamikazeData;
        float delay = data != null ? data.ExplosionDelay : 0.3f;

        // Usar el cache, no GetComponent otra vez
        _networkSync?.SyncAttackIndicator();

        if (data != null)
        {
            _networkSync?.StartExplosionFlash(
                duration: delay,
                interval: data.FlashInterval,
                emissionColor: data.FlashEmissionColor,
                intensity: data.EmissionIntensity
            );
        }

        if (_networkSync != null)
            _networkSync.TriggerExplode();
        else
            _enemy.GetComponent<EnemyAnimationController>()?.PlayExplode(null); // fallback

        _enemy.StartCoroutine(ExplodeAfterDelay(delay));
    }

    private System.Collections.IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _networkSync?.StopExplosionFlash();
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        // NUEVO: Sonido de explosión al explotar realmente
        _networkSync?.TriggerKamikazeExplosionSound();

        var data = _enemy.KamikazeData;
        if (data == null) return;

        int damage = data.Damage;
        float radius = data.HitRadius;
        Vector3 origin = _enemy.transform.position;

        // ========== FIX: Determinar quién provocó la explosión ==========
        PlayerRef killerRef = PlayerRef.None;

        if (_enemy.TargetPlayer != null)
        {
            var targetNetObj = _enemy.TargetPlayer.GetComponent<NetworkObject>()
                            ?? _enemy.TargetPlayer.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null)
                killerRef = targetNetObj.InputAuthority;
        }

        // Fallback: player más cercano si no hay target
        if (killerRef == PlayerRef.None)
        {
            Collider[] nearbyPlayers = Physics.OverlapSphere(origin, radius * 2f, _enemy.PlayerLayer);
            float closestDist = float.MaxValue;
            NetworkObject closestPlayer = null;

            foreach (var hit in nearbyPlayers)
            {
                if (!hit.CompareTag("Player")) continue;
                var netObj = hit.GetComponent<NetworkObject>() ?? hit.GetComponentInParent<NetworkObject>();
                if (netObj == null) continue;

                float dist = Vector3.Distance(origin, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPlayer = netObj;
                }
            }

            if (closestPlayer != null)
                killerRef = closestPlayer.InputAuthority;
        }

        // Reportar kill antes de aplicar daño de explosión
        if (killerRef != PlayerRef.None)
        {
            var health = _enemy.GetComponent<EnemyHealth>();
            health?.ReportDeathForQuest(killerRef);
        }
        // ========== FIN FIX ==========

        Collider[] hits = Physics.OverlapSphere(
            origin,
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
            if (hit.transform.IsChildOf(_enemy.transform) || hit.gameObject == _enemy.gameObject)
                continue;

            var eh = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (eh == null || eh.IsDead || !hitEnemies.Add(eh)) continue;

            eh.ApplyDamageServer(damage, PlayerRef.None);

            float dist = Vector3.Distance(origin, eh.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / radius); // 1 = centro, 0 = borde
            int scaledDamage = Mathf.RoundToInt(data.ExplosionKnockbackDamage * (0.5f + falloff * 0.5f));

            //Knockback desde el origen de la explosión hacia afuera
            EnemyKnockback.TryApplyProjectileKnockback(
                 eh.gameObject,
                 (eh.transform.position - origin).normalized,
                 scaledDamage  // ahora sí usa el valor con falloff
            );     
        }

        SpawnVFX();

        _enemy.StartCoroutine(DespawnAfterVFX());
    }

    private void SpawnVFX()
    {
        var data = _enemy.KamikazeData;
        if (data?.ExplosionVFXPrefab == null) return;

        GameObject vfx = GameObject.Instantiate(
            data.ExplosionVFXPrefab,
            _enemy.transform.position,
            Quaternion.identity
        );

        GameObject.Destroy(vfx, 3f);

        _networkSync?.SyncExplosionVFX(data.ExplosionVFXPrefab, _enemy.transform.position);
    }

    private System.Collections.IEnumerator DespawnAfterVFX()
    {
        // Dos frames es suficiente para que Fusion entregue el RPC
        yield return null;
        yield return null;

        if (_enemy.Runner != null && _enemy.Object.IsValid)
            _enemy.Runner.Despawn(_enemy.Object);
    }

}