using UnityEngine;

public class SwordWave : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 3f;
    public float lifeTime = 3f;

    private float direction = 1f;

    public void SetDirection(float dir)
    {
        direction = dir;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ZombieAI zombie = other.GetComponent<ZombieAI>();

        if (zombie != null)
        {
            zombie.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
        { 
            Destroy(gameObject);
        }
    }
}
