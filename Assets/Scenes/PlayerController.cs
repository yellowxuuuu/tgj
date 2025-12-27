// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float xAccel = 2f;
    public float yAccel = 1.8f;
    public float nyAccel = 1.5f;
    public float AccelRotate = 45f;
    public float AccelScale = 0.0004f;
    public Vector3 Friction = new Vector3(0.98f, 0.97f, 1f);
    public Vector3 Accel = new Vector3(0f, 0f, 0f);
    public Vector3 Motion = new Vector3(0f, 0f, 0f);

    void FixedUpdate()
    {
        // 按下空格向上加速，松开向下
        if (Input.GetKey(KeyCode.Space))
            Accel = new Vector3(xAccel, yAccel, 0f) * AccelScale;
        else
            Accel = new Vector3(xAccel, -nyAccel, 0f) * AccelScale;

        // 计算速度
        Motion += Quaternion.Euler(0, 0, -AccelRotate) * Accel;
        Motion = new Vector3(Motion.x * Friction.x, Motion.y * Friction.y, Motion.z * Friction.z);
        

        // 计算位移
        transform.position += Motion;

        // 根据 Motion 调整朝向
        if (Motion.sqrMagnitude > 0.0001f)   // 防止静止时乱转
        {
            float angle = Mathf.Atan2(Motion.y, Motion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // // 周期边界条件
        // if (transform.position.x > 4.5f)
        //     transform.position = new Vector3(-4.5f, transform.position.y, transform.position.z);
        // if (transform.position.y > 2f)
        //     transform.position = new Vector3(transform.position.x, -2f, transform.position.z);
        // if (transform.position.y < -2f)
        //     transform.position = new Vector3(transform.position.x, 2f, transform.position.z);
    }

}
