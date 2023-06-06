using MT.Laboratory.Balance.XprXsr.V03;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServiceInfrastructure.Configuration;
using WebServiceInfrastructure;
using Weighing;
using System;

namespace APITest
{
    public class DraftShieldsService
    {
        internal static bool SetPosition(string sessionId, DraftShieldsServiceClient doorControlClient, int openingWidth, DraftShieldIdentifier draftShieldIdentifier)
        {
            Logger.Trace("SetPosition");
            var setPositionRequest = new SetPositionRequest
            {
                SessionId = sessionId,
                DraftShieldsPositions = new DraftShieldPosition[]{
                    new DraftShieldPosition()
                    {
                        DraftShieldId= draftShieldIdentifier,
                        OpeningWidth=openingWidth,
                        OpeningSide=null
                    } }

            };
            var Response = doorControlClient.SetPosition(setPositionRequest);
            // Logger.TraceOutcome(Response.Outcome, "SetPosition", Response.ErrorMessage);
            Logger.Trace(Response.ToString());
            return Response.Outcome == Outcome.Success;
        }
        internal static GetPositionResponse GetPosition(string sessionId, DraftShieldsServiceClient doorControlClient, DraftShieldIdentifier draftShieldIdentifier)
        {
            Logger.Trace("GetPosition");
            var getPositionRequest = new GetPositionRequest
            {
                SessionId = sessionId,
                DraftShieldIds = new DraftShieldIdentifier[]{
               draftShieldIdentifier
                 }
            };
            GetPositionResponse Response = doorControlClient.GetPosition(getPositionRequest);
            // Logger.TraceOutcome(Response.Outcome, "GetPosition", Response.ErrorMessage);
            Console.WriteLine("");
            return Response;
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {

            // configure ip/password inside the web config helper class
            WebConfig webConfig = WebConfigHelper.CreateWebConfig("192.168.1.110", "123456789");

            // init service clients
            var notificationClient = webConfig.CreateClient<NotificationServiceClient>();
            var dosingAutomationClient = webConfig.CreateClient<DosingAutomationServiceClient>();
            var weighingClient = webConfig.CreateClient<WeighingServiceClient>();
            var weighingTaskClient = webConfig.CreateClient<WeighingTaskServiceClient>();
            var doorControlClient = webConfig.CreateClient<DraftShieldsServiceClient>();
            using (Session session = new Session(webConfig))
            {
                DraftShieldsService.SetPosition(session.SessionId, doorControlClient, 75, DraftShieldIdentifier.LeftOuter);
                DraftShieldsService.SetPosition(session.SessionId, doorControlClient, 0, DraftShieldIdentifier.LeftOuter);
                // zero 
                if (WeighingService.Zero(session.SessionId, weighingClient))
                {

                    // start automated dosing method if existing on terminal
                    if (WeighingTaskService.TryStartAutomatedDosingMethod(session.SessionId, weighingTaskClient))
                    {
                        // start job list
                        var demoDosingJobList = CreateDosingJobList().ToArray();
                        if (DosingAutomationService.StartJobList(session.SessionId, demoDosingJobList, dosingAutomationClient))
                        {
                            // start dosing automation interaction
                            DosingAutomationService.StartHandlingDosingAutomationNotifications(session.SessionId, notificationClient, dosingAutomationClient);
                        }
                    }
                }

                // notify user
                Logger.Finish();
            }
        }
        private static void RunGetWeighingValuesSample()
        {
            // configure ip/password inside the web config helper class
            var webConfig = WebConfigHelper.CreateWebConfig("192.168.2.101", "123456789");

            // init service clients
            var weighingServiceClient = webConfig.CreateClient<WeighingServiceClient>();
            var weighingTaskServiceClient = webConfig.CreateClient<WeighingTaskServiceClient>();

            using (var session = new Session(webConfig))
            {
                WeighingService.GetWeightValues(session.SessionId, weighingServiceClient, weighingTaskServiceClient);
                Logger.Finish();
            }
        }

        private static IEnumerable<DosingJob> CreateDosingJobList()
        {
            yield return new DosingJob
            {
                SubstanceName = "Al2O3",
                VialName = "xlp",
                TargetWeight = new WeightWithUnit { Unit = Unit.Milligram, Value = 2 },
                LowerTolerance = new WeightWithUnit { Value = 1, Unit = Unit.Milligram },
                UpperTolerance = new WeightWithUnit { Value = 1, Unit = Unit.Milligram }
            };

            //yield return new DosingJob
            //{
            //    SubstanceName = "Sugar",
            //    VialName = "Vial2",
            //    TargetWeight = new WeightWithUnit { Unit = Unit.Milligram, Value = 22 }
            //};
        }
    }
}
