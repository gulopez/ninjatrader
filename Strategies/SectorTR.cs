#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Utilities;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class TRSector : Strategy
    {

        private bool _initialized = false;

        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;

        private TRProperties trProp = new TRProperties();

        private double StopPrice;
        private double TriggerPrice;
        private int TriggerState;


        //private double _StopPrice;
        //private double _TriggerPrice;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"TR Sector Strategy";
                Name = "TRSector";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;

                BreakEvenTrigger = 5;
                InitialStopDistance = -10;
                StopPrice = 0;
                TriggerPrice = 0;
                TriggerState = 0;


            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Minute, 5);
                AddDataSeries(Data.BarsPeriodType.Minute, 15);
            }
            else if (State == State.DataLoaded)
            {
                DireccionTR2Min = DireccionTR(Closes[0], 10, 20, 6, 35, 50, 70);
                sma = SMA(BarsArray[0], 50);
                // atr = ATR(BarsArray[0], 5);

                ema2 = EMA(BarsArray[0], 20);
                ema15 = EMA(BarsArray[2], 20);
                GuiaTR1 = GuiaTR(Closes[1], 5, 2.1);
            }
        }

        protected override void OnBarUpdate()
        {
            //Make sure Daily bars are greater than BarsRequiredToTrade
            if (BarsInProgress == 2 && CurrentBar > BarsRequiredToTrade)
                _initialized = true;

            if (!_initialized)
                return;


            CalculateSectorSignals();

            if (BarsInProgress == 0)
            {
                ProcessSectorSignals();

                BreakEvenExtiStrategy();

            }

        }


        private void CalculateSectorSignals()
        {
            // Calculate Guia
            if (!trProp.GuiaArriba && GuiaTR1[0] > High[0] && GuiaTR1[0] > GuiaTR1[1])
            {
                trProp.GuiaArriba = true;
            }
            else
            {
                if (trProp.GuiaArriba && GuiaTR1[0] < Low[0] && GuiaTR1[0] < GuiaTR1[1])
                    trProp.GuiaArriba = false;
            }

            trProp.IsHighGTGuia = GuiaTR1[0] < High[0];
            trProp.IsLowCrossGuia = GuiaTR1[0] > Low[0];
            trProp.IsFacturoGuia = Close[0] > GuiaTR1[0];
            trProp.IsFacturoLTGuia = Close[0] < GuiaTR1[0];

            trProp.IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
            trProp.IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;

            trProp.IsEma2Rising = (ema2[0] > ema2[1]) ? true : false;
            trProp.IsEma15Rising = (ema15[0] > ema15[1]) ? true : false;
            trProp.IsEma2OverEma15 = (ema2[0] > ema15[0]) ? true : false;

            trProp.IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;

            trProp.IsSectorAlcista = false;
            trProp.IsSectorBajista = false;

            if (!trProp.GuiaArriba && !trProp.IsEma2OverEma15 && trProp.IsEma2Rising && !trProp.IsEma15Rising && trProp.IsCloseGTSMA50Alcista)
            {
                trProp.IsSectorAlcista = true;
            }

            if (trProp.GuiaArriba && trProp.IsEma2OverEma15 && !trProp.IsEma2Rising && trProp.IsEma15Rising && trProp.IsCloseGTSMA50Bajista)
            {
                trProp.IsSectorBajista = true;
            }
        }

        private void ProcessSectorSignals()
        {
            if (trProp.IsSectorAlcista == true || trProp.IsSectorBajista == true)
            {
                string entradamessage = "";
                if (trProp.IsSectorAlcista == true)
                {
                    entradamessage = string.Format("{0}, Sector Alcista", Time[0]);
                    //  TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(true, entradamessage);

                    Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, Low[0] - 1, Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "Sector Alcista", 0, Convert.ToInt32(Low[0]) - 10, ChartControl.Properties.ChartText);

                    StopPrice = Low[0];
                    TriggerPrice = ema15[0];

                    if (Position.MarketPosition == MarketPosition.Flat)
                    {
                        TriggerState = 1;
                        PrintOutput("Entering Long");
                        EnterLong(Convert.ToInt32(DefaultQuantity), @"entry");
                    }
                }
                else
                {
                    //entradamessage = string.Format("{0}, Sector Bajista", Time[0]);
                    //  TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    //PrintOutput(true, entradamessage);

                    //Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, High[0] + 1, Brushes.CornflowerBlue);
                    //Draw.Text(this, "tag1" + CurrentBar, "Sector Bajista", 0, Convert.ToInt32(High[0]) + 10, ChartControl.Properties.ChartText);

                    //_StopPrice = High[0] + 1;
                    //_TriggerPrice = ema15[0];

                }

                //string logEntry = string.Format(BodyText, Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], trProp.GuiaArriba, trProp.IsHighGTGuia, trProp.IsLowCrossGuia, trProp.IsFacturoGuia, trProp.IsFacturoLTGuia, sma[0], ema2[0], ema15[0], trProp.IsCloseGTSMA50Alcista, trProp.IsEma2Rising, trProp.IsEma15Rising, trProp.IsCloseGTEma2min, trProp.IsSectorAlcista, trProp.IsSectorBajista);
                // TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
            }
        }

        private void PrintOutput(string text)
        {

            Print(string.Format("{0},{1},{2},{3}", Time[0], text, TriggerPrice, StopPrice));

        }
        private void BreakEvenExtiStrategy()
        {


            // Set 1
            if ((TriggerState >= 2)
                 && (Position.MarketPosition == MarketPosition.Flat))
            {
                TriggerState = 0;
            }

            // Set 3
            if ((TriggerState == 1)
                 && (Position.MarketPosition == MarketPosition.Long))
            {
                TriggerState = 2;

                // StopPrice = Low[0] - 1;

                //PrintOutput("Long Updating Stop and Target");
            }

            // Set 4
            if ((TriggerState == 2)
                 && (Close[0] >= TriggerPrice))
            {
                TriggerState = 3;

                StopPrice = Low[0];

                // PrintOutput("Resetting Stop");
                Draw.Diamond(this, @"BreakEvenBuilderExample Diamond_1", true, 0, (High[0] + (2 * TickSize)), Brushes.DarkCyan);
            }

            // Set 5
            if (TriggerState >= 2)
            {
                if (TriggerState == 3)
                {
                    StopPrice = Low[0];
                }

                //  PrintOutput("Updating the Stop Exit");
                ExitLongStopMarket(Convert.ToInt32(DefaultQuantity), StopPrice, @"exit", @"entry");
            }

        }
        private void PrintOutput(bool Verbose, string text)
        {
            if (Verbose)
            {
                Print(text);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BreakEvenTrigger", Description = "Number of ticks above entry the breakeven movement trigger is set", Order = 1, GroupName = "Parameters")]
        public int BreakEvenTrigger
        { get; set; }

        [NinjaScriptProperty]
        [Range(-999, int.MaxValue)]
        [Display(Name = "InitialStopDistance", Description = "(use a negative) Number of ticks from entry the stop will initially be placed below", Order = 2, GroupName = "Parameters")]
        public int InitialStopDistance
        { get; set; }
        #endregion
    }
}
