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
    [Header("References")]
    public Transform player;
    public GameObject[] notePrefab;

    [Header("Spawn settings")]
    public float startX = 5.0f;          // 在玩家前方多远开始放
    public float startY = 0.8f;         // 竖直偏移

    [Header("Visual")]
    public Color[] ownerColors;          // [0]=玩家, [1]=NPC1...
    public Transform tracksRoot;         // 生成的音符挂到这里，方便清理

    [Serializable]
    public class TrackContext
    {
        public MidiNotes data;
        public int ownerId;
        public int prefabId;
        public float speedMult;
        public float fluctuateMult;

        public Transform reference;
        public float v;              // world units / sec
        public float beatInterval;   // world units / beat

        public float xStart;         // beat=0 对应的世界 x 原点（固定）
        public float yBase;

        public float spawnedBeatCursor; // 下次要生成到的 beat（由外部调度）

        public float loopBeats;        // 例如 32
        public float loopDurationSec;  // loopBeats * 60 / bpm
    }

    public int nextNoteId = 1;
    public PerformanceJudge judge;

    void Awake()
    {
        judge = FindObjectOfType<PerformanceJudge>();
    }

    Transform ResolveReference(int ownerId, Transform referenceOverride)
    {
        if (referenceOverride != null) return referenceOverride;

        if (player == null) player = GameObject.FindWithTag("Player")?.transform;

        if (ownerId == 0) return player;
        if (ownerId == 1) return GameObject.Find("NPC1")?.transform;
        if (ownerId == 2) return GameObject.Find("NPC2")?.transform;
        if (ownerId == 3) return GameObject.Find("NPC3")?.transform;
        return player;
    }

    MidiNotes LoadMidi(string midiJsonName)
    {
        TextAsset ta = Resources.Load<TextAsset>($"MidiJson/{midiJsonName}");
        if (ta == null)
        {
            Debug.LogError($"Midi json not found: Resources/MidiJson/{midiJsonName}.json");
            return null;
        }
        return JsonUtility.FromJson<MidiNotes>(ta.text);
    }

    float ComputePlayerSpeedX()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) return 0f;

        var pc = player.GetComponent<PlayerController>();
        if (pc == null) return 0f;

        float motionXPerStep = pc.Motion.x;
        return motionXPerStep / Time.fixedDeltaTime;
    }

    public TrackContext CreateTrack(
        string midiJsonName,
        int ownerId,
        int prefabId,
        float speedMult = 1f,
        float fluctuateMult = 1f,
        Transform reference = null,
        float initialBeatOffset = 0f,
        float loopBeatsOverride = 0f   // <= 新增：0 表示自动计算
    )
    {
        var data = LoadMidi(midiJsonName);
        if (data == null) return null;

        player = GameObject.FindWithTag("Player")?.transform;
        var refTf = ResolveReference(ownerId, reference);
        if (refTf == null || player == null)
        {
            Debug.LogError("Reference/player not found.");
            return null;
        }

        float v = ComputePlayerSpeedX();
        float beatInterval = v * (60f / data.bpm) / speedMult;

        // 固定 xStart：beat=0 的世界 x（关键：之后不要再用当前 reference.position.x 来当起点）
        float xStart = refTf.position.x + startX + initialBeatOffset * beatInterval;

        float yBase = (0.0f * refTf.position.y + 1.0f * player.position.y) + startY;

        // 自动计算这段 midi 的长度（beats）
        float maxBeat = 0f;
        foreach (var n in data.notes)
        {
            float b1 = n.t1 * data.bpm / 60f;
            if (b1 > maxBeat) maxBeat = b1;
        }
        float loopBeats = (loopBeatsOverride > 0f) ? loopBeatsOverride : maxBeat;
        float loopDurationSec = loopBeats * 60f / data.bpm;

        return new TrackContext
        {
            data = data,
            ownerId = ownerId,
            prefabId = prefabId,
            speedMult = speedMult,
            fluctuateMult = fluctuateMult,
            reference = refTf,
            v = v,
            beatInterval = beatInterval,
            xStart = xStart,
            yBase = yBase,
            spawnedBeatCursor = initialBeatOffset,
            loopBeats = loopBeats,
            loopDurationSec = loopDurationSec,
        };
    }

    // 分段生成：按 beat 范围过滤（beatFrom inclusive, beatTo exclusive）
    public List<Transform> SpawnRange(TrackContext ctx, float beatFrom, float beatTo)
    {
        if (ctx == null || ctx.data == null) return new List<Transform>();

        var list = new List<Transform>();
        Transform parent = tracksRoot != null ? tracksRoot : null;

        // 你原来 laneOffsetY 写死 0，这里保留
        float laneOffsetY = ctx.ownerId * (-1.6f);

        foreach (var n in ctx.data.notes)
        {
            // 假设 n.t0/n.t1 单位是 “秒”
            float beat0 = n.t0 * ctx.data.bpm / 60f;
            float beat1 = n.t1 * ctx.data.bpm / 60f;

            // 只要起点落在范围内就生成（也可用 beat1 做更严格裁剪）
            if (beat0 < beatFrom || beat0 >= beatTo) continue;

            float x = ctx.xStart + n.t0 * ctx.v / ctx.speedMult;

            // 让 Fluctuate 更“自洽”：用 x 推 beat，而不是把 x 当秒
            float y = ctx.yBase + laneOffsetY + FluctuateByX(x, ctx.beatInterval, ctx.ownerId) * ctx.fluctuateMult;

            GameObject go = Instantiate(notePrefab[ctx.prefabId], parent);
            go.transform.position = new Vector3(x, y, 0f);

            if (ctx.prefabId == 0)
            {
                var ring = go.GetComponent<Ring>();
                ring.volume = n.v * 0.01f;
                ring.midiPitch = n.p;
                ring.ismidi = true;
                ring.isnpc = (ctx.ownerId != 0);
                ring.ownerId = ctx.ownerId;
                ring.DelPos = 50f;
            }
            else if (ctx.prefabId == 2)
            {
                var tr = go.GetComponent<Trail>();
                tr.volume = n.v * 0.002f;
                tr.midiPitch = n.p;
                tr.sustainTime = (n.t1 - n.t0) / ctx.speedMult;
                float widthscaler = (n.p - 60f) / (83f - 60f);
                tr.tailWidth = Mathf.Lerp(1.2f, 0.8f, widthscaler);
                tr.ismidi = true;
                tr.isnpc = (ctx.ownerId != 0);
                tr.ownerId = ctx.ownerId;
                tr.DelPos = 50f;
            }

            var sr = go.GetComponent<SpriteRenderer>();
            if (ownerColors != null && ownerColors.Length > ctx.ownerId) sr.color = ownerColors[ctx.ownerId];

            float scaler = (n.p - 60f) / (83f - 60f);
            float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
            go.transform.localScale *= scale;

            list.Add(go.transform);

        }

        return list;
    }

