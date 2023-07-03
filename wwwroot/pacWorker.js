initializePacWorker = function (dotNetObject) {
    window.dotNetObject = dotNetObject;
}

isPlainHostName = function (host) {
    return window.dotNetObject.invokeMethod('isPlainHostName', host);
}

dnsDomainIs = function (host, domain) {
    return window.dotNetObject.invokeMethod('dnsDomainIs', host, domain);
}

localHostOrDomainIs = function (host, hostdom) {
    return window.dotNetObject.invokeMethod('localHostOrDomainIs', host, hostdom);
}

isResolvable = function (host) {
    return window.dotNetObject.invokeMethod('isResolvable', host);
}

isInNet = function (ipaddr, pattern, maskstr) {
    return window.dotNetObject.invokeMethod('isInNet', ipaddr, pattern, maskstr);
}

dohResolveA = function (name) {
    const fetch = require('sync-fetch')
    let options = {
        method: 'GET',
        headers: {
            accept: 'application/dns-json'
        },
    }
    try {
        let response = fetch(`https://cloudflare-dns.com/dns-query?name=${name}&type=A`, options);
        let responseJson = response.json();
        if (responseJson.Status != 0) {
            return `F:DnsFailure:${responseJson.Status}`;
        } else if (responseJson.Answer == null) {
            return 'F:NoAnswer';
        } else {
            let result = "S:";
            for (let a of responseJson.Answer) {
                if (a.type == 1) {
                    result += a.data;
                    result += ';';
                }
            }
            if (result.length == 2) return 'F:NoAnswer';
            return result;
        }
    } catch {
        return 'F:DohApiFailure';
    }
}

dnsResolve = function (host) {
    return window.dotNetObject.invokeMethod('dnsResolve', host);
}

convert_addr = function (ipchars) {
    return window.dotNetObject.invokeMethod('convert_addr', ipchars);
}

myIpAddress = function () {
    return window.dotNetObject.invokeMethod('myIpAddressOverride');
}

dnsDomainLevels = function (host) {
    return window.dotNetObject.invokeMethod('dnsDomainLevels', host);
}

shExpMatch = function (str, shexp) {
    return window.dotNetObject.invokeMethod('shExpMatch', str, shexp);
}

weekdayRange = function (wd1, wd2, gmt) {
    return window.dotNetObject.invokeMethod('weekdayRange', wd1, wd2, gmt);
}

dateRange = function (date11, date12, date13, date21, date22, date23, gmt) {
    return window.dotNetObject.invokeMethod('dateRange', date11, date12, date13, date21, date22, date23, gmt);
}

timeRange = function (hour1, min1, sec1, hour2, min2, sec2, gmt) {
    return window.dotNetObject.invokeMethod('timeRange', hour1, min1, sec1, hour2, min2, sec2, gmt);
}

alert = function (message) {
    window.dotNetObject.invokeMethod('alert', message);
}

executePac = function (pacScript, url, host) {
    eval(pacScript);
    alert(FindProxyForURL(url, host));
}