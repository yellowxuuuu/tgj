using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteAssignment : MonoBehaviour
{
    public MidiNoteSpawner spawner;
    public float speedMult = 0.8f;

    [Header("Timing")]
    public float startSilenceSeconds = 20f;   // 阶段1：无音符
    public float retryDelaySeconds = 5f;      // 失败后等待
    public float npcEntranceSeconds = 15f;    // 阶段3：NPC进场持续时间
    public int allowedMiss = 1;               // 容错1

    [Header("Refs")]
    public Transform npc1;                    // 直接拖拽NPC1
    public Transform npc2;                    // 直接拖拽NPC1
    public Transform npc3;                    // 直接拖拽NPC1
    // public PerformanceJudge judge;            // 可不填，会自动 GetComponent

    [Header("Spawn loop")]
    public float lookAheadBeats = 16f; // 保证前方永远有 16 beats 的音符
    public float chunkBeats = 16f;      // 每次生成 1 小节(4 beats)；也可改 16

    [Header("Duet settings")]
    public float duetPlayerFromBeat = 0f;
    public float duetPlayerToBeat = 32f;
    public float duetNpcOffsetBeat = 16f; // NPC 相对玩家错开

    public PerformanceJudge judge;
    public GameObject thanksUI;

    // ===== 关卡状态机 =====
    enum LevelState { Intro, Solo, NPC1Entrance, Duet, NPC2Entrance, Trio, NPC3Entrance, Quartet}
    LevelState state;

    MidiNoteSpawner.TrackContext playerTrack;
    MidiNoteSpawner.TrackContext npc1Track;
    MidiNoteSpawner.TrackContext npc2Track;
    MidiNoteSpawner.TrackContext npc3Track;

    Transform player;

    void Start()
    {
        spawner = GetComponent<MidiNoteSpawner>();
        player = GameObject.FindWithTag("Player")?.transform;
        npc1 = GameObject.Find("NPC1").transform;
        npc2 = GameObject.Find("NPC2").transform;
        npc3 = GameObject.Find("NPC3").transform;
        npc1.gameObject.SetActive(false);
        npc2.gameObject.SetActive(false);
        npc3.gameObject.SetActive(false);
        judge = GetComponent<PerformanceJudge>();
        // 进入关卡
        EnterState(LevelState.Intro);
    }

    void EnterState(LevelState s)
    {
        state = s;

        StopAllCoroutines();

        switch (state)
        {
            case LevelState.Intro:
                StartCoroutine(IntroFlow());
                break;
            case LevelState.Solo:
                StartCoroutine(SoloFlow());
                break;
            case LevelState.NPC1Entrance:
                StartCoroutine(NPC1EntranceFlow());
                break;
            case LevelState.Duet:
                StartCoroutine(DuetFlow());
                break;
            case LevelState.NPC2Entrance:
                StartCoroutine(NPC2EntranceFlow());
                break;
            case LevelState.Trio:
                StartCoroutine(TrioFlow());
                break;
            case LevelState.NPC3Entrance:
                StartCoroutine(NPC3EntranceFlow());
                break;
            case LevelState.Quartet:
                StartCoroutine(QuartetFlow());
                break;
        }
    }

    IEnumerator IntroFlow()
    {
        // 例：等 20 秒再开歌
        yield return new WaitForSeconds(startSilenceSeconds);
        // 创建轨道上下文（只创建一次）


        // 先预生成一段（可选）
        // PrimeSpawn(playerTrack, lookAheadBeats);
        // PrimeSpawn(npc1Track, lookAheadBeats);
        EnterState(LevelState.Solo);
    }

    IEnumerator SoloFlow()
    {
        Debug.Log("Solo");
        // 清理旧音符（如果你有 tracksRoot）
        ClearAllSpawnedNotes(); 

        // 2) 重新锚定：每次重试都重新创建 TrackContext（关键）
        playerTrack = spawner.CreateTrack(
            "Canon",
            ownerId: 0,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 1f,
            reference: null,
            initialBeatOffset: 0f,
            loopBeatsOverride: 32f
        );
        

        judge.BeginWindow(ownerId: 0, fromBeat: 0f, toBeat: 16f, allowedMiss_: allowedMiss);

        // 只生成 0~16 beat（一次性）
        spawner.SpawnRangeLoop(playerTrack, 0f, 16f);

        // 等待判定结束
        while (!judge.IsDone(GetCurrentBeat(playerTrack), extraBeats: 2f))
            yield return null;

        Debug.Log("Solo result: " + judge.LastSuccess + " " + judge.DebugInfo);

        if (judge.LastSuccess)
        {
            EnterState(LevelState.NPC1Entrance);
        }
        else
        {
            yield return new WaitForSeconds(retryDelaySeconds);
            EnterState(LevelState.Solo);
        }
    }

    IEnumerator NPC1EntranceFlow()
    {
        Debug.Log("NPC1Entrance");

        // 1) 启用 NPC1
        if (npc1 != null && !npc1.gameObject.activeSelf)
            npc1.gameObject.SetActive(true);

        // 2) 放到玩家前方 6f（你想“移到前方6f的位置”）
        PlaceNpcInFront(npc1, forwardX: 6f, yOffset: 0f);

        // 3) 等待入场时间（用你暴露的 npcEntranceSeconds）
        yield return new WaitForSeconds(npcEntranceSeconds);

        EnterState(LevelState.Duet);
    }

    IEnumerator DuetFlow()
    {
        Debug.Log("Duet");

        // 1) 清场（重要：否则旧音符会影响判定/视觉）
        ClearAllSpawnedNotes();

        // 2) 重新锚定：每次进入/重试都重新 CreateTrack（关键）
        playerTrack = spawner.CreateTrack(
            "Canon",
            ownerId: 0,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 1f,
            reference: null,
            initialBeatOffset: 0f,      // 玩家从 0 beat 作为本段的时间原点
            loopBeatsOverride: 32f
        );

        npc1Track = spawner.CreateTrack(
            "Canon",
            ownerId: 1,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 0.7f,
            reference: npc1,            // 用 npc1 作为 reference
            initialBeatOffset: 16f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        // 3) 开始评判：只判玩家 0~32 beat
        judge.BeginWindow(ownerId: 0, fromBeat: duetPlayerFromBeat, toBeat: 32f, allowedMiss_: 3);

        // 4) 生成本段音符（一次性）
        //    玩家 0~32
        spawner.SpawnRangeLoop(playerTrack, duetPlayerFromBeat, 32f);

        //    NPC 0~32（但他的 initialBeatOffset=16，所以会在世界上落在 16~48 beat 的位置）
        spawner.SpawnRangeLoop(npc1Track, duetPlayerFromBeat, 16f);

        // 5) 等判定结束：以玩家 track 的 world beat 为基准
        while (!judge.IsDone(GetCurrentBeat(playerTrack), extraBeats: 2f))
            yield return null;

        Debug.Log("Duet result: " + judge.LastSuccess + " " + judge.DebugInfo);

        if (judge.LastSuccess)
        {
            EnterState(LevelState.NPC2Entrance);
        }
        else
        {
            yield return new WaitForSeconds(retryDelaySeconds);
            EnterState(LevelState.Duet); // 重来
        }
    }

    IEnumerator NPC2EntranceFlow()
    {
        Debug.Log("NPC2Entrance");

        // 1) 启用 NPC1
        if (npc2 != null && !npc2.gameObject.activeSelf)
            npc2.gameObject.SetActive(true);

        // 2) 放到玩家前方 6f（你想“移到前方6f的位置”）
        PlaceNpcInFront(npc2, forwardX: 6.5f, yOffset: 0f);

        // 3) 等待入场时间（用你暴露的 npcEntranceSeconds）
        yield return new WaitForSeconds(npcEntranceSeconds);

        EnterState(LevelState.Trio);
    }


    IEnumerator TrioFlow()
    {
        Debug.Log("Trio");

        // 1) 清场（重要：否则旧音符会影响判定/视觉）
        ClearAllSpawnedNotes();

        // 2) 重新锚定：每次进入/重试都重新 CreateTrack（关键）
        playerTrack = spawner.CreateTrack(
            "Canon",
            ownerId: 0,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 1f,
            reference: null,
            initialBeatOffset: 0f,      // 玩家从 0 beat 作为本段的时间原点
            loopBeatsOverride: 32f
        );

        npc1Track = spawner.CreateTrack(
            "Canon",
            ownerId: 1,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 0.7f,
            reference: npc1,            // 用 npc1 作为 reference
            initialBeatOffset: 16f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        npc2Track = spawner.CreateTrack(
            "Canon4",
            ownerId: 2,
            prefabId: 0,
            speedMult: speedMult,
            fluctuateMult: 0.7f,
            reference: npc2,            // 用 npc1 作为 reference
            initialBeatOffset: 32f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        // 3) 开始评判：只判玩家 0~32 beat
        judge.BeginWindow(ownerId: 0, fromBeat: duetPlayerFromBeat, toBeat: 64, allowedMiss_: 5);

        // 4) 生成本段音符（一次性）
        //    玩家 0~32
        spawner.SpawnRangeLoop(playerTrack, duetPlayerFromBeat, 64f);

        //    NPC 0~32（但他的 initialBeatOffset=16，所以会在世界上落在 16~48 beat 的位置）
        spawner.SpawnRangeLoop(npc1Track, duetPlayerFromBeat, 48f);
        spawner.SpawnRangeLoop(npc2Track, duetPlayerFromBeat, 32f);

        // 5) 等判定结束：以玩家 track 的 world beat 为基准
        while (!judge.IsDone(GetCurrentBeat(playerTrack), extraBeats: 2f))
            yield return null;

        Debug.Log("Duet result: " + judge.LastSuccess + " " + judge.DebugInfo);

        if (judge.LastSuccess)
        {
            EnterState(LevelState.NPC3Entrance);
        }
        else
        {
            yield return new WaitForSeconds(retryDelaySeconds);
            EnterState(LevelState.Trio); // 重来
        }
    }


    IEnumerator NPC3EntranceFlow()
    {
        Debug.Log("NPC3Entrance");

        // 1) 启用 NPC3
        if (npc3 != null && !npc3.gameObject.activeSelf)
            npc3.gameObject.SetActive(true);

        // 2) 放到玩家前方 6f（你想“移到前方6f的位置”）
        PlaceNpcInFront(npc3, forwardX: 6.5f, yOffset: 0f);

        // 3) 等待入场时间（用你暴露的 npcEntranceSeconds）
        yield return new WaitForSeconds(npcEntranceSeconds);

        EnterState(LevelState.Quartet);
    }


    IEnumerator QuartetFlow()
    {
        Debug.Log("Quartet");

        // 1) 清场（重要：否则旧音符会影响判定/视觉）
        ClearAllSpawnedNotes();

        // 2) 重新锚定：每次进入/重试都重新 CreateTrack（关键）
        playerTrack = spawner.CreateTrack(
            "Canon5",
            ownerId: 0,
            prefabId: 0,
            speedMult: speedMult,
            fluctuateMult: 1f,
            reference: null,
            initialBeatOffset: 32f,      // 玩家从 0 beat 作为本段的时间原点
            loopBeatsOverride: 32f
        );

        npc1Track = spawner.CreateTrack(
            "Canon",
            ownerId: 1,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 0.7f,
            reference: npc1,            // 用 npc1 作为 reference
            initialBeatOffset: 16f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        npc2Track = spawner.CreateTrack(
            "Canon4",
            ownerId: 2,
            prefabId: 0,
            speedMult: speedMult,
            fluctuateMult: 0.7f,
            reference: npc2,            // 用 npc1 作为 reference
            initialBeatOffset: 32f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        npc3Track = spawner.CreateTrack(
            "Canon",
            ownerId: 3,
            prefabId: 2,
            speedMult: speedMult,
            fluctuateMult: 0.6f,
            reference: npc3,            // 用 npc1 作为 reference
            initialBeatOffset: 0f, // NPC 延后 16 beat
            loopBeatsOverride: 32f
        );

        // 3) 开始评判：只判玩家 0~32 beat
        judge.BeginWindow(ownerId: 0, fromBeat: 0f, toBeat: 96f, allowedMiss_: 10);

        // 4) 生成本段音符（一次性）
        //    玩家 0~32
        spawner.SpawnRangeLoop(playerTrack, duetPlayerFromBeat, 96f);

        //    NPC 0~32（但他的 initialBeatOffset=16，所以会在世界上落在 16~48 beat 的位置）
        spawner.SpawnRangeLoop(npc1Track, duetPlayerFromBeat, 64f+48f);
        spawner.SpawnRangeLoop(npc2Track, duetPlayerFromBeat, 64f+32f);
        spawner.SpawnRangeLoop(npc3Track, duetPlayerFromBeat, 64f+64f);

        // 5) 等判定结束：以玩家 track 的 world beat 为基准
        while (!judge.IsDone(GetCurrentBeat(playerTrack), extraBeats: 2f))
            yield return null;

        Debug.Log("Duet result: " + judge.LastSuccess + " " + judge.DebugInfo);

        if (judge.LastSuccess)
        {
            // EnterState(LevelState.NPC3Entrance);
            Debug.Log("GameOver!");
            thanksUI.SetActive(true);

        }
        else
        {
            yield return new WaitForSeconds(retryDelaySeconds);
            EnterState(LevelState.Trio); // 重来
        }
    }

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// 函数
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~



    void PrimeSpawn(MidiNoteSpawner.TrackContext ctx, float beats)
    {
        if (ctx == null) return;
        float from = ctx.spawnedBeatCursor;
        float to = from + beats;
        spawner.SpawnRangeLoop(ctx, from, to);
        ctx.spawnedBeatCursor = to;
    }

    IEnumerator SpawnLoop(MidiNoteSpawner.TrackContext ctx)
    {
        if (ctx == null) yield break;

        while (true)
        {
            // 用玩家的位置估计“当前 beat”
            float currentBeat = GetCurrentBeat(ctx);
            float targetBeat = currentBeat + lookAheadBeats;

            while (ctx.spawnedBeatCursor < targetBeat)
            {
                float from = ctx.spawnedBeatCursor;
                float to = from + chunkBeats;
                spawner.SpawnRangeLoop(ctx, from, to);
                ctx.spawnedBeatCursor = to;
            }

            // 每隔一小段检查一次就够了
            yield return new WaitForSeconds(0.25f);
        }
    }

    float GetCurrentBeat(MidiNoteSpawner.TrackContext ctx)
    {
        if (player == null) return 0f;

        // 以 xStart 为 beat=0 原点，把玩家 x 投影到 beat
        float dx = player.position.x - ctx.xStart;
        return dx /ctx.beatInterval;
    }

    void ClearAllSpawnedNotes()
    {

        foreach (var n in FindObjectsOfType<Ring>()) 
            if (n.ismidi) Destroy(n.gameObject);
        foreach (var t in FindObjectsOfType<Trail>())
            if (t.ismidi) Destroy(t.gameObject);
        
    }

    void PlaceNpcInFront(Transform npc, float forwardX = 6f, float yOffset = 0f)
    {
        if (npc == null || player == null) return;
        Vector3 p = player.position;
        npc.position = new Vector3(p.x + forwardX, p.y + yOffset, npc.position.z);
    }












}

