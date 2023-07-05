using Microsoft.JSInterop;
using System.Net;
using System.Text;

namespace KzA.Blazor.PacParser
{
    internal class JsFunctions
    {
        private StringBuilder debugOutput = new();
        private StringBuilder jsConsole = new();

        public string DebugOutput => debugOutput.ToString();
        public string JsConsole => jsConsole.ToString();

        public IPAddress MyIpAddress = IPAddress.Loopback;

        private readonly JsDohResolver _resolver;
        private readonly HostsResolver _hostsResolver = new();

        public JsFunctions(JsDohResolver resolver)
        {
            _resolver = resolver;
        }

        public void SetHosts(IEnumerable<string> hosts)
        {
            _hostsResolver.Clear();
            _hostsResolver.AddHostsFromStandardHosts(hosts);
        }

        public void ClearDebug() { debugOutput.Clear(); }
        public void ClearConsole() { jsConsole.Clear(); }

        public void CrackFqdn(string fqdn, out string host, out string domain)
        {
            if (fqdn.Contains('.'))
            {
                host = fqdn[..fqdn.IndexOf(".")];
                domain = fqdn[fqdn.IndexOf(".")..];
            }
            else
            {
                host = fqdn;
                domain = string.Empty;
            }
        }

        [JSInvokable("isPlainHostName")]
        public bool isPlainHostName(string host)
        {
            var result = !host.Contains('.');
            debugOutput.AppendLine($"[INFO] isPlainHostName(host:{host}) => {result}");
            return result;
        }

        [JSInvokable("dnsDomainIs")]
        public bool dnsDomainIs(string host, string domain)
        {
            CrackFqdn(host, out _, out var _domain);
            var result = _domain == domain;
            debugOutput.AppendLine($"[INFO] dnsDomainIs(host:{host}, domain:{domain}) => {result}");
            return result;
        }

        [JSInvokable("localHostOrDomainIs")]
        public bool localHostOrDomainIs(string host, string hostdom)
        {
            CrackFqdn(host, out _, out var _domain);
            var result = host == hostdom || _domain == string.Empty;
            debugOutput.AppendLine($"[INFO] localHostOrDomainIs(host:{host}, hostdom:{hostdom}) => {result}");
            return result;
        }

        [JSInvokable("isResolvable")]
        public bool isResolvable(string host)
        {
            var hostAddr = _hostsResolver.GetIPAddress(host);
            if (hostAddr != null)
            {
                debugOutput.AppendLine($"[INFO] isResolvable(host:{host}) => {true} (From hosts)");
                return true;
            }
            var result = _resolver.DohResolveA(host);
            if (result.StartsWith("F:"))
            {
                debugOutput.AppendLine($"[INFO] isResolvable(host:{host}) => {false}");
                return false;
            }
            debugOutput.AppendLine($"[INFO] isResolvable(host:{host}) => {true} (From public DNS)");
            return true;

        }

        [JSInvokable("isInNet")]
        public bool isInNet(string host, string pattern, string mask)
        {
            var patterAddr = IPAddress.Parse(pattern);
            var maskAddr = IPAddress.Parse(mask);
            var addrFamily = patterAddr.AddressFamily;

            if (!IPAddress.TryParse(host, out var hostAddr))
            {
                hostAddr = _hostsResolver.GetIPAddress(host);
                if (hostAddr == null) // Not found in hosts
                {
                    var dnsRes = _resolver.DohResolveA(host);
                    if (dnsRes.StartsWith("F:")) // Not found in public DNS
                    {
                        debugOutput.AppendLine($"[WARNING] isInNet(host:{host}, pattern:{pattern}, mask:{mask}) => Host {host} name resolution failed");
                        debugOutput.AppendLine($"[INFO] isInNet(host:{host}, pattern:{pattern}, mask:{mask}) => False");
                        return false;
                    }
                    else // Found in public DNS
                    {
                        dnsRes = dnsRes[2..].Split(';').First();
                        hostAddr = IPAddress.Parse(dnsRes);
                    }
                }
            }
            if (hostAddr.AddressFamily != addrFamily)
            {
                debugOutput.AppendLine($"[WARNING] isInNet(host:{host}, pattern:{pattern}, mask:{mask}) => Host and pattern address family mismatch");
                debugOutput.AppendLine($"[INFO] isInNet(host:{host}, pattern:{pattern}, mask:{mask}) => False");
                return false;
            }
            var result = hostAddr.IsInSameSubnet(patterAddr, maskAddr);
            debugOutput.AppendLine($"[INFO] isInNet(host:{host}, pattern:{pattern}, mask:{mask}) => {result}");

            return result;
        }

