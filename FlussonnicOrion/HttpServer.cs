using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class HttpServer
    {
        private HttpListener _httpListener;
        private bool _started;
        public event EventHandler<string> DataReceived;

        public void Start(params string[] addresses)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_started)
                        return;

                    _httpListener = new HttpListener();
                    foreach (var address in addresses)
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
                            Task.Run(() => DataReceived?.Invoke(this, requestData));
                            context.Response.StatusCode = 200;
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
            _httpListener.Close();
        }

        private string GetStreamData(Stream stream, Encoding encoding)
        {
            using (var streamReader = new StreamReader(stream, encoding))
            {
                return streamReader.ReadToEnd();
            }
        }

    }
}
