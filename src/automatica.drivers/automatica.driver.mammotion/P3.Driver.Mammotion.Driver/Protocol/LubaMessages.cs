using System;
using System.Collections.Generic;

namespace P3.Driver.Mammotion.Driver.Protocol
{
    /// <summary>
    /// Task control actions of NavTaskCtrl (PyMammotion navigation.py)
    /// </summary>
    internal enum MowerAction
    {
        Start = 1,
        Pause = 2,
        Resume = 3,
        Cancel = 4,
        ReturnToDock = 5,
        CancelReturnToDock = 12
    }

    /// <summary>
    /// State reported by the mower, every field is optional
    /// </summary>
    internal sealed record MowerStatus
    {
        public int? Battery { get; init; }
        public int? DeviceState { get; init; }
        public int? ChargeState { get; init; }
        /// <summary>
        /// Progress of the current job in % (high 16 bits of work.area)
        /// </summary>
        public int? Progress { get; init; }

        /// <summary>
        /// Area of the current job in m² (low 16 bits of work.area)
        /// </summary>
        public int? Area { get; init; }

        /// <summary>
        /// Remaining time of the current job in minutes (high 16 bits of work.progress)
        /// </summary>
        public int? RemainingMinutes { get; init; }

        public int? KnifeHeight { get; init; }

        public bool IsEmpty => Battery == null && DeviceState == null && ChargeState == null && Progress == null && Area == null && RemainingMinutes == null && KnifeHeight == null;
    }

    /// <summary>
    /// LubaMsg (pymammotion/proto/luba_msg.proto): builds the commands and decodes the status parts of the reports
    /// </summary>
    internal static class LubaMessages
    {
        private const int MsgCmdTypeNav = 240;
        private const int MsgCmdTypeEmbedSys = 244;
        private const int DevMobileApp = 7;
        private const int DevMainCtl = 1;
        private const int MsgAttrReq = 1;

        // rpt_info_type: DEV_STA, DEV_LOCAL, WORK, MAINTAIN, BASESTATION_INFO, VIO (as the ioBroker adapter subscribes)
        private static readonly int[] ReportSubscriptions = { 1, 3, 4, 6, 10, 8 };

        public static readonly IReadOnlyDictionary<int, string> DeviceStates = new Dictionary<int, string>
        {
            [0] = "not_active", [1] = "online", [2] = "offline", [8] = "disabled", [10] = "initializing", [11] = "ready", [12] = "unconnected",
            [13] = "working", [14] = "returning", [15] = "charging", [16] = "updating", [17] = "locked", [19] = "paused", [20] = "manual_mowing",
            [22] = "update_success", [23] = "update_failed", [31] = "drawing_job", [32] = "drawing_obstacle", [34] = "drawing_channel",
            [35] = "erasing", [36] = "editing_boundary", [37] = "location_error", [38] = "boundary_jump", [39] = "charging_paused"
        };

        private static ProtoWriter Envelope(int msgType, int seq, int userAccount)
        {
            return new ProtoWriter()
                .Varint(1, msgType)
                .Varint(2, DevMobileApp)
                .Varint(3, DevMainCtl)
                .Varint(4, MsgAttrReq)
                .Varint(5, seq & 0xFF)
                .Varint(6, 1)
                .Varint(7, userAccount);
        }

        /// <summary>
        /// LubaMsg { msgtype NAV, nav { todev_taskctrl (37) { type 1, action, result 0 } } }
        /// </summary>
        public static byte[] TaskControl(MowerAction action, int seq, int userAccount, long timestampMs)
        {
            var taskCtrl = new ProtoWriter().Varint(1, 1).Varint(2, (int)action).Varint(3, 0);
            var nav = new ProtoWriter().Message(37, taskCtrl);
            return Envelope(MsgCmdTypeNav, seq, userAccount)
                .Message(11, nav)
                .Varint(15, timestampMs)
                .ToArray();
        }

