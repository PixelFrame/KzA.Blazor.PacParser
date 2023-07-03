using Microsoft.JSInterop;
using System.Net;

namespace KzA.Blazor.PacParser
{
    public class PacParserService
    {
        private readonly JsFunctions jsFunctions;
        private readonly IJSInProcessRuntime runtime;

        public string JsConsole => jsFunctions.JsConsole;
        public string DebugOutput => jsFunctions.DebugOutput;

        public PacParserService(IJSInProcessRuntime runtime)
        {
            this.runtime = runtime;
            var resolver = new JsDohResolver(runtime);
            jsFunctions = new(resolver);
            var dotNetReference = DotNetObjectReference.Create(jsFunctions);
            runtime.InvokeVoid("initializePacWorker", dotNetReference);
        }

        public void Parse(string PAC, string Url, string Host, IPAddress MyIpAddress)
        {
            jsFunctions.ClearConsole();
            jsFunctions.ClearDebug();
            jsFunctions.MyIpAddress = MyIpAddress;
            runtime.InvokeVoid("executePac", PAC, Url, Host);
        }
    }
}