using UnityEngine;

[CreateAssetMenu(fileName = "KamikazeAttack_001_Data", menuName = "Data/KamikazeAttack")]
public class KamikazeAttackData : AttackData
{
    [SerializeField] private GameObject _explosionVFXPrefab;
    public GameObject ExplosionVFXPrefab => _explosionVFXPrefab;
}