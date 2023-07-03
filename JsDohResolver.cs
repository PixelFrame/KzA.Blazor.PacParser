using Microsoft.JSInterop;

namespace KzA.Blazor.PacParser
{
    internal class JsDohResolver
    {
        private readonly IJSInProcessRuntime _jsRuntime;

        public JsDohResolver(IJSInProcessRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string DohResolveA(string name)
        {
            return _jsRuntime.Invoke<string>("dohResolveA", name);
        }
    }
}