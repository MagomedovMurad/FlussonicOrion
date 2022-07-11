using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlussonicOrion
{
    public class HttpServer
    {
        private HttpListener _httpListener;
        private bool _started;
        private Dictionary<string, Func<string, HttpResponse>> _subscriptions;
        private List<string> _prefixes;
        private IServiceSettingsController _serviceSettingsController;

        public HttpServer(IServiceSettingsController serviceSettingsController)
        {
            _serviceSettingsController = serviceSettingsController;
            _subscriptions = new Dictionary<string, Func<string, HttpResponse>>();
            _prefixes = new List<string>();
        }
        
        public void Subscribe(string prefix, Func<string, HttpResponse> func)
        {
            prefix = prefix.Replace("port", _serviceSettingsController.Settings.ServerSettings.ServerPort.ToString());
            _prefixes.Add(prefix);
            var url = new Uri(prefix.Replace("+", "127.0.0.1"));
            _subscriptions.Add(url.AbsolutePath.Remove(url.AbsolutePath.Length -1), func);
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_started)
                        return;

                    _httpListener = new HttpListener();
                    foreach (var address in _prefixes)
                        _httpListener.Prefixes.Add(address);

                    _httpListener.Start();
                    _started = true;
                    while (_started)
                    {
                        try
                        {
                            HttpListenerContext context = await _httpListener.GetContextAsync();
                            HttpListenerRequest request = context.Request;
                            var requestData = GetStreamData(request.InputStream, request.ContentEncoding);
                            var response = _subscriptions[request.RawUrl]?.Invoke(requestData);

                            context.Response.StatusCode = response.Code;
                            if (response.Data != null)
                                context.Response.Close(response.Data, false);
                            else
                                context.Response.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        public void Stop()
        {
            _started = false;
            _httpListener?.Close();
        }

        private string GetStreamData(Stream stream, Encoding encoding)
        {
            using (var streamReader = new StreamReader(stream, encoding))
            {
                return streamReader.ReadToEnd();
            }
        }

    }

    public class HttpResponse
    {
        public HttpResponse(int code, byte[] data)
        {
            Code = code;
            Data = data;
        }

        public int Code { get; set; }
        public byte[] Data { get; set; }
    }
}
