using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    
    [SerializeField] GameObject[] ammoDrop;
    [SerializeField] GameObject healthDrop;
    [SerializeField] GameObject smallExplosionFX;
    [SerializeField] float startHealth = 3f;
    [SerializeField] float chance4drop = 1f;
    [SerializeField] float chance4ammoDrop = 0.6f;

    SpawnGate parentGate = null;
    GameManager gameManager;
    Vector3 dropSpawnPos;
    float currentHealth;

    void Awake()
    {
        currentHealth = startHealth;
    }

    public void Init(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.gameManager.AdjustEnemiesLeft(1);
    }

    public void Init(GameManager gameManager, SpawnGate parentGate)
    {
        Init(gameManager);
        this.parentGate = parentGate;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            SelfDestruct();
        }
    }

    public void SelfDestruct()
    {
        gameManager.AdjustEnemiesLeft(-1);
        if (Random.value < chance4drop)
        {
            dropSpawnPos = transform.position - new Vector3(0, .5f, 0);
            if (Random.value > chance4ammoDrop)
            {
                Instantiate(healthDrop, dropSpawnPos, Quaternion.identity);
            }
            else
            {
                int lootNr = Random.Range(0, ammoDrop.Length);
                Instantiate(ammoDrop[lootNr], dropSpawnPos, Quaternion.identity);
            }
        }
        Instantiate(smallExplosionFX,transform.position,Quaternion.identity);
        parentGate?.ChildRobotDestroyed();
        Destroy(gameObject);
    }
}