using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;
using System.Windows.Input;

namespace Ched.UI
{
    class RecorderInput {

        public interface IRecorderInput
        {
            void Start();
            void End();
            List<bool> Read();
        }

        public class KeyboardInput: IRecorderInput
        {
            public void Start() { }
            public void End() { }
            public virtual List<bool> Read()
            {
                return Enumerable.Repeat(false, 38).ToList();
            }
        }

        public class YuanconKeyboard: KeyboardInput
        {
            private static Key[] KEYBOARD_YUANCON_LAYOUT = {
                Key.D6, Key.D5, Key.D4, Key.D3, Key.D2, Key.D1, Key.Z, Key.Y,
                Key.X, Key.W, Key.V, Key.U, Key.T, Key.S, Key.R, Key.Q,
                Key.P, Key.O, Key.N, Key.M, Key.L, Key.K, Key.J, Key.I,
                Key.H, Key.G, Key.F, Key.E, Key.D, Key.C, Key.B, Key.A,
                Key.OemMinus, Key.OemPlus, Key.Oem4,
                Key.Oem6, Key.Oem5, Key.Oem1
            };
            public override List<bool> Read()
            {
                return KEYBOARD_YUANCON_LAYOUT.Select(p => Keyboard.IsKeyDown(p)).ToList();
            }
        }

        public class TasollerKeyboard: KeyboardInput
        {
            private static Key[] KEYBOARD_TASOLLER_LAYOUT = {
                Key.A, Key.D1, Key.Z, Key.Q, Key.S, Key.D2, Key.X, Key.W,
                Key.D, Key.D3, Key.C, Key.E, Key.F, Key.D4, Key.V, Key.R,
                Key.G, Key.D5, Key.B, Key.T, Key.H, Key.D6, Key.N, Key.Y,
                Key.J, Key.D7, Key.M, Key.U, Key.K, Key.D8, Key.OemComma, Key.I,
                Key.Oem2, Key.Oem7, Key.OemPeriod,
                Key.Oem1, Key.Oem6, Key.Oem4
            };
            public override List<bool> Read()
            {
                return KEYBOARD_TASOLLER_LAYOUT.Select(p => Keyboard.IsKeyDown(p)).ToList();
            }
        }

        public abstract class HidInput: IRecorderInput
        {
            protected bool active;
            protected HidDevice device;
            protected List<bool> state;

            protected abstract HidDevice GetDevice();
            protected abstract List<bool> ProcessReport(HidReport report);

            public HidInput()
            {
                active = false;
                device = null;
                state = Enumerable.Repeat(false, 38).ToList();
            }

            public void Start()
            {
                if (device == null)
                {
                    device = GetDevice();
                    if (device == null) return;
                }

                lock (state)
                {
                    for (int i = 0; i < 38; i++)
                    {
                        state[i] = false;
                    }
                }
                active = true;
                device.MonitorDeviceEvents = true;
                device.ReadReport(Update);
            }

            private void Update(HidReport report)
            {
                if (!active || device == null) return;

                var data = ProcessReport(report);

                lock (state)
                {
                    for (int i = 0; i < 38; i++)
                    {
                        state[i] = data[i];
                    }
                }

                device.ReadReport(Update);
            }

            public void End()
            {
                active = false;
                if (device != null)
                {
                    device.Dispose();
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
        }

        public class YuanconHid: HidInput
        {
            private const int VendorId = 0x1973;
            private const int ProductId = 0x2001;

            protected override HidDevice GetDevice()
            {
                return HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();
            }

            protected override List<bool> ProcessReport(HidReport report)
            {
                if (report.Data.Length == 34)
                {
                    var groundData = report.Data
                        .Skip(2)
                        .Select(p => p > 128)
                        .ToList();
                    var airData = Convert
                        .ToString(report.Data[0], 2)
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

        public class TasollerHidIsno : HidInput
        {
            private const int VendorId = 0x1ccf;
            private const int ProductId = 0x2333;

            protected override HidDevice GetDevice()
            {
                return HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();
            }

            protected override List<bool> ProcessReport(HidReport report)
            {
                if (report.Data.Length == 11)
                {
                    var bitData = report.Data
                        .Skip(3)
                        .Select(p => Convert.ToString(p, 2).PadLeft(8, '0'))
                        .SelectMany(p => p.AsEnumerable())
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
