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
    private wclBluetoothRadio bluetoothRadio;
    private List<wclRfCommClient> clients = new List<wclRfCommClient>();

    private wclPowerEventsMonitor FPowerMonitor;

    private Dictionary<long, DiscoveredAudioDeviceInfo> discoveredAudioDevices = new Dictionary<long, DiscoveredAudioDeviceInfo>();
    private KeyValuePair<long, DiscoveredAudioDeviceInfo> selectedDevice;

    private CancellationTokenSource updateDevicesCancellationTokenSource = new CancellationTokenSource();
    private CancellationTokenSource connectToDeviceTokenSource = new CancellationTokenSource();

    private Boolean WasOpened = false;

    private void Start()
    {
        bluetoothManager = new wclBluetoothManager();

        bluetoothManager.OnNumericComparison += NumericComparisonHandler;
        bluetoothManager.OnPasskeyRequest += PasskeyRequestHandler;
        bluetoothManager.OnPinRequest += PinRequestHandler;
        bluetoothManager.OnConfirm += ConfirmHandler;
        bluetoothManager.OnAuthenticationCompleted += BluetoothManagerOnOnAuthenticationCompleted;
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

        bluetoothManager.GetRadio(out bluetoothRadio);

        Int32 Res = bluetoothRadio.Discover(60, wclBluetoothDiscoverKind.dkClassic);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Debug.LogError(Res.ToString("X8"));
    }

    private void BluetoothManagerOnOnAuthenticationCompleted(object sender, wclBluetoothRadio radio, long address, int error)
    {
        Debug.Log($"Authentication completed. Error: {error.ToString("X8")}");
    }

    private void OnDisable()
    {
        Debug.LogWarning($"Disable!");

        bluetoothManager.OnNumericComparison -= NumericComparisonHandler;
        bluetoothManager.OnPasskeyRequest -= PasskeyRequestHandler;
        bluetoothManager.OnPinRequest -= PinRequestHandler;
        bluetoothManager.OnConfirm -= ConfirmHandler;
        bluetoothManager.OnAuthenticationCompleted -= BluetoothManagerOnOnAuthenticationCompleted;
        bluetoothManager.OnIoCapabilityRequest -= IoCapabilityRequestHandler;
        bluetoothManager.OnOobDataRequest -= OobDataRequestHandler;
        bluetoothManager.OnProtectionLevelRequest -= ProtectionLevelRequestHandler;
        bluetoothManager.OnDeviceFound -= DeviceFoundHandler;
        bluetoothManager.OnDiscoveringCompleted -= DiscoveringCompletedHandler;


        FPowerMonitor.OnPowerStateChanged -= PowerStateChangedHandler;

        updateDevicesCancellationTokenSource.Cancel();
        updateDevicesCancellationTokenSource.Dispose();

        connectToDeviceTokenSource.Cancel();
        connectToDeviceTokenSource.Dispose();

        foreach (var client in clients)
        {
            var driverInstallResult = bluetoothRadio.UninstallDevice(selectedDevice.Key, client.Service);
            Debug.LogWarning($"Driver uninstall for service: {client.Service}, channel: {client.Channel} result: {driverInstallResult.ToString("X8")}");

            client.Disconnect();

        }
        clients.Clear();

        if (IsSelectedDevicePaired())
        {
            var unPairingResult = bluetoothRadio.RemoteUnpair(selectedDevice.Key);
            Debug.LogError($"Unpairing result of device {selectedDevice.Value.DeviceName} with address {selectedDevice.Key.ToString("X12")}: {unPairingResult.ToString("X8")}");
        }

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogWarning($"Cancel key pressed!");
            updateDevicesCancellationTokenSource.Cancel();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.LogWarning($"Pair key pressed!");
            updateDevicesCancellationTokenSource.Cancel();
            PairWithSelectedDevice();
            TryInstallDriversForSelectedDeviceServices();
            ConnectToServices();
        }
    }

    private void DeviceFoundHandler(object sender, wclBluetoothRadio radio, long address)
    {
        radio.GetRemoteCod(address, out var remoteClassOfDevice);
        var classOfDevice = remoteClassOfDevice.ToString("x8");
        Debug.Log($"Found device with address: {address.ToString("X12")}, class of device: {classOfDevice}");

        if (classOfDevice.Contains(AudioDevicesClass))
        {
            var discoveredAudioDevice = new KeyValuePair<long, DiscoveredAudioDeviceInfo>(address, new DiscoveredAudioDeviceInfo());
            discoveredAudioDevices.Add(discoveredAudioDevice.Key, discoveredAudioDevice.Value);

            Task.Run(() => UpdateDeviceInfo(radio, discoveredAudioDevice.Key, discoveredAudioDevice.Value), updateDevicesCancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Debug.LogError(t.Exception);
                }
            });

            Debug.LogWarning($"Found audio device with address: {address.ToString("X12")}, audio devices count: {discoveredAudioDevices.Count}");
        }
    }

    private async Task UpdateDeviceInfo(wclBluetoothRadio radio, long deviceAddress, DiscoveredAudioDeviceInfo discoveredAudioDeviceInfo)
    {
        radio.GetRemoteName(deviceAddress, out var deviceName);
        discoveredAudioDeviceInfo.DeviceName = deviceName;
        Debug.Log($"GetRemoteName {deviceName}!");

        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            Debug.LogWarning($"UpdateDeviceInfo Cancel!");

            return;
        }

        radio.GetRemoteRssi(deviceAddress, out var rssi);
        discoveredAudioDeviceInfo.RSSI = rssi;
        Debug.Log($"GetRemoteRssi {rssi} deviceName: {deviceName}!");
        UpdateSelectedDevice();

        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            Debug.LogWarning($"UpdateDeviceInfo Cancel!");

            return;
        }

        radio.IsRemoteDeviceInRange(deviceAddress, out var isInRange);
        discoveredAudioDeviceInfo.IsInRange = isInRange;
        Debug.Log($"IsRemoteDeviceInRange {isInRange}! deviceName: {deviceName}");
        UpdateSelectedDevice();

        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            Debug.LogWarning($"UpdateDeviceInfo Cancel!");

            return;
        }

        Guid g = Guid.Empty;
        radio.EnumRemoteServices(deviceAddress, g, out var services);
        discoveredAudioDeviceInfo.Services = services;
        UpdateSelectedDevice();

        Debug.Log($"EnumRemoteServices {services}! deviceName: {deviceName}");

        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            Debug.LogWarning($"UpdateDeviceInfo Cancel!");

            return;
        }

        Debug.LogWarning($"Device info updated for {deviceName}, RSSI: {discoveredAudioDeviceInfo.RSSI} isInRange: {isInRange} services: {services}");
        await Task.Delay(TimeSpan.FromSeconds(3), updateDevicesCancellationTokenSource.Token).ContinueWith(tsk => { });

        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            Debug.LogWarning($"UpdateDeviceInfo Cancel!");

            return;
        }

        await UpdateDeviceInfo(radio, deviceAddress, discoveredAudioDeviceInfo);
    }

    private void UpdateSelectedDevice()
    {
        if (updateDevicesCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        var selectedDevices = discoveredAudioDevices
            .Where(x => x.Value.Services != null && x.Value.IsInRange && x.Value.RSSI > -128)
            .ToList();
        Debug.LogWarning($"Selected devices count: {selectedDevices.Count}");

        if (selectedDevices.Count == 0)
        {
            selectedDevice = default;
            Debug.LogWarning($"NO SELECTED DEVICE!");
            return;
        }

        selectedDevice = selectedDevices.OrderByDescending(x => x.Value.RSSI).FirstOrDefault();
        Debug.LogWarning($"SELECTED DEVICE is {selectedDevice.Value.DeviceName}");
    }

    private void PairWithSelectedDevice()
    {
        updateDevicesCancellationTokenSource.Cancel();

        if (selectedDevice.Equals(default(KeyValuePair<long, DiscoveredAudioDeviceInfo>)))
        {
            Debug.LogError($"NO SELECTED DEVICE TO PAIR WITH!");
            return;
        }

        Debug.LogWarning($"Start new pairing with selected device {selectedDevice.Value.DeviceName} with address {selectedDevice.Key.ToString("X12")}");

        var pairingResult = bluetoothRadio.RemotePair(selectedDevice.Key, wclBluetoothPairingMethod.pmClassic);
        if (pairingResult != wclErrors.WCL_E_SUCCESS)
        {
            Debug.LogError($"Error pairing with device. Error code: {pairingResult.ToString("X8")}");
        }
        else
        {
            Debug.Log($"Pairing completed successfully! Check for authentification! pairing device: {selectedDevice.Value.DeviceName} with address: {selectedDevice.Key} pairingResult: {pairingResult.ToString("X8")}");
        }
    }

    private bool IsSelectedDevicePaired()
    {
        var checkPairedResult = bluetoothRadio.GetRemotePaired(selectedDevice.Key, out var paired);
        if (checkPairedResult != wclErrors.WCL_E_SUCCESS)
            Debug.LogError($"Error checking if device is paired: {checkPairedResult.ToString("X8")}");

        Debug.LogWarning($"Selected device paired: {paired}");
        return paired;
    }

    private void TryInstallDriversForSelectedDeviceServices()
    {
        Guid g = Guid.Empty;
        bluetoothRadio.EnumRemoteServices(selectedDevice.Key, g, out var services);
        selectedDevice.Value.Services = services;

        if (selectedDevice.Value.Services == null)
        {
            Debug.LogError($"Can't install drivers for services. Services are null!");
            return;
        }

        foreach (var service in selectedDevice.Value.Services)
        {
            if (connectToDeviceTokenSource.IsCancellationRequested)
            {
                Debug.LogError($"TryInstallDriversForSelectedDeviceServices cancelled!");
                return;
            }

            var driverInstallResult = bluetoothRadio.InstallDevice(selectedDevice.Key, service.Uuid);
            Debug.LogWarning($"Driver install for service: {service.Name}, channel: {service.Channel} result: {driverInstallResult.ToString("X8")}");
        }
    }

    private void ConnectToServices()
    {
        Guid g = Guid.Empty;
        bluetoothRadio.EnumRemoteServices(selectedDevice.Key, g, out var services);
        selectedDevice.Value.Services = services;

        if (selectedDevice.Value.Services == null)
        {
            Debug.LogError($"Can't connect to services. Services are null!");
            return;
        }

        foreach (var selectedDeviceService in selectedDevice.Value.Services)
        {
            if (connectToDeviceTokenSource.IsCancellationRequested)
            {
                Debug.LogError($"ConnectToServices cancelled!");
                return;
            }

            var client = new wclRfCommClient();
            clients.Add(client);

            client.Address = selectedDevice.Key;
            client.Authentication = false;
            client.Encryption = false;
            client.Timeout = 50;

            client.Channel = selectedDeviceService.Channel;
            client.Service = selectedDeviceService.Uuid;

            Int32 connectionResult = client.Connect(bluetoothRadio);
            if (connectionResult != wclErrors.WCL_E_SUCCESS)
            {
                Debug.LogError($"Failed to connect to service Error: {connectionResult.ToString("X8")}");
            }
            else
            {
                Debug.Log($"Successfully connected to service channel {selectedDeviceService.Channel}, Uuid: {selectedDeviceService.Uuid}");
            }
        }
    }

    private void DiscoveringCompletedHandler(object sender, wclBluetoothRadio radio, int error)
    {
        Debug.Log($"DiscoveringCompleted, Error: {error.ToString("X8")}");
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

internal class DiscoveredAudioDeviceInfo
{
    public string DeviceName { get; set; }

    public wclBluetoothService[] Services { get; set; }

    public int RSSI { get; set; }

    public bool IsInRange { get; set; }
}