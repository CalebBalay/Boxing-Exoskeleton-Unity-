using UnityEngine;
using UnityEngine.InputSystem;
using WiimoteApi;

public class WiiMoteTest : MonoBehaviour
{

    Wiimote remoteL;
    private int calCount = 0;
    InputAction enter;
    public GameObject con;
    public Vector3 speedMod;
    public float accelThreshold;
    Vector3 oldAccel;
    Vector3 newAccel;
    Vector3 accelDif;
    Rigidbody conRB;
    Transform conTR;
    public int updateBuffer;
    int updateCount = 0;
    bool rumble = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enter = InputSystem.actions.FindAction("Enter");
        WiimoteManager.FindWiimotes();
        foreach(Wiimote remote in WiimoteManager.Wiimotes) {
            print("wiimote found!");
        }
        remoteL = WiimoteManager.Wiimotes[0];
        print(remoteL.Type);
        print(remoteL.ActivateWiiMotionPlus());
        remoteL.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);

        print(remoteL.SendPlayerLED(false, false, false, false));
        //print(remoteL.SendStatusInfoRequest());
        //print(remoteL.ReadWiimoteData());
        //print(remoteL.Accel.GetCalibratedAccelData()[2]);
        
        //print(remoteL.Status.led[0] + ", " + remoteL.Status.led[1] + ", " + remoteL.Status.led[2] + ", " + remoteL.Status.led[3]);
        

        oldAccel = Vector3.zero;
        newAccel = Vector3.zero;

        conRB = con.GetComponent<Rigidbody>();
        conTR = con.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (calCount < 3)
        {
            if (enter.WasPerformedThisFrame())
            {
                remoteL.ReadWiimoteData();
                print("LR: " + remoteL.Accel.GetCalibratedAccelData()[0] +
                " UD: " + remoteL.Accel.GetCalibratedAccelData()[2] + 
                " FB: " + remoteL.Accel.GetCalibratedAccelData()[1]);
                print("Calibration for count " + calCount);
                remoteL.ReadWiimoteData();
                remoteL.Accel.CalibrateAccel((AccelCalibrationStep)calCount);
                print("LR: " + remoteL.Accel.GetCalibratedAccelData()[0] +
                " UD: " + remoteL.Accel.GetCalibratedAccelData()[2] + 
                " FB: " + remoteL.Accel.GetCalibratedAccelData()[1]);
                calCount++;
            }
        }
        else
        {
            oldAccel = newAccel;
            remoteL.ReadWiimoteData();
            newAccel = new Vector3(remoteL.Accel.GetCalibratedAccelData()[0], remoteL.Accel.GetCalibratedAccelData()[2],
                remoteL.Accel.GetCalibratedAccelData()[1]);

            if (newAccel.magnitude > accelThreshold)
            {
                conRB.linearVelocity = new Vector3(newAccel.x * Time.deltaTime * speedMod.x, 
                    newAccel.y * Time.deltaTime * speedMod.y,
                    newAccel.z * Time.deltaTime * speedMod.z);
            }
            else
            {
                conRB.linearVelocity = Vector3.zero;
                //conRB.rotation = Quaternion.Euler((newAccel.y - 1) * 90, (newAccel.x - 1) * 90, (newAccel.z - 1) * 90);
                
                //conTR.LookAt(conTR.position + new Vector3(newAccel.x, newAccel.y, newAccel.z), Vector3.up);
            }
            
            print("LR: " + newAccel.x +
                " UD: " + newAccel.y + 
                " FB: " + newAccel.z);

            if (enter.WasPerformedThisFrame())
            {
                conRB.position = Vector3.zero;
            }
        
            
        }
        
    }
    void OnApplicationQuit()
    {
        foreach(Wiimote remote in WiimoteManager.Wiimotes) {
            WiimoteManager.Cleanup(remote);
        }
    }
}
