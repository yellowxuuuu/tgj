// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("玩家贴图")]
    public float AngleOffset = 45f;

    [Header("玩家运动")]
    public float AccelScale = 0.0003f;  // 速度缩放
    public float AccelTurningScaleUp = 2.2f;
    public float AccelTurningScaleDown = 1.7f;
    public Vector3 AccelUp =  new Vector3(1.2f, 1.7f, 0f);
    public Vector3 AccelDown =  new Vector3(1.2f, -1.5f, 0f);
    public Vector3 Friction = new Vector3(0.98f, 0.985f, 1f);
    public Vector3 Accel = new Vector3(0f, 0f, 0f);
    public Vector3 Motion = new Vector3(0.01f, -0.01f, 0f);

    void FixedUpdate()
    {
        // 按下空格向上加速，松开向下
        if (Input.GetKey(KeyCode.Space)){
            Accel = AccelUp * AccelScale;
            if (Accel.y * Motion.y < 0) Accel.y *= AccelTurningScaleUp;  // 掉头速度快一点
            }
        else {
            Accel = AccelDown * AccelScale;
            if (Accel.y * Motion.y < 0) Accel.y *= AccelTurningScaleDown;
            }

        // 计算速度
        Motion += Accel;

        float upfriction = (Motion.y > 0f) ? (0.985f / Friction.y) : 1f;  // 向上运动阻力降低
        Motion = new Vector3(Motion.x * Friction.x, Motion.y * Friction.y, Motion.z * Friction.z);
        

        // 计算位移
        transform.position += Motion;

        // 根据 Motion 调整朝向
        if (Motion.sqrMagnitude > 0.000001f)   // 防止静止时乱转
        {
            float angle = Mathf.Atan2(Motion.y, Motion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - AngleOffset);
        }

    }

}
