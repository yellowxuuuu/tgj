using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Spring : MonoBehaviour
{
    [Header("Sound settings")]
    public AudioClip[] sounds;
    public float volume = 1.5f;
    public float DelPos = 3f;
    public bool OnCollisionDestroy = false;
    public Color hitColor = Color.green;

    [Header("Bounce Settings")]
    public float bounceStrength = 0.2f;   // 弹起的力度
    public float bounceAngle = 45f;      // 弹起的角度

    public Transform player;
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
            PlaySound();
            PlayAnimation(OnCollisionDestroy);
            BouncePlayer(other);
        }
    }

    void FixedUpdate()
    {
        if (transform.position.x < player.position.x - DelPos)
        {
            Destroy(gameObject);
        }
    }

    void PlaySound()
    {
        if (sounds != null && sounds.Length > 0)
        {
            int index = Random.Range(0, sounds.Length);
            AudioClip clip = sounds[index];
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
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

    void BouncePlayer(Collider2D other)
    {
        PlayerController playercontroller = other.GetComponent<PlayerController>();
        bounceAngle = - transform.rotation.eulerAngles.z - 90f;
        Vector3 BounceMotion = new Vector3(
                                    bounceStrength * Mathf.Sin(bounceAngle * Mathf.Deg2Rad),
                                    bounceStrength * Mathf.Cos(bounceAngle * Mathf.Deg2Rad),
                                    0f);        
        playercontroller.Motion = new Vector3(
                                    0.4f * playercontroller.Motion.x + BounceMotion.x,
                                    0.0f * playercontroller.Motion.x + BounceMotion.y,
                                    0f);

    }

}

