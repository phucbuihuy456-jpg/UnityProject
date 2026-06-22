using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] public float damageAmount;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthManager healthManager = collision.gameObject.GetComponent<HealthManager>();
            if (healthManager != null)
            {
                healthManager.TakeDamage(damageAmount);
            }
        }
    }
}
