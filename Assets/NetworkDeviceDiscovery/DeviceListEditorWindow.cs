using UnityEditor; 
using NetworkDeviceDiscovery;
using UnityEngine;

    #if UNITY_EDITOR
public class DeviceListEditorWindow : EditorWindow {
	Probe probe = new Probe();
	Device selectedDevice;
    DeviceList beaconList;
	
	void OnEnable() {
		beaconList = new DeviceList (probe);
		probe.Start();
	}

	void OnDisable() {
		probe.Stop ();
	}

	public void OnGUI ()
	{
		beaconList.Draw ();
		Repaint ();
	}
}
    #endif
