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
    public float captureDriftDuration;
    public float accelThreshold, rotationThreshold;

    public bool debugToggle, accountDrift, addDrift, printAccel, printRotateBy, printAccelMag;

    Quaternion jcLQ, jcRQ;
    Quaternion[] initialRotation = new Quaternion[2];
    Vector3 jcLA, jcRA;
    Vector3[] initialDrift = new Vector3[2];
    Vector3 activeDrift;
    Vector3 rotateBy;
    string[] args = {};
    CancellationTokenSource source;
    CancellationToken token;
    double timeAtLastCheck = -1.0;
    bool hasInitialized = false;

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
        jcLA = new Vector3(); jcRA = new Vector3();
        initialDrift[0] = new Vector3();
        activeDrift = new Vector3();
        rotateBy = new Vector3();

        t.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (manager.j.Count > 0 && !manager.calibrate && manager.count == 0)
        {
            if (timeAtLastCheck == -1.0)
                timeAtLastCheck = Time.realtimeSinceStartupAsDouble;
            else if (Time.realtimeSinceStartupAsDouble >= timeAtLastCheck + captureDriftDuration && !hasInitialized)
            {
                initialDrift[0].x = manager.j[0].GetAccel().X;
                    initialDrift[0].y = manager.j[0].GetAccel().Y;
                    initialDrift[0].z = manager.j[0].GetAccel().Z;
                initialRotation[0][0] = manager.j[0].AHRS.Quaternion[0];
                    initialRotation[0][1] = manager.j[0].AHRS.Quaternion[1];
                    initialRotation[0][2] = manager.j[0].AHRS.Quaternion[2];
                    initialRotation[0][3] = manager.j[0].AHRS.Quaternion[3];
                initialRotation[0] = initialRotation[0].normalized;
                hasInitialized = true;
            }
            else if (hasInitialized)
            {
                jcRQ[0]= manager.j[0].AHRS.Quaternion[0];
                    jcRQ[1] = manager.j[0].AHRS.Quaternion[1];
                    jcRQ[2] = manager.j[0].AHRS.Quaternion[2];
                    jcRQ[3] = manager.j[0].AHRS.Quaternion[3];
                if (Quaternion.Angle(jcRRB.gameObject.transform.rotation, jcRQ.normalized) > rotationThreshold)
                    jcRRB.gameObject.transform.rotation = jcRQ.normalized;
                
                jcRRB.linearVelocity = Vector3.zero;
                jcRA.x = manager.j[0].GetAccel().X;
                    jcRA.y = manager.j[0].GetAccel().Y;
                    jcRA.z = manager.j[0].GetAccel().Z;

                rotateBy = Quaternion.FromToRotation(initialRotation[0].eulerAngles, jcRQ.normalized.eulerAngles).eulerAngles;
                activeDrift = Quaternion.FromToRotation(initialRotation[0].eulerAngles, jcRQ.normalized.eulerAngles)
                    * initialDrift[0];
                
                if (jcRA.magnitude > accelThreshold)
                    jcRRB.AddForce((jcRA + (accountDrift ? (addDrift ? activeDrift : activeDrift * -1) : Vector3.zero)) 
                        * accelMod.x, ForceMode.Acceleration);

                if (debugToggle)
                {
                    if (printAccel)
                        Debug.Log("JCR Acceleration: " + (jcRA + 
                            (accountDrift ? (addDrift ? activeDrift : activeDrift * -1) : Vector3.zero)));
                    if (printAccelMag)
                        Debug.Log("JC Accel Mag: " + (jcRA + 
                            (accountDrift ? (addDrift ? activeDrift : activeDrift * -1) : Vector3.zero)).magnitude);
                    if (printRotateBy)
                        Debug.Log("Difference in rotation from initial:" + rotateBy);
                }
                
            }
            


            /*jcLA.x = manager.j[1].GetAccel().X * Time.deltaTime * accelMod.x;
                jcLA.y = manager.j[1].GetAccel().Y * Time.deltaTime * accelMod.y;
                jcLA.z = manager.j[1].GetAccel().Z * Time.deltaTime * accelMod.z;
            jcLRB.linearVelocity = jcLA;

            jcLQ[0]= manager.j[1].AHRS.Quaternion[0];
                jcLQ[1] = manager.j[1].AHRS.Quaternion[1];
                jcLQ[2] = manager.j[1].AHRS.Quaternion[2];
                jcLQ[3] = manager.j[1].AHRS.Quaternion[3];
            jcLRB.gameObject.transform.rotation = jcLQ.normalized;*/
        }
        
    }

    void OnApplicationQuit()
    {
        manager.OnApplicationQuit();
        source.Cancel();
    }
}
