using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Ring : MonoBehaviour
{
    [Header("Sound settings")]
    // public AudioClip[] sounds;
    public float volume = 1f;
    public float DelPos = 3f;
    public bool OnCollisionDestroy = false;
    public Color hitColor = Color.green;

    [Header("MIDI")]
    public int midiPitch = 60; // 由生成器赋值
    public bool ismidi = false;  // 由生成器赋值
    public bool isnpc = false;  // 属于NPC乐符
    public int ownerId = 0;
    public int noteId = -1;
    PerformanceJudge judge;

    [Header("Resources path (under Assets/Resources/)")]
    public string resourcesFolder = "Material";
    private Sprite hitSprite;

    private Transform player;
    private bool used = false;


    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        hitSprite = Resources.Load<Sprite>($"{resourcesFolder}/Ring4");
        judge = FindObjectOfType<PerformanceJudge>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if ((!isnpc && other.CompareTag("Player")) || (ownerId == 1 && other.name == "NPC1") ||
            (ownerId == 2 && other.name == "NPC2") ||(ownerId == 3 && other.name == "NPC3") ||
            (!ismidi && (other.CompareTag("Player") || other.CompareTag("NPC"))))
        {
            used = true;
            // PlaySound();
            PlaySoundByPitch(midiPitch);
            PlayAnimation(OnCollisionDestroy);
            if (judge != null) judge.ReportHit(ownerId, noteId);
        }
    }

    void FixedUpdate()
    {
        if (transform.position.x < player.position.x - 1f)
        {
            if (!used)
            {
                if (judge != null) judge.ReportMiss(ownerId, noteId);
            }
        }
        if (transform.position.x < player.position.x - DelPos) Destroy(gameObject);
    }

    // void PlaySound()
    // {
    //     if (sounds != null && sounds.Length > 0)
    //     {
    //         int index = Random.Range(0, sounds.Length);
    //         AudioClip clip = sounds[index];
    //         AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    //     }
    // }

    void PlaySoundByPitch(int pitch)
    {
        var sampler = PianoSampler.I;
        if (sampler == null) return;

        AudioClip clip = sampler.GetClipForMidiPitch(pitch);

        AudioSource asrc = GetComponent<AudioSource>();
        if (asrc == null) asrc = gameObject.AddComponent<AudioSource>();
        asrc.playOnAwake = false;
        asrc.loop = false;          // 关键：循环播放持续音
        asrc.spatialBlend = 0f;    // 2D 声音
        asrc.volume = volume;       

        asrc.clip = clip;
        asrc.Play();
    }

    void PlayAnimation(bool OnCollisionDestroy)
    {
        if (OnCollisionDestroy)
        {
            Destroy(gameObject);
            return;
        }
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = hitSprite;
    }



}

