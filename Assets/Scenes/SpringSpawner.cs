using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringSpawner : MonoBehaviour
{
    public GameObject ringPrefab;
    public float spawnInterval = 1.0f;
    public float verticalRangemax = 4f;
    public float verticalRangemin = 24f;
    public float startX = 8.5f;   // spawn ahead of player
    public int ringCount = 1;  // number of rings to spawn

    public float AngleMin = 0f;
    public float AngleMax = 0f;

    private float timer = 0f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            for (int i = 0; i < ringCount; i++)
            {
                SpawnRing();
            }
        }
    }

    void SpawnRing()
    {
        Vector3 pos = new Vector3(
            player.position.x + startX,
            player.position.y + Random.Range(-verticalRangemin, verticalRangemax),
            0
        );
        float randomAngle = Random.Range(90f + AngleMin, 90f + AngleMax);
        Quaternion rotation = Quaternion.Euler(0, 0, -randomAngle);
        Instantiate(ringPrefab, pos, rotation);
    }
}
