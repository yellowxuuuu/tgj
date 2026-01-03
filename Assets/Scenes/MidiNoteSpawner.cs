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
    public GameObject notePrefab;

    [Header("Spawn settings")]
    public float startX = 5.0f;          // 在玩家前方多远开始放
    public float startY = 0.8f;         // 竖直偏移

    [Header("Visual")]
    public Color[] ownerColors;          // [0]=玩家, [1]=NPC1...
    public Transform tracksRoot;         // 生成的音符挂到这里，方便清理


    public List<Transform> SpawnTrack(
        string midiJsonName,
        int ownerId,
        float speedMult = 1f,
        float fluctuateMult = 1f,
        Transform reference = null
    )
    {
        // 1) 加载 JSON（推荐放到 Resources/MidiJson/xxx 里）
        TextAsset ta = Resources.Load<TextAsset>($"MidiJson/{midiJsonName}");
        if (ta == null)
        {
            Debug.LogError($"Midi json not found: Resources/MidiJson/{midiJsonName}.json");
            return new List<Transform>();
        }
        MidiNotes data = JsonUtility.FromJson<MidiNotes>(ta.text);

        // 2) 推导水平世界速度 v（单位：世界单位/秒）
        //    你玩家 Motion.x 的稳态： M = (M*Friction.x) + (AccelScale*AccelUp.x)
        //    => M = a / (1 - fx)
        //    其中 a = AccelScale * 1
        player = GameObject.FindWithTag("Player").transform;
        if (ownerId == 0) reference = player;
        else if (ownerId == 1) reference = GameObject.Find("NPC1").transform;
        else if (ownerId == 2) reference = GameObject.Find("NPC2").transform;
        else if (ownerId == 3) reference = GameObject.Find("NPC3").transform;
        var pc = player.GetComponent<PlayerController>();
        float motionXPerStep = pc.Motion.x; // 每 FixedUpdate 位移
        float v = motionXPerStep / Time.fixedDeltaTime;
        float beatInterval = v * (60f / data.bpm);

        // 3) 轨道基准点
        float x0 = reference.position.x + startX;
        float y0 = (0.2f * reference.position.y + 0.8f * player.position.y) + startY;

        // 4) 生成
        var list = new List<Transform>(data.notes.Count);

        // 每个 owner 给一个固定 y 偏移，让三 NPC 不挤在一起（你也可以关掉）
        float laneOffsetY = (ownerId) * 0f; // 按需调
        Transform parent = tracksRoot != null ? tracksRoot : null;

        foreach (var n in data.notes)
        {
            float x = x0 + n.t0 * v / speedMult;
            float y = y0 + laneOffsetY + Fluctuate(n.t0, data.bpm, ownerId) * fluctuateMult;

            GameObject go = Instantiate(notePrefab, parent);
            go.transform.position = new Vector3(x, y, 0f);

            var ring = go.GetComponent<Ring>();
            ring.midiPitch = n.p;
            ring.ismidi = true;
            ring.isnpc = (ownerId != 0);
            ring.ownerId = ownerId;

            var sr = go.GetComponent<SpriteRenderer>();
            if (ownerColors != null && ownerColors.Length > ownerId) sr.color = ownerColors[ownerId];
            // 缩放按 pitch（你原来的逻辑）
            float scaler = (ring.midiPitch - 60f) / (83f - 60f);
            float scale = Mathf.Lerp(1.2f, 0.8f, scaler);
            go.transform.localScale *= scale;
            list.Add(go.transform);
        }

        return list;
    }


    float Fluctuate(float t, float bpm, int ownerId)
    {
        float beats = t * bpm / 60f;

        float wave =
            0.65f * Mathf.Sin(2f * Mathf.PI * 0.125f * beats) +
            0.25f * Mathf.Sin(2f * Mathf.PI * 0.0833333f * beats + 1.7f) +
            6f    * Mathf.Sin(2f * Mathf.PI * 0.01f * beats + 3.2f);

        // 用 ownerId 做种子偏移，保证不同 NPC 波动不同但稳定
        float noiseSeed = 12.345f + ownerId * 100.0f;
        float noiseFreq = 0.55f;
        float pn = Mathf.PerlinNoise(noiseSeed, beats * noiseFreq);
        float noise = (pn - 0.5f) * 2f;

        const float waveAmp = 0.6f;
        const float noiseAmp = 0.6f;

        return waveAmp * wave + noiseAmp * noise;
    }
}
