using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BMDSwitcherAPI;
using System.Runtime.InteropServices;

namespace Trinity_ATEM_Switcher
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IBMDSwitcherDiscovery m_switcherDiscovery;
        private IBMDSwitcher m_switcher;
        private IBMDSwitcherMixEffectBlock m_mixEffectBlock1;
        private SwitcherMonitor m_switcherMonitor;
        private MixEffectBlockMonitor m_mixEffectBlockMonitor;
        private IBMDSwitcherTransitionParameters m_transition;
        private bool m_moveSliderDownwards = false;
        private _BMDSwitcherTransitionStyle existing_style;
        private List<InputMonitor> m_inputMonitors = new List<InputMonitor>();
        private long currentPreview;
        private long currentProgram;
        private long currentKey;
        public MainWindow()
        {
            InitializeComponent();

            m_switcherMonitor = new SwitcherMonitor();
            

            m_mixEffectBlockMonitor = new MixEffectBlockMonitor();
            

            m_switcherDiscovery = new CBMDSwitcherDiscovery();
            if (m_switcherDiscovery == null)
            {
                MessageBox.Show("Could not create Switcher Discovery Instance.\nATEM Switcher Software may not be installed.", "Error");
                Environment.Exit(1);
            }

            SwitcherDisconnected();		// start with switcher disconnected

            _BMDSwitcherConnectToFailure failReason = 0;
            string address = "192.168.111.102";

            try
            {
                // Note that ConnectTo() can take several seconds to return, both for success or failure,
                // depending upon hostname resolution and network response times, so it may be best to
                // do this in a separate thread to prevent the main GUI thread blocking.
                m_switcherDiscovery.ConnectTo(address, out m_switcher, out failReason);
            }
            catch (COMException)
            {
                // An exception will be thrown if ConnectTo fails. For more information, see failReason.
                switch (failReason)
                {
                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
                        MessageBox.Show("No response from Switcher", "Error");
                        break;
                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
                        MessageBox.Show("Switcher has incompatible firmware", "Error");
                        break;
                    default:
                        MessageBox.Show("Connection failed for unknown reason", "Error");
                        break;
                }
                return;
            }

            SwitcherConnected();
        }


        private void OnInputLongNameChanged(object sender, object args)
        {
            //this.Invoke((Action)(() => UpdatePopupItems()));
        }

        private void SwitcherConnected()
        {
            //buttonConnect.Enabled = false;

            // Get the switcher name:
            string switcherName;
            m_switcher.GetProductName(out switcherName);
            //textBoxSwitcherName.Text = switcherName;
            
            // Install SwitcherMonitor callbacks:
            m_switcher.AddCallback(m_switcherMonitor);

            // We create input monitors for each input. To do this we iterate over all inputs:
            // This will allow us to update the combo boxes when input names change:
            IBMDSwitcherInputIterator inputIterator = null;
            IntPtr inputIteratorPtr;
            Guid inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator != null)
            {
                IBMDSwitcherInput input;
                inputIterator.Next(out input);
                while (input != null)
                {
                    InputMonitor newInputMonitor = new InputMonitor(input);
                    input.AddCallback(newInputMonitor);
                    newInputMonitor.LongNameChanged += new SwitcherEventHandler(OnInputLongNameChanged);

                    m_inputMonitors.Add(newInputMonitor);

                    inputIterator.Next(out input);
                }
            }

            // We want to get the first Mix Effect block (ME 1). We create a ME iterator,
            // and then get the first one:
            m_mixEffectBlock1 = null;

            IBMDSwitcherMixEffectBlockIterator meIterator = null;
            IntPtr meIteratorPtr;
            Guid meIteratorIID = typeof(IBMDSwitcherMixEffectBlockIterator).GUID;
            m_switcher.CreateIterator(ref meIteratorIID, out meIteratorPtr);
            if (meIteratorPtr != null)
            {
                meIterator = (IBMDSwitcherMixEffectBlockIterator)Marshal.GetObjectForIUnknown(meIteratorPtr);
            }

            if (meIterator == null)
                return;

            if (meIterator != null)
            {
                meIterator.Next(out m_mixEffectBlock1);
            }

            if (m_mixEffectBlock1 == null)
            {
                MessageBox.Show("Unexpected: Could not get first mix effect block", "Error");
                return;
            }

            // Install MixEffectBlockMonitor callbacks:
            m_mixEffectBlock1.AddCallback(m_mixEffectBlockMonitor);

            //MixEffectBlockSetEnable(true);
            getInputNames();
            //UpdateTransitionFramesRemaining();
            //UpdateSliderPosition();

            //setCurrent preview ID
            m_mixEffectBlock1.GetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput, out currentPreview);
            currentKey = -1;
            //setCurrent Program ID
            m_mixEffectBlock1.GetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdProgramInput, out currentProgram);
            updateProgPrevUI(currentPreview, false);
            updateProgPrevUI(currentProgram, true);
            UpdateAuxSourceCombos();
        }

        private void getInputNames()
        {

            // Get an input iterator.
            IBMDSwitcherInputIterator inputIterator = null;
            IntPtr inputIteratorPtr;
            Guid inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator == null)
                return;

            IBMDSwitcherInput input;
            inputIterator.Next(out input);
            while (input != null)
            {
                string inputName;
                long inputId;

                input.GetInputId(out inputId);
                input.GetLongName(out inputName);
                Console.WriteLine(inputId + " " + inputName);
                switch (inputId)
                {
                    case 2:
                        progBut1.Content = inputName;
                        prevBut1.Content = inputName;
                        break;
                    case 3:
                        progBut2.Content = inputName;
                        prevBut2.Content = inputName;
                        break;
                    case 4:
                        progBut3.Content = inputName;
                        prevBut3.Content = inputName;
                        break;
                    case 5:
                        progBut4.Content = inputName;
                        prevBut4.Content = inputName;
                        break;
                    case 6:
                        progBut5.Content = inputName;
                        prevBut5.Content = inputName;
                        break;
                    case 3010:
                        progBut6.Content = inputName;
                        prevBut6.Content = inputName;
                        break;
                }
               
                inputIterator.Next(out input);
            }
        }

        private void SwitcherDisconnected()
        {

            if (m_mixEffectBlock1 != null)
            {
                // Remove callback
                m_mixEffectBlock1.RemoveCallback(m_mixEffectBlockMonitor);

                // Release reference
                m_mixEffectBlock1 = null;
            }

            if (m_switcher != null)
            {
                // Remove callback:
                m_switcher.RemoveCallback(m_switcherMonitor);

                // release reference:
                m_switcher = null;
            }
        }

        private void updateProgPrevUI(long inputID,Boolean progBut)
        {
            Console.WriteLine("inputID" + inputID);
            switch (inputID)
            {
                case 2:
                    if (progBut == true)
                    {
                        progBut1.IsChecked = true;
                    } else {
                        prevBut1.IsChecked = true;
                    }
                    
                    break;
                case 3:
                    if (progBut == true)
                    {
                        progBut2.IsChecked = true;
                    }
                    else
                    {
                        prevBut2.IsChecked = true;
                    }
                    break;
                case 4:
                    if (progBut == true)
                    {
                        progBut3.IsChecked = true;
                    }
                    else
                    {
                        prevBut3.IsChecked = true;
                    }
                    break;
                case 5:
                    if (progBut == true)
                    {
                        progBut4.IsChecked = true;
                    }
                    else
                    {
                        prevBut4.IsChecked = true;
                    }
                    break;
                case 6:
                    if (progBut == true)
                    {
                        progBut5.IsChecked = true;
                    }
                    else
                    {
                        prevBut5.IsChecked = true;
                    }
                    break;
                case 3010:
                    if (progBut == true)
                    {
                        progBut6.IsChecked = true;
                    }
                    else
                    {
                        prevBut6.IsChecked = true;
                    }
                    break;
            }
        }
        private void ChangeAUX(int AuxNumber, long inputId )
        {
           
            //Console.WriteLine("AuxNumber=" + AuxNumber + " inputId=" + inputId);
            IBMDSwitcherInputIterator inputIterator = null;
            IntPtr inputIteratorPtr;
            Guid inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            this.m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator != null)
            {
                IBMDSwitcherInput input;
                inputIterator.Next(out input);
                int AUXCount = 0;
                while (input != null)
                {
                    BMDSwitcherAPI._BMDSwitcherPortType inputPortType;
                    input.GetPortType(out inputPortType);
                    if (inputPortType == BMDSwitcherAPI._BMDSwitcherPortType.bmdSwitcherPortTypeAuxOutput)
                    {
                        AUXCount++;
                        if (AUXCount == AuxNumber)
                        {
                            IBMDSwitcherInputAux WkAux = (IBMDSwitcherInputAux)input;
                            WkAux.SetInputSource(inputId);
                            break;
                        }
                    }
                    inputIterator.Next(out input);
                }
            }


        }

        private void UpdateAuxSourceCombos()
        {
            long lvSource = 0;
            IBMDSwitcherInputIterator inputIterator = null;
            IntPtr inputIteratorPtr;
            Guid inputIteratorIID = typeof(IBMDSwitcherInputIterator).GUID;
            this.m_switcher.CreateIterator(ref inputIteratorIID, out inputIteratorPtr);
            if (inputIteratorPtr != null)
            {
                inputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(inputIteratorPtr);
            }

            if (inputIterator != null)
            {
                IBMDSwitcherInput input;
                inputIterator.Next(out input);
                int AUXCount = 0;
  
                while (input != null)
                {
                    BMDSwitcherAPI._BMDSwitcherPortType inputPortType;
                    input.GetPortType(out inputPortType);
                    if (inputPortType == BMDSwitcherAPI._BMDSwitcherPortType.bmdSwitcherPortTypeAuxOutput)
                        
                    {
                       
                        IBMDSwitcherInputAux WkAux = (IBMDSwitcherInputAux)input;
                        WkAux.GetInputSource(out lvSource);
                        AUXCount++;
                        if (AUXCount == 1)
                        {
                            switch (lvSource)
                            {
                                case 6:
                                    aux1But1.IsChecked = true;
                                    break;
                                case 10010:
                                    aux1But2.IsChecked = true;
                                    break;
                                case 3010:
                                    aux1But3.IsChecked = true;
                                    break;
                                case 2:
                                    aux1But4.IsChecked = true;
                                    break;
                                case 3:
                                    aux1But5.IsChecked = true;
                                    break;
                                case 4:
                                    aux1But7.IsChecked = true;
                                    break;
                                case 5:
                                    aux1But8.IsChecked = true;
                                    break;

                            }
                            //ComboBox WkCombo = (ComboBox)this.Controls.Find("comboBoxAUX" + AUXCount, true)[0];
                            //foreach (StringObjectPair<long> item in WkCombo.Items)
                            //{
                            //    if (item.value == lvSource)
                            //    {
                            //WkCombo.SelectedIndex = WkCombo.Items.IndexOf(item);
                            //        break;
                            //    }
                            //}
                        }
                        if (AUXCount == 3)
                            {
                                
                                switch (lvSource)
                                {
                                   
                                    case 10010:
                                        aux2But1.IsChecked = true;
                                        break;
                                    case 3010:
                                        aux2But2.IsChecked = true;
                                        break;
                                    case 2:
                                        aux2But3.IsChecked = true;
                                        break;
                                    case 3:
                                        aux2But4.IsChecked = true;
                                        break;
                                    case 4:
                                        aux2But6.IsChecked = true;
                                        break;
                                    case 5:
                                        aux2But7.IsChecked = true;
                                        break;
                                    case 6:
                                        aux2But5.IsChecked = true;
                                        break;

                                }
                                
                            
                            
                            }
                        

                    }
                    inputIterator.Next(out input);
                }
            }
        }

        private void progBut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            var butVal = button.Tag;
            int butNum = Convert.ToInt32(butVal);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdProgramInput,
                   butNum);
            }
        }

        private void prevBut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            var butVal = button.Tag;
            int butNum = Convert.ToInt32(butVal);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                   butNum);
                currentPreview = butNum;
            }
        }

        private void Aux_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            string butVals = button.Tag as string;
            string[] namesArray = butVals.Split(',');
            int auxNum = Convert.ToInt32(namesArray[0]);
            int inputId = Convert.ToInt32(namesArray[1]);
            ChangeAUX(auxNum, inputId);
        }

        private void autoBut_Click(object sender, RoutedEventArgs e)
        {
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
        }

        private void cutBut_Click(object sender, RoutedEventArgs e)
        {
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformCut();
            }
        }

        private void fadeBlackBut_Click(object sender, RoutedEventArgs e)
        {
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformFadeToBlack();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_mixEffectBlock1 != null)
            {
                double position = trackBarTransitionPos.Value / 100.0;
                if (m_moveSliderDownwards)
                    position = (100 - trackBarTransitionPos.Value) / 100.0;

                m_mixEffectBlock1.SetFloat(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdTransitionPosition,
                    position);
            }
        }

        private void keyWordsBut_Click(object sender, RoutedEventArgs e)
        {
            m_transition = (BMDSwitcherAPI.IBMDSwitcherTransitionParameters)m_mixEffectBlock1;
            _BMDSwitcherTransitionSelection transitionselection;
            m_transition.GetNextTransitionSelection(out transitionselection);
            string stringtransitionselection = transitionselection.ToString();
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground + 1);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
            if (stringtransitionselection != "bmdSwitcherTransitionSelectionBackground")
            {
                m_transition.GetNextTransitionStyle(out existing_style);
                m_transition.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                m_mixEffectBlock1.PerformAutoTransition();
            }
            m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                 currentPreview);

            System.Threading.Thread.Sleep(100);
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
            currentKey = 1;


        }

        
        private void keyClearBut_Click(object sender, RoutedEventArgs e)
        {
            if (currentKey!= -1) {
                keyFullBut.IsChecked = false;
                m_transition = (BMDSwitcherAPI.IBMDSwitcherTransitionParameters)m_mixEffectBlock1;
            _BMDSwitcherTransitionSelection transitionselection;
            m_transition.GetNextTransitionSelection(out transitionselection);
            string stringtransitionselection = transitionselection.ToString();

            switch (currentKey)
            {
                case 1:
                    m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground + 1);
                    keyWordsBut.IsChecked = false;

                        break;
                case 2:
                    m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey2);
                    keyLeftBut.IsChecked = false;
                        break;
                case 3:
                    m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey3);
                    keyRightBut.IsChecked = false;
                        break;
                case 4:
                    m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey4);
                    keyGreenScreenBut.IsChecked = false;
                        break;
            }
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
            if (stringtransitionselection != "bmdSwitcherTransitionSelectionBackground")
            {
                m_transition.GetNextTransitionStyle(out existing_style);
                m_transition.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                m_mixEffectBlock1.PerformAutoTransition();
            }
            m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                 currentPreview);

            System.Threading.Thread.Sleep(100);
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
            currentKey = -1;
            }
        }

        private void keyLeftBut_Click(object sender, RoutedEventArgs e)
        {

            m_transition = (BMDSwitcherAPI.IBMDSwitcherTransitionParameters)m_mixEffectBlock1;
            _BMDSwitcherTransitionSelection transitionselection;
            m_transition.GetNextTransitionSelection(out transitionselection);
            string stringtransitionselection = transitionselection.ToString();
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey2);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
            if (stringtransitionselection != "bmdSwitcherTransitionSelectionBackground")
            {
                m_transition.GetNextTransitionStyle(out existing_style);
                m_transition.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                m_mixEffectBlock1.PerformAutoTransition();
            }
            m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                 currentPreview);

            System.Threading.Thread.Sleep(100);
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
            currentKey = 2;
        }

        private void keyRightBut_Click(object sender, RoutedEventArgs e)
        {
            m_transition = (BMDSwitcherAPI.IBMDSwitcherTransitionParameters)m_mixEffectBlock1;
            _BMDSwitcherTransitionSelection transitionselection;
            m_transition.GetNextTransitionSelection(out transitionselection);
            string stringtransitionselection = transitionselection.ToString();
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey3);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
            if (stringtransitionselection != "bmdSwitcherTransitionSelectionBackground")
            {
                m_transition.GetNextTransitionStyle(out existing_style);
                m_transition.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                m_mixEffectBlock1.PerformAutoTransition();
            }
            m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                 currentPreview);

            System.Threading.Thread.Sleep(100);
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
            currentKey = 3;
        }

        private void keyFullBut_Click(object sender, RoutedEventArgs e)
        {
            keyClearBut_Click(this, null);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdProgramInput,
                   6);
                keyFullBut.IsChecked = true;
            }

        }

        private void keyGreenScreenBut_Click(object sender, RoutedEventArgs e)
        {
            m_transition = (BMDSwitcherAPI.IBMDSwitcherTransitionParameters)m_mixEffectBlock1;
            _BMDSwitcherTransitionSelection transitionselection;
            m_transition.GetNextTransitionSelection(out transitionselection);
            string stringtransitionselection = transitionselection.ToString();
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionKey4);
            if (m_mixEffectBlock1 != null)
            {
                m_mixEffectBlock1.PerformAutoTransition();
            }
            if (stringtransitionselection != "bmdSwitcherTransitionSelectionBackground")
            {
                m_transition.GetNextTransitionStyle(out existing_style);
                m_transition.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                m_mixEffectBlock1.PerformAutoTransition();
            }
            m_mixEffectBlock1.SetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput,
                 currentPreview);

            System.Threading.Thread.Sleep(100);
            m_transition.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
            currentKey = 4;
        }
    }
}
