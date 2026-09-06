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

    public float rotationThreshold, debugStep, accelMod, accelThreshold, velDifPercent, velDifAngle;

    public bool printAccel, printGyro, printRotationStep, printAHRSEulers, printUnityEulers,
        useEuler;

    Quaternion jcLQ, jcRQ, lastRotationRQ;

    Vector3 jcLA, jcRA, jcRRS, lastRotationRV, lastAccelerationR, tempVel, lastVel;

    float timeSinceLastPrint;


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
        lastRotationRQ = new Quaternion();
        jcLA = new Vector3(); jcRA = new Vector3(); jcRRS = new Vector3();
        lastRotationRV = new Vector3();
        lastAccelerationR = new Vector3();
        tempVel = new Vector3(); lastVel = new Vector3();

        t.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (manager.j.Count > 0 && !manager.calibrate && manager.count == 0)
        {
            jcRQ[0]= manager.j[0].AHRS.Quaternion[0];
                jcRQ[1] = manager.j[0].AHRS.Quaternion[1];
                jcRQ[2] = manager.j[0].AHRS.Quaternion[2];
                jcRQ[3] = manager.j[0].AHRS.Quaternion[3];  

            jcRA.x = manager.j[0].GetAccel().X;
                jcRA.y = manager.j[0].GetAccel().Y;
                jcRA.z = manager.j[0].GetAccel().Z;

            //if (Quaternion.Angle(lastRotationRQ.normalized, jcRQ.normalized) >= rotationThreshold && lastRotationRQ.eulerAngles != Vector3.zero)
            //{

                /* note: something is wrong with the AHRS euler angles function.
                   for future reference, setting transform.rotation directly with Quaternion.Euler works as intended,
                   but the GetEulerAngles function is wildly jank 
                   update: Unity's .eulerAngles property also seems really jank.
                   update: my initial method of flipping the controller's axes seems to be the issue.

                   I'm going to try and apply acceleration without adjusting the rotation to see
                   if I can get by without needing an accurate model of the joycon, and just
                   use a sphere with no (visible) orientation.*/
                /*jcRRS.x = manager.j[0].AHRS.GetEulerAngles()[2] * Mathf.Rad2Deg;
                    jcRRS.y = manager.j[0].AHRS.GetEulerAngles()[1] * Mathf.Rad2Deg;
                    jcRRS.z = manager.j[0].AHRS.GetEulerAngles()[0] * Mathf.Rad2Deg;*/ 


                
                //jcRRB.transform.rotation = Quaternion.Euler(jcRRB.transform.rotation.eulerAngles + jcRRS).normalized;
                //jcRRB.transform.Rotate(jcRRS);

                
            //}

            if (useEuler)
            {
                jcRRS.x = jcRQ.eulerAngles.z;
                    jcRRS.y = jcRQ.eulerAngles.x;
                    jcRRS.z = jcRQ.eulerAngles.y;
                jcRRB.transform.rotation = Quaternion.Euler(jcRRS).normalized;
            }
            else
            {
                jcRRB.transform.rotation = jcRQ;
            }

            tempVel.x = jcRA.x * accelMod;
                tempVel.y = jcRA.y * accelMod;
                tempVel.z = jcRA.z * accelMod;
            tempVel = jcRRB.transform.TransformDirection(tempVel);
            // 1.5 seems good currently for accelThreshold
            if (jcRA.magnitude > accelThreshold && 
                !(Vector3.Angle(lastVel * -1f, tempVel) < velDifAngle && 
                lastVel.magnitude > velDifPercent * tempVel.magnitude))
            {
                
                jcRRB.linearVelocity = tempVel;
            }
            else
                jcRRB.linearVelocity = Vector3.zero;
            


            if (Time.realtimeSinceStartup >= timeSinceLastPrint + debugStep)
            {
                if (printGyro)
                    Debug.Log("Right JC gyro: " + manager.j[0].GetGyro());
                if (printAccel)
                    Debug.Log("Right JC acceleration: " + jcRA);
                if (printRotationStep)
                    Debug.Log("Rich JC rotation increment: " + (jcRRS - lastRotationRV));
                if (printAHRSEulers)
                    Debug.Log("AHRS algo's Euler angles: (" 
                        + manager.j[0].AHRS.GetEulerAngles()[0] * Mathf.Rad2Deg 
                        + ", " + manager.j[0].AHRS.GetEulerAngles()[1] * Mathf.Rad2Deg 
                        + ", " + manager.j[0].AHRS.GetEulerAngles()[2] * Mathf.Rad2Deg + ")");
                if (printUnityEulers)
                    Debug.Log("Unity's Euler angles: (" + jcRQ.eulerAngles.x + ", " +
                        jcRQ.eulerAngles.y + ", " +
                        jcRQ.eulerAngles.z + ")");
                timeSinceLastPrint = Time.realtimeSinceStartup;
            }

            lastRotationRQ = jcRQ;
            lastRotationRV = jcRRS;
            lastAccelerationR = jcRA;
            lastVel = tempVel;
        }
        
    }

    void OnApplicationQuit()
    {
        manager.OnApplicationQuit();
        source.Cancel();
    }
}