public List<Transform> SpawnRangeLoop(TrackContext ctx, float beatFromWorld, float beatToWorld)
{
    var outList = new List<Transform>();
    if (ctx == null || ctx.data == null) return outList;
    if (ctx.loopBeats <= 0.0001f) return outList;

    // 覆盖到的 loop index 范围
    int kStart = Mathf.FloorToInt(beatFromWorld / ctx.loopBeats);
    int kEnd   = Mathf.FloorToInt((beatToWorld - 1e-4f) / ctx.loopBeats);

    for (int k = kStart; k <= kEnd; k++)
    {
        float segFromWorld = Mathf.Max(beatFromWorld, k * ctx.loopBeats);
        float segToWorld   = Mathf.Min(beatToWorld,  (k + 1) * ctx.loopBeats);

        float localFrom = segFromWorld - k * ctx.loopBeats;
        float localTo   = segToWorld   - k * ctx.loopBeats;

        float timeShiftSec = k * ctx.loopDurationSec;

        Transform parent = tracksRoot != null ? tracksRoot : null;
        
        float laneOffsetY = 0;
        if (ctx.ownerId == 1) laneOffsetY = -0.2f;
        if (ctx.ownerId == 2) laneOffsetY = -0.5f;
        if (ctx.ownerId == 3) laneOffsetY = -0.8f;


        foreach (var n in ctx.data.notes)
        {
            float beat0 = n.t0 * ctx.data.bpm / 60f;
            if (beat0 < localFrom || beat0 >= localTo) continue;

            float worldT0 = n.t0 + timeShiftSec;
            float x = ctx.xStart + worldT0 * ctx.v / ctx.speedMult;
            float y = ctx.yBase + laneOffsetY + FluctuateByX(x, ctx.beatInterval, ctx.ownerId) * ctx.fluctuateMult;

            GameObject go = Instantiate(notePrefab[ctx.prefabId], parent);
            go.transform.position = new Vector3(x, y, 0f);

            if (ctx.prefabId == 0)
            {
                var ring = go.GetComponent<Ring>();
                ring.volume = n.v * 0.01f;
                ring.midiPitch = n.p;
                ring.ismidi = true;
                ring.isnpc = (ctx.ownerId != 0);
                ring.ownerId = ctx.ownerId;
                ring.DelPos = 50f;
            }

            if (ctx.prefabId == 2)
            {
                var tr = go.GetComponent<Trail>();
                tr.volume = n.v * 0.002f;
                tr.midiPitch = n.p;
                tr.sustainTime = (n.t1 - n.t0) / ctx.speedMult;
                float widthscaler = (n.p - 60f) / (83f - 60f);
                tr.tailWidth = Mathf.Lerp(1.2f, 0.8f, widthscaler);
                tr.ismidi = true;
                tr.isnpc = (ctx.ownerId != 0);
                tr.ownerId = ctx.ownerId;
                tr.DelPos = 50f;
            }

            var sr = go.GetComponent<SpriteRenderer>();
            if (ownerColors != null && ownerColors.Length > ctx.ownerId) sr.color = ownerColors[ctx.ownerId];

            float scaler = (n.p - 60f) / (83f - 60f);
            float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
            go.transform.localScale *= scale;

            outList.Add(go.transform);

            float beat0Local = n.t0 * ctx.data.bpm / 60f;
            float beat0World = (k * ctx.loopBeats) + beat0Local;

            int noteId = nextNoteId++;

            // 赋 id（Ring/Trail 都要支持这个字段）
            var ri = go.GetComponent<Ring>();
            if (ri != null) ri.noteId = noteId;

            var trail = go.GetComponent<Trail>();
            if (trail != null) trail.noteId = noteId;

            // 注册到 judge（只在 judge 正在跑时才会记）
            if (judge != null)
                judge.RegisterNote(ctx.ownerId, beat0World, noteId);
        }
    }

    return outList;
}


    // 让波动按“世界 x”稳定变化：beats = x / beatInterval
    float FluctuateByX(float xWorld, float beatInterval, int ownerId)
    {
        float beats = xWorld / Mathf.Max(beatInterval, 0.0001f);

        float wave =
            0.55f * Mathf.Sin(2f * Mathf.PI * 0.125f * beats) +
            0.25f * Mathf.Sin(2f * Mathf.PI * 0.0833333f * beats + 1.7f) +
            5f    * Mathf.Sin(2f * Mathf.PI * 0.01f * beats + 3.14f);

        float noiseSeed = 12.345f + ownerId * 100.0f;
        float pn = Mathf.PerlinNoise(noiseSeed, beats * 0.55f);
        float noise = (pn - 0.5f) * 2f;

        return 0.6f * wave + 0.6f * noise;
    }


}
//     public List<Transform> SpawnTrack(
//         string midiJsonName,
//         int ownerId,
//         int prefabId,
//         float speedMult = 1f,
//         float fluctuateMult = 1f,
//         Transform reference = null,
//         float beatoffset = 0f
//     )
//     {
//         // 1) 加载 JSON（推荐放到 Resources/MidiJson/xxx 里）
//         TextAsset ta = Resources.Load<TextAsset>($"MidiJson/{midiJsonName}");
//         if (ta == null)
//         {
//             Debug.LogError($"Midi json not found: Resources/MidiJson/{midiJsonName}.json");
//             return new List<Transform>();
//         }
//         MidiNotes data = JsonUtility.FromJson<MidiNotes>(ta.text);

