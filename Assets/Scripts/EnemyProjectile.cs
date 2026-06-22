using UnityEngine;

public class EnemyProjectile : Damageable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float speed;
    [SerializeField] private float resetTime;
    private float lifeTime;
    public void ActivateProjectile()
    {
        lifeTime = 0;
        gameObject.SetActive(true);
    }
    private void Update()
    {
        float movementSpeed = speed * Time.deltaTime;
        transform.Translate(-movementSpeed,0,0);
        lifeTime += Time.deltaTime;
        if (lifeTime >= resetTime)
        {
            gameObject.SetActive(false);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            HealthManager healthManager = collision.gameObject.GetComponent<HealthManager>();
            if(healthManager != null)
            {
                healthManager.TakeDamage(damageAmount);
            }
             gameObject.SetActive(false);
        }
        if (collision.CompareTag("Ground"))
        {
            gameObject.SetActive(false);
        }
    }
}
