using System.Collections.Generic;
using UnityEngine;

public class PianoSampler : MonoBehaviour
{
    public static PianoSampler I;

    [Header("Resources path (under Assets/Resources/)")]
    public string resourcesFolder = "Sound/Piano";

    // 文件名（小写）-> clip
    private Dictionary<string, AudioClip> clipByName = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadAllClips();
    }

    void LoadAllClips()
    {
        clipByName.Clear();
        var clips = Resources.LoadAll<AudioClip>(resourcesFolder);

        foreach (var c in clips)
        {
            // c.name 是不含扩展名的（比如 "c4"）
            var key = c.name.ToLower();
            clipByName[key] = c;
        }

        Debug.Log($"[PianoSampler] Loaded {clipByName.Count} clips from Resources/{resourcesFolder}");
    }

    // MIDI 60 = C4
    public AudioClip GetClipForMidiPitch(int midiPitch)
    {
        // 先把 pitch 转成音名（含#）和八度
        // 然后如果找不到（因为你只有白键），就降级到白键
        string nameSharp = MidiPitchToName(midiPitch, preferSharps: true);   // e.g. "c#4"
        if (clipByName.TryGetValue(nameSharp, out var clipSharp)) return clipSharp;

        string nameNatural = MidiPitchToNearestNaturalName(midiPitch);      // e.g. "c4"
        if (clipByName.TryGetValue(nameNatural, out var clipNatural)) return clipNatural;

        // 再不行：按八度平移找最近（比如你只有 c4~b5）
        for (int shift = 12; shift <= 48; shift += 12)
        {
            string up = MidiPitchToNearestNaturalName(midiPitch + shift);
            if (clipByName.TryGetValue(up, out var cu)) return cu;

            string down = MidiPitchToNearestNaturalName(midiPitch - shift);
            if (clipByName.TryGetValue(down, out var cd)) return cd;
        }

        return null;
    }

    static readonly string[] NoteNamesSharp = { "c","c#","d","d#","e","f","f#","g","g#","a","a#","b" };
    static readonly HashSet<int> Naturals = new HashSet<int> { 0,2,4,5,7,9,11 }; // C D E F G A B

    static string MidiPitchToName(int midiPitch, bool preferSharps)
    {
        int pc = ((midiPitch % 12) + 12) % 12;
        int octave = (midiPitch / 12) - 1; // MIDI 标准：60 -> C4
        string note = NoteNamesSharp[pc];  // 先用#体系
        return (note + octave).ToLower();
    }

    static string MidiPitchToNearestNaturalName(int midiPitch)
    {
        int pc = ((midiPitch % 12) + 12) % 12;
        int octave = (midiPitch / 12) - 1;

        // 如果本身是白键，直接用
        if (Naturals.Contains(pc))
            return (NoteNamesSharp[pc] + octave).ToLower(); // NoteNamesSharp里白键就是 "c","d"...没#

        // 黑键：就近映射到相邻白键（简单策略）
        // pc 1(C#)->C(0) or D(2) 取更近；这里选“向下”
        // 你也可以改成向上或根据调性决定
        int down = pc - 1;
        int up = pc + 1;

        if (down < 0) down += 12;
        if (up >= 12) up -= 12;

        int chosen = Naturals.Contains(down) ? down : up;
        return (NoteNamesSharp[chosen] + octave).ToLower();
    }
}
