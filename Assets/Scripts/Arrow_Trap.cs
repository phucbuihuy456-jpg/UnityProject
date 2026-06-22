using UnityEngine;

public class Arrow_Trap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float attackDelay;
    [SerializeField] private Transform arrowPoint;
    [SerializeField] private GameObject[] arrows;
    private float timer;
    private void Attack()
    {
        timer = 0;
        arrows[FindInactiveArrowIndex()].transform.position = arrowPoint.position;
        arrows[FindInactiveArrowIndex()].GetComponent<EnemyProjectile>().ActivateProjectile();
    }

    private int FindInactiveArrowIndex()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            if (!arrows[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0; // No inactive arrow found
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= attackDelay)
        {
            Attack();
        }
    }
}