//         // 2) 推导水平世界速度 v（单位：世界单位/秒）
//         //    你玩家 Motion.x 的稳态： M = (M*Friction.x) + (AccelScale*AccelUp.x)
//         //    => M = a / (1 - fx)
//         //    其中 a = AccelScale * 1
//         player = GameObject.FindWithTag("Player").transform;
//         if (ownerId == 0) reference = player;
//         else if (ownerId == 1) reference = GameObject.Find("NPC1").transform;
//         else if (ownerId == 2) reference = GameObject.Find("NPC2").transform;
//         else if (ownerId == 3) reference = GameObject.Find("NPC3").transform;
//         var pc = player.GetComponent<PlayerController>();
//         float motionXPerStep = pc.Motion.x; // 每 FixedUpdate 位移
//         float v = motionXPerStep / Time.fixedDeltaTime;
//         float beatInterval = v * (60f / data.bpm) / speedMult;

//         // 3) 轨道基准点
//         float x0 = reference.position.x + startX + beatoffset * beatInterval;
//         float y0 = (0.0f * reference.position.y + 1.0f * player.position.y) + startY;

//         // 4) 生成
//         var list = new List<Transform>(data.notes.Count);

//         // 每个 owner 给一个固定 y 偏移，让三 NPC 不挤在一起（你也可以关掉）
//         float laneOffsetY = (ownerId) * 0f; // 按需调
//         Transform parent = tracksRoot != null ? tracksRoot : null;

