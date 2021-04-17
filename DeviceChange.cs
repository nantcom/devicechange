using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DeviceChange
{
    public enum DeviceChangeType
    {
        Added,
        Removed,
        Status
    }

    /// <summary>
    /// Represents a change
    /// </summary>
    public class DeviceChange
    {
        /// <summary>
        /// Changed device
        /// </summary>
        public PnpDevice Device { get; set; }

        /// <summary>
        /// What's changed
        /// </summary>
        public DeviceChangeType ChangeType { get; set; }
    }

}
