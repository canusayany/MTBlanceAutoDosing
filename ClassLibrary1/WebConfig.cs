using log4net;
using log4net.Repository.Hierarchy;
using MT.Laboratory.Balance.XprXsr.V03;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;

namespace WebServiceInfrastructure
{
    public class DraftShieldsService
    {
        static ILog _log = LogManager.GetLogger(typeof(WeighingTaskService));
        internal static bool SetPosition(string sessionId, DraftShieldsServiceClient doorControlClient, int openingWidth, DraftShieldIdentifier draftShieldIdentifier)
        {
            _log.Debug("SetPosition");
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
            _log.Debug($"设置门宽度为{openingWidth},成功与否:{Response.Outcome} {Response.ErrorMessage}");
            if (string.IsNullOrEmpty(Response.ErrorMessage))
            {
                throw new Exception(Response.ErrorMessage);
            }
            return Response.Outcome == Outcome.Success;
        }
        internal static GetPositionResponse GetPosition(string sessionId, DraftShieldsServiceClient doorControlClient, DraftShieldIdentifier draftShieldIdentifier)
        {
            _log.Debug("GetPosition");
            var getPositionRequest = new GetPositionRequest
            {
                SessionId = sessionId,
                DraftShieldIds = new DraftShieldIdentifier[]{
               draftShieldIdentifier
                 }
            };
            GetPositionResponse Response = doorControlClient.GetPosition(getPositionRequest);
            // Logger.TraceOutcome(Response.Outcome, "GetPosition", Response.ErrorMessage);
            // Console.WriteLine("");
            _log.Debug(Response.DraftShieldsInformation[0].OpeningWidth.ToString());
            return Response;
        }
    }
    public class WeighingTaskService
    {
        static ILog _log = LogManager.GetLogger(typeof(WeighingTaskService));
        public static bool TryStartAutomatedDosingMethod(string sessionId, IWeighingTaskService weighingTaskService)
        {
            _log.Debug("Start dosing automation method...");
            var listOfMethods = weighingTaskService.GetListOfMethods(new GetListOfMethodsRequest(sessionId));
            var automatedDosingMethod = listOfMethods.Methods.FirstOrDefault(m => m.MethodType == MethodType.AutomatedDosing);
            if (automatedDosingMethod == null)
            {
                _log.Error("No automated dosing method with type AutomatedDosing found (must be created manually on the terminal).");
                return false;
            }

            var response = weighingTaskService.StartTask(new StartTaskRequest(sessionId, automatedDosingMethod.Name));
            if (response.Outcome != Outcome.Success)
            {
                _log.Error("Automated dosing method could not be started.");
                return false;
            }

            return true;
        }
    }
    public class DosingAutomationService
    {
        static ILog _log = LogManager.GetLogger(typeof(WeighingTaskService));
        public static bool StartJobList(string sessionId, DosingJob[] dosingJobs, IDosingAutomationService dosingAutomationClient)
        {
            _log.Debug("Starting dosing job list...");
            var startJobListRequest = new StartExecuteDosingJobListAsyncRequest(sessionId, dosingJobs);
            var response = dosingAutomationClient.StartExecuteDosingJobListAsync(startJobListRequest);
            //  Logger.TraceOutcome(response.Outcome, "StartDosingJobList", response.ErrorMessage);
            if (response.Outcome != Outcome.Success)
            {
                if (response.JobErrors != null)
                {
                    foreach (var responseJobError in response.JobErrors)
                    {
                        _log.Error("DosingJob validation error: " + responseJobError.Error);
                    }
                }

                return false;
            }

            return true;
        }

