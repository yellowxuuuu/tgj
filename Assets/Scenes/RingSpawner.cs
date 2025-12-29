using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingSpawner : MonoBehaviour
{
    public GameObject ringPrefab;
    public float spawnInterval = 1.0f;
    public float verticalRangemax = 4f;
    public float verticalRangemin = 24f;
    public float startX = 8f;   // spawn ahead of player
    public int ringCount = 30;  // number of rings to spawn

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
        float xrandPos = Random.Range(-1f, 1f);
        Vector3 pos = new Vector3(
            player.position.x + startX + xrandPos,
            player.position.y + Random.Range(-verticalRangemin, verticalRangemax),
            0
        );

        Instantiate(ringPrefab, pos, Quaternion.identity);
    }
}
