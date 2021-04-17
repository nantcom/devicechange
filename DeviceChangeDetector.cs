using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NC.DeviceChange
{
    /// <summary>
    /// Detect changes in device list
    /// </summary>
    public static class DeviceChangeDetector
    {

        private class DeviceChangeForm : Form
        {
            private bool _IsFirst = true;
            private Action _Callback;

            public DeviceChangeForm(Action callback)
            {
                _Callback = callback;
            }

            protected override void SetVisibleCore(bool value)
            {
                if (_IsFirst)
                {
                    this.CreateHandle();
                    _IsFirst = false;
                }
                base.SetVisibleCore(false);
            }
            protected override void WndProc(ref Message m)
            {
                // Trap WM_DEVICECHANGE
                if (m.Msg == 0x219)
                {
                    _Callback();
                }
                base.WndProc(ref m);
            }
        }

        private static IObservable<DeviceChange> _DeviceChangeNotifier;
        private static Dictionary<string, PnpDevice> _CurrentDeviceList;
        private static IEqualityComparer<PnpDevice> _Comparer = new PnpDeviceComparer();

        /// <summary>
        /// Get list of PnP Devices
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<PnpDevice> GetPnpDevices()
        {
            using (ManagementClass mc = new ManagementClass("Win32_PnPEntity"))
            using (ManagementObjectCollection moc = mc.GetInstances())
            {
                foreach (ManagementObject obj in moc)
                {
                    if (obj != null && obj["DeviceID"] != null)
                    {
                        yield return new PnpDevice()
                        {
                            Id = (string)obj["DeviceID"],
                            DeviceAvailability = (string)obj["Status"],
                        };
                    }

                    obj.Dispose();
                }
            }
        }

        /// <summary>
        /// Observe when device has changed
        /// </summary>
        /// <param name="callback">Callback to receive Pnp Device Id of new device</param>
        /// <returns></returns>
        public static IDisposable ObserveDeviceChange(Action<DeviceChange> callback)
        {
            if (_DeviceChangeNotifier == null)
            {
                _DeviceChangeNotifier = Observable.Create<DeviceChange>(observer =>
                {
                    DeviceChangeForm dcf = null;

                    _CurrentDeviceList = DeviceChangeDetector.GetPnpDevices().ToDictionary(d => d.Id);

                    Thread t = new Thread(() =>
                    {
                        var sync = new ManualResetEvent(true);

                        dcf = new DeviceChangeForm(() =>
                        {
                            sync.WaitOne();

                            Task.Run(() =>
                            {
                                sync.Reset();
                                var newList = DeviceChangeDetector.GetPnpDevices().ToDictionary(d => d.Id);

                                try
                                {

                                    foreach (var oldItem in _CurrentDeviceList)
                                    {
                                        PnpDevice newItem;
                                        if (newList.TryGetValue(oldItem.Key, out newItem))
                                        {
                                            if (_Comparer.Equals(oldItem.Value, newItem))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                // device has changed
                                                observer.OnNext(new DeviceChange()
                                                {
                                                    Device = newItem,
                                                    ChangeType = DeviceChangeType.Status
                                                });
                                            }
                                        }
                                        else
                                        {
                                            // device was removed
                                            observer.OnNext(new DeviceChange()
                                            {
                                                Device = oldItem.Value,
                                                ChangeType = DeviceChangeType.Removed
                                            });
                                        }
                                    }

                                    foreach (var newItem in newList)
                                    {
                                        PnpDevice oldItem;
                                        if (_CurrentDeviceList.TryGetValue(newItem.Key, out oldItem) == false)
                                        {
                                            // device was added
                                            observer.OnNext(new DeviceChange()
                                            {
                                                Device = newItem.Value,
                                                ChangeType = DeviceChangeType.Added
                                            });
                                        }
                                    }

                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    _CurrentDeviceList = newList;
                                    sync.Set();
                                }
                            });
                        });
                        Application.Run(dcf);
                    });

                    t.SetApartmentState(ApartmentState.STA);
                    t.IsBackground = true;
                    t.Start();


                    return () =>
                    {
                        _DeviceChangeNotifier = null;

                        dcf?.Close();
                        dcf?.Dispose();
                    };

                }).Replay(1).RefCount(1);
            }

            return _DeviceChangeNotifier.Subscribe(callback);
        }
    }
}