        [JSInvokable("dnsResolve")]
        public string dnsResolve(string host)
        {
            var hostAddr = _hostsResolver.GetIPAddress(host);
            if (hostAddr != null)
            {
                debugOutput.AppendLine($"[INFO] dnsResolve(host:{host}) => {hostAddr} (From hosts)");
                return hostAddr.ToString();
            }
            try
            {
                var result = _resolver.DohResolveA(host);
                if (result.StartsWith("F:"))
                {
                    throw new Exception(result);
                }
                result = result[2..].Split(';').First();
                debugOutput.AppendLine($"[INFO] dnsResolve(host:{host}) => {result} (From public DNS)");
                return result;
            }
            catch (Exception e)
            {
                debugOutput.AppendLine($"[WARNING] dnsResolve(host:{host}) => Host {host} name resolution failed. {e.Message}");
                return "255.255.255.255";
            }
        }

        [JSInvokable("convert_addr")]
        public uint convert_addr(string ipaddr)
        {
            try
            {
                if (IPAddress.TryParse(ipaddr, out var ip))
                {
                    var result = ip.GetAddressNumberV4();
                    debugOutput.AppendLine($"[INFO] convert_addr(ipaddr:{ipaddr}) => {result}");
                    return result;
                }
                else
                {
                    debugOutput.AppendLine($"[WARNING] convert_addr(ipaddr:{ipaddr}) => {ipaddr} is not a valid IP address.");
                    debugOutput.AppendLine($"[INFO] convert_addr(ipaddr:{ipaddr}) => {0}");
                    return 0;
                }
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] convert_addr(ipaddr:{ipaddr}) => {ipaddr} is not IPv4 address.");
                debugOutput.AppendLine($"[INFO] convert_addr(ipaddr:{ipaddr}) => {0}");
                return 0;
            }
        }

        /* Not supported on browser
        public string myIpAddress()
        {
            try
            {
                var remoteAddr = Dns.GetHostEntry(Host, AddressFamily.InterNetwork).AddressList.First();
                var remoteEndPoint = new IPEndPoint(remoteAddr, 0);
                var result = Helper.GetMyIpAddress(remoteEndPoint).ToString();
                debugOutput.AppendLine($"[INFO] myIpAddress() => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] myIpAddress() => Host {Host} name resolution failed");
                debugOutput.AppendLine($"[WARNING] myIpAddress() => Failing back to remote address 13.107.4.52");
                var remoteAddr = new IPAddress(new byte[] { 13, 107, 4, 52 });
                var remoteEndPoint = new IPEndPoint(remoteAddr, 0);
                var result = Helper.GetMyIpAddress(remoteEndPoint).ToString();
                debugOutput.AppendLine($"[INFO] myIpAddress() => {result}");
                return result;
            }
        }
        */

        [JSInvokable("myIpAddressOverride")]
        public string myIpAddressOverride()
        {
            var result = MyIpAddress.ToString();
            debugOutput.AppendLine($"[INFO] myIpAddress() => {result} (Overridden)");
            return result;
        }

        [JSInvokable("dnsDomainLevels")]
        public int dnsDomainLevels(string host)
        {
            var result = host.Count(c => c == '.');
            debugOutput.AppendLine($"[INFO] dnsDomainLevels(host:{host}) => {result}");
            return result;
        }

        [JSInvokable("shExpMatch")]
        public bool shExpMatch(string str, string shexp)
        {
            var exp = new ShellExpression(shexp);
            var result = exp.IsMatch(str);
            debugOutput.AppendLine($"[INFO] shExpMatch(str:{str}, shexp:{shexp}) => {result}");
            return result;
        }

