using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace USB_Relay_Monitor
{
    public partial class RelayMonitorForm : Form
    {
        // defines the internal driver resource
        private const string RESOURCE_NAME = "USB_Relay_Monitor.USB_RELAY_DEVICE.dll";
        private const string LIBRARY_NAME = "USB_RELAY_DEVICE.dll";

        private const int RELAY_POLL_INTERVAL_MILLIS = 1000;

        // The location of the installed driver
        private readonly string dlldir;

        // How many relays are available and connected
        public int connectedRelayCount;

        private readonly List<Panel> relayPanels;
        private readonly List<Label> relayStatusLabels;

        // Loads the driver from embedded resource
        static public class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr LoadLibrary(string dllToLoad);
        }

        public static class CommonUtils
        {
            public static string LoadUnmanagedLibraryFromResource(Assembly assembly,
                string libraryResourceName,
                string libraryName)
            {
                string tempDllPath = string.Empty;
                using (Stream s = assembly.GetManifestResourceStream(libraryResourceName))
                {
                    byte[] data = new BinaryReader(s).ReadBytes((int)s.Length);

                    string assemblyPath = Path.GetDirectoryName(assembly.Location);
                    tempDllPath = Path.Combine(assemblyPath, libraryName);

                    File.WriteAllBytes(tempDllPath, data);
                }

                NativeMethods.LoadLibrary(libraryName);
                return tempDllPath;
            }
        }

        public RelayMonitorForm()
        {
            // create and load library from the resource
            string tempDllPath = CommonUtils.LoadUnmanagedLibraryFromResource(
                Assembly.GetExecutingAssembly(), RESOURCE_NAME, LIBRARY_NAME);
            dlldir = tempDllPath;

            InitializeComponent();

            relayPanels = new List<Panel>()
            {
                relay1Panel, relay2Panel, relay3Panel, relay4Panel,
                relay5Panel, relay6Panel, relay7Panel, relay8Panel
            };
            relayStatusLabels = new List<Label>()
            {
                relay1StatusLabel, relay2StatusLabel, relay3StatusLabel, relay4StatusLabel, 
                relay5StatusLabel, relay6StatusLabel, relay7StatusLabel, relay8StatusLabel
            };

            // Start the driver
            RelayManager.Init();

            // Check is USB Relay board is connected
            if (RelayManager.DevicesCount() == 0)
            {
                connectedRelayCount = 0;
                foreach (Panel p in relayPanels)
                {
                    p.BackColor = SystemColors.ControlDark;
                    p.Enabled = false;
                }
            }
            else
            {
                // Open the first USB Relay board found
                RelayManager.OpenDevice(0);

                // Enable relay panels for connected relays
                connectedRelayCount = RelayManager.ChannelsCount();
                for (int i = 0; i < relayPanels.Count; i++)
                {
                    relayPanels[i].Enabled = connectedRelayCount > i;
                    relayPanels[i].BackColor = connectedRelayCount > i ? Color.Green : SystemColors.ControlDark;
                }
            }

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += (o, e) =>
            {
                while (true)
                {
                    Thread.Sleep(RELAY_POLL_INTERVAL_MILLIS);
                    PollAvailableRelays();
                }
            };
            backgroundWorker.RunWorkerAsync();
        }

        private void PollAvailableRelays()
        {
            for (int i = 0; i < connectedRelayCount; i++)
            {
                bool relayStatus = RelayManager.ChannelOpened(i + 1);
                Console.WriteLine("Relay " + i + ": " + relayStatus);
                this.Invoke((MethodInvoker)delegate
                {
                    relayPanels[i].BackColor = relayStatus ? Color.Tomato : Color.Green;
                    relayStatusLabels[i].Text = relayStatus ? "On" : "Off";
                });                               
            }
        }
        
    }
}