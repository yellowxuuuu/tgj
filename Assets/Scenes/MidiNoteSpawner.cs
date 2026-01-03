using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Note {
    public float t0;
    public float t1;
    public int p;
    public int v;
}

[Serializable]
public class MidiNotes {
    public float bpm;
    public List<Note> notes;
}

public class MidiNoteSpawner : MonoBehaviour
{
    [Header("Input JSON")]
    public TextAsset midiJson;

    [Header("References")]
    public Transform player;
    public GameObject notePrefab;

    [Header("Timing")]
    public float musicStartDelay = 5f;     // 你要不要延迟开播
    public float spawnLeadTime = 2.0f;     // 提前多少秒生成
    public float startX = 6.5f;              // spawn ahead of player
    public float speed = 1f;

    [Header("Ring")]
    public Color initColor = Color.yellow;

    private MidiNotes data;
    private float bpm;
    private float v;                 // 世界速度（单位/秒）
    private float beatInterval;      // 每拍世界距离
    private float startDspTime;

    void Start()
    {
        // 1) 解析 JSON（JsonUtility 要求字段名一致）
        data = JsonUtility.FromJson<MidiNotes>(midiJson.text);
        bpm = data.bpm;

        // 2) 从 player Motion 推速度
        player = GameObject.FindWithTag("Player").transform;
        var pc = player.GetComponent<PlayerController>();
        float motionXPerStep = pc.Motion.x; // 每 FixedUpdate 位移
        v = motionXPerStep / Time.fixedDeltaTime;

        // 3) 你要的 BeatInterval
        beatInterval = v * (60f / bpm);
        Debug.Log($"bpm={bpm}, v={v}, beatInterval={beatInterval}");

        // 4) 记录起始时间（也可以用 AudioSettings.dspTime 做更稳的音画同步）
        // startDspTime = (float)AudioSettings.dspTime + musicStartDelay;

        // 5) 直接一次性生成所有 note 的“静态位置”（如果你是滚动轨道式）

        StartCoroutine(SpawnAllNotesStatic(musicStartDelay));
    }

    float SongTime()
    {
        return (float)AudioSettings.dspTime - startDspTime;
    }

    IEnumerator SpawnAllNotesStatic(float t)
    {
        yield return new WaitForSeconds(t);

        // 选择一个“t=0 对应的基准点”：这里用 hitLine 的位置
        float x0 = player.position.x + startX;
        float y0 = player.position.y;

        foreach (var n in data.notes)
        {
            // 推荐：用秒映射位置
            float x = x0 + n.t0 * v / speed;
            float y = y0 + Fluctuate(n.t0, data.bpm);

            var go = Instantiate(notePrefab);
            go.transform.position = new Vector3(x, y, 0f);
            var ring = go.GetComponent<Ring>();
            ring.midiPitch = n.p;
            ring.ismidi = true;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = initColor;
            float scaler =  (ring.midiPitch - 60f) / (83f - 60f);
            float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
            go.transform.localScale *= scale;
            // 你可以把 pitch/vel 存到 note 脚本里
            // go.GetComponent<NoteView>().Init(n.p, n.v, n.t0, n.t1);
        }
    }

    // 让生成的音符上下起伏
    float Fluctuate(float t, float bpm)
    {
        // t: seconds since song start (e.g. n.t0)
        // bpm: beats per minute

        // ---- 1) 秒 -> 拍（beats）
        float beats = t * bpm / 60f;

        // ---- 2) 主波：两条低频正弦叠加（按拍走）
        // 频率单位：cycles per beat（每拍多少个周期）
        // 例如 0.125 = 8拍一个周期；0.0833 ≈ 12拍一个周期
        float wave =
            0.75f * Mathf.Sin(2f * Mathf.PI * 0.125f * beats) +
            0.25f * Mathf.Sin(2f * Mathf.PI * 0.0833333f * beats + 1.7f) +
            6f    * Mathf.Sin(2f * Mathf.PI * 0.01f * beats + 3.2f);

        // ---- 3) 小噪声：Perlin（连续、不会跳）
        // PerlinNoise 输出 [0,1] -> 转成 [-1,1]
        const float noiseSeed = 12.345f;     // 固定种子，保证同一首歌每次生成一致
        const float noiseFreq = 0.55f;       // 噪声频率（按拍的尺度），越小越平滑
        float pn = Mathf.PerlinNoise(noiseSeed, beats * noiseFreq);
        float noise = (pn - 0.5f) * 2f;

        // ---- 4) 振幅（你可按场景尺度调整）
        const float waveAmp = 0.6f;          // 主波幅度（单位：世界坐标）
        const float noiseAmp = 0.2f;        // 噪声幅度（比主波小）

        // 返回 y 偏移量（你外面用 y = y0 + Fluctuate(...)）
        return waveAmp * wave + noiseAmp * noise - 0.5f;
    }
}