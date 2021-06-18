using BMDSwitcherAPI;
using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;


namespace Trinity_ATEM_Switcher
{
    /** Listen for MIDI signals on the first input device. As notes come in, trigger button presses. Only listen to MIDI channel 4 to avoid conflicts.
     */
    public class MidiListener
    {
        MainWindow mainWindow;
        public void StartListening(MainWindow autoBut)
        {
            mainWindow = autoBut;
            Console.WriteLine("MIDI LISTENER");

            if (InputDevice.InstalledDevices.Count > 0)
            {
                InputDevice inputDevice = InputDevice.InstalledDevices[0];
                inputDevice.Open();
                inputDevice.NoteOn += new InputDevice.NoteOnHandler(NoteOn);
                inputDevice.StartReceiving(null);
                // Note events will be received in another thread Console.ReadKey(); // This thread waits for a keypress ...
            }
        }

        public void NoteOn(NoteOnMessage msg)
        {
            int channel = (int)msg.Channel + 1; //MIDI starts at 1, this starts at 0
            int noteNumber = (int)msg.Pitch;

            Console.WriteLine("MIDI: " + channel + " - " + noteNumber);

            if (channel == 4)
            {

                switch (noteNumber)
                {
                    case 0:
                        Console.WriteLine("MIDI: autoBut transition");
                        mainWindow.ClickButton(mainWindow.autoBut);
                        break;
                    case 1:
                        Console.WriteLine("MIDI: Preview Camera 1 ");
                        mainWindow.ClickButton(mainWindow.prevBut1);
                        break;
                    case 2:
                        Console.WriteLine("MIDI: Preview Camera 2 ");
                        mainWindow.ClickButton(mainWindow.prevBut2);
                        break;
                    case 3:
                        Console.WriteLine("MIDI: Preview Camera 3 ");
                        mainWindow.ClickButton(mainWindow.prevBut3);
                        break;
                    case 4:
                        Console.WriteLine("MIDI: Preview Camera 4 ");
                        mainWindow.ClickButton(mainWindow.prevBut4);
                        break;
                    case 5:
                        Console.WriteLine("MIDI: Preview Slides ");
                        mainWindow.ClickButton(mainWindow.prevBut5);
                        break;
                    case 6:
                        Console.WriteLine("MIDI: Preview Announcements ");
                        mainWindow.ClickButton(mainWindow.prevBut6);
                        break;
                    case 7:
                        Console.WriteLine("MIDI: Insert Left ");
                        mainWindow.ClickButton(mainWindow.keyLeftBut);
                        break;
                    case 8:
                        Console.WriteLine("MIDI: Insert Words ");
                        mainWindow.ClickButton(mainWindow.keyWordsBut);
                        break;
                    case 9:
                        Console.WriteLine("MIDI: Insert Right ");
                        mainWindow.ClickButton(mainWindow.keyRightBut);
                        break;
                    case 10:
                        Console.WriteLine("MIDI: Insert Full ");
                        mainWindow.ClickButton(mainWindow.keyFullBut);
                        break;
                    case 11:
                        Console.WriteLine("MIDI: Insert Clear ");
                        mainWindow.ClickButton(mainWindow.keyClearBut);
                        break;
                    case 12:
                        //Console.WriteLine("MIDI: Insert Green ");
                        //mainWindow.ClickButton(mainWindow.keyGreenScreenBut);
                        break;

                }
            }
        }
    }
}
