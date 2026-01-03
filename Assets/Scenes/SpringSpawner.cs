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
    public float avoidRadius = 1.0f;
    public LayerMask ringLayer;    

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        ringLayer = LayerMask.GetMask("Ring");
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
        float randomAngle = Random.Range(AngleMin, AngleMax);
        Quaternion rotation = Quaternion.Euler(0, 0, -randomAngle);

        if (HasMidiRingNearby(pos, avoidRadius)) return;

        Instantiate(ringPrefab, pos, rotation);
    }

    bool HasMidiRingNearby(Vector3 pos, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        foreach (var h in hits)
        {
            var r = h.GetComponent<Ring>();
            if (r != null && r.ismidi)
                return true;
        }
        return false;
    }
    
}
