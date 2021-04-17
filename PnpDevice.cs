using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DeviceChange
{
    public class PnpDevice
    {
        public string Id { get; set; }


        public string DeviceAvailability { get; set; }
    }

    public class PnpDeviceComparer : IEqualityComparer<PnpDevice>
    {
        public bool Equals(PnpDevice x, PnpDevice y)
        {
            return x.Id == y.Id &&
                    x.DeviceAvailability == y.DeviceAvailability;
        }

        public int GetHashCode([DisallowNull] PnpDevice obj)
        {
            return $"{obj.Id}{obj.DeviceAvailability}".GetHashCode();
        }
    }

}
