using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("玩家")]
    public float AngleOffset = 45f;
    public int ownerId = 1;

    [Header("玩家运动")]
    public float AccelScale = 0.0003f;  // 速度缩放
    public float AccelTurningScaleUp = 2.2f;
    public float AccelTurningScaleDown = 1.7f;
    public Vector3 AccelUp =  new Vector3(1.2f, 1.7f, 0f);
    public Vector3 AccelDown =  new Vector3(1.2f, -1.5f, 0f);
    public Vector3 Friction = new Vector3(0.98f, 0.985f, 1f);
    public Vector3 Accel = new Vector3(0f, 0f, 0f);
    public Vector3 Motion = new Vector3(0.01764f, -0.01f, 0f);

    [Header("NPC运动控制")]
    public float desiredOffsetD = 1.0f;   // 用 x0_npc - x0_player 算出来赋值
    public float kpX = 2.0f;
    public float kdX = 0.0f;
    public float catchMinBias = 0.3f, catchMaxBias = 1.6f;
    public float lockMinBias  = 0.7f, lockMaxBias  = 1.2f;
    public float lockThreshold = 1.0f;
    public bool locked = false;
    public List<Transform> notes = new();
    public int index = 0;
    public Transform target;
    public bool pressUp = false;
    public float yDeadzone = 0.008f;
    public float wanderscale = 0.8f;

    private Transform player;
    private PlayerController pc;
    private int tick = 0;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        pc = player.GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        tick ++;
        if (tick % 5 == 0  && notes.Count != 0)
        {
            target = CurrentTarget(transform.position.x);
            pressUp = VerticalAI(target);
        }
        if (tick % 20 == 0 && notes.Count == 0) BuildCache();
        if (tick % 100 == 0 && notes.Count == 0) pressUp = NoTargetWandering();
        if (tick % 95 == 0 && notes.Count == 0) pressUp = NoTargetWandering();


        StepMovement(pressUp);

        // 根据 Motion 调整朝向
        if (Motion.sqrMagnitude > 0.000001f)   // 防止静止时乱转
        {
            float angle = Mathf.Atan2(Motion.y, Motion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - AngleOffset);
        }

        // DEBUG
        // transform.position = new Vector3(transform.position.x, player.position.y, transform.position.z);

    }


    void StepMovement(bool pressUp)
    {
        float xBias = ComputeXBias();

        if (pressUp)
        {
            Accel = AccelUp * AccelScale;
            if (Accel.y * Motion.y < 0) Accel.y *= AccelTurningScaleUp;
        }
        else
        {
            Accel = AccelDown * AccelScale;
            if (Accel.y * Motion.y < 0) Accel.y *= AccelTurningScaleDown;
        }

        // ★只在这里对 x 做倍率偏置
        Accel.x *= xBias;
        if (!locked) Accel.y *= 0.5f;

        Motion += Accel;
        Motion = new Vector3(Motion.x * Friction.x, Motion.y * Friction.y, Motion.z * Friction.z);
        transform.position += Motion;

    }


    float ComputeXBias()
    {
        float xTarget = player.position.x + desiredOffsetD;
        float e = xTarget - transform.position.x;
        float ev = pc.Motion.x - Motion.x;

        // 进入锁相
        if (!locked && Mathf.Abs(e) < lockThreshold) locked = true;

        float u = kpX * e + kdX * ev;
        float bias = 1f + u;

        if (locked) bias = Mathf.Clamp(bias, lockMinBias, lockMaxBias);
        else        bias = Mathf.Clamp(bias, catchMinBias, catchMaxBias);

        return bias;
    }

    void BuildCache()
    {
        notes.Clear();
        var rings = GameObject.FindObjectsOfType<Ring>(includeInactive: false);
        foreach (var r in rings)
        {
            if (r != null && r.ownerId == ownerId && r.transform.position.x > transform.position.x)
                notes.Add(r.transform);
        }
        notes.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        index = 0;
    }

    Transform CurrentTarget(float npcX)
    {
        // 跳过已经在身后的音符
        while (index < notes.Count && notes[index] != null && notes[index].position.x < npcX)
            index++;

        if (index >= notes.Count)
        {
            notes.Clear();
            // Debug.LogError($"notes Clear!");
            return null;
        }
        return notes[index];
    }

    bool VerticalAI(Transform target)
    {
        if (target == null) return false;

        // 估计到达时间 tau（基于当前 Motion.x）
        float dx = target.position.x - transform.position.x;
        float dy = target.position.y - transform.position.y;
        float vx = Motion.x;
        float tau = dx / vx;
        float vyNeeded = dy / tau;

        float yDeadzonedynamic = yDeadzone * Mathf.Clamp(tau / 50, 0.5f, 1.7f);

        // 防抖：带 deadzone 的滞回
        if (Motion.y < vyNeeded - yDeadzonedynamic) return true;
        else if (Motion.y > vyNeeded + yDeadzonedynamic) return false;
        else return pressUp;
    }

    bool NoTargetWandering()
    {
        float dy = transform.position.y - player.position.y;
        float p = 0.5f - 0.5f * (float) Math.Tanh(dy / wanderscale);
        bool isUp = UnityEngine.Random.value < p;
        return isUp ? true : false;
    }

}
