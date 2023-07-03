# KzA.Blazor.PacParser

[PacParser](https://github.com/PixelFrame/PacParser) ported to Blazor WASM.

## About JS Runtime

Since we have a real browser providing the JavaScript runtime, we don't need JINT any more. Although I've tested that we actually can run JINT on Blazor WASM with good performance (Yes, a JS runtime on another JS runtime).

## About DNS Resolution

The most tricky part to port the project to WASM is doing DNS resolution. Note that in WSAM, we cannot perform any network requsts directly, so surely DNS related API cannot be called. Thus, the only way to do DNS resolution is to use DoH API from Google DNS or CloudFlare,.

But in PAC, dnsResolve is called synchronizely, which makes us have to also implement a synchronize DNS resolution method. The `HttpClient` API can only do async requests. And again in WASM we cannot block the only thread to wait for result. As a result, we need [a synchronize HTTP request method in JS](https://github.com/larsgw/sync-fetch), so that we can call it synchronizely from C#.

However, if some hosts are only resolvable from a private DNS server that does not support DoH, we won't be able to resolve them correctly. Customizing hosts is planned to be supported in the future, although it may be time-consuming for user to input all hosts.

## About myIpAddress

We have no way to know the client real IP address in Blazor WASM, so this must be set by user input.

## Usage

JS required

``` HTML
<script src="https://unpkg.com/sync-fetch"></script>
<script src="_content/KzA.Blazor.PacParser/pacWorker.js"></script>
```

Register services

``` CSharp
builder.Services.AddSingleton(services => (IJSInProcessRuntime)services.GetRequiredService<IJSRuntime>());
builder.Services.AddSingleton<PacParserService>();
```

Inject and use

``` razor
@inject PacParserService PacParserService

@code {
    string PacScript = "function FindProxyForURL(url, host) { return 'DIRECT'; }";
    string Url = "https://www.example.com";
    string Host = "www.example.com";
    System.Net.IPAddress myIpAddr = System.Net.IPAddress.Parse("127.0.0.1");

    protected override async Task OnInitializedAsync()
    {
        PacParserService.Parse(PacScript, Url, Host, myIpAddr);
        Console.WriteLine(PacParserService.DebugOutput);
        Console.WriteLine(PacParserService.JsConsole);
    }
}
```

## TODO

- [ ] Allow custom hosts
