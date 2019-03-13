using BMDSwitcherAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Trinity_ATEM_Switcher
{
    public class ATEMSystem
    {
        public IBMDSwitcher Switcher { get; private set; }

        public void ConnectTo(String address)
        {
            IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();

            _BMDSwitcherConnectToFailure failReason = 0;

            try
            {
                IBMDSwitcher switcher;
                // Note that ConnectTo() can take several seconds to return, both for success or failure,
                // depending upon hostname resolution and network response times, so it may be best to
                // do this in a separate thread to prevent the main GUI thread blocking.
                discovery.ConnectTo(address, out switcher, out failReason);
                Switcher = switcher;
            }
            catch (COMException ex)
            {
                Switcher = null;
                // An exception will be thrown if ConnectTo fails. For more information, see failReason.
                switch (failReason)
                {
                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
                        throw new Exception("No response from Switcher", ex);
                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
                        throw new Exception("Switcher has incompatible firmware", ex);
                    default:
                        throw new Exception("Connection failed for unknown reason", ex);
                }
            }

        }
        public void Disconnect()
        {
            Switcher = null;
        }
    }
}