        /// <summary>
        /// LubaMsg { msgtype EMBED_SYS, sys { todev_report_cfg (38) { act START, timeout, period, no_change_period, count 0 (continuous), sub... } } }
        /// </summary>
        public static byte[] RequestReports(int seq, int userAccount, long timestampMs)
        {
            var cfg = new ProtoWriter().Varint(1, 0).Varint(2, 10_000).Varint(3, 3_000).Varint(4, 4_000).Varint(5, 0);
            foreach (var sub in ReportSubscriptions)
            {
                cfg.Varint(6, sub);
            }
            var sys = new ProtoWriter().Message(38, cfg);
            return Envelope(MsgCmdTypeEmbedSys, seq, userAccount)
                .Message(10, sys)
                .Varint(15, timestampMs)
                .ToArray();
        }

        /// <summary>
        /// Extracts battery, state, charging, progress, area and knife height from a LubaMsg. Unknown messages give an empty status.
        /// </summary>
        public static MowerStatus Decode(ReadOnlyMemory<byte> lubaMsg)
        {
            var msg = ProtoReader.Read(lubaMsg);
            var sys = ProtoReader.Sub(msg, 10);
            if (sys == null)
            {
                return new MowerStatus();
            }

            int? battery = null, state = null, charge = null, progress = null, area = null, remaining = null, knife = null;

            // toapp_batinfo (1) { batVal (1) }
            var bat = ProtoReader.Sub(sys, 1);
            if (bat != null)
            {
                battery = ProtoReader.Find(bat, 1)?.Int32;
            }

            // toapp_work_state (2) { deviceState (1), chargeState (2) }
            var work = ProtoReader.Sub(sys, 2);
            if (work != null)
            {
                state = ProtoReader.Find(work, 1)?.Int32 ?? state;
                charge = ProtoReader.Find(work, 2)?.Int32 ?? charge;
            }

            // toapp_mow_info (11) { deviceState (1), batVal (2), knifeHeight (3) }
            var mow = ProtoReader.Sub(sys, 11);
            if (mow != null)
            {
                state = ProtoReader.Find(mow, 1)?.Int32 ?? state;
                battery = ProtoReader.Find(mow, 2)?.Int32 ?? battery;
                knife = ProtoReader.Find(mow, 3)?.Int32 ?? knife;
            }

            // toapp_report_data (39) { dev (2) { sys_status (1), charge_state (2), battery_val (3) }, work (5) { progress (3), area (4), knife_height (20) } }
            var report = ProtoReader.Sub(sys, 39);
            if (report != null)
            {
                var dev = ProtoReader.Sub(report, 2);
                if (dev != null)
                {
                    state = ProtoReader.Find(dev, 1)?.Int32 ?? state;
                    charge = ProtoReader.Find(dev, 2)?.Int32 ?? charge;
                    battery = ProtoReader.Find(dev, 3)?.Int32 ?? battery;
                }
                var rptWork = ProtoReader.Sub(report, 5);
                if (rptWork != null)
                {
                    // as the Mammotion HA integration splits them: area = progress % << 16 | m², progress = remaining min << 16 | total min
                    var rawProgress = ProtoReader.Find(rptWork, 3);
                    if (rawProgress.HasValue)
                    {
                        remaining = (int)((rawProgress.Value.Varint >> 16) & 0xFFFF);
                    }
                    var rawArea = ProtoReader.Find(rptWork, 4);
                    if (rawArea.HasValue)
                    {
                        progress = (int)((rawArea.Value.Varint >> 16) & 0xFFFF);
                        area = (int)(rawArea.Value.Varint & 0xFFFF);
                    }
                    knife = ProtoReader.Find(rptWork, 20)?.Int32 ?? knife;
                }
            }

            return new MowerStatus { Battery = battery, DeviceState = state, ChargeState = charge, Progress = progress, Area = area, RemainingMinutes = remaining, KnifeHeight = knife };
        }
    }
}
