using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using wclBluetooth;
using wclCommon;

public class AltisAdvertisementWatcher : MonoBehaviour
{
    private const string AudioDevicesClass = "2404";

    private wclBluetoothManager bluetoothManager;
    private wclPowerEventsMonitor FPowerMonitor;

    private Dictionary<long, DiscoveredDeviceInfo> discoveredDevices = new Dictionary<long, DiscoveredDeviceInfo>();
    private List<Task> deviceUpdateTasks = new List<Task>();
    private List<wclRfCommClient> clients = new List<wclRfCommClient>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    private Boolean WasOpened = false;

    private void Start()
    {
        bluetoothManager = new wclBluetoothManager();

        bluetoothManager.OnNumericComparison += NumericComparisonHandler;
        bluetoothManager.OnPasskeyRequest += PasskeyRequestHandler;
        bluetoothManager.OnPinRequest += PinRequestHandler;
        bluetoothManager.OnConfirm += ConfirmHandler;
        bluetoothManager.OnIoCapabilityRequest += IoCapabilityRequestHandler;
        bluetoothManager.OnOobDataRequest += OobDataRequestHandler;
        bluetoothManager.OnProtectionLevelRequest += ProtectionLevelRequestHandler;
        bluetoothManager.OnDeviceFound += DeviceFoundHandler;
        bluetoothManager.OnDiscoveringCompleted += DiscoveringCompletedHandler;

        FPowerMonitor = new wclPowerEventsMonitor();
        FPowerMonitor.OnPowerStateChanged += PowerStateChangedHandler;
        FPowerMonitor.Open();

        var openResult = bluetoothManager.Open();
        if (openResult != wclErrors.WCL_E_SUCCESS)
        {
            Debug.LogError(openResult.ToString("X8"));
        }

        bluetoothManager.GetRadio(out var bluetoothRadio);

        Int32 Res = bluetoothRadio.Discover(Convert.ToByte(50), wclBluetoothDiscoverKind.dkClassic);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.LogError(Res.ToString("X8"));
    }

    private void OnDisable()
    {
        Debug.LogWarning($"Disable!");

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();

        Task.Run(() =>
        {
            foreach (var client in clients)
            {
                client.Disconnect();
            }
        });

        Int32 Res = bluetoothManager.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.LogError(Res.ToString("X8"));
        bluetoothManager = null;

        FPowerMonitor.Close();
        FPowerMonitor = null;

    }

    private void PinRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
    {
        Pin = "0000";
    }

    private void NumericComparisonHandler(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
    {
        Confirm = true;
    }

    private void PasskeyRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
    {
        Passkey = 123456;
    }

    void ConfirmHandler(object Sender, wclBluetoothRadio Radio, long Address, out bool Confirm)
    {
        Confirm = true;
    }

    void OobDataRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out wclBluetoothOobData OobData)
    {
        // This event fires when a remote device requests OOB data.
        OobData = null;
    }

