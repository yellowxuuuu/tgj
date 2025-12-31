using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundZoom : MonoBehaviour
{
    private Transform maincamera;         // 以摄像机为中心
    private CameraFollow maincameraFollow;   // 主摄像机跟随脚本
    public float cameraBaseSize = 2f;  // 主摄像机初始缩放
    public Vector3 BackgroundBaseSize = Vector3.zero;  // 背景初始缩放
    public float ZoomRatio = 1f;  // 缩放系数

    void Start()
    {
        maincamera = GameObject.FindWithTag("MainCamera").transform;
        maincameraFollow = maincamera.GetComponent<CameraFollow>();
        BackgroundBaseSize = transform.localScale;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float ScaleSize = maincameraFollow.targetSize / cameraBaseSize;
        transform.localScale = BackgroundBaseSize *ScaleSize;
        // transform.localScale = BackgroundBaseSize + BackgroundBaseSize * (ScaleSize - 1f) * ZoomRatio;
    }
}
