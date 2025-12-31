using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trail : MonoBehaviour
{
    [Header("Sound settings")]
    public AudioClip[] sounds;
    public float volume = 1.5f;
    public float DelPos = 20f;
    public float releaseTime = 0.2f;   // 淡出时间
    public bool OnCollisionDestroy = false;
    public Color hitColor = Color.green;

    [Header("Trail Settings")]
    public float sustainTime = 2.0f;   // 持续时间
    public float tailWidth = 0.03f;      // 尾迹宽度

    public Transform player;
    private bool used = false;

    // 画线相关
    private LineRenderer lr;
    private readonly List<Vector3> pts = new List<Vector3>();
    private Vector3 lastPoint;
    private bool drawing = false;          // 正在采样绘制
    private Collider2D ColliderPlayer;   // 碰到的角色
    private Coroutine stopCoroutine;
    private float maxTrailLength = 0f;  // 最大尾迹长度，未启用
    private float fadeOutTime = 1f;  // 淡化时间，未启用


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
            ColliderPlayer = other;
            PlaySound();
            PlayAnimation(OnCollisionDestroy);
            TailSpawner();
        }
    }

    void FixedUpdate()
    {
        if (transform.position.x < player.position.x - DelPos)
        {
            Destroy(gameObject);
        }

        if (drawing) 
        {
            if (ColliderPlayer.transform.position.x - transform.position.x > 0f)  // 角色穿过预制体开始画线
            TryAddPoint();
            // TrimToMaxLength();
            UpdateLineRenderer();
        }
    }

    void PlaySound()
    {
        if (sounds != null && sounds.Length > 0)
        {
            int index = Random.Range(0, sounds.Length);
            AudioClip clip = sounds[index];
            StartCoroutine(PlaySustainFor(clip, sustainTime));
            // AudioSource.PlayClipAtPoint(clip, transform.position, volume);
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

IEnumerator PlaySustainFor(AudioClip clip, float t)
{
    AudioSource asrc = GetComponent<AudioSource>();
    if (asrc == null) asrc = gameObject.AddComponent<AudioSource>();

    asrc.playOnAwake = false;
    asrc.loop = false;          // 关键：循环播放持续音
    asrc.spatialBlend = 0f;    // 2D 声音
    asrc.volume = volume;       

    asrc.clip = clip;
    asrc.Play();
    // Sustain：保持到时间结束
    yield return new WaitForSeconds(t);

    // Release：淡出并停止
    float r = 0f;
    float startVol = asrc.volume;
    while (r < releaseTime)
    {
        r += Time.deltaTime;
        asrc.volume = Mathf.Lerp(startVol, 0f, releaseTime <= 0 ? 1f : r / releaseTime);
        yield return null;
    }

    asrc.Stop();
    asrc.volume = 0f;
}


// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// 绘制曲线
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    void TailSpawner()  // 启用画线程序
    {
        // 1) 绑定玩家
        player = ColliderPlayer.transform;

        // 2) 确保有 LineRenderer
        EnsureLineRenderer();

        // 4) 开始绘制(播放音乐)
        pts.Clear();
        drawing = true;

        // 5) sustainTime 后停止绘制（但线不消失）
        if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        stopCoroutine = StartCoroutine(StopDrawingAfter(sustainTime));

        // 6) 触发后，这个拾取物的“实体”可以隐藏，只留下线
        //    不想隐藏就注释掉
        // var sr = GetComponent<SpriteRenderer>();
        // if (sr != null) sr.enabled = false;

        // var col = GetComponent<Collider2D>();
        // if (col != null) col.enabled = false;
    }

    void EnsureLineRenderer()  // 确保有 LineRenderer
    {
        // lr is LineRender
        if (lr != null) return;

        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.widthMultiplier = tailWidth;

        // 圆角好看点
        lr.numCapVertices = 6;
        lr.numCornerVertices = 6;

        // 颜色：起点更亮，末端略透明（你也可以用 hitColor 做主题色）
        Color c0 = hitColor; c0.a = 1f;
        Color c1 = hitColor; c1.a = 0.7f;
        lr.startColor = c0;
        lr.endColor = c1;

        // 如果线渲染不出来，给一个简单材质（内置也能用）
        // URP 下更推荐用 Unlit 材质，但先这样能跑
        if (lr.material == null)
            lr.material = new Material(Shader.Find("Sprites/Default"));
    }

    IEnumerator StopDrawingAfter(float t)  // 协程：中止绘制计时器
    {
        yield return new WaitForSeconds(t);

        drawing = false;
        // waitingToVanish = true;
    }

    void TryAddPoint()  // 尝试添加绘制点
    {
        Vector3 p = player.position;
        // if ((p - lastPoint).sqrMagnitude >= pointMinDistance * pointMinDistance)
        //{
        pts.Add(p);
        lastPoint = p;
        //}
    }

    void TrimToMaxLength()  // 修剪到最大长度, 未启用
    {
        while (pts.Count >= 2 && GetPolylineLength(pts) > maxTrailLength)
        {
            pts.RemoveAt(0);
        }
    }

    float GetPolylineLength(List<Vector3> list)  // 获取多段线长度，未启用
    {
        float len = 0f;
        for (int i = 1; i < list.Count; i++)
            len += Vector3.Distance(list[i - 1], list[i]);
        return len;
    }

    void UpdateLineRenderer()
    {
        if (lr == null) return;
        lr.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            lr.SetPosition(i, pts[i]);
    }

    IEnumerator FadeOutAndDestroy()
    {
        if (lr == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;
        Color c0 = lr.startColor;
        Color c1 = lr.endColor;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / fadeOutTime);

            Color a0 = c0; a0.a = c0.a * k;
            Color a1 = c1; a1.a = c1.a * k;
            lr.startColor = a0;
            lr.endColor = a1;

            yield return null;
        }

        Destroy(gameObject);
    }

}
