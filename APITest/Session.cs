namespace WebServiceInfrastructure
{
    using MT.Laboratory.Balance.XprXsr.V03;
    using System;

    public class Session : IDisposable
    {
        private SessionServiceClient sessionServiceClient;
        private volatile bool disposed;

        public Session(WebConfig webConfig)
        {
            // create session client
            sessionServiceClient = webConfig.CreateClient<SessionServiceClient>();

            // open session
            var openSessionResponse = sessionServiceClient.OpenSession(new OpenSessionRequest());
            SessionId = SessionIdDecryptor.Decrypt(webConfig.Password, openSessionResponse.SessionId, openSessionResponse.Salt);
        }

        public string SessionId { get; private set; }

        public void CancelAll()
        {
            sessionServiceClient.Cancel(new CancelRequest(SessionId, CancelType.All, null));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                if (!disposed)
                {
                    CancelAll();
                    sessionServiceClient.CloseSession(new CloseSessionRequest(SessionId));
                    SessionId = null;
                    sessionServiceClient = null;
                }
            }
        }
    }
}
