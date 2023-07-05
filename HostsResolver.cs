using System.Net;

namespace KzA.Blazor.PacParser
{
    internal class HostsResolver
    {
        private readonly Dictionary<string, List<IPAddress>> HostsDic = new();
        private readonly Random rnd = new();

        public IPAddress? GetIPAddress(string host)
        {
            if (HostsDic.TryGetValue(host, out var addresses))
            {
                var idx = rnd.Next(addresses.Count);    // Maybe we can also implement subnet priority here...
                return addresses[idx];
            }
            return null;
        }

        public void AddHost(string host, IPAddress address)
        {
            if (HostsDic.TryGetValue(host, out var addresses))
            {
                addresses.Add(address);
            }
            else
            {
                HostsDic.Add(host, new List<IPAddress> { address });
            }
        }

        public void AddHostsFromStandardHosts(IEnumerable<string> hosts)
        {
            foreach (var line in hosts)
            {
                var lineCommentRemoved = line;
                if (line.Contains('#'))
                {
                    lineCommentRemoved = line.Substring(0, line.IndexOf('#'));
                }
                var lineParts = lineCommentRemoved.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (lineParts.Length < 2)
                {
                    continue;
                }
                if (!IPAddress.TryParse(lineParts[0], out var address) || address == null)
                {
                    continue;
                }
                for (var i = 1; i < lineParts.Length; i++)
                {
                    AddHost(lineParts[i], address);
                }
            }
        }

        public void Clear()
        {
            HostsDic.Clear();
        }
    }
}