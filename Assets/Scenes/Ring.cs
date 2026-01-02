using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Ring : MonoBehaviour
{
    [Header("Sound settings")]
    // public AudioClip[] sounds;
    public float volume = 1.5f;
    public float DelPos = 3f;
    public bool OnCollisionDestroy = false;
    public Color hitColor = Color.green;

    [Header("MIDI")]
    public int midiPitch = 60; // 由生成器赋值
    public bool ismidi = false;

    private Transform player;
    private bool used = false;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (other.CompareTag("Player"))
        {
            used = true;
            // PlaySound();
            PlaySoundByPitch(midiPitch);
            PlayAnimation(OnCollisionDestroy);
        }
    }

    void FixedUpdate()
    {
        if (transform.position.x < player.position.x - DelPos)
        {
            Destroy(gameObject);
        }
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
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        else
            Debug.LogWarning($"No clip for pitch {pitch}");
    }

    void PlayAnimation(bool OnCollisionDestroy)
    {
        if (OnCollisionDestroy)
        {
            Destroy(gameObject);
            return;
        }
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.color = hitColor;
    }



}

