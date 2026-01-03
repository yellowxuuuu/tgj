using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteAssignment : MonoBehaviour
{
    public MidiNoteSpawner spawner;
    public float speedMult = 0.8f;

    public NPCController npc1crtl;
    public NPCController npc2crtl;
    public NPCController npc3crtl;


    void Start()
    {
        spawner = GetComponent<MidiNoteSpawner>();
        StartCoroutine(Note1(15f));
        
    }

    IEnumerator Note1 (float t)
    {
        yield return new WaitForSeconds(t);
        var playerTrack = spawner.SpawnTrack("HappyBirthday", ownerId: 0, speedMult);
        Transform npc1 = GameObject.Find("NPC1").transform;
        var npc1Track = spawner.SpawnTrack("HappyBirthday", ownerId: 1, speedMult, fluctuateMult: 0.7f, reference: npc1);
    }
}
