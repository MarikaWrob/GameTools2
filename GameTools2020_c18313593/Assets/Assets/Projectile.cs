using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage;
    public bool isEnemy;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) {
            Enemy e = other.transform.root.GetComponent<Enemy>();
            if (e && !isEnemy)
                e.ApplyDamage(damage);
        }
    }
}
