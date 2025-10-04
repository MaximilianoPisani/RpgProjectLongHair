using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Validaci�n de impacto (server)")]
    [SerializeField] private LayerMask hitMask;           // Debe incluir la capa de Hitbox
    [SerializeField] private bool stopOnFirstHit = true;  // Detenerse en el primer enemigo impactado

    private static readonly List<LagCompensatedHit> _hitsBuffer = new List<LagCompensatedHit>(16);
    /// El due�o del proyectil (InputAuthority) llama esto al detectar impacto local.
    /// El server valida con Lag Compensation y aplica da�o si corresponde.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_TryProjectileHit(Vector3 point, float radius, int damage, RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        if (damage <= 0) return;

        _hitsBuffer.Clear();

        // FIRMA para tu versi�n:
        // OverlapSphere(Vector3 center, float radius, PlayerRef src, List<LagCompensatedHit> results, int layerMask, HitOptions options)
        Runner.LagCompensation.OverlapSphere(
            point,
            radius,
            info.Source,                         // retrotiempo del atacante
            _hitsBuffer,                         // 4� par�metro: lista de resultados
            (int)hitMask,                        // 5� par�metro: layer mask
            HitOptions.IncludePhysX | HitOptions.SubtickAccuracy
        );

        if (_hitsBuffer.Count == 0)
            return;

        for (int i = 0; i < _hitsBuffer.Count; i++)
        {
            var hb = _hitsBuffer[i].Hitbox;
            if (hb == null) continue;

            if (EnemyHealth.TryApplyFromHitbox(hb, damage, info.Source))
            {
                RPC_OnProjectileHitAll(point); // solo feedback
                if (stopOnFirstHit) break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnProjectileHitAll(Vector3 worldPoint)
    {
        // VFX/SFX locales (no tocar estado ni l�gica)
        // VFXManager.SpawnImpact(worldPoint);
    }

    // Para pedir despawn de un proyectil cuando el jugador no tiene StateAuthority sobre �l
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestDespawn(NetworkId projectileId)
    {
        if (Runner.TryFindObject(projectileId, out var obj))
            Runner.Despawn(obj);
    }
}