        public static bool StartHandlingDosingAutomationNotifications(string sessionId, NotificationServiceClient notificationClient, DosingAutomationServiceClient dosingAutomationClient, out DosingResult dosingResult, out DosingAutomationFinishedAsyncNotification dosingAutomationFinished)
        {
            _log.Debug("Starting handling dosing automation notifications...");
            var pollingCount = 0;
            dosingAutomationFinished = new DosingAutomationFinishedAsyncNotification();
            dosingResult = null;
            while (true)
            {
                var response = notificationClient.GetNotifications(new GetNotificationsRequest(sessionId, 500));
                //if (pollingCount > 200 || response.Notifications.Any(n => n is DosingAutomationFinishedAsyncNotification))
                //{
                //    _log.Debug("> Checking for notifications...");


                //    _log.Debug("> Stop checking for notifications.");
                //    // stop polling
                //    return;
                //}
                if (pollingCount > 200)
                {
                    _log.Debug("> Checking for notifications...");
                    _log.Debug("> Stop checking for notifications.");
                    // stop polling
                    return false;
                }
                foreach (var notificationNotification in response.Notifications)
                {
                    var actionNotification = notificationNotification as DosingAutomationActionAsyncNotification;
                    if (actionNotification != null)
                    {
                        HandleActionRequest(actionNotification.DosingJobActionType, actionNotification.ActionItem, sessionId, dosingAutomationClient);
                    }

                    var jobFinished = notificationNotification as DosingAutomationJobFinishedAsyncNotification;
                    if (jobFinished != null)
                    {
                        if (jobFinished.Outcome == Outcome.Success)
                        {
                            _log.Info($"DosingJob result: Dosing job success (duration {jobFinished.DosingResult.DurationInSeconds}s).");
                            //打印毛重净重去皮重
                            _log.Info($"jobFinished.DosingResult GrossWeight:{jobFinished.DosingResult.WeightSample.GrossWeight.Value} {jobFinished.DosingResult.WeightSample.GrossWeight.Unit.ToString()}");
                            //Logger.Trace($"jobFinished.DosingResult TareWeight:{jobFinished.DosingResult.WeightSample.TareWeight}{jobFinished.DosingResult.WeightSample.TareWeight.Unit}");
                            _log.Info($"jobFinished.DosingResult NetWeight:{jobFinished.DosingResult.WeightSample.NetWeight.Value}{jobFinished.DosingResult.WeightSample.NetWeight.Unit}");
                            dosingResult = jobFinished.DosingResult;
                            return true;
                        }
                        else
                        {
                            _log.Error($"DosingJob result: Dosing job failure: {jobFinished.DosingError}.");
                            return false;
                        }
                    }

                    dosingAutomationFinished = notificationNotification as DosingAutomationFinishedAsyncNotification;
                    if (dosingAutomationFinished != null)
                    {
                        _log.Error("DosingAutomationFinished result: " + dosingAutomationFinished.FailureReason);
                        return false;
                    }
                }


                Thread.Sleep(800);
                pollingCount++;
            }
        }

        private static void HandleActionRequest(DosingJobActionType actionType, string actionItem, string sessionId, DosingAutomationServiceClient dosingAutomationClient)
        {
            _log.Error($"Executing action: {actionType} '{actionItem}'...");

            // simulate a realistic action execution
            Thread.Sleep(2000);

            switch (actionType)
            {
                case DosingJobActionType.PlaceDosingHead:
                    dosingAutomationClient.ConfirmDosingJobAction(new ConfirmDosingJobActionRequest(sessionId, DosingJobActionType.PlaceDosingHead, actionItem));
                    break;
                case DosingJobActionType.RemoveDosingHead:
                    dosingAutomationClient.ConfirmDosingJobAction(new ConfirmDosingJobActionRequest(sessionId, DosingJobActionType.RemoveDosingHead, actionItem));
                    break;
                case DosingJobActionType.PlaceVial:
                    dosingAutomationClient.ConfirmDosingJobAction(new ConfirmDosingJobActionRequest(sessionId, DosingJobActionType.PlaceVial, actionItem));
                    break;
                case DosingJobActionType.RemoveVial:
                    dosingAutomationClient.ConfirmDosingJobAction(new ConfirmDosingJobActionRequest(sessionId, DosingJobActionType.RemoveVial, actionItem));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
            }

            _log.Info("Action confirmed.");
        }
    }
    public class WeighingService
    {
        static ILog _log = LogManager.GetLogger(typeof(WeighingTaskService));
        public static bool Zero(string sessionId, IWeighingService weighingService)
        {
            //Zeroing
            _log.Debug("Zeroing");
            var zeroRequest = new ZeroRequest
            {
                SessionId = sessionId
            };
            var zeroResponse = weighingService.Zero(zeroRequest);
            _log.Error($"Zero 失败 {zeroResponse.Outcome},失败原因:{zeroResponse.ErrorMessage}");

            return zeroResponse.Outcome == Outcome.Success;
        }
        //Tare
        public static bool Tare(string sessionId, IWeighingService weighingService)
        {
            //Tare
            _log.Debug("Taring");
            var tareRequest = new TareRequest
            {
                SessionId = sessionId
            };
            var tareResponse = weighingService.Tare(tareRequest);
            _log.Error($"Tare 失败 {tareResponse.Outcome},失败原因:{tareResponse.ErrorMessage}");
            return tareResponse.Outcome == Outcome.Success;
        }

