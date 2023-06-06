namespace WebServiceInfrastructure.Configuration
{
    public static class WebConfigHelper
    {
        public static WebConfig CreateWebConfig()
        {
            // "localhost" must be replaced by the IP of the balance
            const string Url = "http://192.168.2.101:8080/MT/Laboratory/Balance/XprXsr/V02/MT";
            const string Password = "123456789";

            var webConfig = new WebConfig(Url, Password);
            return webConfig;
        }
    }
}
