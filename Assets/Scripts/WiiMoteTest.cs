using UnityEngine;
using UnityEngine.InputSystem;
using WiimoteApi;
using extOSC;

public class WiiMoteTest : MonoBehaviour
{
    OSCReceiver receiver;

    public GameObject con;
    public Vector3 accelMod;

    Rigidbody conRB;

    Vector3 accelData;
    Vector3 gyroData;
    bool accelUpdated;
    bool gyroUpdated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        conRB = con.GetComponent<Rigidbody>();

        receiver = transform.GetComponent<OSCReceiver>();
        receiver.Bind("/message/accel/x", ReceiveAccel);
        receiver.Bind("/message/accel/y", ReceiveAccel);
        receiver.Bind("/message/accel/z", ReceiveAccel);
        receiver.Bind("/message/accel/f", ReceiveAccel);

        receiver.Bind("/message/gyro/pitch", ReceiveGyro);
        receiver.Bind("/message/gyro/yaw", ReceiveGyro);
        receiver.Bind("/message/gyro/roll", ReceiveGyro);
        receiver.Bind("/message/gyro/f", ReceiveGyro);  
    }

    // Update is called once per frame
    void Update()
    {
        if (accelUpdated)
        {
            conRB.linearVelocity = new Vector3(accelData.x * Time.deltaTime * accelMod.x, 
                accelData.y * Time.deltaTime * accelMod.y,
                accelData.z * Time.deltaTime * accelMod.z);
            
        }

    }
    protected void ReceiveAccel(OSCMessage message)
    {
        switch (message.Address)
        {
            case "/message/accel/x":
                accelData.x = message.Values[0].FloatValue;
                break;
            case "/message/accel/y":
                accelData.y = message.Values[0].FloatValue;
                break;
            case "/message/accel/z":
                accelData.z = message.Values[0].FloatValue;
                break;
            case "/message/accel/f":
                accelUpdated = message.Values[0].BoolValue;
                break;
        }
        
    }


    protected void ReceiveGyro(OSCMessage message)
    {
        switch (message.Address)
        {
            case "/message/gyro/pitch":
                gyroData.x = message.Values[0].FloatValue;
                break;
            case "/message/gyro/yaw":
                gyroData.y = message.Values[0].FloatValue;
                break;
            case "/message/gyro/roll":
                gyroData.z = message.Values[0].FloatValue;
                break;
            case "/message/gyro/f":
                gyroUpdated = message.Values[0].BoolValue;
                break;
        }
        
    }
    void OnApplicationQuit()
    {
        
    }
}
