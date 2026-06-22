using UnityEngine;
public struct DamageInfo
{
    public float damage;
    public float knockbackForce;
    public Vector3 hitDirection;
    public GameObject attacker;
}
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}