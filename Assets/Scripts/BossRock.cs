using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRock : Bullet
{
    Rigidbody rb;
    float angularPower = 2f;
    float scaleValue = 0.1f;
    bool isShoot;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }

    IEnumerator GainPowerTimer()
    {
        yield return new WaitForSeconds(2.0f);
        isShoot = true;
    }

    IEnumerator GainPower()
    {
        while (!isShoot)
        {
            angularPower += 10f * Time.deltaTime;
            scaleValue += 0.5f * Time.deltaTime;
            transform.localScale = Vector3.one * scaleValue;
            rb.AddTorque(transform.right * angularPower, ForceMode.Acceleration);
            yield return null;
        }
    }
}
