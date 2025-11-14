using System.Net.WebSockets;
using System.Text;
using Avalonia.Threading;
using Newtonsoft.Json;
using Nolvus.Core.Services;
using Nolvus.NexusApi.SSO.Events;
using Nolvus.NexusApi.SSO.Responses;
using Nolvus.Core.Misc;


namespace Nolvus.NexusApi.SSO
{
    public class NexusSSOManager
    {
        #region Field
        private bool isAuthenticated = false;
        private ClientWebSocket? webSocket;
        private NexusSSORequest? currentRequest;
        private readonly string endpoint = "wss://sso.nexusmods.com";
        #endregion

        #region Handlers
        public event OnAuthenticatingHandler? OnAuthenticating;
        public event OnAuthenticatedHandler? OnAuthenticated;
        public event OnRequestErrorHandler? OnRequestError;
        #endregion

        public NexusSSOManager() { }

        public bool Authenticated => isAuthenticated;

        public async Task Connect()
        {
            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(endpoint), CancellationToken.None);
                StartListenerThread();
            }
            catch (Exception ex)
            {
                OnRequestError?.Invoke(this, new RequestErrorEventArgs(ex.Message));
            }
        }

        public async Task Authenticate()
        {
            if (isAuthenticated)
                return;

            try
            {
                if (webSocket == null || webSocket.State != WebSocketState.Open)
                {
                    TriggerError("Websocket not connected");
                    return;
                }

                currentRequest = new NexusSSORequest { id = Guid.NewGuid().ToString() };
                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(currentRequest))), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log(ex.Message);
            }
        }

        public void Close()
        {
            try
            {
                webSocket?.Dispose();
            }
            catch { }
            webSocket = null;
        }

        private void StartListenerThread()
        {
            Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];

                while (webSocket != null && (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent))
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log(ex.Message);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    NexusSSOResponse response = null;

                    try
                    {
                        response = JsonConvert.DeserializeObject<NexusSSOResponse>(json);
                    }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log(ex.Message);
                    }

                    if (response == null)
                    {
                        ServiceSingleton.Logger.Log("Received null response from server");
                        continue;
                    }

                    await HandleServerResponse(response);
                }
            });
        }

        private async Task HandleServerResponse(NexusSSOResponse response)
        {
            if (!response.Success)
            {
                ServiceSingleton.Logger.Log(response.Error);
                return;
            }

            if (!string.IsNullOrEmpty(response?.Data?.ApiKey))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TriggerAuthenticated(response.Data.ApiKey);
                    isAuthenticated = true;
                });
                //OnBrowserClosedEvent?.Invoke(this, EventArgs.Empty); //why
                return;
            }

            if (!string.IsNullOrEmpty(response?.Data?.Token))
            {
                currentRequest.SetToken(response.Data.Token);

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    TriggerAuthenticating(currentRequest.id);
                });

                return;
            }
            ServiceSingleton.Logger.Log("Unknown SSO Response");
        }

        private void TriggerAuthenticated(string apiKey)
        {
            OnAuthenticated?.Invoke(this, new AuthenticationEventArgs(apiKey));
        }

        private void TriggerAuthenticating(string uuid)
        {
            OnAuthenticating?.Invoke(this, new AuthenticatingEventArgs(uuid));
        }
        
        private void TriggerError(string message)
        {
            OnRequestError?.Invoke(this, new RequestErrorEventArgs(message));
        }
    }
}