namespace WebServiceInfrastructure.Configuration
{
    public static class WebConfigHelper
    {
        public static WebConfig CreateWebConfig(string balanceip, string passWord)
        {
            // "localhost" must be replaced by the IP of the balance
            string Url = $"http://{balanceip}:8080/MT/Laboratory/Balance/XprXsr/V03/MT";
            string Password = passWord;

            var webConfig = new WebConfig(Url, Password);
            return webConfig;
        }
    }
}
