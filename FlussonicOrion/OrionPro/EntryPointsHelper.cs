using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro
{
    public class EntryPointsHelper
    {
        private ILogger<EntryPointsHelper> _logger;
        private HttpServer _server;
        private IOrionClient _orionClient;
        public EntryPointsHelper(HttpServer server, IOrionClient orionClient, ILogger<EntryPointsHelper> logger)
        {
            _orionClient = orionClient;
            _server = server;
            _logger = logger;
        }

        public void Initialize()
        {
            _server.Subscribe($"http://+:port/entry_points/", DataReceived);
        }

        public HttpResponse DataReceived(string data)
        {
            try
            {
                var response = CreateEntryPointsResponse().Result;
                return new HttpResponse(200, Encoding.UTF8.GetBytes(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке запроса на получение точек доступа");
                return new HttpResponse(500, null);
            }
        }

        public async Task<string> CreateEntryPointsResponse()
        {
            var html = "<!DOCTYPE html>" +
                       "<html>" +
                       "<body>" +
                       "<meta charset=\"utf-8\">" +
                       "<h2>Точки доступа</h2>" +
                       "<table style=\"width: 100 % \">" +
                       "<tr>" +
                       "<th>ID</th>" +
                       "<th>Дверь</th>" +
                       "<th>Зона на вход</th>" +
                       "<th>Зона на выход</th>" +
                       "</tr>";

            var entryPoints = await GetEntryPionts();
            foreach (var entryPoint in entryPoints)
            {
                html += "<tr>" +
                        $"<td>{entryPoint.Id}</td>" +
                        $"<td>{entryPoint.Name}</td>" +
                        $"<td>{entryPoint.EnterZone}</td>" +
                        $"<td>{entryPoint.ExitZone}</td>" +
                        $"</tr>"; 
            }

            html += "</table>" +
                    "</body>" +
                    "</html>";
            return html;
        }

        public async Task<EntryPoint[]> GetEntryPionts()
        {
            var entryPoints = await _orionClient.GetEntryPoints(0, 0);
            var accessZones = await _orionClient.GetAccessZones();

            return entryPoints.Select(ep =>
            {
                var enterZone = accessZones.FirstOrDefault(az => az.Id == ep.EnterAccessZoneId);
                var exitZone = accessZones.FirstOrDefault(az => az.Id == ep.ExitAccessZoneId);
                return new EntryPoint()
                {
                    Id = ep.Id,
                    Name = ep.Name,
                    EnterZone = enterZone?.Name,
                    ExitZone = exitZone?.Name
                };
            }).ToArray();
        }
    }

    public class EntryPoint
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EnterZone { get; set; }
        public string ExitZone { get; set; }
    }
}
