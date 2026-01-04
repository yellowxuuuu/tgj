using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingSpawner : MonoBehaviour
{
    public GameObject ringPrefab;
    public float spawnInterval = 1.0f;
    public float horizonalRange = 0f;
    public float verticalRangemax = 24f;
    public float verticalRangemin = 24f;
    public float startX = 6f;   // spawn ahead of player
    public int ringCount = 30;  // number of rings to spawn

    public float avoidRadius = 1.0f;          // 检测半径（按你的单位调）
    public LayerMask ringLayer;               // 把 Ring 的 collider 放在这个 layer 上

    private float timer = 0f;
    private Transform player;

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
        float xrandPos = Random.Range(-horizonalRange, horizonalRange);
        Vector3 pos = new Vector3(
            player.position.x + startX + xrandPos,
            player.position.y + Random.Range(-verticalRangemin, verticalRangemax),
            0
        );

        if (HasMidiRingNearby(pos, avoidRadius))
        {
            // 附近已有 MIDI 音符，直接不生成
            return;
        }

        GameObject obj = Instantiate(ringPrefab, pos, Quaternion.identity);
        var ring = obj.GetComponent<Ring>();
        ring.midiPitch = (int) Random.Range(60, 83);
        float scaler =  (ring.midiPitch - 60f) / (83f - 60f);
        float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
        obj.transform.localScale *= scale;
    }


    bool HasMidiRingNearby(Vector3 pos, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        foreach (var h in hits)
        {
            var r = h.GetComponent<Ring>();
            var s = h.GetComponent<Spring>();
            var t = h.GetComponent<Trail>();
            if (r != null &&  r.ismidi ||
                s != null &&  s.ismidi ||
                t != null &&  t.ismidi )
                return true;
        }
        return false;
    }

}
