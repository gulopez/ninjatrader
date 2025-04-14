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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TRTardeEntradaSignal : Indicator
	{
        private string path;
        private StreamWriter sw; // a variable for the StreamWriter that will be used 

        private bool _initialized = false;
        bool GuiaArriba = false;
        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;
        private ATR atr;

        private string FileNamePrefix = "Tarde";
        private string FileName = "";
        private string HeaderText = "Time[0], BarsInProgress, Step,IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista";

        private int BarsRequiredToTrade = 20;
        bool PreviousGuiaArriba = false;

        private bool IsCruzoPorDebajo = false;
        private bool IsCruzoPorArriba = false;

        private bool IsEMA2CrossAbove = false;
        private bool IsEMA2CrossBelow = false;

        private bool IsPrimeraCondicion = false;
        private bool IsSegundaCondicion = false;
        private bool IsTerceraCondicion = false;

        private void WriteToFile(string text)
        {
            sw = File.AppendText(path);  // Open the path for writing
            sw.WriteLine(text); // Append a new line to the file
            sw.Close(); // Close the file to allow future calls to access the file again.
        }

        private void SaveToFile(string logEntry)
        {
            if (IsWriteToFile)
            {
                WriteToFile(logEntry);
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
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                    sw = null;
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
                    path = NinjaTrader.Core.Globals.UserDataDir + FileName; // Define the Path to our test file
                    WriteToFile(HeaderText);
                }
            }



            //Make sure Daily bars are greater than BarsRequiredToTrade
            if (BarsInProgress == 2 && CurrentBar > BarsRequiredToTrade)
                _initialized = true;

            if (!_initialized)
                return;

            PreviousGuiaArriba = GuiaArriba;

            //Calculate Guia
            if (GuiaTR1[0] > Closes[1][0])
            {
                GuiaArriba = true;
            }
            else if (GuiaTR1[0] < Closes[1][0])
            {
                GuiaArriba = false;
            }

            bool IsCambioDeGuia = (PreviousGuiaArriba != GuiaArriba) ? true : false;
    

            bool IsHighGTGuia = GuiaTR1[0] < High[0];
            bool IsLowCrossGuia = GuiaTR1[0] > Low[0];
            bool IsFacturoGuia = Close[0] > GuiaTR1[0];
            bool IsFacturoLTGuia = Close[0] < GuiaTR1[0];

            bool IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
            bool IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;

            double avg2min = (ema2[0] + ema2[1] + ema2[2] ) / 3;
            double avg15min = (ema15[0] + ema15[1] + ema15[2]  ) / 3;

            bool IsEma2Rising = (ema2[0] >= avg2min) ? true : false;
            bool IsEma15Rising = (ema15[0] >= avg15min) ? true : false;
            bool IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;

            bool IsEma2OverEma15 = (ema2[0] > ema15[0]) ? true : false;


            bool IsTardeEntradaAlcista = false;
            bool IsTardeEntradaBajista = false;


            string logEntry = "";

            if (BarsInProgress == 0)
            {
                IsEMA2CrossAbove = CrossAbove(ema2, ema15, 1);
                if (IsEMA2CrossAbove)
                {
                    logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "IsEma2CrossAbove", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                    SaveToFile(logEntry);
                    return;
                }

                if (IsEMA2CrossAbove && IsEma2Rising && IsEma15Rising && !GuiaArriba && !IsPrimeraCondicion)
                {
                    IsPrimeraCondicion = true;
                   
                    logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "IsPrimeraCondicion", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                    SaveToFile(logEntry);
                    return;
                }

                if (IsPrimeraCondicion && !GuiaArriba)
                {
                    IsCruzoPorDebajo = Closes[0][0] < ema2[0] ? true : false;
                    
                    logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "Step 1: Primera Condicion + Guia a Favor", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                    SaveToFile(logEntry);

                    if (IsCruzoPorDebajo && !IsSegundaCondicion)
                    {
                        IsSegundaCondicion = true;                        
                       
                        logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "Step 2: Segunda Condicion", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                        SaveToFile(logEntry);

                        return;                   
                    }

                    IsCruzoPorArriba = Closes[0][0] > ema2[0] ? true : false;

                    if (IsSegundaCondicion && IsCruzoPorArriba)
                    {
                        logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "SIsTardeEntradaAlcista", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                        SaveToFile(logEntry);

                       
                        IsTardeEntradaAlcista = true;
                        IsPrimeraCondicion = false; ;
                        IsSegundaCondicion = false;
                    }                   
                }
                
                   
                IsEMA2CrossBelow = CrossBelow(ema2, ema15, 1);
                if (IsEMA2CrossBelow)
                {
                    IsPrimeraCondicion = false;
                    IsSegundaCondicion = false;
                    IsEMA2CrossAbove = false;
                    IsCruzoPorDebajo = false;
                    IsCruzoPorArriba = false;
                    IsTardeEntradaAlcista = false;
                    Print(string.Format("{0},{1}", Time[0], "IsEma2CrossBelow"));
                }

                               
                
                logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", Time[0], BarsInProgress, "Step 0", IsEMA2CrossAbove, Closes[0][0], ema2[0], IsEma2Rising, IsEma15Rising, !GuiaArriba, IsCruzoPorDebajo, IsCruzoPorArriba, IsTardeEntradaAlcista);
                Print(logEntry);

                if (IsWriteToFile)
                {
                    WriteToFile(logEntry);
                }


                if (IsTardeEntradaAlcista || IsTardeEntradaBajista)
                {
                    if (IsTardeEntradaAlcista)
                    {
                        _ = Draw.Dot(this, @"TardeTR" + CurrentBar, true, 0, Lows[1][0], Brushes.CornflowerBlue);
                        Draw.Text(this, "tag1" + CurrentBar, "Entrada Tarde Alcista", 0, Convert.ToInt32(Lows[1][0]) - 10, ChartControl.Properties.ChartText);
                    }
                    else
                    {
                        _ = Draw.Dot(this, @"TardeTR" + CurrentBar, true, 0, Highs[1][0], Brushes.CornflowerBlue);
                        Draw.Text(this, "tag1" + CurrentBar, "Entrada Tarde Bajista", 0, Convert.ToInt32(Highs[1][0]) + 10, ChartControl.Properties.ChartText);
                    }

                   //  logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}", Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], GuiaArriba, IsCambioDeGuia, atr[0], IsHighGTGuia, IsLowCrossGuia, IsFacturoGuia, IsFacturoLTGuia, sma[0], ema2[0], ema15[0], IsCloseGTSMA50Alcista, IsEma2Rising, IsEma15Rising, IsCloseGTEma2min, IsTardeEntradaAlcista, IsTardeEntradaBajista);
                   //// Print(logEntry);

                   // if (IsWriteToFile)
                   // {
                   //     WriteToFile(logEntry);
                   // }
                }
            }

        }
        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "IsWriteToFile", Description = "Save Entries to File", Order = 1, GroupName = "Parameters")]
        public bool IsWriteToFile
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
		public TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile)
		{
			return TRTardeEntradaSignal(Input, isWriteToFile);
		}

		public TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input, bool isWriteToFile)
		{
			if (cacheTRTardeEntradaSignal != null)
				for (int idx = 0; idx < cacheTRTardeEntradaSignal.Length; idx++)
					if (cacheTRTardeEntradaSignal[idx] != null && cacheTRTardeEntradaSignal[idx].IsWriteToFile == isWriteToFile && cacheTRTardeEntradaSignal[idx].EqualsInput(input))
						return cacheTRTardeEntradaSignal[idx];
			return CacheIndicator<TRTardeEntradaSignal>(new TRTardeEntradaSignal(){ IsWriteToFile = isWriteToFile }, input, ref cacheTRTardeEntradaSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile)
		{
			return indicator.TRTardeEntradaSignal(Input, isWriteToFile);
		}

		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input , bool isWriteToFile)
		{
			return indicator.TRTardeEntradaSignal(input, isWriteToFile);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(bool isWriteToFile)
		{
			return indicator.TRTardeEntradaSignal(Input, isWriteToFile);
		}

		public Indicators.TRTardeEntradaSignal TRTardeEntradaSignal(ISeries<double> input , bool isWriteToFile)
		{
			return indicator.TRTardeEntradaSignal(input, isWriteToFile);
		}
	}
}

#endregion
