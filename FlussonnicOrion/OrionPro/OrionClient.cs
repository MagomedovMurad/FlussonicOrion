using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.OrionPro.Models;
using Orion;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlussonnicOrion.OrionPro
{
    public class OrionClient
    {
        private OrionProClient _client;
        private EndpointAddress _remoteAddress;
        private OrionSettings _settings;
        private string _token;
        private Timer _timer;

        public OrionClient()
        {
        }

        #region Initialize

        public async Task Initialize(OrionSettings settings)
        {
            _settings = settings;
            _remoteAddress = new EndpointAddress($"http://{settings.IPAddress}:{settings.Port}/soap/IOrionPro");

            InitializeClient();
            await InitializeToken(_settings.TokenLifetime);
        }
        private void InitializeClient()
        {
            var binding = CreateBinding();

            _client = new OrionProClient(binding, _remoteAddress);
            if (_settings.ModuleUserName!= null && _settings.ModulePassword != null)
            {
                _client.ClientCredentials.UserName.UserName = _settings.ModuleUserName;
                _client.ClientCredentials.UserName.Password = _settings.ModulePassword;
            }
        }
        private async Task InitializeToken(int tokenLifetime)
        {
            var hash = GetMd5Hash(_settings.EmployeePassword);
            var response = await _client.GetLoginTokenAsync(_settings.EmployeeUserName, hash);
            if (!CheckResponse(response.@return.Success, response.@return.ServiceError))
                return;

            _token = response.@return.OperationResult;
            StartTokenExpirationExtending(tokenLifetime);
        }

        private BasicHttpBinding CreateBinding()
        {
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            return binding;
        }
        private void StartTokenExpirationExtending(int tokenLifetime)
        {
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval = tokenLifetime * 1000;
            _timer.Start();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ExtendTokenExpiration();
        }
        private async Task ExtendTokenExpiration()
        {
            var response = await _client.ExtendTokenExpirationAsync(_token);
            if (CheckResponse(response.@return.Success, response.@return.ServiceError))
                _token = response.@return.OperationResult;
        }
        private string GetMd5Hash(string data)
        {
            using (var md5 = MD5.Create())
            {
                var sourceBytes = Encoding.UTF8.GetBytes(data);
                var hashBytes = md5.ComputeHash(sourceBytes);
                var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                return hash;
            }
        }

        #endregion

        #region Utils
        private bool CheckResponse(bool isSucces, TServiceError error)
        {
            if (isSucces)
                return true;

            Console.WriteLine($"Error code: {error.ErrorCode}. {error.Description}. InnerException: {error.InnerExceptionMessage}");
            return false;
        }
        #endregion

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            }
        }

        public async Task Test()
        {
            try
            {
                var key = _client.GetKeyDataAsync("Е340РХ126", 5, _token).Result;
                var person = _client.GetPersonByPassAsync("Е340РХ126", true, 5, _token).Result;
                var tt = _client.GetPersonByIdAsync(2, true, _token).Result;
                var tpersondata = new TPersonData()
                {
                    Id = 2, 
                    Photo = new byte[0]
                };
                var t = _client.GetPersonPassListAsync(tpersondata, _token).Result;
                var cars = await _client.GetCarsAsync(_token);
                var items = await _client.GetItemsAsync(_token);
                await ControlAccesspoint(1, 4, ActionType.Passage, 1);
               // await AddExternalEvent(2, 2, "Тестовое событие");

                //var tt2 = _client.GetPersonsCountAsync(null).Result;
                //var tt1 = _client.GetPersonsAsync(true, 0, 0, null, false, true, null).Result;
                //var tt = _client.GetCarsAsync(null).Result;
                //var t = _client.GetKeysAsync(0, 50, null).Result;
            }
            catch (Exception ex)
            { 
            
            }
        }


        public async Task AddExternalEvent(int keyId, int personId, string text)
        {
            var externalEvent = new TExternalEvent()
            {
                Id = 12,
                ItemId = 1,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Event = 2,
                KeyId = keyId,
                PersonId = personId,
                TimeStamp = DateTime.Now,
                Text = text
            };
            var tt = await _client.AddExternalEventAsync(externalEvent, _token);
            var yy = tt.@return.OperationResult;
        }

        public async Task<bool> ControlAccesspoint(int accesspointId, int commandId, ActionType action, int personId)
        {
            var accesspoint = new TItem()
            {
                ItemId = accesspointId,
                ItemType = ItemType.ACCESSPOINT.ToString(),
                Timestamp = DateTime.Now
            };
            var response = await _client.ControlItemsAsync(_token, new[] { accesspoint }, commandId, (int)action, personId);
            var result = response.@return.OperationResult;
            return response.@return.Success && result.Select(x => x.ItemId).Contains(accesspointId);
        }
    }
}