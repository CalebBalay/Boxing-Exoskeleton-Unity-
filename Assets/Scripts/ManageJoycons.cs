using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using BetterJoyForCemu.Collections;
using BetterJoyForCemu;
//using Nefarius.ViGEm.Client;
using static BetterJoyForCemu._3rdPartyControllers;
using static BetterJoyForCemu.HIDapi;
using UnityEngine;

public class ManageJoycons {
        public bool EnableIMU = true;
        public bool EnableLocalize = false;

        private const ushort vendor_id = 0x57e;
        private const ushort product_l = 0x2006;
        private const ushort product_r = 0x2007;
        private const ushort product_pro = 0x2009;
        private const ushort product_snes = 0x2017;
        private const ushort product_n64 = 0x2019;

        // MainForm
        public List<int> xG, yG, zG, xA, yA, zA;
        public List<KeyValuePair<string, float[]>> caliData;
        private System.Windows.Forms.Timer countDown;
        private int count;
        public bool calibrate = true;
        public bool allowCalibration = true;

        // Program
        static string pid;
        public static PhysicalAddress btMAC = new PhysicalAddress(new byte[] { 0, 0, 0, 0, 0, 0 });

        public ConcurrentList<Joycon> j { get; private set; } // Array of all connected Joy-Cons
        static ManageJoycons instance;

        public MainForm form;

        System.Timers.Timer controllerCheck;
    

    public static ManageJoycons Instance {
            get { return instance; }
        }

        public void Awake() {
            instance = this;
            j = new ConcurrentList<Joycon>();
            HIDapi.hid_init();
        }

        public void Start() {

            // Program
            SetupDlls();

            // MainForm

            xG = new List<int>(); yG = new List<int>(); zG = new List<int>();
            xA = new List<int>(); yA = new List<int>(); zA = new List<int>();

            caliData = new List<KeyValuePair<string, float[]>> {
                new KeyValuePair<string, float[]>("0", new float[6] {0,0,0,-710,0,0})
            };

            Config.Init(caliData);

            // Program

            pid = Process.GetCurrentProcess().Id.ToString();

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                // Get local BT host MAC
                if (nic.NetworkInterfaceType != NetworkInterfaceType.FastEthernetFx && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) {
                    if (nic.Name.Split()[0] == "Bluetooth") {
                        btMAC = nic.GetPhysicalAddress();
                    }
                }
            }

            // Former Awake()
            instance = this;
            j = new ConcurrentList<Joycon>();
            HIDapi.hid_init();

            CheckForNewControllers();

