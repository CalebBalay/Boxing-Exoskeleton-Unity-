using System.Linq;
using BetterJoyForCemu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityJoyCon;
using System.Threading;

public class JoyconTest : MonoBehaviour
{
    public Rigidbody jcLRB;
    public Rigidbody jcRRB;

    public Vector3 accelMod;
    Quaternion jcLQ, jcRQ;
    Vector3 jcLV, jcRV;
    string[] args = {};
    CancellationTokenSource source;
    CancellationToken token;

    ManageJoycons manager;


    void Awake()
    {
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        manager = new ManageJoycons();
        source = new CancellationTokenSource();
        token = source.Token;
        Thread t = new Thread(new ThreadStart(manager.Start));
        jcLQ = new Quaternion(); jcRQ = new Quaternion();
        jcLV = new Vector3(); jcRV = new Vector3();
        t.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (manager.j.Count > 0 && !manager.calibrate && manager.count == 0)
        {
            jcRV.x = manager.j[0].GetAccel().X * Time.deltaTime * accelMod.x;
                jcRV.y = manager.j[0].GetAccel().Y * Time.deltaTime * accelMod.y;
                jcRV.z = manager.j[0].GetAccel().Z * Time.deltaTime * accelMod.z;
            jcRRB.linearVelocity = jcRV;

            jcRQ[0]= manager.j[0].AHRS.Quaternion[0];
                jcRQ[1] = manager.j[0].AHRS.Quaternion[1];
                jcRQ[2] = manager.j[0].AHRS.Quaternion[2];
                jcRQ[3] = manager.j[0].AHRS.Quaternion[3];
            jcRRB.gameObject.transform.rotation = jcRQ.normalized;


            jcLV.x = manager.j[1].GetAccel().X * Time.deltaTime * accelMod.x;
                jcLV.y = manager.j[1].GetAccel().Y * Time.deltaTime * accelMod.y;
                jcLV.z = manager.j[1].GetAccel().Z * Time.deltaTime * accelMod.z;
            jcLRB.linearVelocity = jcLV;

            jcLQ[0]= manager.j[1].AHRS.Quaternion[0];
                jcLQ[1] = manager.j[1].AHRS.Quaternion[1];
                jcLQ[2] = manager.j[1].AHRS.Quaternion[2];
                jcLQ[3] = manager.j[1].AHRS.Quaternion[3];
            jcLRB.gameObject.transform.rotation = jcLQ.normalized;
        }
        
    }

    void OnApplicationQuit()
    {
        manager.OnApplicationQuit();
        source.Cancel();
    }
}
