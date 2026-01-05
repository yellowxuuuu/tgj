using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceJudge : MonoBehaviour
{
    public bool IsRunning { get; private set; }
    public bool LastSuccess { get; private set; }

    int targetOwnerId;
    float beatFrom, beatTo;
    int allowedMiss;

    // 只统计当前段落内的 noteId
    readonly HashSet<int> pending = new HashSet<int>();
    readonly HashSet<int> resolved = new HashSet<int>();

    int hitCount;
    int missCount;

    public void BeginWindow(int ownerId, float fromBeat, float toBeat, int allowedMiss_)
    {
        targetOwnerId = ownerId;
        beatFrom = fromBeat;
        beatTo = toBeat;
        allowedMiss = allowedMiss_;

        pending.Clear();
        resolved.Clear();
        hitCount = 0;
        missCount = 0;

        IsRunning = true;
        LastSuccess = false;
    }

    // 由 Spawner 在生成时注册（只注册落在窗口内的）
    public void RegisterNote(int ownerId, float worldBeat, int noteId)
    {
        if (!IsRunning) return;
        if (ownerId != targetOwnerId) return;
        if (worldBeat < beatFrom || worldBeat >= beatTo) return;

        pending.Add(noteId);
    }

    public void ReportHit(int ownerId, int noteId)
    {
        if (!IsRunning) return;
        if (ownerId != targetOwnerId) return;
        if (resolved.Contains(noteId)) return;

        resolved.Add(noteId);
        if (pending.Contains(noteId)) pending.Remove(noteId);
        hitCount++;
    }

    public void ReportMiss(int ownerId, int noteId)
    {
        if (!IsRunning) return;
        if (ownerId != targetOwnerId) return;
        if (resolved.Contains(noteId)) return;

        resolved.Add(noteId);
        if (pending.Contains(noteId)) pending.Remove(noteId);
        missCount++;
    }

    // 给 NoteAssignment 轮询用：是否已经判定结束
    public bool IsFailed => IsRunning && missCount > allowedMiss;

    public bool IsDone(float currentWorldBeat, float extraBeats = 2f)
    {
        if (!IsRunning) return true;

        // // 1) 如果 miss 超了，立刻结束
        // if (missCount > allowedMiss)
        // {
        //     LastSuccess = false;
        //     IsRunning = false;
        //     return true;
        // }

        // 2) 玩家已经走过窗口末尾一小段后（extraBeats），就可以判定结束
        if (currentWorldBeat >= beatTo + extraBeats)
        {
            // 还没被 resolved 的 pending，都按 miss 算（防止漏通知）
            missCount += pending.Count;
            pending.Clear();

            LastSuccess = (missCount <= allowedMiss);
            IsRunning = false;
            return true;
        }

        // // 3) 或者已经把窗口内所有 note 都 resolved，也可以提前结束
        // if (pending.Count == 0)
        // {
        //     LastSuccess = (missCount <= allowedMiss);
        //     IsRunning = false;
        //     return true;
        // }

        return false;
    }

    public string DebugInfo => $"Hit={hitCount}, Miss={missCount}, Pending={pending.Count}";
}