//         foreach (var n in data.notes)
//         {
//             float x = x0 + n.t0 * v / speedMult;
//             float y = y0 + laneOffsetY + Fluctuate(x0 + n.t0, data.bpm, ownerId) * fluctuateMult;

//             GameObject go = Instantiate(notePrefab[prefabId], parent);
//             go.transform.position = new Vector3(x, y, 0f);

//             if (prefabId == 0)  // Ring
//             {
//                 var ring = go.GetComponent<Ring>();
//                 ring.volume = n.v * 0.01f;
//                 ring.midiPitch = n.p;
//                 ring.ismidi = true;
//                 ring.isnpc = (ownerId != 0);
//                 ring.ownerId = ownerId;
//             }

//             if (prefabId == 2)  // Trail
//             {
//                 var ring = go.GetComponent<Trail>();
//                 ring.volume = n.v * 0.002f;
//                 ring.midiPitch = n.p;
//                 ring.sustainTime = (n.t1 - n.t0) / speedMult ;
//                 float widthscaler = (n.p - 60f) / (83f - 60f);
//                 ring.tailWidth = Mathf.Lerp(1.2f, 0.8f, widthscaler);
//                 ring.ismidi = true;
//                 ring.isnpc = (ownerId != 0);
//                 ring.ownerId = ownerId;
//             }
            
//             var sr = go.GetComponent<SpriteRenderer>();
//             if (ownerColors != null && ownerColors.Length > ownerId) sr.color = ownerColors[ownerId];
//             // 缩放按 pitch（你原来的逻辑）
//             float scaler = (n.p - 60f) / (83f - 60f);
//             float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
//             go.transform.localScale *= scale;
//             list.Add(go.transform);
//         }

//         return list;
//     }


//     float Fluctuate(float t, float bpm, int ownerId)
//     {
//         float beats = t * bpm / 60f;

//         float wave =
//             0.55f * Mathf.Sin(2f * Mathf.PI * 0.125f * beats) +
//             0.25f * Mathf.Sin(2f * Mathf.PI * 0.0833333f * beats + 1.7f) +
//             5f    * Mathf.Sin(2f * Mathf.PI * 0.01f * beats + 3.2f);

//         // 用 ownerId 做种子偏移，保证不同 NPC 波动不同但稳定
//         float noiseSeed = 12.345f + ownerId * 100.0f;
//         float noiseFreq = 0.55f;
//         float pn = Mathf.PerlinNoise(noiseSeed, beats * noiseFreq);
//         float noise = (pn - 0.5f) * 2f;

//         const float waveAmp = 0.6f;
//         const float noiseAmp = 0.6f;

//         return waveAmp * wave + noiseAmp * noise;
//     }
// }
