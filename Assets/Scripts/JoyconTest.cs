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

        t.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (manager.j.Count > 0)
        {
            jcRRB.linearVelocity = new Vector3(
                manager.j[0].GetAccel().X * Time.deltaTime * accelMod.x,
                manager.j[0].GetAccel().Y * Time.deltaTime * accelMod.y,
                manager.j[0].GetAccel().Z * Time.deltaTime * accelMod.z);
            jcRRB.gameObject.transform.rotation = new Quaternion(
                manager.j[0].AHRS.Quaternion[0],
                manager.j[0].AHRS.Quaternion[1],
                manager.j[0].AHRS.Quaternion[2],
                manager.j[0].AHRS.Quaternion[3]).normalized;
        }
            
            /*new Quaternion(
                manager.j[0].AHRS.Quaternion[0], 
                manager.j[0].AHRS.Quaternion[1], 
                manager.j[0].AHRS.Quaternion[2], 
                manager.j[0].AHRS.Quaternion[3]);*/
    }

    void OnApplicationQuit()
    {
        manager.OnApplicationQuit();
        source.Cancel();
    }
}
