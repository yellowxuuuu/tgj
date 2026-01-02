using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("Spawner settings")]
    public float startTime = 7f;
    public float BeatInterval = 1f;

    [Header("Sound settings")]
    public float BPM = 120f;
    public float midiTimeDivision = 480f;

    private Transform player;
    // private Coroutine StartSpawner;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        StartCoroutine(StartSpawner(startTime));
    }




    IEnumerator StartSpawner(float t)  // 协程：中止绘制计时器
    {
        yield return new WaitForSeconds(t);

        PlayerController playerController = player.GetComponent<PlayerController>();
        float playerXMotion = playerController.Motion.x;
        BeatInterval = 60f / BPM / 0.02f * playerXMotion;

    }


}
