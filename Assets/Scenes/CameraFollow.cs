using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;      
    public Vector3 offset = new Vector3(2.8f, -1f, -10f);
    public float stiffness = 0.015f;      // 弹簧劲度系数（越大越“跟手”）
    public float damping = 0.8f;     // 阻尼（0~1，越小越粘稠）
    public Vector3 Motion = Vector3.zero;  // 速度

    private Vector3 Distance = Vector3.zero;  // 距离

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        // 摄像机理想位置（想去但立刻去不了）
        Distance = player.position + offset - transform.position;

        // 更新速度
        Motion += stiffness * Distance;
        Motion *= damping;
        Motion = new Vector3(Motion.x, Motion.y, 0);   //（避免被拉到奇怪的 z）

        // 移动摄像机
        transform.position += Motion;

        // // 硬跟随玩家
        // float x = player.position.x + offset.x;
        // float y = player.position.y + offset.y;   
        // transform.position = new Vector3(x, y, offset.z);

    }
}
