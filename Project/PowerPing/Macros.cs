﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;

/* Macros class */
// Class for miscellaneous methods 

namespace PowerPing
{
    class Macros
    {
        /// <summary>
        /// Gets location information about IP Address
        /// IP location info by freegeoip.net
        /// </summary>
        /// <param name="addr">Address to get location info on. Can be in IP or address format.</param>
        /// <param name="detailed">Display detailed or simplified location info</param>
        /// <returns>none detailed information string</returns>
        public static string getAddressLocation(string addr, bool detailed)
        {
            string loc = null;

            try
            {
                using (var objClient = new System.Net.WebClient())
                {
                    var strFile = objClient.DownloadString("http://freegeoip.net/xml/" + addr);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(strFile);
                    XmlNodeList elements = (xmlDoc.DocumentElement).ChildNodes;

                    if (detailed)
                    {
                        Console.WriteLine("Queried address: --{0}--", addr);
                        foreach (XmlElement element in elements)
                            Console.WriteLine(element.Name + ": " + (element.InnerText == "" ? "NA" : element.InnerText));
                        Console.WriteLine("(IP location info by freegeoip.net)");
                    }
                    else
                    {
                        if (elements[2].InnerText != "")
                            loc = "[" + elements[2].InnerText;
                        if (elements[3].InnerText != "")
                            loc = loc + ", " + elements[3].InnerText;
                        if (elements[5].InnerText != "")
                            loc = loc + ", " + elements[5].InnerText;
                        loc += "]";
                    }
                }
            }
            catch (WebException)
            {
                loc = "[Location unavaliable]";
                Console.WriteLine("[Location unavaliable]");
            }

            Console.WriteLine(loc);

            Macros.pause();

            return loc;
        }

        /// <summary>
        /// Gets location information of current host
        /// </summary>
        public static void whoami()
        {
            getAddressLocation("", true);
            // TODO: Add some pc information too
        }

        /// <summary>
        /// Pause program and wait for user input
        /// </summary>
        /// <param name="exit">switch to use word "exit" instead of "continue"</param>
        public static void pause(bool exit = false)
        {
            Console.Write("Press any key to " + (exit ? "exit" : "continue") + " . . .");
            Console.ReadLine();
        }

        /// <summary>
        /// Check if long value is between a range
        /// </summary>
        /// <param name="value"></param>
        /// <param name="left">Lower range</param>
        /// <param name="right">Upper range</param>
        /// <returns></returns>
        public static bool isBetween(long value, long left, long right)
        {
            return value > left && value < right;
        }
    }
}
