using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRolling : MonoBehaviour
{
    public float width = 7.11111f;        // 背景宽度
    public float height = 4.0f;        // 背景高度
    public int tileOffset = 3;       // 向右平移多少块宽度，通常 = 背景块数量
    public Transform maincamera;         // 以摄像机为中心
    public CameraFollow maincameraFollow;   // 主摄像机跟随脚本
    public float xparallax = 0.05f;         // 横向视差系数
    public float yparallax = 0.05f;          // 纵向视差系数

    private Vector3 parallaxMotion;      // 视差速度

    void Start()
    {
        maincamera = GameObject.FindWithTag("MainCamera").transform;
        maincameraFollow = maincamera.GetComponent<CameraFollow>();
    }

    void FixedUpdate()
    {
        parallaxMotion = new Vector3(maincameraFollow.Motion.x * xparallax, maincameraFollow.Motion.y * yparallax, 0);
        transform.position += parallaxMotion;

        // 如果这块背景的右边界比玩家落后很多，就搬到前面
        float rightDistance = maincamera.position.x - transform.position.x;
        float yDistance = maincamera.position.y - transform.position.y;
        if (rightDistance > 1.5f * width + 0.1f)  // 落后 1.5 块宽度
        {
            transform.position += tileOffset * width * Vector3.right;
        }
        if (yDistance > 1.5f * height + 0.2f)  // 高度差 1.5 块高度
        {
            transform.position += tileOffset * height * Vector3.up;
        }
        if (yDistance < -1.5f * height - 0.2f)  // 高度差 1.5 块高度
        {
            transform.position += tileOffset * height * Vector3.down;
        }
    }
}