    private void ProtectionLevelRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out wclBluetoothleProtectionLevel Protection)
    {
        Protection = wclBluetoothleProtectionLevel.pplDefault;
    }

    private void IoCapabilityRequestHandler(object Sender, wclBluetoothRadio Radio, long Address, out wclBluetoothMitmProtection Mitm, out wclBluetoothIoCapability IoCapability, out bool OobPresent)
    {
        Mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredBonding;
        IoCapability = wclBluetoothIoCapability.iocapDisplayYesNo;

        // To accept OOB pairing set this parameter to True.
        OobPresent = false;
    }

    private void DeviceFoundHandler(object sender, wclBluetoothRadio radio, long address)
    {
        radio.GetRemoteCod(address, out var remoteClassOfDevice);
        var classOfDevice = remoteClassOfDevice.ToString("x8");
        Debug.Log($"Found device with address: {address.ToString("X12")}, class of device: {classOfDevice}");

        var discoveredDevice = new KeyValuePair<long, DiscoveredDeviceInfo>(address, new DiscoveredDeviceInfo());
        discoveredDevices.Add(discoveredDevice.Key, discoveredDevice.Value);

        //deviceUpdateTasks.Add(Task.Run(() => UpdateDeviceInfo(radio, discoveredDevice.Key, discoveredDevice.Value), cancellationTokenSource.Token));
    }

    private void DiscoveringCompletedHandler(object sender, wclBluetoothRadio radio, int error)
    {
        Debug.Log($"DiscoveringCompleted, Error: {error.ToString("X8")}");

        //Task.Run(() => ConnectToDevice(radio));
    }

    private async void ConnectToDevice(wclBluetoothRadio radio)
    {
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

            var pairingResult = radio.RemotePair(closestAudioDevice.Key);
            Debug.LogWarning($"{pairingResult.ToString("X8")}");

            foreach (var service in closestAudioDevice.Value.Services)
            {
                ConnectToService(radio, closestAudioDevice.Key, service);
            }
        }
    }

    private void ConnectToService(wclBluetoothRadio radio, long audioDeviceAddress, wclBluetoothService service)
    {
        var driverInstallResult = radio.InstallDevice(audioDeviceAddress, service.Uuid);
        Debug.LogWarning($"Driver install for service: {service.Name}, channel: {service.Channel} result: {driverInstallResult.ToString("X8")}");

        var deviceClient = new wclRfCommClient();
        clients.Add(deviceClient);

        deviceClient.Address = audioDeviceAddress;
        deviceClient.Authentication = false;
        deviceClient.Encryption = false;
        deviceClient.Timeout = 20;

        deviceClient.Channel = service.Channel;
        deviceClient.Service = service.Uuid;

        var connectionResult = deviceClient.Connect(radio);
        Debug.LogWarning($"Connection to service: {service.Name}, channel: {service.Channel} result: {connectionResult.ToString("X8")}");
    }

    private void UpdateDeviceInfo(wclBluetoothRadio radio, long deviceAddress, DiscoveredDeviceInfo discoveredDeviceInfo)
    {
        Debug.Log($"Start updating device info!");
        radio.GetRemoteCod(deviceAddress, out var remoteClassOfDevice);
        discoveredDeviceInfo.ClassOfDevice = remoteClassOfDevice.ToString("x8");

        if (discoveredDeviceInfo.ClassOfDevice.Contains(AudioDevicesClass))
        {
            radio.GetRemoteName(deviceAddress, out var deviceName);
            discoveredDeviceInfo.DeviceName = deviceName;

            radio.GetRemoteRssi(deviceAddress, out var rssi);
            discoveredDeviceInfo.RSSI = rssi;

            Guid g = Guid.Empty;
            radio.EnumRemoteServices(deviceAddress, g, out var services);
            discoveredDeviceInfo.Services = services;

            Debug.LogWarning($"Device info updated for {deviceName} ClassOfDevice: {discoveredDeviceInfo.ClassOfDevice}, RSSI: {discoveredDeviceInfo.RSSI} services: {services}");
        }
        else
        {
            Debug.LogWarning($"Non-relevant Class of Device!");
        }
    }

    private void PowerStateChangedHandler(object Sender, wclPowerState State)
    {
        switch (State)
        {
            case wclPowerState.psResumeAutomatic:
                if (WasOpened)
                {
                    WasOpened = false;
                    bluetoothManager.Open();
                }
                break;

            case wclPowerState.psResume:
                break;

            case wclPowerState.psSuspend:
                if (bluetoothManager.Active)
                {
                    WasOpened = true;
                    bluetoothManager.Close();
                }
                break;

            case wclPowerState.psUnknown:
                break;
        }
    }
}

internal class DiscoveredDeviceInfo
{
    public string DeviceName { get; set; }

    public wclBluetoothService[] Services { get; set; }

    public string ClassOfDevice { get; set; }

    public int RSSI { get; set; }
}
