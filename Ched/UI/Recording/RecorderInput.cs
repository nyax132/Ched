using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Forms;

using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Ched.UI.Recording
{
    class RecorderInput {

        public interface IRecorderInput
        {
            void Start();
            void Stop();
            List<bool> Read();
            bool ShouldInterceptKey(Keys keys);
        }

        public static void Cleanup()
        {
            // Must call or the program will hang during exiting
            // https://github.com/LibUsbDotNet/LibUsbDotNet/blob/5dd91a2fda393cf11db2072f2b45c3aee2750388/stage/LibUsbDotNet/UsbDevice.cs#L488
            // Yuck.
            UsbDevice.Exit();
        }

        public class KeyboardInput: IRecorderInput
        {
            public void Start() { }
            public void Stop() { }
            public virtual List<bool> Read()
            {
                return Enumerable.Repeat(false, 38).ToList();
            }
            public virtual bool ShouldInterceptKey(Keys keys) { return false; }
        }

        public class YuanconKeyboard: KeyboardInput
        {
            private static Key[] Layout = {
                Key.D6, Key.D5, Key.D4, Key.D3, Key.D2, Key.D1, Key.Z, Key.Y,
                Key.X, Key.W, Key.V, Key.U, Key.T, Key.S, Key.R, Key.Q,
                Key.P, Key.O, Key.N, Key.M, Key.L, Key.K, Key.J, Key.I,
                Key.H, Key.G, Key.F, Key.E, Key.D, Key.C, Key.B, Key.A,
                Key.OemMinus, Key.OemPlus, Key.OemOpenBrackets,
                Key.OemCloseBrackets, Key.OemPipe, Key.OemSemicolon
            };
            public override List<bool> Read()
            {
                return Layout.Select(p => Keyboard.IsKeyDown(p)).ToList();
            }
            public override bool ShouldInterceptKey(Keys keys) {
                return Layout.Contains(KeyInterop.KeyFromVirtualKey((int)keys));
            }
        }

        public class TasollerKeyboard: KeyboardInput
        {
            private static Key[] Layout = {
                Key.A, Key.D1, Key.Z, Key.Q, Key.S, Key.D2, Key.X, Key.W,
                Key.D, Key.D3, Key.C, Key.E, Key.F, Key.D4, Key.V, Key.R,
                Key.G, Key.D5, Key.B, Key.T, Key.H, Key.D6, Key.N, Key.Y,
                Key.J, Key.D7, Key.M, Key.U, Key.K, Key.D8, Key.OemComma, Key.I,
                Key.OemQuestion, Key.OemQuotes, Key.OemPeriod,
                Key.OemSemicolon, Key.OemCloseBrackets, Key.OemOpenBrackets
            };
            public override List<bool> Read()
            {
                return Layout.Select(p => Keyboard.IsKeyDown(p)).ToList();
            }
            public override bool ShouldInterceptKey(Keys keys)
            {
                return Layout.Contains(KeyInterop.KeyFromVirtualKey((int)keys));
            }
        }

        public class OpenithmKeyboard : KeyboardInput
        {
            private static Key[] Layout = {
                Key.A, Key.D1, Key.Z, Key.Q, Key.S, Key.D2, Key.X, Key.W,
                Key.D, Key.D3, Key.C, Key.E, Key.F, Key.D4, Key.V, Key.R,
                Key.G, Key.D5, Key.B, Key.T, Key.H, Key.D6, Key.N, Key.Y,
                Key.J, Key.D7, Key.M, Key.U, Key.K, Key.D8, Key.OemComma, Key.I,
                Key.OemQuestion, Key.OemPeriod, Key.OemQuotes,
                Key.OemSemicolon, Key.OemCloseBrackets, Key.OemOpenBrackets
            };
            public override List<bool> Read()
            {
                return Layout.Select(p => Keyboard.IsKeyDown(p)).ToList();
            }
            public override bool ShouldInterceptKey(Keys keys)
            {
                return Layout.Contains(KeyInterop.KeyFromVirtualKey((int)keys));
            }
        }

        public abstract class HidInput: IRecorderInput
        {
            protected bool active;
            protected UsbDevice device;
            protected UsbEndpointReader reader;
            protected List<bool> state;

            protected abstract UsbDevice GetDevice();
            protected abstract ReadEndpointID GetReadEndpoint();
            protected abstract List<bool> ProcessReport(byte[] reportData);

            public HidInput()
            {
                active = false;
                reader = null;
                state = Enumerable.Repeat(false, 38).ToList();
            }

            public void Start()
            {
                UsbDevice.ForceLibUsbWinBack = true;

                if (device == null) {

#if DEBUG
                    Console.WriteLine("All HID Devices");
                    foreach (UsbRegistry enumDevice in UsbDevice.AllDevices)
                    {
                        Console.WriteLine(enumDevice.DevicePath);
                        Console.WriteLine("VID {0:X} PID {1:X}", enumDevice.Vid, enumDevice.Pid);
                    }
#endif

#if DEBUG
                    Console.WriteLine("Getting HID device...");
#endif
                    device = GetDevice();
                    if (device == null)
                    {
#if DEBUG
                        Console.WriteLine("HID device not found");
#endif
                        return;
                    }
#if DEBUG
                    Console.WriteLine("HID device found");
#endif

                    // Don't understand this 100% yet but it was in the examples
                    // Also for some godforsaken reason the library will only load "libusb-1.0" without ".dll" extension >_>
                    // Also remember to copy over libusb to the build folder like with bass
                    IUsbDevice wholeUsbDevice = device as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // This is a "whole" USB device. Before it can be used, 
                        // the desired configuration and interface must be selected

                        // Select config #1
                        wholeUsbDevice.SetConfiguration(1);

                        // Claim interface #0.
                        wholeUsbDevice.ClaimInterface(0);
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("Not whole USB, not configuring");
#endif
                    }
                }

                if (reader == null)
                {
                    reader = device.OpenEndpointReader(GetReadEndpoint());
                    if (reader == null)
                    {
#if DEBUG
                        Console.WriteLine("Getting reader failed");
#endif
                        device.Close();
                        return;
                    }
#if DEBUG
                    Console.WriteLine("Obtained reader");
#endif
                }

                lock (state)
                {
                    for (int i = 0; i < 38; i++)
                    {
                        state[i] = false;
                    }
                }
                active = true;

                reader.DataReceivedEnabled = true;
                reader.DataReceived += Update;
            }

            private void Update(object sender, EndpointDataEventArgs e)
            {
                if (!active || reader == null) return;

                var reportData = e.Buffer.Take(e.Count).ToArray();
#if DEBUG
                Console.WriteLine(String.Join("/", reportData.Select(p => p.ToString())));
#endif

                var data = ProcessReport(reportData);

                lock (state)
                {
                    for (int i = 0; i < 38; i++)
                    {
                        state[i] = data[i];
                    }
                }
            }

            public void Stop()
            {
                active = false;
                if (reader != null)
                {
                    reader.DataReceived -= Update;
                    reader.DataReceivedEnabled = false;
                    reader.Dispose();
                    reader = null;
                }

                if (device != null)
                {
                    IUsbDevice wholeUsbDevice = device as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                        wholeUsbDevice.Close();
                    }

                    device.Close();
                    device = null;
                }
            }

            public List<bool> Read()
            {
                lock (state)
                {
                    return state.Select(x => x).ToList();
                }
            }

            public bool ShouldInterceptKey(Keys keys) { return false; }
        }

        public class YuanconHid: HidInput
        {
            private const int VendorId = 0x1973;
            private const int ProductId = 0x2001;

            protected override UsbDevice GetDevice()
            {
                return UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));
            }

            protected override ReadEndpointID GetReadEndpoint() {
                return ReadEndpointID.Ep01;
            }

            protected override List<bool> ProcessReport(byte[] reportData)
            {
                if (reportData.Length == 34)
                {
                    var groundData = reportData
                        .Skip(2)
                        .Select(p => p > 20)
                        .ToList();
                    var airData = Convert
                        .ToString(reportData[0], 2)
                        .PadLeft(6, '0')
                        .AsEnumerable()
                        .Reverse()
                        .Select(p => p == '1')
                        .ToList();

                    for (int i = 0; i < 6; i += 2)
                    {
                        var temp = airData[i];
                        airData[i] = airData[i + 1];
                        airData[i + 1] = temp;
                    }

                    return groundData.Concat(airData).ToList();
                }

                return Enumerable.Repeat(false, 38).ToList();
            }
        }

        public class TasollerHidTwo : HidInput
        {
            private const int VendorId = 0x1ccf;
            private const int ProductId = 0x2333;

            protected override UsbDevice GetDevice()
            {
                return UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));
            }

            protected override ReadEndpointID GetReadEndpoint()
            {
                return ReadEndpointID.Ep04;
            }

            protected override List<bool> ProcessReport(byte[] reportData)
            {
                if (reportData.Length == 36)
                {
                    var groundData = reportData
                        .Skip(4)
                        .Select(p => p > 20)
                        .ToList();
                    var airData = Convert
                        .ToString(reportData[3], 2)
                        .PadLeft(8, '0')
                        .AsEnumerable()
                        .Reverse()
                        .Select(p => p == '1')
                        .Take(6)
                        .ToList();

                    return groundData.Concat(airData).ToList();
                }

                return Enumerable.Repeat(false, 38).ToList();
            }
        }

        public class TasollerHidIsno : HidInput
        {
            private const int VendorId = 0x1ccf;
            private const int ProductId = 0x2333;

            protected override UsbDevice GetDevice()
            {
                return UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));
            }

            protected override ReadEndpointID GetReadEndpoint()
            {
                return ReadEndpointID.Ep04;
            }

            protected override List<bool> ProcessReport(byte[] reportData)
            {
                if (reportData.Length == 11)
                {
                    var bitData = reportData
                        .Skip(3)
                        .Select(p => Convert.ToString(p, 2).PadLeft(8, '0'))
                        .SelectMany(p => p.AsEnumerable().Reverse())
                        .Select(p => p == '1');
                    var groundData = bitData.Skip(10).Take(32).ToList();
                    var airData = bitData.Skip(4).Take(6).ToList();

                    return groundData.Concat(airData).ToList();
                }

                return Enumerable.Repeat(false, 38).ToList();
            }
        }
    }
}
