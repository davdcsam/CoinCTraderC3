using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using SectionTime;
using Trade;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class CoinCTraderC3 : Robot
    {
        // -- Data Trader Parameters and Place Order Declaration -- // 

        [Parameter("Trade Type", Group = "Data Trader")]

        public TradeType TradeTypeOperation { get; set; }

        [Parameter("Lot Size", Group = "Data Trader", DefaultValue = 1.0, MinValue = 0.1)]
        public double LotSize { get; set; }

        [Parameter("Take Profit", Group = "Data Trader", DefaultValue = 100, MinValue = 1.0)]
        public double TakeProfit { get; set; }

        [Parameter("Stop Loss", Group = "Data Trader", DefaultValue = 50, MinValue = 0)]
        public double StopLoss { get; set; }

        [Parameter("Label to Identify", Group = "Data Trader", DefaultValue = "studying at uca sv is a disrepute")]
        public string LabelIdentifier { get; set; }

        protected PlaceOrders PlaceOrder { get; set; }

        // -- Section Time Parameters and Declaration -- //

        [Parameter("UserTimeOffset", Group = "Section Time")]
        public Switcher SwitcherUserTimeOffset { get; set; }

        [Parameter("Start Hour", Group = "Section Time", DefaultValue = 8, MaxValue = 23, MinValue = 0)]
        public int StartHour { get; set; }

        [Parameter("Start Min", Group = "Section Time", DefaultValue = 0, MaxValue = 59, MinValue = 0)]
        public int StartMin { get; set; }

        [Parameter("Start Sec", Group = "Section Time", DefaultValue = 0, MaxValue = 59, MinValue = 0)]
        public int StartSec { get; set; }

        [Parameter("End Hour", Group = "Section Time", DefaultValue = 13, MaxValue = 23, MinValue = 0)]
        public int EndHour { get; set; }

        [Parameter("End Min", Group = "Section Time", DefaultValue = 0, MaxValue = 59, MinValue = 0)]
        public int EndMin { get; set; }

        [Parameter("End Sec", Group = "Section Time", DefaultValue = 0, MaxValue = 59, MinValue = 0)]
        public int EndSec { get; set; }

        protected RangeTime RangeTimeOperative { get; private set; }

        // -- Removal Parameters and Declaration -- //

        [Parameter("Removal Out", Group = "Section Time")]
        public Switcher SwitcherRemoval { get; set; }

        protected Removal Removal { get; private set; }

        // -- Independet vars -- //

        private int lastDay = -1;

        // -- Methods to OnStart Event -- // 

        protected void InitializeInstances()
        {
            PlaceOrder = new PlaceOrders(this, LotSize, StopLoss, TakeProfit, LabelIdentifier);

            RangeTimeOperative = new RangeTime(this, StartHour, StartMin, StartSec, EndHour, EndMin, EndSec);
            if (SwitcherUserTimeOffset == Switcher.Activated)
            {
                Application.UserTimeOffsetChanged += RangeTimeOperative.AppUserTimeOffsetChanged;
                RangeTimeOperative.UpdateUserTimeOffset();
            }
            RangeTimeOperative.UpdateDatetimeInterval();

            if (SwitcherRemoval == Switcher.Activated) { Removal = new Removal(this, LabelIdentifier); }
        }

        protected bool VerifyInputs()
        {
            if (Symbol.TradingMode != SymbolTradingMode.FullAccess) { Print($"Symbol has mode {Symbol.TradingMode}, cBot require {SymbolTradingMode.FullAccess}"); return false; }

            if (LotSize < Symbol.VolumeInUnitsMin || LotSize > Symbol.VolumeInUnitsMax || Math.Abs(LotSize / Symbol.VolumeInUnitsStep - Math.Round(LotSize / Symbol.VolumeInUnitsStep)) > 0.00001) { Print($"LotSize {LotSize} have to between {Symbol.VolumeInUnitsMin} and {Symbol.VolumeInUnitsMax} in step of {Symbol.VolumeInUnitsStep}"); return false; }

            if (StopLoss < Symbol.MinStopLossDistance) { Print($"StopLoss {StopLoss} have to min {Symbol.MinStopLossDistance}"); return false; }

            if (TakeProfit < Symbol.MinTakeProfitDistance) { Print($"TakeProfit {TakeProfit} have to min {Symbol.MinTakeProfitDistance}"); return false; }

            if (!RangeTimeOperative.VerifyFormattingTime()) { Print($"Formatting datetime is incorrect"); return false; }

            if (RangeTimeOperative.StartDateTime >= RangeTimeOperative.EndDateTime) { Print($"StartDateTime {RangeTimeOperative.StartDateTime} is over EndDateTime then {RangeTimeOperative.EndDateTime}"); return false; }

            return true;
        }

        protected override void OnStart()
        {
            InitializeInstances();

            if (!VerifyInputs()) { Stop(); }

            base.OnStart();
        }

        // -- Methods to OnTick -- //

        protected void RunRemoval()
        {
            if (SwitcherRemoval == Switcher.Deactivated)
            { return; }

            if (!RangeTimeOperative.VerifyInsideInterval()) { Removal.ByLabelIdentifier(null, null); }
        }
        protected void Deploy()
        {
            if (RangeTimeOperative.VerifyInsideInterval() && Bars.Last(1).OpenTime.Day != lastDay)
            {
                ExecuteMarketOrder(TradeTypeOperation, Symbol.Name, LotSize, LabelIdentifier, StopLoss, TakeProfit);

                lastDay = Bars.Last(1).OpenTime.Day;
            }
        }

        protected override void OnTick()
        {
            RangeTimeOperative.UpdateDatetimeInterval();

            RunRemoval();

            Deploy();

            base.OnTick();
        }

        protected override void OnStop()
        {
            if (!IsBacktesting) return;
            Removal.ByLabelIdentifier();

            base.OnStop();
        }
    }
}