        public static void GetWeightValues(string sessionId, IWeighingService weighingService, IWeighingTaskService weighingTaskService)
        {
            // start method "General weighing"
            _log.Debug("Starting task");
            var startTaskResult = weighingTaskService.StartTask(new StartTaskRequest
            {
                SessionId = sessionId,
                MethodName = "General Weighing"
            }
            );

            if (startTaskResult.Outcome != Outcome.Success)
            {
                _log.Debug("Could not start task.");
            }

            //set target and tolerances
            _log.Debug("Setting task and tolerances");
            var setTTkResponse = weighingTaskService.SetTargetValueAndTolerances(new SetTargetValueAndTolerancesRequest
            {
                SessionId = sessionId,
                LowerTolerance = new WeightWithUnit { Value = 1, Unit = Unit.Milligram },
                TargetWeight = new WeightWithUnit { Value = 5, Unit = Unit.Milligram },
                UpperTolerance = new WeightWithUnit { Value = 1, Unit = Unit.Milligram }
            });
            _log.Error($"SetTargetValueAndTolerances 失败 {setTTkResponse.Outcome},失败原因:{setTTkResponse.ErrorMessage}");

            //Zeroing
            _log.Debug("Zeroing");
            var zeroRequest = new ZeroRequest
            {
                SessionId = sessionId
            };
            var zeroResponse = weighingService.Zero(zeroRequest);
            _log.Error($"Zero 失败 {zeroResponse.Outcome},失败原因:{zeroResponse.ErrorMessage}");

            //Add to protocol
            _log.Debug("Adding result to protocol.");
            var result = weighingTaskService.AddToProtocol(new AddToProtocolRequest
            {
                SessionId = sessionId
            });

            _log.Debug($"The weight value is: {result.WeighingItem.WeightSample.NetWeight.Value} {result.WeighingItem.WeightSample.NetWeight.Unit}, Alibi ID: {result.WeighingItem.AlibiId}");

            //Complete the Task
            _log.Debug("Completing the task.");
            var completeTaskResponse = weighingTaskService.CompleteCurrentTask(new CompleteCurrentTaskRequest
            {
                SessionId = sessionId
            });
            _log.Error($"CompleteCurrentTask 失败 {completeTaskResponse.Outcome},失败原因:{completeTaskResponse.ErrorMessage}");
        }
    }
    public static class WebConfigHelper
    {
        public static WebConfig CreateWebConfig(string balanceip, string passWord,int port=8080)
        {
            // "localhost" must be replaced by the IP of the balance
            string Url = $"http://{balanceip}:{port}/MT/Laboratory/Balance/XprXsr/V03/MT";
            string Password = passWord;

            var webConfig = new WebConfig(Url, Password);
            return webConfig;
        }
    }
    public static class SessionIdDecryptor
    {
        public static string Decrypt(string password, string encryptedSessionId, string encodedSalt)
        {
            var encryptedSessionIdData = Convert.FromBase64String(encryptedSessionId);
            var decodedSaltData = Convert.FromBase64String(encodedSalt);
            var key = ComputeKeyFromPassword(password, decodedSaltData);
            var sessionIdData = Decrypt(key, encryptedSessionIdData);
            var sessionId = System.Text.Encoding.UTF8.GetString(sessionIdData);
            return sessionId;
        }

        private static byte[] ComputeKeyFromPassword(string password, byte[] saltData)
        {
            var passwordData = System.Text.Encoding.UTF8.GetBytes(password);
            var key = GetEncryptionKeyFromPassword(passwordData, saltData);
            return key;
        }

        private static byte[] Decrypt(byte[] key, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var rijndaelManaged = new RijndaelManaged())
                {
                    rijndaelManaged.Key = key;
                    rijndaelManaged.Mode = CipherMode.ECB;
                    using (var cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(),
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        private static byte[] GetEncryptionKeyFromPassword(byte[] password, byte[] salt)
        {
            var keyGen = new Rfc2898DeriveBytes(password, salt, 1000); // 1000 fix
            var key = keyGen.GetBytes(32);
            return key;
        }
    }

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
            if (openSessionResponse.ErrorMessage != null)
            {
                throw new Exception(openSessionResponse.ErrorMessage);

            }
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
