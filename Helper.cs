using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KzA.Blazor.PacParser
{
    internal static class Helper
    {
        /* Not supported on browser
        public static IPAddress GetMyIpAddress(IPEndPoint remoteEndPoint)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RouteQueryWindows(remoteEndPoint);
            }
            else
            {
                return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].GetIPProperties().UnicastAddresses[0].Address;
            }
        }

        private static IPAddress RouteQueryWindows(IPEndPoint remoteEndPoint)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var sockAddr = remoteEndPoint.Serialize();
            var remoteAddrBytes = new byte[sockAddr.Size];

            for (int i = 0; i < sockAddr.Size; i++)
            {
                remoteAddrBytes[i] = sockAddr[i];
            }

            byte[] outBytes = new byte[remoteAddrBytes.Length];

            _ = sock.IOControl(IOControlCode.RoutingInterfaceQuery,
                remoteAddrBytes,
                outBytes
            );

            for (int i = 0; i < sockAddr.Size; i++)
            {
                sockAddr[i] = outBytes[i];
            }

            var localEndPoint = (IPEndPoint)remoteEndPoint.Create(sockAddr);
            sock.Close();
            return localEndPoint.Address;
        }
        */

        public static DayOfWeek DayOfWeekFromString(string wd)
        {
            return wd switch
            {
                "SUN" => DayOfWeek.Sunday,
                "MON" => DayOfWeek.Monday,
                "TUE" => DayOfWeek.Tuesday,
                "WED" => DayOfWeek.Wednesday,
                "THU" => DayOfWeek.Thursday,
                "FRI" => DayOfWeek.Friday,
                "SAT" => DayOfWeek.Saturday,
                _ => DayOfWeek.Sunday,
            };
        }

        public static int MonthFromString(string month)
        {
            return month switch
            {
                "JAN" => 1,
                "FEB" => 2,
                "MAR" => 3,
                "APR" => 4,
                "MAY" => 5,
                "JUN" => 6,
                "JUL" => 7,
                "AUG" => 8,
                "SEP" => 9,
                "OCT" => 10,
                "NOV" => 11,
                "DEC" => 12,
                _ => 0,
            };
        }
    }
}
