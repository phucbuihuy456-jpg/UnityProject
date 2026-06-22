using System;
using UnityEngine;

public class FireTrap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float damageAmount;
    [SerializeField] private float activeDuration;
    [SerializeField] private float inactiveDuration;
    private bool isActive = false;
    private float timer = 0f;
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        timer = inactiveDuration;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(!isActive && timer >= inactiveDuration)
        {
            ActivateTrap();
        }
        else if(isActive && timer >= activeDuration)
        {
            DeactivateTrap();
        }
    }

    private void DeactivateTrap()
    {
        isActive = false;
        timer = 0f;
        animator.SetBool("IsActive", false);
        GetComponent<Collider2D>().enabled = false; // Tắt collider
    }

    private void ActivateTrap()
    {
        isActive = true;
        timer = 0f;
        animator.SetBool("isActive", true);
        GetComponent<Collider2D>().enabled = true; // Bật collider gây damage
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealthManager hm = collision.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.TakeDamage(damageAmount); // Damage theo thời gian
            }
        }
    }
}