        [JSInvokable("weekdayRange")]
        public bool weekdayRange(string wd1, string wd2, string gmt)
        {
            bool result;
            if (wd2 == "GMT")
            {
                var weekOfTodayGmt = DateTime.UtcNow.DayOfWeek;
                result = weekOfTodayGmt == Helper.DayOfWeekFromString(wd1);
                debugOutput.AppendLine($"[INFO] weekdayRange(wd1:{wd1}, gmt: GMT) => {result}");
                return result;
            }

            var d1 = Helper.DayOfWeekFromString(wd1);
            var d2 = Helper.DayOfWeekFromString(wd2);
            if (gmt == null)
            {
                var weekOfToday = DateTime.Now.DayOfWeek;
                if (d1 < d2)
                {
                    result = d1 <= weekOfToday && weekOfToday <= d2;
                    debugOutput.AppendLine($"[INFO] weekdayRange(wd1:{wd1}, wd2:{wd2}) => {result}");
                    return result;
                }
                else
                {
                    debugOutput.AppendLine($"[WARNING] weekdayRange(wd1:{wd1}, wd2:{wd2}) => wd1 >= wd2");
                    result = weekOfToday == d1 || weekOfToday == d2;
                    debugOutput.AppendLine($"[INFO] weekdayRange(wd1:{wd1}, wd2:{wd2}) => {result}");
                    return result;
                }
            }
            else
            {
                var weekOfToday = DateTime.UtcNow.DayOfWeek;
                if (d1 < d2)
                {
                    result = d1 <= weekOfToday && weekOfToday <= d2;
                    debugOutput.AppendLine($"[INFO] weekdayRange(wd1:{wd1}, wd2:{wd2}, gmt:{gmt}) => {result}");
                    return result;
                }
                else
                {
                    debugOutput.AppendLine($"[WARNING] weekdayRange(wd1:{wd1}, wd2:{wd2}) => wd1 >= wd2");
                    result = weekOfToday == d1 || weekOfToday == d2;
                    debugOutput.AppendLine($"[INFO] weekdayRange(wd1:{wd1}, wd2:{wd2}, gmt:{gmt}) => {result}");
                    return result;
                }
            }
        }

        private enum DateType
        {
            Day, Month, Year
        }

