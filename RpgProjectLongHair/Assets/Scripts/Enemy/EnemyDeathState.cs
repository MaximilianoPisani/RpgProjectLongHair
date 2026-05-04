using UnityEngine;

public class EnemyDeathState : IEnemyState
{
    private readonly EnemyBaseController _enemy;
    private bool _hasTriggeredDeath = false;

    public EnemyDeathState(EnemyBaseController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState()
    {
        if (_hasTriggeredDeath) return;
        _hasTriggeredDeath = true;

        if (_enemy.Agent != null)
            _enemy.Agent.enabled = false;

        var ragdoll = _enemy.GetComponent<EnemyRagdoll>();
        var networkSync = _enemy.GetComponent<EnemyNetworkSync>();

        if (ragdoll != null)
        {
            Vector3 deathForce = CalculateDeathForce();

            // El host activa localmente Y notifica proxies vía RPC
            ragdoll.ActivateRagdoll(deathForce);
            networkSync?.TriggerDeath(); // ya envía RPC_ActivateRagdoll a proxies

            if (_enemy.Runner != null && _enemy.Object.HasStateAuthority)
                _enemy.StartCoroutine(FadeOutAndDespawn(ragdoll.RagdollDuration));
        }
        else
        {
            if (_enemy.Runner != null && _enemy.Object.HasStateAuthority)
                _enemy.Runner.Despawn(_enemy.Object);
        }
    }

    private Vector3 CalculateDeathForce()
    {
        return -_enemy.transform.forward + Vector3.up * 0.5f;
    }

    private System.Collections.IEnumerator FadeOutAndDespawn(float totalDuration)
    {
        yield return new WaitForSeconds(totalDuration * 0.6f);

        float fadeDuration = totalDuration * 0.4f;
        float elapsed = 0f;

        var renderers = _enemy.GetComponentsInChildren<Renderer>();

        // Cambiar a modo transparente
        foreach (var r in renderers)
        {
            var mat = r.material;
            mat.SetFloat("_Surface", 1f);      // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);        // 0 = Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (var r in renderers)
            {
                var col = r.material.color;
                r.material.color = new Color(col.r, col.g, col.b, alpha);
            }

            yield return null;
        }

        if (_enemy.Runner != null && _enemy.Object != null)
            _enemy.Runner.Despawn(_enemy.Object);
    }

    public void ExitState() { }
    public void UpdateState() { }
}