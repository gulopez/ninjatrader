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
using System.IO;
using NinjaTrader.NinjaScript.Utilities;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TRTardeEntradaSignal : Indicator
	{
        private string _Path;
   
        private bool _initialized = false;
       
        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;
        private ATR atr;

        private string FileNamePrefix = "Tarde";
        private string FileName = "";
        private string HeaderText = "Time[0], BarsInProgress, Steps, Step Details, IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista";
        private string BodyText = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}";
        private int BarsRequiredToTrade = 20;     

        private TRProperties trProp = new TRProperties();        
      
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
				Description									= @"Entrada TR Tarde";
				Name										= "TRTardeEntradaSignal";
				Calculate									= Calculate.OnBarClose;
				IsOverlay = true ;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                IsWriteToFile = false;
                IsVerboseLogs = false;
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
                atr = ATR(BarsArray[0], 5);

                ema2 = EMA(BarsArray[0], 20);
                ema15 = EMA(BarsArray[2], 20);

                GuiaTR1 = GuiaTR(Closes[1], 5, 2.1);
            }
            else if (State == State.Terminated)
            {
               
            }
        }
        private void CalculateTardeSignals()
        {

            trProp.PreviousGuiaArriba = trProp.GuiaArriba;

            //Calculate Guia
            if (GuiaTR1[0] > Closes[1][0])
            {
                trProp.GuiaArriba = true;
            }
            else if (GuiaTR1[0] < Closes[1][0])
            {
                trProp.GuiaArriba = false;
            }

            trProp.IsCambioDeGuia = (trProp.PreviousGuiaArriba != trProp.GuiaArriba) ? true : false;

            trProp.IsHighGTGuia = GuiaTR1[0] < High[0];
            trProp.IsLowCrossGuia = GuiaTR1[0] > Low[0];
            trProp.IsFacturoGuia = Close[0] > GuiaTR1[0];
            trProp.IsFacturoLTGuia = Close[0] < GuiaTR1[0];

            trProp.IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
            trProp.IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;

            double avg2min = (ema2[0] + ema2[1] + ema2[2]) / 3;
            double avg15min = (ema15[0] + ema15[1] + ema15[2]) / 3;

            trProp.IsEma2Rising = (ema2[0] >= avg2min) ? true : false;
            trProp.IsEma15Rising = (ema15[0] >= avg15min) ? true : false;
            trProp.IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;

            trProp.IsEma2OverEma15 = (ema2[0] > ema15[0]) ? true : false;

            trProp.IsTardeEntradaAlcista = false;
            trProp.IsTardeEntradaBajista = false;

            string logEntry = "";

            if (BarsInProgress == 0)
            {
                trProp.IsEMA2CrossAbove = CrossAbove(ema2, ema15, 1);

                if (trProp.IsEMA2CrossAbove)
                {
                    trProp.Steps = 1;
                    trProp.IsTardeEntradaBajista = false;

                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsEma2CrossAbove", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                if (trProp.Steps == 1 && trProp.IsEma2Rising && trProp.IsEma15Rising)
                {
                    trProp.Steps = 2;

                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "Both direction rising", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;

                }

                if (trProp.Steps == 2 && !trProp.GuiaArriba)
                {
                    trProp.Steps = 3;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "Guia a Favor", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                trProp.IsCruzoPorDebajo = Closes[0][0] < ema2[0] ? true : false;

                if (trProp.Steps == 3 && trProp.IsCruzoPorDebajo)
                {
                    trProp.Steps = 4;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsCruzoPorDebajo", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                trProp.IsCruzoPorArriba = Closes[0][0] > ema2[0] ? true : false;

                if (trProp.Steps == 4 && trProp.IsCruzoPorArriba)
                {
                    trProp.Steps = 5;
                    trProp.IsTardeEntradaAlcista = true;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsCruzoPorArriba", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);

                    string entradamessage = string.Format("{0}, Tarde Alcista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(true, entradamessage);
                }

                /////////////////////////            

                trProp.IsEMA2CrossBelow = CrossBelow(ema2, ema15, 1);
                if (trProp.IsEMA2CrossBelow)
                {
                    trProp.Steps = -1;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsEma2CrossBellow", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                if (trProp.Steps == -1 && !trProp.IsEma2Rising && !trProp.IsEma15Rising)
                {
                    trProp.Steps = -2;

                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "Both direction falling", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;

                }

                if (trProp.Steps == -2 && trProp.GuiaArriba)
                {
                    trProp.Steps = -3;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "Guia a Favor", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                trProp.IsCruzoPorArriba = Closes[0][0] > ema2[0] ? true : false;


                if (trProp.Steps == -3 && trProp.IsCruzoPorArriba)
                {
                    trProp.Steps = -4;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsCruzoPorArriba", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
                    return;
                }

                trProp.IsCruzoPorDebajo = Closes[0][0] < ema2[0] ? true : false;

                if (trProp.Steps == -4 && trProp.IsCruzoPorDebajo)
                {
                    trProp.Steps = -5;
                    trProp.IsTardeEntradaBajista = true;
                    logEntry = string.Format(BodyText, Time[0], BarsInProgress, trProp.Steps, "IsCruzoPorDebajo", trProp.IsEMA2CrossAbove, Closes[0][0], ema2[0], trProp.IsEma2Rising, trProp.IsEma15Rising, !trProp.GuiaArriba, trProp.IsCruzoPorDebajo, trProp.IsCruzoPorArriba, trProp.IsTardeEntradaAlcista, trProp.IsTardeEntradaBajista);
                    TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);

                    string entradamessage = string.Format("{0}, Tarde Bajista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(true, entradamessage);
                }


            }

        }

        private void ProcessTardeSignals()
        {

            if (trProp.IsTardeEntradaAlcista || trProp.IsTardeEntradaBajista)
            {
                if (trProp.IsTardeEntradaAlcista)
                {
                    _ = Draw.Dot(this, @"TardeTR" + CurrentBar, true, 0, Lows[1][0], Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "Tarde Alcista", 0, Convert.ToInt32(Lows[1][0]) - 10, ChartControl.Properties.ChartText);
                }
                else
                {
                    _ = Draw.Dot(this, @"TardeTR" + CurrentBar, true, 0, Highs[1][0], Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "Tarde Bajista", 0, Convert.ToInt32(Highs[1][0]) + 10, ChartControl.Properties.ChartText);
                }
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
                    TRUtilities.SaveToFile(_Path, IsVerboseLogs, HeaderText);                   
                }
            }

            //Make sure Daily bars are greater than BarsRequiredToTrade
            if (BarsInProgress == 2 && CurrentBar > BarsRequiredToTrade)
                _initialized = true;

            if (!_initialized)
                return;


            CalculateTardeSignals();

            if (BarsInProgress == 0)
            {
                ProcessTardeSignals();
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
		private TRTardeEntradaSignal[] cacheTRTardeEntradaSignal;
		public TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			return TRTardeEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, isTardeEntradaAlcista, isTardeEntradaBajista);
		}

		public TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input, bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			if (cacheTRTardeEntradaSignal != null)
				for (int idx = 0; idx < cacheTRTardeEntradaSignal.Length; idx++)
					if (cacheTRTardeEntradaSignal[idx] != null && cacheTRTardeEntradaSignal[idx].IsWriteToFile == isWriteToFile && cacheTRTardeEntradaSignal[idx].IsVerboseLogs == isVerboseLogs && cacheTRTardeEntradaSignal[idx].IsPrintOutput == isPrintOutput && cacheTRTardeEntradaSignal[idx].EqualsInput(input))
						return cacheTRTardeEntradaSignal[idx];
			return CacheIndicator<TRTardeEntradaSignal>(new TRTardeEntradaSignal(){ IsWriteToFile = isWriteToFile, IsVerboseLogs = isVerboseLogs, IsPrintOutput = isPrintOutput }, input, ref cacheTRTardeEntradaSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			return indicator.TRTardeEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, isTardeEntradaAlcista, isTardeEntradaBajista);
		}

		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input , bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			return indicator.TRTardeEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput, isTardeEntradaAlcista, isTardeEntradaBajista);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			return indicator.TRTardeEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, isTardeEntradaAlcista, isTardeEntradaBajista);
		}

		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input , bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool isTardeEntradaAlcista, bool isTardeEntradaBajista)
		{
			return indicator.TRTardeEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput, isTardeEntradaAlcista, isTardeEntradaBajista);
		}
	}
}

#endregion
