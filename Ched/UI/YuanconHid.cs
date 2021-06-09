using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;

namespace Ched.UI
{
    class YuanconHid
    {
        private const int VendorId = 0x1973;
        private const int ProductId = 0x2001;

        private bool active;
        private HidDevice device;
        private List<bool> state;

        public YuanconHid() {
            active = false;
            device = null;
            state = Enumerable.Repeat(false, 38).ToList();
        }

        public void Connect()
        {
            if (device == null)
            {
                device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();
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

                lock (state)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        state[i] = groundData[i];
                    }
                    state[32] = airData[1];
                    state[32 + 1] = airData[0];
                    state[32 + 2] = airData[3];
                    state[32 + 3] = airData[2];
                    state[32 + 4] = airData[5];
                    state[32 + 5] = airData[4];
                }
            }

            device.ReadReport(Update);
        }

        public void Disconnect()
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
}
