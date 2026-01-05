import json
import mido
from collections import defaultdict

def us_per_qn_to_bpm(us_per_qn: int) -> float:
    return 60_000_000 / us_per_qn

def build_tempo_segments(mid: mido.MidiFile):
    """
    返回 tempo 段：[(start_tick, start_sec, us_per_qn), ...]
    用来把任意 tick 转成秒（支持 tempo change）。
    """
    TPQ = mid.ticks_per_beat
    tempo = 500000  # default 120 BPM
    abs_tick = 0
    abs_sec = 0.0
    segments = [(0, 0.0, tempo)]

    for msg in mid:  # 合并后的按时间排序事件流
        dt = msg.time
        abs_tick += dt
        abs_sec += mido.tick2second(dt, TPQ, tempo)
        if msg.type == "set_tempo":
            tempo = msg.tempo
            segments.append((abs_tick, abs_sec, tempo))

    return segments

def tick_to_sec(tick: int, TPQ: int, tempo_segments):
    # 找最后一个 start_tick <= tick 的段（线性扫够用了）
    seg = tempo_segments[0]
    for s in tempo_segments:
        if s[0] <= tick:
            seg = s
        else:
            break
    t0, s0, tempo = seg
    return s0 + mido.tick2second(tick - t0, TPQ, tempo)

def midi_to_min_json(midi_path: str, json_path: str):
    mid = mido.MidiFile(midi_path)
    TPQ = mid.ticks_per_beat

    tempo_segments = build_tempo_segments(mid)
    first_bpm = us_per_qn_to_bpm(tempo_segments[0][2])  # 仅用于调试/显示

    notes = []

    # 把所有轨的 note 合并成一个列表（你要“更省”）
    for track in mid.tracks:
        abs_tick = 0
        pending = defaultdict(list)  # (channel, pitch) -> [(start_tick, vel), ...]

        for msg in track:
            abs_tick += msg.time

            # Note On (vel>0) 记下起点
            if msg.type == "note_on" and msg.velocity > 0:
                pending[(msg.channel, msg.note)].append((abs_tick, msg.velocity))

            # Note Off 或 Note On vel=0 视作结束
            elif msg.type == "note_off" or (msg.type == "note_on" and msg.velocity == 0):
                key = (msg.channel, msg.note)
                if pending[key]:
                    st_tick, vel = pending[key].pop(0)
                    et_tick = abs_tick

                    t0 = tick_to_sec(st_tick, TPQ, tempo_segments)
                    t1 = tick_to_sec(et_tick, TPQ, tempo_segments)

                    notes.append({
                        "t0": round(t0, 6),
                        "t1": round(t1, 6),
                        "p": msg.note,
                        "v": vel
                    })

    # 按开始时间排序（Unity spawn 会很舒服）
    notes.sort(key=lambda x: x["t0"])

    out = {
        "bpm": round(first_bpm, 3),
        "notes": notes
    }

    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, separators=(",", ":"))

    print(f"[OK] wrote {json_path}, notes={len(notes)}, bpm~{out['bpm']}")

# 用法
midi_to_min_json("midi/Canon5.mid", "midi/Canon5.json")
