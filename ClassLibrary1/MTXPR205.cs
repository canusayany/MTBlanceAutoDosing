using log4net;
using MT.Laboratory.Balance.XprXsr.V03;
using System;
using WebServiceInfrastructure;

namespace ClassLibrary1
{
    public class MTXPR205
    {
        private static ILog _log = LogManager.GetLogger(typeof(WeighingTaskService));
        private Session _session;
        private WebConfig _webConfig;
        NotificationServiceClient notificationClient;
        DosingAutomationServiceClient dosingAutomationClient;
        WeighingServiceClient weighingClient;
        WeighingTaskServiceClient weighingTaskClient;
        DraftShieldsServiceClient doorControlClient;
        public void UpdateInfo()
        {
            _webConfig = WebConfigHelper.CreateWebConfig("192.168.1.110", "123456789");
        }
        public int Connect()
        {
            notificationClient = _webConfig.CreateClient<NotificationServiceClient>();
            dosingAutomationClient = _webConfig.CreateClient<DosingAutomationServiceClient>();
            weighingClient = _webConfig.CreateClient<WeighingServiceClient>();
            weighingTaskClient = _webConfig.CreateClient<WeighingTaskServiceClient>();
            doorControlClient = _webConfig.CreateClient<DraftShieldsServiceClient>();
            try
            {
                _session = new Session(_webConfig);

            }
            catch (Exception ex)
            {
                _log.Error($"设备连接失败.{ex.Message}");
                return -1;
            }
            if (string.IsNullOrEmpty(_session.SessionId))
            {
                _log.Error($"设备连接失败.{_session}");
                return -1;
            }
            else
            {
                _log.Info($"梅特勒称重连接成功!");
                return 0;
            }
        }
        public int Disconnect()
        {
            try
            {
                _session.CancelAll();
                _session.Dispose();
                _log.Info($"梅特勒称重断开成功!");
                return 0;
            }
            catch (Exception ex)
            {
                _log.Error($"梅特勒称重断开失败.{ex.Message}");
                return -1;
            }
        }
        /// <summary>
        /// 设置屏蔽门位置
        /// </summary>
        /// <param name="openingWidth">门开的宽度(0为关闭,)</param>
        /// <param name="shieldSide">屏蔽门的选择(0左,1右)</param>
        /// <returns>0成功其余失败</returns>
        public int SetDoorPosition(int openingWidth, int shieldSide)
        {
            if (openingWidth < 0 || shieldSide < 0 || shieldSide > 1)
            {
                _log.Error($"梅特勒称重仪门位置设置失败.门位置:{openingWidth}. 门位置:{shieldSide}. 失败原因:参数错误");
                return -1;
            }
            // var doorControlClient = _webConfig.CreateClient<DraftShieldsServiceClient>();
            try
            {
                DraftShieldsService.SetPosition(_session.SessionId, doorControlClient, openingWidth, (DraftShieldIdentifier)shieldSide);
                _log.Debug($"梅特勒称重仪门位置设置成功!门位置:{openingWidth}. 门位置:{(DraftShieldIdentifier)shieldSide}");
                return 0;
            }
            catch (Exception ex)
            {
                _log.Error($"梅特勒称重仪门位置设置失败.门位置:{openingWidth}. 门位置:{(DraftShieldIdentifier)shieldSide}. 失败原因{ex.Message}");
                return -1;
            }
        }
        /// <summary>
        /// 获取门位置
        /// </summary>
        /// <param name="shieldSide">那边的门(0左,1右)</param>
        /// <returns>-1失败</returns>
        public int GetDoorPosition(int shieldSide)
        {
            if (shieldSide < 0 || shieldSide > 1)
            {
                _log.Error($"梅特勒称重仪门位置获取失败.门位置:{shieldSide}. 失败原因:参数错误");
                return -1;
            }
            // var doorControlClient = _webConfig.CreateClient<DraftShieldsServiceClient>();
            try
            {
                var response = DraftShieldsService.GetPosition(_session.SessionId, doorControlClient, (DraftShieldIdentifier)shieldSide);
                _log.Debug($"梅特勒称重仪门位置获取成功!门边:{(DraftShieldIdentifier)shieldSide}. 门位置:{response.DraftShieldsInformation[0].OpeningWidth}");
                return response.DraftShieldsInformation[0].OpeningWidth;
            }
            catch (Exception ex)
            {
                _log.Error($"梅特勒称重仪门位置获取失败.门位置:{(DraftShieldIdentifier)shieldSide}. 失败原因{ex.Message}");
                return -1;
            }
        }
        /// <summary>
        /// 开始加样品,并且等待称重完成
        /// </summary>
        /// <param name="substanceName">药品名称</param>
        /// <param name="vialName">试剂瓶名称</param>
        /// <param name="targetWeightValue">目标值</param>
        /// <param name="targetWeightUnit">单位(27为毫克,30为克)</param>
        /// <param name="lowerWeightValue">下限值</param>
        /// <param name="lowerWeightUnit">下限单位(27为毫克,30为克)</param>
        /// <param name="upperWeightValue">上限值</param>
        /// <param name="upperWeightUnit">上限单位(27为毫克,30为克)</param>
        /// <returns>成功返回称重结果失败返回-1,单位为传入的单位</returns>
        private double StartAutomateDosing(string substanceName, string vialName, decimal targetWeightValue, int targetWeightUnit, decimal lowerWeightValue, int lowerWeightUnit, decimal upperWeightValue, int upperWeightUnit)
        {
            if (string.IsNullOrEmpty(substanceName) || string.IsNullOrEmpty(vialName) || targetWeightValue < 0 || targetWeightUnit < 0 || targetWeightUnit > 3 || lowerWeightValue < 0 || lowerWeightUnit < 0 || lowerWeightUnit > 3 || upperWeightValue < 0 || upperWeightUnit < 0 || upperWeightUnit > 3)
            {
                _log.Error($"梅特勒称重仪开始称重失败.药品名称:{substanceName}. 试剂名称:{vialName}. 目标重量:{targetWeightValue}{(Unit)targetWeightUnit}. 下限重量:{lowerWeightValue}{(Unit)lowerWeightUnit}. 上限重量:{upperWeightValue}{(Unit)upperWeightUnit}. 失败原因:参数错误");
                return -1;
            }
            // var weighingTaskClient = _webConfig.CreateClient<WeighingTaskServiceClient>();

            if (WeighingTaskService.TryStartAutomatedDosingMethod(_session.SessionId, weighingTaskClient))
            {
                DosingJob[] demoDosingJobList = new DosingJob[1];
                demoDosingJobList[0] = new DosingJob
                {
                    SubstanceName = substanceName,
                    VialName = vialName,
                    TargetWeight = new WeightWithUnit { Value = targetWeightValue, Unit = (Unit)targetWeightUnit },
                    LowerTolerance = new WeightWithUnit { Value = lowerWeightValue, Unit = (Unit)lowerWeightUnit },
                    UpperTolerance = new WeightWithUnit { Value = upperWeightValue, Unit = (Unit)upperWeightUnit }
                };
                if (DosingAutomationService.StartJobList(_session.SessionId, demoDosingJobList, dosingAutomationClient))
                {
                    if (!DosingAutomationService.StartHandlingDosingAutomationNotifications(_session.SessionId, notificationClient, dosingAutomationClient, out DosingResult dosingResult, out DosingAutomationFinishedAsyncNotification dosingAutomationFinished))
                    {
                        _log.Error($"梅特勒称重仪开始称重失败.药品名称:{substanceName}. 试剂名称:{vialName}. 目标重量:{targetWeightValue}{(Unit)targetWeightUnit}. 下限重量:{lowerWeightValue}{(Unit)lowerWeightUnit}. 上限重量:{upperWeightValue}{(Unit)upperWeightUnit}. 失败原因{dosingAutomationFinished.FailureDescription}");

                        throw new Exception(dosingAutomationFinished.FailureDescription);
                    }
                    else
                    {
                        _log.Debug($"梅特勒称重仪开始称重成功!药品名称:{substanceName}. 试剂名称:{vialName}. 目标重量:{targetWeightValue}{(Unit)targetWeightUnit}. 下限重量:{lowerWeightValue}{(Unit)lowerWeightUnit}. 上限重量:{upperWeightValue}{(Unit)upperWeightUnit}");
                        return double.Parse(dosingResult.WeightSample.NetWeight.Value);
                    }
                }
            }
            return -1;
        }
    }
}