            controllerCheck = new System.Timers.Timer(2000); // check for new controllers every 2 seconds
            controllerCheck.Elapsed += CheckForNewControllersTime;
            controllerCheck.Start();

            
        }

        // MainForm
        private void StartGetData() {
            this.xG.Clear(); this.yG.Clear(); this.zG.Clear();
            this.xA.Clear(); this.yA.Clear(); this.zA.Clear();
            countDown = new System.Windows.Forms.Timer();
            this.count = 3;
            this.calibrate = true;
            countDown.Tick += new EventHandler(CalcData);
            countDown.Interval = 1000;
            countDown.Enabled = true;
        }
        private void CalcData(object sender, EventArgs e) {
            if (this.count == 0) {
                countDown.Stop();
                this.calibrate = false;
                string serNum = Program.mgr.j.First().serial_number;
                int serIndex = this.findSer(serNum);
                float[] Arr = new float[6] { 0, 0, 0, 0, 0, 0 };
                if (serIndex == -1) {
                    this.caliData.Add(new KeyValuePair<string, float[]>(
                         serNum,
                         Arr
                    ));
                } else {
                    Arr = this.caliData[serIndex].Value;
                }
                System.Random rnd = new System.Random();
                Arr[0] = (float)quickselect_median(this.xG, rnd.Next);
                Arr[1] = (float)quickselect_median(this.yG, rnd.Next);
                Arr[2] = (float)quickselect_median(this.zG, rnd.Next);
                Arr[3] = (float)quickselect_median(this.xA, rnd.Next);
                Arr[4] = (float)quickselect_median(this.yA, rnd.Next);
                Arr[5] = (float)quickselect_median(this.zA, rnd.Next) - 4010; //Joycon.cs acc_sen 16384
                Config.SaveCaliData(this.caliData);
                Program.mgr.j.First().getActiveData();
            } else {
                this.count--;
            }

        }
        private int findSer(string serNum) {
            for (int i = 0; i < this.caliData.Count; i++) {
                if (this.caliData[i].Key == serNum) {
                    return i;
                }
            }
            return -1;
        }
        private double quickselect_median(List<int> l, Func<int, int> pivot_fn) {
            int ll = l.Count;
            if (ll % 2 == 1) {
                return this.quickselect(l, ll / 2, pivot_fn);
            } else {
                return 0.5 * (quickselect(l, ll / 2 - 1, pivot_fn) + quickselect(l, ll / 2, pivot_fn));
            }
        }
        private int quickselect(List<int> l, int k, Func<int, int> pivot_fn) {
            if (l.Count == 1 && k == 0) {
                return l[0];
            }
            int pivot = l[pivot_fn(l.Count)];
            List<int> lows = l.Where(x => x < pivot).ToList();
            List<int> highs = l.Where(x => x > pivot).ToList();
            List<int> pivots = l.Where(x => x == pivot).ToList();
            if (k < lows.Count) {
                return quickselect(lows, k, pivot_fn);
            } else if (k < (lows.Count + pivots.Count)) {
                return pivots[0];
            } else {
                return quickselect(highs, k - lows.Count - pivots.Count, pivot_fn);
            }
        }
        public float[] activeCaliData(string serNum) {
            for (int i = 0; i < this.caliData.Count; i++) {
                if (this.caliData[i].Key == serNum) {
                    return this.caliData[i].Value;
                }
            }
            return this.caliData[0].Value;
        }
        
        // Program

        static void SetupDlls() {
            string archPath = $"{AppDomain.CurrentDomain.BaseDirectory}{(Environment.Is64BitProcess ? "x64" : "x86")}\\";
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            pathVariable = $"{archPath};{pathVariable}";
            Environment.SetEnvironmentVariable("PATH", pathVariable);
        }

        

        // Original
        bool ControllerAlreadyAdded(string path) {
            foreach (Joycon v in j)
                if (v.path == path)
                    return true;
            return false;
        }

        void CleanUp() { // removes dropped controllers from list
            List<Joycon> rem = new List<Joycon>();
            foreach (Joycon joycon in j) {
                if (joycon.state == Joycon.state_.DROPPED) {
                    if (joycon.other != null)
                        joycon.other.other = null; // The other of the other is the joycon itself

                    joycon.Detach(true);
                    rem.Add(joycon);

                    UnityEngine.Debug.Log("Removed dropped controller. Can be reconnected.\r\n"); 
                }
            }

            foreach (Joycon v in rem)
                j.Remove(v);
        }

        void CheckForNewControllersTime(System.Object source, ElapsedEventArgs e) {
            CleanUp();
            if (Config.IntValue("ProgressiveScan") == 1) {
                CheckForNewControllers();
            }
        }

        private ushort TypeToProdId(byte type) {
            switch (type) {
                case 1:
                    return product_pro;
                case 2:
                    return product_l;
                case 3:
                    return product_r;
            }
            return 0;
        }

        public void CheckForNewControllers() {
            // move all code for initializing devices here and well as the initial code from Start()
            bool isLeft = false;
            IntPtr ptr = HIDapi.hid_enumerate(0x0, 0x0);
            IntPtr top_ptr = ptr;

            HIDapi.hid_device_info enumerate; // Add device to list
            bool foundNew = false;
            while (ptr != IntPtr.Zero) {
                SController thirdParty = null;
                enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

                if (enumerate.serial_number == null) {
                    ptr = enumerate.next; // can't believe it took me this long to figure out why USB connections used up so much CPU.
                                          // it was getting stuck in an inf loop here!
                    continue;
                }

                bool validController = (enumerate.product_id == product_l || enumerate.product_id == product_r ||
                                        enumerate.product_id == product_pro || enumerate.product_id == product_snes || enumerate.product_id == product_n64) && enumerate.vendor_id == vendor_id;
                // check list of custom controllers specified
                foreach (SController v in Program.thirdPartyCons) {
                    if (enumerate.vendor_id == v.vendor_id && enumerate.product_id == v.product_id && enumerate.serial_number == v.serial_number) {
                        validController = true;
                        thirdParty = v;
                        break;
                    }
                }

                ushort prod_id = thirdParty == null ? enumerate.product_id : TypeToProdId(thirdParty.type);
                if (prod_id == 0) {
                    ptr = enumerate.next; // controller was not assigned a type, but advance ptr anyway
                    continue;
                }

                if (validController && !ControllerAlreadyAdded(enumerate.path)) {
                    switch (prod_id) {
                        case product_l:
                            isLeft = true;
                            UnityEngine.Debug.Log("Left Joy-Con connected.\r\n"); break; 
                        case product_r:
                            isLeft = false;
                            UnityEngine.Debug.Log("Right Joy-Con connected.\r\n"); break; 
                        case product_pro:
                            isLeft = true;
                            UnityEngine.Debug.Log("Pro controller connected.\r\n"); break; 
                        case product_snes:
                            isLeft = true;
                            UnityEngine.Debug.Log("SNES controller connected.\r\n"); break; 
                        case product_n64:
                            isLeft = true;
                            UnityEngine.Debug.Log("N64 controller connected.\r\n"); break; 
                        default:
                            UnityEngine.Debug.Log("Non Joy-Con Nintendo input device skipped.\r\n"); break; 
                    }

                    // Add controller to block-list for HidGuardian
                    // -------------------- //

                    IntPtr handle = HIDapi.hid_open_path(enumerate.path);
                    try {
                        HIDapi.hid_set_nonblocking(handle, 1);
                    } catch {
                        UnityEngine.Debug.Log("Unable to open path to device - are you using the correct (64 vs 32-bit) version for your PC?\r\n"); 
                        break;
                    }

                    bool isPro = prod_id == product_pro;
                    bool isSnes = prod_id == product_snes;
                    bool is64 = prod_id == product_n64;
                    j.Add(new Joycon(handle, EnableIMU, EnableLocalize & EnableIMU, 0.05f, isLeft, enumerate.path, enumerate.serial_number, j.Count, isPro, isSnes, is64,thirdParty != null));

                    foundNew = true;
                    j.Last().manager = this;

                    if (j.Count < 5) {
                        int ii = -1;
                    }

                    byte[] mac = new byte[6];
                    try {
                        for (int n = 0; n < 6; n++)
                            mac[n] = byte.Parse(enumerate.serial_number.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
                    } catch (Exception e) {
                        // could not parse mac address
                    }
                    j[j.Count - 1].PadMacAddress = new PhysicalAddress(mac);
                }

                ptr = enumerate.next;
            }

            if (foundNew) { // attempt to auto join-up joycons on connection
                Joycon temp = null;
                foreach (Joycon v in j) {
                    // Do not attach two controllers if they are either:
                    // - Not a Joycon
                    // - Already attached to another Joycon (that isn't itself)
                    if (v.isPro || (v.other != null && v.other != v)) {
                        continue;
                    }

                    // Otherwise, iterate through and find the Joycon with the lowest
                    // id that has not been attached already (Does not include self)
                    if (temp == null)
                        temp = v;
                    else if (temp.isLeft != v.isLeft && v.other == null) {
                        temp.other = v;
                        v.other = temp;

                        if (temp.out_xbox != null) {
                            try {
                                temp.out_xbox.Disconnect();
                            } catch (Exception e) {
                                // it wasn't connected in the first place, go figure
                            }
                        }
                        if (temp.out_ds4 != null) {
                            try {
                                temp.out_ds4.Disconnect();
                            } catch (Exception e) {
                                // it wasn't connected in the first place, go figure
                            }
                        }
                        temp.out_xbox = null;
                        temp.out_ds4 = null;

                        temp = null;    // repeat
                    }
                }
            }

            HIDapi.hid_free_enumeration(top_ptr);

            bool on = true; //ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).AppSettings.Settings["HomeLEDOn"].Value.ToLower() == "true";
            foreach (Joycon jc in j) { // Connect device straight away
                if (jc.state == Joycon.state_.NOT_ATTACHED) {
                    if (jc.out_xbox != null)
                        jc.out_xbox.Connect();
                    if (jc.out_ds4 != null)
                        jc.out_ds4.Connect();

                    try {
                        jc.Attach();
                    } catch (Exception e) {
                        jc.state = Joycon.state_.DROPPED;
                        continue;
                    }

                    jc.SetHomeLight(on);

                    jc.Begin();
                    if (allowCalibration) {
                        jc.getActiveData(activeCaliData(jc.serial_number));
                    }
                }
            }
        }

        public void OnApplicationQuit() {
            foreach (Joycon v in j) {
                if (false)
                    v.PowerOff();

                v.Detach();

                if (v.out_xbox != null) {
                    v.out_xbox.Disconnect();
                }

                if (v.out_ds4 != null) {
                    v.out_ds4.Disconnect();
                }
            }

            controllerCheck.Stop();
            HIDapi.hid_exit();
        }
    }
