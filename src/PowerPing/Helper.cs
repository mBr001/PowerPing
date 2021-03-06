﻿/*
MIT License - PowerPing 

Copyright (c) 2019 Matthew Carney

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace PowerPing
{
    /// <summary>
    /// Class for miscellaneous methods 
    /// </summary>
    public static class Helper
    {
        private static readonly string m_IPv4Regex = @"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}";
        private static readonly string m_UrlRegex = @"[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?";
        private static readonly string m_ValidScanRangeRegex = @"((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|\-|$)){5}";

        private static readonly double m_StopwatchToTimeSpanTicksScale = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        private static readonly double m_TimeSpanToStopwatchTicksScale = (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond;

        /// <summary>
        /// Pause program and wait for user input
        /// </summary>
        public static void WaitForUserInput()
        {
            // Only ask for user input if NoInput hasn't been set
            if (!Properties.Settings.Default.RequireInput)
                return;

            Console.Write("Press any key to continue...");
            Console.WriteLine();

            // Work around if readkey isnt supported
            try { Console.ReadKey(); }
            catch (InvalidOperationException) { Console.Read(); }
        }

        /// <summary>
        /// Prints and error message and then exits with exit code 1
        /// </summary>
        /// <param name="msg">Error message to print</param>
        /// <param name="pause">Wait for user input before exitingt</param>
        public static void ErrorAndExit(string msg)
        {
            Display.Error(msg);
            WaitForUserInput();

            Environment.Exit(1);
        }

        /// <summary>
        /// Check if long value is between a range
        /// </summary>
        /// <param name="value"></param>
        /// <param name="left">Lower range</param>
        /// <param name="right">Upper range</param>
        /// <returns></returns>
        public static bool IsBetween(long value, long left, long right)
        {
            return value > left && value < right;
        }

        /// <summary>
        /// Produces cryprographically secure string of specified length
        /// </summary>
        /// <param name="len"></param>
        /// <source>https://stackoverflow.com/a/1668371</source>
        /// <returns></returns>
        public static String RandomString(int len = 11)
        {
            string result;

            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider()) {
                byte[] rngToken = new byte[len + 1];
                rng.GetBytes(rngToken);
               
                result = Convert.ToBase64String(rngToken);
                
            }

            // Remove '=' from end of string
            return result.Remove(result.Length - 1);
        }

        /// <summary>
        /// Produces cryprographically secure int of specified length
        /// </summary>
        /// <param name="len"></param>
        /// <source>http://www.vcskicks.com/code-snippet/rng-int.php</source>
        /// <returns></returns>
        public static int RandomInt(int min, int max)
        {
            int result;

            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider()) {
                byte[] rngToken = new byte[4];
                rng.GetBytes(rngToken);

                result = BitConverter.ToInt32(rngToken, 0);
            }

            return new Random(result).Next(min, max);
        }

        /// <summary>
        /// Generates a byte array of a given size, used for adding size
        /// to icmp packet
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static byte[] GenerateByteArray(int size)
        {
            byte[] array = new byte[size];
            for (int i = 0; i < size; i++) {
                array[i] = 0x00;
            }

            return array;
        }

        /// <summary>
        /// Extension method for determining build time
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="target"></param>
        /// <source>http://stackoverflow.com/a/1600990</source>
        /// <returns></returns>
        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                stream.Read(buffer, 0, 2048);
            }

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        /// <summary>
        /// Checks if a string is a valid IPv4 address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsIPv4Address(string address)
        {
            return Regex.Match(address, m_IPv4Regex).Success;
        }

        /// <summary>
        /// Checks if a string is a valid url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsURL(string url)
        {
            return Regex.Match(url, m_UrlRegex).Success;
        }

        /// <summary>
        /// Checks if a string is a valid range.
        /// Range looks like with normal IP address with dash to specify range to scan:
        /// EG 192.168.1.1-255 to scan every address between 192.168.1.1-192.168.1.255
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        /// TODO: Allow for scan range to specified in any segment
        public static bool IsValidScanRange(string range)
        {
            return Regex.Match(range, m_ValidScanRangeRegex).Success;
        }

        /// <summary>
        /// Runs a function inside a Task instead of on the current thread. This allows for use of a cancellation
        /// token to resume the current thread (by throwing OperationCanceledException) before the function finishes.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static T RunWithCancellationToken<T>(Func<T> func, CancellationToken cancellationToken)
        {
            return Task.Run<T>(func, cancellationToken).WaitForResult(cancellationToken);
        }

        /// <summary>
        /// Generates session id to store in packet using underlying process id
        /// </summary>
        /// <returns></returns>
        public static ushort GenerateSessionId()
        {
            uint n = (uint)Process.GetCurrentProcess().Id;
            return (ushort)(n ^ (n >> 16));
        }

        /// <summary>
        /// Checks github api for latest release of PowerPing against current assembly version
        /// Prints message to update if newer version has been released.
        /// </summary>
        /// <returns></returns>
        public static void CheckRecentVersion()
        {
            using (var webClient = new System.Net.WebClient())
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
                webClient.Headers["User-Agent"] = "PowerPing (version_check)"; // Need to specif a valid user agent for github api: https://stackoverflow.com/a/39912696

                try
                {
                    // Fetch latest release info from github api
                    string jsonResult = webClient.DownloadString(
                        $"http://api.github.com/repos/killeroo/powerping/releases/latest");

                    // Extract version from returned json
                    Regex regex = new Regex(@"""(tag_name)"":""((\\""|[^""])*)""");
                    Match result = regex.Match(jsonResult);
                    if (result.Success) {
                        string matchString = result.Value;
                        Version theirVersion = new Version(matchString.Split(':')[1].Replace("\"", string.Empty).Replace("v", string.Empty));
                        Version ourVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        
                        if (theirVersion > ourVersion) {
                            Console.WriteLine();
                            Console.WriteLine("=========================================================");
                            Console.WriteLine("A new version of PowerPing is available ({0})", theirVersion);
                            Console.WriteLine("Download the new version at: {0}", @"https://github.com/killeroo/powerping/releases/latest");
                            Console.WriteLine("=========================================================");
                            Console.WriteLine();
                        }
                    }

                }
                catch (Exception) { } // We just want to blanket catch any exception and silently continue

            }

        }

        public static long StopwatchToTimeSpanTicks(long stopwatchTicks)
        {
            return (long)(stopwatchTicks * m_StopwatchToTimeSpanTicksScale);
        }

        public static long TimeSpanToStopwatchTicks(long timeSpanTicks)
        {
            return (long)(timeSpanTicks * m_TimeSpanToStopwatchTicksScale);
        }
    }
}
