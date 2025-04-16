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
using NinjaTrader.NinjaScript.DrawingTools;

using NinjaTrader.NinjaScript.Utilities;
using NinjaTrader.NinjaScript.Indicators;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
    public class TRSectorEntradaSignal : Indicator
    {
        private string _Path;


        private bool _initialized = false;
        bool GuiaArriba = false;
        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;
        private string FileNamePrefix = "Sector";
        private string FileName = "";
        private string HeaderText = "Time, BarInProgress,Open,Close,High,Low,Guia,GuiaArriba,IsHighGTGuia, IsLowCrossGuia,IsFacturoGuia, IsFacturoLTGuia,sma50, ema2, ema15,IsCloseGTSMA50, IsEma2Rising,IsEma15Rising, IsCloseGTema2,IsSectorAlcista,IsSectorBajista";
        private string BodyText = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}";

        private int BarsRequiredToTrade = 20;
        private bool IsSectorAlcista = false;
        private bool IsSectorBajista = false;

        private bool IsHighGTGuia = false;
        private bool IsLowCrossGuia = false;
        private bool IsFacturoGuia = false;
        private bool IsFacturoLTGuia = false;
        private bool IsCloseGTSMA50Alcista = false;
        private bool IsCloseGTSMA50Bajista = false;

        private bool IsEma2Rising = false;
        private bool IsEma15Rising = false;

        private bool IsCloseGTEma2min = false;

        private void PrintOutput(bool Verbose, string text)
        {
            if (Verbose)
            {
                Print(text);
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Marca Entrada Sector";
                Name = "TRSectorEntradaSignal";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;
                IsWriteToFile = false;
                IsVerboseLogs = false;
                IsPrintOutput = true;

                //AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Dot, "IsSectorAlcista");

                //AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Dot, "IsSectorAlcista");
                //AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Dot, "StopAlcista");
                //AddPlot(new Stroke(Brushes.Fuchsia, 2), PlotStyle.Dot, "IsSectorBajista");
                //AddPlot(new Stroke(Brushes.Fuchsia, 2), PlotStyle.Dot, "stopBajista");

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
            else if (State == State.Terminated)
            {

            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress == 0 && CurrentBar == 1)
            {
                if (IsWriteToFile)
                {
                    FileName = FileNamePrefix + DateTime.Now.Ticks.ToString() + ".csv";
                    _Path = NinjaTrader.Core.Globals.UserDataDir + FileName; // Define the Path to our test file
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, HeaderText);
                }
            }

            //Make sure Daily bars are greater than BarsRequiredToTrade
            if (BarsInProgress == 2 && CurrentBar > BarsRequiredToTrade)
                _initialized = true;

            if (!_initialized)
                return;

            CalculateSectorSignals();

            if (BarsInProgress == 0)
            {
                ProcessSectorSignals();
            }
        }

        private void CalculateSectorSignals()
        {
            // Calculate Guia
            if (!GuiaArriba && GuiaTR1[0] > High[0] && GuiaTR1[0] > GuiaTR1[1])
            {
                GuiaArriba = true;
            }
            else
            {
                if (GuiaArriba && GuiaTR1[0] < Low[0] && GuiaTR1[0] < GuiaTR1[1])
                    GuiaArriba = false;
            }

            IsHighGTGuia = GuiaTR1[0] < High[0];
            IsLowCrossGuia = GuiaTR1[0] > Low[0];
            IsFacturoGuia = Close[0] > GuiaTR1[0];
            IsFacturoLTGuia = Close[0] < GuiaTR1[0];

            bool IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
            bool IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;

            bool IsEma2Rising = (ema2[0] > ema2[1]) ? true : false;
            bool IsEma15Rising = (ema15[0] > ema15[1]) ? true : false;

            bool IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;

            IsSectorAlcista = false;
            IsSectorBajista = false;



            if (!GuiaArriba && IsEma2Rising && !IsEma15Rising && IsCloseGTSMA50Alcista)
            {
                IsSectorAlcista = true;
            }

            if (GuiaArriba && !IsEma2Rising && IsEma15Rising && IsCloseGTSMA50Bajista)
            {
                IsSectorBajista = true;
            }

        }

        private void ProcessSectorSignals()
        {
            if (IsSectorAlcista == true || IsSectorBajista == true)
            {
                string entradamessage = "";
                if (IsSectorAlcista == true)
                {
                    entradamessage = string.Format("{0}, Sector Alcista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(IsPrintOutput, entradamessage);

                    Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, Low[0] - 1, Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "Sector Alcista", 0, Convert.ToInt32(Low[0]) - 10, ChartControl.Properties.ChartText);

                }
                else
                {
                    entradamessage = string.Format("{0}, Sector Bajista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(IsPrintOutput, entradamessage);

                    Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, High[0] + 1, Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "Sector Bajista", 0, Convert.ToInt32(High[0]) + 10, ChartControl.Properties.ChartText);

                }

                string logEntry = string.Format(BodyText, Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], GuiaArriba, IsHighGTGuia, IsLowCrossGuia, IsFacturoGuia, IsFacturoLTGuia, sma[0], ema2[0], ema15[0], IsCloseGTSMA50Alcista, IsEma2Rising, IsEma15Rising, IsCloseGTEma2min, IsSectorAlcista, IsSectorBajista);
                TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
            }
        }


        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "IsWriteToFile", Description = "Save Entries to File", Order = 1, GroupName = "Parameters")]
        public bool IsWriteToFile
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "IsVerboseLogs", Description = "Verbose log details", Order = 2, GroupName = "Parameters")]
        public bool IsVerboseLogs
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "IsPrintOutput", Description = "Print Entries in Output Window", Order = 3, GroupName = "Parameters")]
        public bool IsPrintOutput
        { get; set; }



        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private TRSectorEntradaSignal[] cacheTRSectorEntradaSignal;
        public TRSectorEntradaSignal TRSectorEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            return TRSectorEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput);
        }

        public TRSectorEntradaSignal TRSectorEntradaSignal(ISeries<double> input, bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            if (cacheTRSectorEntradaSignal != null)
                for (int idx = 0; idx < cacheTRSectorEntradaSignal.Length; idx++)
                    if (cacheTRSectorEntradaSignal[idx] != null && cacheTRSectorEntradaSignal[idx].IsWriteToFile == isWriteToFile && cacheTRSectorEntradaSignal[idx].IsVerboseLogs == isVerboseLogs && cacheTRSectorEntradaSignal[idx].IsPrintOutput == isPrintOutput && cacheTRSectorEntradaSignal[idx].EqualsInput(input))
                        return cacheTRSectorEntradaSignal[idx];
            return CacheIndicator<TRSectorEntradaSignal>(new TRSectorEntradaSignal() { IsWriteToFile = isWriteToFile, IsVerboseLogs = isVerboseLogs, IsPrintOutput = isPrintOutput }, input, ref cacheTRSectorEntradaSignal);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.TRSectorEntradaSignal TRSectorEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            return indicator.TRSectorEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput);
        }

        public Indicators.TRSectorEntradaSignal TRSectorEntradaSignal(ISeries<double> input, bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            return indicator.TRSectorEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.TRSectorEntradaSignal TRSectorEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            return indicator.TRSectorEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput);
        }

        public Indicators.TRSectorEntradaSignal TRSectorEntradaSignal(ISeries<double> input, bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput)
        {
            return indicator.TRSectorEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput);
        }
    }
}

#endregion
