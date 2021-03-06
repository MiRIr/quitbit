// Copyright (c) 2015, Joel Longanecker
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
// disclaimer in the documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
// OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// QuitBit
// Emulator Killer written by Joel Longanecker
// 2015
//
// Usage:
//		qb.exe --controller=2 --buttons=2+0+1 --exec=c:\emulators\nes\nes.exe --params=c:\roms\nes\mario.nes
//

// Example emulation station usage:
// <system>
//		<name>genesis</name>
//		<fullname>Sega Genesis</fullname>
//		<path>C:\Roms\genesis</path>
//		<extension>.bin .zip</extension>
//		<command>qb.exe --buttons=6 --e=c:\retroarch\retroarch.exe --p=-D -L C:\retroarch\cores\genesis_plus_gx_libretro.dll "%ROM_RAW%"</command>
//		<platform>genesis</platform>
//		<theme>genesis</theme>
// </system>
//
// Note: --controller and --params are not necessary

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuitBit
{
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYINFOEX
    {
        public int dwSize;
        public int dwFlags;
        public int dwXpos;
        public int dwYpos;
        public int dwZpos;
        public int dwRpos;
        public int dwUpos;
        public int dwVpos;
        public int dwButtons;
        public int dwButtonNumber;
        public int dwPOV;
        public int dwReserved1;
        public int dwReserved2;
    }

    internal sealed class Controller
    {
        [DllImport("winmm.dll")]
        internal static extern int joyGetPosEx(int uJoyID, ref JOYINFOEX pji); //Get the state of a controller with their ID
        [DllImport("winmm.dll")]
        public static extern Int32 joyGetNumDevs(); //How many controllers are plugged in

        private int controllerNum;
        private int combo;
        private JOYINFOEX state = new JOYINFOEX();

        public Controller(int n, int c)
        {
            controllerNum = n;
            combo = c;

            state.dwFlags = 128;
            state.dwSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(JOYINFOEX));
        }

        public bool comboPressed()
        {
            if (controllerNum > -1) //Checking one controller
            {
                joyGetPosEx(controllerNum, ref state);
                return (combo == state.dwButtons);
            }
            else //Checking all controllers
            {
                for (int i = 0; i < joyGetNumDevs(); i++)
                {
                    joyGetPosEx(i, ref state);
                    if (combo == state.dwButtons)
                        return true;
                }
                return false;
            }
        }
    }

    internal sealed class Program
    {
        [STAThread]
        private static void Main()
        {
            Controller controller;
            System.Diagnostics.Process runProgram;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            int time;

            {
                string 
                    controllerString = "-1",
                    buttonsString = "",
                    execString = "",
                    paramsString = "",
                    timeString = "0",
                    clString = Environment.CommandLine;

                string[] stringElements = clString.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                int buttonCombo = 0, controllerNum = -1;

                foreach (var s in stringElements)
                {
                    if (s.Contains("="))
                    {
                        var lSide = s.Split('=')[0];
                        var rSide = s.Split('=')[1];

                        if (lSide == "buttons" || lSide == "b")
                            buttonsString = rSide;
                        else if (lSide == "exec" || lSide == "e")
                            execString = rSide;
                        else if (lSide == "params" || lSide == "p")
                            paramsString = rSide;
                        else if (lSide == "time" || lSide == "t")
                            timeString = rSide;
                        else if (lSide == "contoller" || lSide == "c")
                            controllerString = rSide;
                    }
                }

                {
                    bool error = false;
                    int oVal = 0;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (var b in buttonsString.Split('+')) //Find Button Combo that is required
                    {
                        if (int.TryParse(b, out oVal))
                            buttonCombo += (int)Math.Pow(2, oVal);
                        else
                        {
                            if (buttonsString == string.Empty)
                                Console.WriteLine("A button combination is not specififed.");
                            else
                                Console.WriteLine("The button argument is not used properly.");
                            error = true;
                            break;
                        }
                    }
                    if (!System.IO.File.Exists(execString))
                    {
                        if (execString == string.Empty)
                            Console.WriteLine("An executable is not specififed.");
                        else
                            Console.WriteLine("The executable does not exist, it's possibly an invalid path.");
                        error = true;
                    }
                    if (!int.TryParse(timeString, out time))
                    {
                        Console.WriteLine("The time argument not used properly.");
                        error = true;
                    }
                    if (!int.TryParse(controllerString, out controllerNum))
                    {
                        Console.WriteLine("The controller argument not used properly.");
                        error = true;
                    }
                    if(error)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Command      Alt   Purpose");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("--buttons    --b   Button combination to close the program" + Environment.NewLine +
                                          "                       --b=0+8+6" + Environment.NewLine +
                                          "--exec       --e   Full path to the executable" + Environment.NewLine +
                                          "                       --e=C:\\Emulators\\nestopia.exe" + Environment.NewLine +
                                          "--controller --c   ID of specific controller to use           [Optional]" + Environment.NewLine +
                                          "                       --c=0" + Environment.NewLine +
                                          "--time       --t   Milliseconds to hold down the combination  [Optional]" + Environment.NewLine +
                                          "                       --t=2500" + Environment.NewLine +
                                          "--params     --p   Parameters when launching the program      [Optional]" + Environment.NewLine +
                                          "                       --p=C:\\roms\\NES\\Super Mario Bros..nes");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return;
                    }
                    else
                        Console.ForegroundColor = ConsoleColor.Gray;
                }

                controller = new Controller(controllerNum, buttonCombo); //Controller class that handles button presses when checked

                runProgram = new System.Diagnostics.Process(); //Start up the program
                runProgram.StartInfo.FileName = execString;
                runProgram.StartInfo.Arguments = paramsString;
                runProgram.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(execString);
                runProgram.Start();
            }

            while(true)
            {
                if (!controller.comboPressed())
                {
                    timer.Restart();
                }
                else if(timer.ElapsedMilliseconds >= time)
                {
                    try
                    {
                        runProgram.Kill();
                    }
                    catch { }
                    return;
                }

                System.Threading.Thread.Sleep(35);
            }
        }
    }
}