        private bool dateRange(string date1, string date2)
        {
            var result = false;
            judgeDateType(date1, out var d1, out var type1);

            if (date2 == "null")
            {
                result = dateRangeSingle(d1, type1, false);
                debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}) => {result}");
                return result;
            }

            if (date2 == "GMT")
            {
                result = dateRangeSingle(d1, type1, true);
                debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, gmt:GMT) => {result}");
                return result;
            }

            judgeDateType(date2, out var d2, out var type2);
            if (type1 != type2)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date1:{date1}, date2:{date2}) => Two dates cannot be compared");
                debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, date2:{date2}) => False");
                return false;
            }

            result = dateRangeDouble(d1, d2, type1, false);
            debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, date2:{date2}) => {result}");
            return result;
        }

        private bool dateRange(string date1, string date2, string gmt)
        {
            if (gmt == null) { return dateRange(date1, date2); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date1:{date1}, date2:{date2}, gmt:{gmt}) => Invalid argument");
                debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, date2:{date2}, gmt:{gmt}) => False");
                return false;
            }

            var result = false;
            judgeDateType(date1, out var d1, out var type1);
            judgeDateType(date2, out var d2, out var type2);
            if (type1 != type2)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date1:{date1}, date2:{date2}, gmt:GMT) => Two dates cannot be compared");
                debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, date2:{date2}, gmt:GMT) => False");
                return false;
            }

            result = dateRangeDouble(d1, d2, type1, true);
            debugOutput.AppendLine($"[INFO] dateRange(date1:{date1}, date2:{date2}, gmt:GMT) => {result}");
            return result;
        }

        private bool dateRange(string date11, string date12, string date21, string date22)
        {
            if (date22 == null) { return dateRange(date11, date12, date21); }

            var result = false;
            var now = DateTime.Now;

            judgeDateType(date11, out var d11, out var type11);
            judgeDateType(date12, out var d12, out var type12);
            judgeDateType(date21, out var d21, out var type21);
            judgeDateType(date22, out var d22, out var type22);

            if (type11 != type21 || type12 != type22)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => Two dates cannot be compared");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => False");
                return false;
            }

            if (type11 == DateType.Year || type12 != type11 + 1)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => False");
                return false;
            }

            try
            {
                if (type11 == DateType.Day)
                {
                    var dt1 = new DateTime(now.Year, d12, d11);
                    var dt2 = new DateTime(now.Year, d22, d21);
                    result = now >= dt1 && now <= dt2;
                    debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => {result}");
                    return result;
                }
                else
                {
                    var dt1 = new DateTime(d12, d11, 1);
                    var dt2 = new DateTime(d22, d21, DateTime.DaysInMonth(d22, d21));
                    result = now >= dt1 && now <= dt2;
                    debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => {result}");
                    return result;
                }
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}) => False");
            }

            return result;
        }

        private bool dateRange(string date11, string date12, string date21, string date22, string gmt)
        {
            if (gmt == null) { return dateRange(date11, date12, date21, date22); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:{gmt}) => Invalid argument");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:{gmt}) => False");
                return false;
            }

            var result = false;
            var now = gmt == null ? DateTime.Now : DateTime.UtcNow;

            judgeDateType(date11, out var d11, out var type11);
            judgeDateType(date12, out var d12, out var type12);
            judgeDateType(date21, out var d21, out var type21);
            judgeDateType(date22, out var d22, out var type22);

            if (type11 != type21 || type12 != type22)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => Two dates cannot be compared");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => False");
                return false;
            }

            if (type11 == DateType.Year || type12 != type11 + 1)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => False");
                return false;
            }

            try
            {
                if (type11 == DateType.Day)
                {
                    var dt1 = new DateTime(now.Year, d12, d11);
                    var dt2 = new DateTime(now.Year, d22, d21);
                    result = now >= dt1 && now <= dt2;
                    debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => {result}");
                    return result;
                }
                else
                {
                    var dt1 = new DateTime(d12, d11, 1);
                    var dt2 = new DateTime(d22, d21, DateTime.DaysInMonth(d22, d21));
                    result = now >= dt1 && now <= dt2;
                    debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => {result}");
                    return result;
                }
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date21:{date21}, date22:{date22}, gmt:GMT) => False");
            }

            return result;
        }

        private bool dateRange(string date11, string date12, string date13, string date21, string date22, string date23)
        {
            if (date23 == null) { return dateRange(date11, date12, date13, date21, date22); }

            var result = false;
            var now = DateTime.Now;

            judgeDateType(date11, out var d11, out var type11);
            judgeDateType(date12, out var d12, out var type12);
            judgeDateType(date13, out var d13, out var type13);
            judgeDateType(date21, out var d21, out var type21);
            judgeDateType(date22, out var d22, out var type22);
            judgeDateType(date23, out var d23, out var type23);

            if (type11 != DateType.Day || type12 != DateType.Month || type13 != DateType.Year || type11 != type21 || type12 != type22 || type13 != type23)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}) => False");
                return false;
            }
            try
            {
                var dt1 = new DateTime(d13, d12, d11);
                var dt2 = new DateTime(d23, d22, d21);
                result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}) => False");
            }
            return result;
        }

        [JSInvokable("dateRange")]
        public bool dateRange(string date11, string date12, string date13, string date21, string date22, string date23, string gmt)
        {
            if (gmt == null) { return dateRange(date11, date12, date13, date21, date22, date23); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:{gmt}) => Invalid argument");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:{gmt}) => False");
                return false;
            }

            var result = false;
            var now = DateTime.UtcNow;

            judgeDateType(date11, out var d11, out var type11);
            judgeDateType(date12, out var d12, out var type12);
            judgeDateType(date13, out var d13, out var type13);
            judgeDateType(date21, out var d21, out var type21);
            judgeDateType(date22, out var d22, out var type22);
            judgeDateType(date23, out var d23, out var type23);

            if (type11 != DateType.Day || type12 != DateType.Month || type13 != DateType.Year || type11 != type21 || type12 != type22 || type13 != type23)
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:GMT) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:GMT) => False");
                return false;
            }
            try
            {
                var dt1 = new DateTime(d13, d12, d11);
                var dt2 = new DateTime(d23, d22, d21);
                result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:GMT) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:GMT) => Invalid date");
                debugOutput.AppendLine($"[INFO] dateRange(date11:{date11}, date12:{date12}, date13:{date13}, date21:{date21}, date22:{date22}, date22:{date23}, gmt:GMT) => False");
            }
            return result;
        }

        private void judgeDateType(string date, out int d, out DateType type)
        {
            d = Helper.MonthFromString(date);
            if (d == 0)
            {
                try
                {
                    d = int.Parse(date);
                    if (d > 31)
                    {
                        debugOutput.AppendLine($"[INFO] judgeDateType(date:{date}) => Year");
                        type = DateType.Year;
                    }
                    else
                    {
                        debugOutput.AppendLine($"[INFO] judgeDateType(date:{date}) => Day");
                        type = DateType.Day;
                    }
                }
                catch
                {
                    type = DateType.Year;
                    debugOutput.AppendLine($"[WARNING] judgeDateType(date:{date}) => Not a valid day, month or year");
                }
            }
            else
            {
                debugOutput.AppendLine($"[INFO] judgeDateType(date:{date}) => Month");
                type = DateType.Month;
            }
        }

        private bool dateRangeSingle(int d1, DateType type, bool isGmt)
        {
            var now = isGmt ? DateTime.UtcNow : DateTime.Now;
            return type switch
            {
                DateType.Day => now.Day == d1,
                DateType.Month => now.Month == d1,
                DateType.Year => now.Year == d1,
                _ => false,
            };
        }

        private bool dateRangeDouble(int d1, int d2, DateType type, bool isGmt)
        {
            var now = isGmt ? DateTime.UtcNow : DateTime.Now;
            return type switch
            {
                DateType.Day => now.Day >= d1 && now.Day <= d2,
                DateType.Month => now.Month >= d1 && now.Month <= d2,
                DateType.Year => now.Year >= d1 && now.Year <= d2,
                _ => false,
            };
        }

        private bool timeRange(string hour)
        {
            try
            {
                var h = int.Parse(hour);
                var now = DateTime.Now;
                var result = now.Hour == h;
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}) => Invalid hour");
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}) => False");
                return false;
            }
        }

        private bool timeRangeGmt(string hour)
        {
            try
            {
                var h = int.Parse(hour);
                if (h < 0 || h > 23) throw new Exception();
                var now = DateTime.UtcNow;
                var result = now.Hour == h;
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}, gmt:GMT) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}, gmt:GMT) => Invalid hour");
                debugOutput.AppendLine($"[INFO] timeRange(hour:{hour}, gmt:GMT) => False");
                return false;
            }
        }

        private bool timeRange(string hour1, string hour2)
        {
            if (hour2 == null) { return timeRange(hour1); }
            if (hour2 == "GMT") { return timeRangeGmt(hour1); }

            try
            {
                var h1 = int.Parse(hour1);
                var h2 = int.Parse(hour2);
                if (h1 < 0 || h1 > 23 || h2 < 0 || h2 > 23) throw new Exception();
                var now = DateTime.Now;
                var result = now.Hour >= h1 && now.Hour <= h2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}) => Invalid hour");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}) => False");
                return false;
            }
        }

        private bool timeRange(string hour1, string hour2, string gmt)
        {
            if (gmt == null) { return timeRange(hour1, hour2); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, hour2:{hour2}, gmt:{gmt}) => Invalid arguments");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}, gmt:{gmt}) => False");
            }

            try
            {
                var h1 = int.Parse(hour1);
                var h2 = int.Parse(hour2);
                if (h1 < 0 || h1 > 23 || h2 < 0 || h2 > 23) throw new Exception();
                var now = DateTime.UtcNow;
                var result = now.Hour >= h1 && now.Hour <= h2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}, gmt:GMT) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, hour2:{hour2}, gmt:GMT) => Invalid hour");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, hour2:{hour2}, gmt:GMT) => False");
                return false;
            }
        }

        private bool timeRange(string hour1, string min1, string hour2, string min2)
        {
            if (min2 == null) { return timeRange(hour1, min1, hour2); }

            try
            {
                var h1 = int.Parse(hour1);
                var m1 = int.Parse(min1);
                var h2 = int.Parse(hour2);
                var m2 = int.Parse(min2);
                var now = DateTime.Now;
                var dt1 = new DateTime(now.Year, now.Month, now.Day, h1, m1, 0);
                var dt2 = new DateTime(now.Year, now.Month, now.Day, h2, m2, 59);
                var result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}) => Invalid time");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}) => False");
                return false;
            }
        }

        private bool timeRange(string hour1, string min1, string hour2, string min2, string gmt)
        {
            if (gmt == null) { return timeRange(hour1, min1, hour2, min2); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}, gmt:{gmt}) => Invalid arguments");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}, gmt:{gmt}) => False");
            }

            try
            {
                var h1 = int.Parse(hour1);
                var m1 = int.Parse(min1);
                var h2 = int.Parse(hour2);
                var m2 = int.Parse(min2);
                var now = DateTime.UtcNow;
                var dt1 = new DateTime(now.Year, now.Month, now.Day, h1, m1, 0);
                var dt2 = new DateTime(now.Year, now.Month, now.Day, h2, m2, 59);
                var result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}, gmt:GMT) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}, gmt:GMT) => Invalid time");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, hour2:{hour2}, min2:{min2}, gmt:GMT) => False");
                return false;
            }
        }

        private bool timeRange(string hour1, string min1, string sec1, string hour2, string min2, string sec2)
        {
            if (sec2 == null) { return timeRange(hour1, min1, sec1, hour2, min2); }

            try
            {
                var h1 = int.Parse(hour1);
                var m1 = int.Parse(min1);
                var s1 = int.Parse(sec1);
                var h2 = int.Parse(hour2);
                var m2 = int.Parse(min2);
                var s2 = int.Parse(sec2);
                var now = DateTime.Now;
                var dt1 = new DateTime(now.Year, now.Month, now.Day, h1, m1, s1);
                var dt2 = new DateTime(now.Year, now.Month, now.Day, h2, m2, s2);
                var result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}) => Invalid time");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}) => False");
                return false;
            }
        }

        [JSInvokable("timeRange")]
        public bool timeRange(string hour1, string min1, string sec1, string hour2, string min2, string sec2, string gmt)
        {
            if (gmt == null) { return timeRange(hour1, min1, sec1, hour2, min2, sec2); }
            if (gmt != "GMT")
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}, gmt:{gmt}) => Invalid arguments");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}, gmt:{gmt}) => False");
            }

            try
            {
                var h1 = int.Parse(hour1);
                var m1 = int.Parse(min1);
                var s1 = int.Parse(sec1);
                var h2 = int.Parse(hour2);
                var m2 = int.Parse(min2);
                var s2 = int.Parse(sec2);
                var now = DateTime.UtcNow;
                var dt1 = new DateTime(now.Year, now.Month, now.Day, h1, m1, s1);
                var dt2 = new DateTime(now.Year, now.Month, now.Day, h2, m2, s2);
                var result = now >= dt1 && now <= dt2;
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}, gmt:GMT) => {result}");
                return result;
            }
            catch
            {
                debugOutput.AppendLine($"[WARNING] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}, gmt:GMT) => Invalid time");
                debugOutput.AppendLine($"[INFO] timeRange(hour1:{hour1}, min1:{min1}, sec1:{sec1}, hour2:{hour2}, min2:{min2}, sec2:{sec2}, gmt:GMT) => False");
                return false;
            }
        }

        [JSInvokable("alert")]
        public void alert(string message)
        {
            jsConsole.AppendLine(message);
        }
    }
}
