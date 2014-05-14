﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS4Library;
namespace DS4Control
{
    class DS4LightBar
    {
        private readonly static byte[/* Light On duration */, /* Light Off duration */] BatteryIndicatorDurations =
        {
            { 0, 0 }, // 0 is for "charging" OR anything sufficiently-"charged"
            { 28, 252 },
            { 56, 224 },
            { 84, 196 },
            { 112, 168 },
            { 140, 140 },
            { 168, 112 },
            { 196, 84 },
            { 224, 56}, // on 80% of the time at 80, etc.
            { 252, 28 } // on 90% of the time at 90
        };
        static double[] counters = new double[4] {0,0,0,0};

        static DateTime oldnow = DateTime.Now;
        public static void updateLightBar(DS4Device device, int deviceNum)
        {
            DS4Color color;
            if (Global.getRainbow(deviceNum) > 0)
            {// Display rainbow
                DateTime now = DateTime.Now;
                if (now >= oldnow + TimeSpan.FromMilliseconds(10)) //update by the millisecond that way it's a smooth transtion
                {
                    oldnow = now;
                    counters[deviceNum] += 1.5*3 / Global.getRainbow(deviceNum);
                }
                if (Global.getLedAsBatteryIndicator(deviceNum) && (device.Charging == false || device.Battery >= 100))// when charged, don't show the charging animation
                    color = HuetoRGB((float)counters[deviceNum] % 360, (byte)(2.55 * device.Battery));
                else
                    color = HuetoRGB((float)counters[deviceNum] % 360, 255);
            }
            else if (Global.getLedAsBatteryIndicator(deviceNum))
            {
                if (device.Charging == false || device.Battery >= 100) // when charged, don't show the charging animation
                {
                    DS4Color fullColor = new DS4Color
                    {
                        red = Global.loadColor(deviceNum).red,
                        green = Global.loadColor(deviceNum).green,
                        blue = Global.loadColor(deviceNum).blue
                    };

                    color = Global.loadLowColor(deviceNum);
                    DS4Color lowColor = new DS4Color
                    {
                        red = color.red,
                        green = color.green,
                        blue = color.blue
                    };

                    color = Global.getTransitionedColor(lowColor, fullColor, (uint)device.Battery);
                }
                else // Display rainbow when charging.
                {
                    counters[deviceNum]+= .167;
                    color = HuetoRGB((float)counters[deviceNum] % 360, 255);
                }
            }
            else
            {
                color = Global.loadColor(deviceNum);
            }

            DS4HapticState haptics = new DS4HapticState
            {
                LightBarColor = color
            };
            if (haptics.IsLightBarSet())
            {
                if (Global.getFlashWhenLowBattery(deviceNum))
                {
                    int level = device.Battery / 10;
                    if (level >= 10)
                        level = 0; // all values of ~0% or >~100% are rendered the same
                    haptics.LightBarFlashDurationOn = BatteryIndicatorDurations[level, 0];
                    haptics.LightBarFlashDurationOff = BatteryIndicatorDurations[level, 1];
                }
                else
                {
                    haptics.LightBarFlashDurationOff = haptics.LightBarFlashDurationOn = 0;
                }
            }
            else
            {
                haptics.LightBarExplicitlyOff = true;
            }
            device.pushHapticState(haptics);
        }

        public static DS4Color HuetoRGB(float hue, byte sat)
        {
            byte C = sat;
            int X = (int)((C * (float)(1 - Math.Abs((hue / 60) % 2 - 1))));
            if (0 <= hue && hue < 60)
                return new DS4Color { red = C, green = (byte)X, blue = 0 };
            else if (60 <= hue && hue < 120)
                return new DS4Color { red = (byte)X, green = C, blue = 0 };
            else if (120 <= hue && hue < 180)
                return new DS4Color { red = 0, green = C, blue = (byte)X };
            else if (180 <= hue && hue < 240)
                return new DS4Color { red = 0, green = (byte)X, blue = C };
            else if (240 <= hue && hue < 300)
                return new DS4Color { red = (byte)X, green = 0, blue = C };
            else if (300 <= hue && hue < 360)
                return new DS4Color { red = C, green = 0, blue = (byte)X };
            else
                return new DS4Color { red = 255, green = 0, blue = 0 };
        }
    }
}