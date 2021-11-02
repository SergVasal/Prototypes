using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using wclBluetooth;
using wclCommon;

public class AltisAdvertisementWatcher : MonoBehaviour
{
    private const string AudioDevicesClass = "2404";

    private wclBluetoothManager bluetoothManager;
    private Dictionary<long, DiscoveredDeviceInfo> discoveredDevices = new Dictionary<long, DiscoveredDeviceInfo>();
    List<Task> deviceUpdateTasks = new List<Task>();

    private void Start()
    {
        bluetoothManager = new wclBluetoothManager();

        bluetoothManager.OnPasskeyRequest += PasskeyRequestHandler;
        bluetoothManager.OnNumericComparison += NumericComparisonHandler;
        bluetoothManager.OnIoCapabilityRequest += IoCapabilityRequestHandler;
        bluetoothManager.OnDeviceFound += DeviceFoundHandler;
        bluetoothManager.OnDiscoveringCompleted += DiscoveringCompletedHandler;

        var openResult = bluetoothManager.Open();
        if (openResult != wclErrors.WCL_E_SUCCESS)
        {
            Debug.LogError(openResult);
        }

        bluetoothManager.GetRadio(out var bluetoothRadio);

        bluetoothRadio.Discover(20, wclBluetoothDiscoverKind.dkClassic);
    }

    private void OnDestroy()
    {
        bluetoothManager.Close();
    }

    private void NumericComparisonHandler(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
    {
        Confirm = true;
    }

    private void PasskeyRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
    {
        Passkey = 123456;
    }

    private void IoCapabilityRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out wclBluetoothMitmProtection Mitm, out wclBluetoothIoCapability IoCapability, out bool OobPresent)
    {
        Debug.LogWarning($"IoCapabilityRequestHandler!!!!!!!!!!!!!!!!!");
        Mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredBonding;
        IoCapability = wclBluetoothIoCapability.iocapDisplayYesNo;

        // To accept OOB pairing set this parameter to True.
        OobPresent = false;
    }

    private void DeviceFoundHandler(object sender, wclBluetoothRadio radio, long address)
    {
        Debug.Log($"Found device with address: {address.ToString("X12")}");
        var discoveredDevice = new KeyValuePair<long, DiscoveredDeviceInfo>(address, new DiscoveredDeviceInfo());
        discoveredDevices.Add(discoveredDevice.Key, discoveredDevice.Value);
        deviceUpdateTasks.Add(Task.Run(() => UpdateDeviceInfo(radio, discoveredDevice.Key, discoveredDevice.Value)));
    }

    private async void DiscoveringCompletedHandler(object sender, wclBluetoothRadio radio, int error)
    {
        Debug.Log($"Radio: {radio} DiscoveringCompleted, Error: {error.ToString()}");

        if (discoveredDevices.Count == 0)
        {
            Debug.Log($"There are no devices discovered!");
            return;
        }

        await Task.WhenAll(deviceUpdateTasks);

        var audioDevices = discoveredDevices
            .Where(x => x.Value.ClassOfDevice.Contains(AudioDevicesClass) && x.Value.Services != null)
            .ToList();

        foreach (var audioDevice in audioDevices)
        {
            Debug.LogWarning($"Audio device: {audioDevice.Value.DeviceName}");
        }

        var closestAudioDevice = audioDevices.OrderByDescending(x => x.Value.RSSI).FirstOrDefault();
        if (!closestAudioDevice.Equals(default(KeyValuePair<long, DiscoveredDeviceInfo>)))
        {
            Debug.LogWarning($"ClosestAudioDevice Audio device: {closestAudioDevice.Value.DeviceName}");
        }

        var pairingResult = radio.RemotePair(closestAudioDevice.Key);
        Debug.LogWarning(pairingResult.ToString("x8"));

        // install the services
    }

    private void UpdateDeviceInfo(wclBluetoothRadio radio, long deviceAddress, DiscoveredDeviceInfo discoveredDeviceInfo)
    {
        radio.GetRemoteName(deviceAddress, out var deviceName);
        discoveredDeviceInfo.DeviceName = deviceName;

        radio.GetRemoteCod(deviceAddress, out var remoteClassOfDevice);
        discoveredDeviceInfo.ClassOfDevice = remoteClassOfDevice.ToString("x8");

        radio.GetRemoteRssi(deviceAddress, out var rssi);
        discoveredDeviceInfo.RSSI = rssi;

        Guid g = Guid.Empty;
        radio.EnumRemoteServices(deviceAddress, g, out var services);
        discoveredDeviceInfo.Services = services;

        Debug.LogWarning($"Device info updated for {deviceName} ClassOfDevice: {discoveredDeviceInfo.ClassOfDevice}, RSSI: {discoveredDeviceInfo.RSSI}");
    }
}

internal class DiscoveredDeviceInfo
{
    public string DeviceName { get; set; }

    public wclBluetoothService[] Services { get; set; }

    public string ClassOfDevice { get; set; }

    public int RSSI { get; set; }
}
