namespace WebServiceInfrastructure
{
    using System;
    using System.Net;
    using System.ServiceModel;

    public class WebConfig
    {
        public WebConfig(string url, string password)
        {
            Password = password;
            Uri uri = (new UriBuilder(url)).Uri;
            Endpoint = new EndpointAddress(uri);
            Binding = new BasicHttpBinding();
        }

        public string Password { get; private set; }

        private EndpointAddress Endpoint { get; set; }

        private BasicHttpBinding Binding { get; set; }

        public T CreateClient<T>()
            where T : ICommunicationObject
        {
            var client = (T)Activator.CreateInstance(typeof(T), Binding, Endpoint);
            client.Open();
            return client;
        }
    }
}
