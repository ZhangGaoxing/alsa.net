// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Iot.Device.Media;

namespace Alsa.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            SoundConnectionSettings settings = new SoundConnectionSettings();
            using SoundDevice device = SoundDevice.Create(settings);

            device.Play("/home/pi/44.wav");

            Console.WriteLine("Play stop.");
        }
    }
}
