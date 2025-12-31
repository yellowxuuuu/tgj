using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform player;      
    private Camera cam;

    [Header("摄像机跟随参数")]
    public Vector3 offset = new Vector3(2.8f, -1f, -10f);
    public float stiffness = 0.015f;      // 弹簧劲度系数（越大越“跟手”）
    public float damping = 0.8f;     // 阻尼（0~1，越小越粘稠）
    public Vector3 Motion = Vector3.zero;  // 速度
    private Vector3 Distance = Vector3.zero;  // 距离

    [Header("摄像机缩放参数")]
    public float baseOrthoSize = 2f;   // 静止/慢速时的视野
    public float minOrthoSize = 1.8f;  // 下限
    public float maxOrthoSize = 2.2f;  // 上限
    public float speedForMaxZoom = 0.1f; // 速度达到这个值时，缩放到最大（按你 Motion 数值调）
    public float targetSize = 2f;      // 目标视野（按你 Motion 数值调）



    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        cam = Camera.main;
    }

    void FixedUpdate()
    {
        // 摄像机理想位置（想去但立刻去不了）
        Distance = player.position + offset - transform.position;
        if (Distance.magnitude < 0.1f) Distance = Vector3.zero;  // 防止抖动

        // 更新速度
        Motion += stiffness * Distance;
        Motion *= damping;
        Motion = new Vector3(Motion.x, Motion.y, 0);   //（避免被拉到奇怪的 z）

        // 移动摄像机
        transform.position += Motion;

        // 缩放摄像机
        Zoom();


        // // 硬跟随玩家
        // float x = player.position.x + offset.x;
        // float y = player.position.y + offset.y;   
        // transform.position = new Vector3(x, y, offset.z);

    }

void Zoom()
    {
        float speed = Motion.magnitude;
        float t = Mathf.InverseLerp(0f, speedForMaxZoom, speed);  // 0..1
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);   // SmoothStep
        // 得到目标视野（从 base → max）
        targetSize = Mathf.Lerp(baseOrthoSize, maxOrthoSize, t);
        // 再做上下限（一般 base 已经在范围内，但保险）
        targetSize = Mathf.Clamp(targetSize, minOrthoSize, maxOrthoSize);
        cam.orthographicSize = targetSize;

    }


}
