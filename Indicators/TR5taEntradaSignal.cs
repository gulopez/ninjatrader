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
	public class TR5taEntradaSignal : Indicator
	{
        private string _Path;
        private StreamWriter sw; // a variable for the StreamWriter that will be used 

        private bool _initialized = false;
       
        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;
        private ATR atr;

        private string FileNamePrefix = "TR5taEntrada";
        private string FileName = "";
        private string HeaderText = "Time, BarInProgress,Open,Close,High,Low,Guia,GuiaArriba,IsCambioDeGuia,ATR,IsHighGTGuia, IsLowCrossGuia,IsFacturoGuia, IsFacturoLTGuia,sma50, ema2, ema15,IsCloseGTSMA50, IsEma2Rising,IsEma15Rising, IsCloseGTema2,Is5taEntradaAlcista,Is5taEntradaBajista";

        private int BarsRequiredToTrade = 20;

        private TRProperties trProp = new TRProperties();

        //bool PreviousGuiaArriba = false;
        //bool IsCambioDeGuia = false;
        //bool IsHighGTGuia = false;
        //bool IsLowCrossGuia = false;
        //bool IsFacturoGuia = false;
        //bool IsFacturoLTGuia = false;
        //bool IsCloseGTSMA50Alcista = false;
        //bool IsCloseGTSMA50Bajista = false;
        //bool IsEma2Rising = false;
        //bool IsEma15Rising = false;
        //bool IsCloseGTEma2min = false;
        //bool IsEma2OverEma15 = false;

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
				Description									= @"5ta Entrada TR";
				Name										= "TR5taEntradaSignal";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
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
        private void Calculate5taEntradaSignals()
        {


             trProp.IsHighGTGuia = GuiaTR1[0] < High[0];
             trProp.IsLowCrossGuia = GuiaTR1[0] > Low[0];
             trProp.IsFacturoGuia = Close[0] > GuiaTR1[0];
             trProp.IsFacturoLTGuia = Close[0] < GuiaTR1[0];

             trProp.IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
             trProp.IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;
             trProp.IsEma2Rising = (ema2[0] > ema2[1]) ? true : false;
             trProp.IsEma15Rising = (ema15[0] > ema15[1]) ? true : false;
             trProp.IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;
            trProp.IsEma2OverEma15 = (ema2[0] > ema15[0]) ? true : false;

            trProp.IsCambioDeGuia = (trProp.PreviousGuiaArriba != trProp.GuiaArriba) ? true : false;
            

            //double changeofGuiaValue = Math.Abs(GuiaTR1[0] - GuiaTR1[1]);
            //if (changeofGuiaValue > atr[0] && GuiaArriba != PreviousGuiaArriba)
            //{
            //    //    IsCambioDeGuia = true;
            //}

            Is5taEntradaAlcista = false;
            Is5taEntradaBajista = false;


            if (!trProp.GuiaArriba && trProp.IsEma2Rising && trProp.IsEma15Rising && trProp.IsEma2OverEma15 & trProp.IsCambioDeGuia)
            {
                Is5taEntradaAlcista = true;

            }

            if (trProp.GuiaArriba && !trProp.IsEma2Rising && !trProp.IsEma15Rising && !trProp.IsEma2OverEma15 & trProp.IsCambioDeGuia)
            {
                Is5taEntradaBajista = true;

            }
        }

        private void Processar5taEetrada()
        {
            string entradamessage = "";
            if (Is5taEntradaAlcista || Is5taEntradaBajista)
            {
                if (Is5taEntradaAlcista)
                {
                    entradamessage = string.Format("{0}, 5ta Entrada Alcista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(true, entradamessage);

                    _ = Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, Lows[1][0], Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "5ta Entrada Alcista", 0, Convert.ToInt32(Lows[1][0]) - 10, ChartControl.Properties.ChartText);
                }
                else
                {
                    entradamessage = string.Format("{0}, 5ta Entrada Alcista", Time[0]);
                    TRUtilities.SaveToFile(_Path, IsWriteToFile, entradamessage);
                    PrintOutput(true, entradamessage);

                    _ = Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, Highs[1][0], Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, "5ta Entrada Bajista", 0, Convert.ToInt32(Highs[1][0]) + 10, ChartControl.Properties.ChartText);
                }

                string logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}", Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], trProp.GuiaArriba, trProp.IsCambioDeGuia, atr[0], trProp.IsHighGTGuia, trProp.IsLowCrossGuia, trProp.IsFacturoGuia, trProp.IsFacturoLTGuia, sma[0], ema2[0], ema15[0], trProp.IsCloseGTSMA50Alcista, trProp.IsEma2Rising, trProp.IsEma15Rising, trProp.IsCloseGTEma2min, Is5taEntradaAlcista, Is5taEntradaBajista);
                PrintOutput(IsVerboseLogs, entradamessage);
                TRUtilities.SaveToFile(_Path, (IsWriteToFile && IsVerboseLogs), logEntry);
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

            
            trProp.PreviousGuiaArriba = trProp.GuiaArriba;

            //Calculate Guia
            if ( GuiaTR1[0] > Closes[1][0] )
            {
                trProp.GuiaArriba = true;
            }
            else if(GuiaTR1[0] < Closes[1][0])
            {
                trProp.GuiaArriba = false;
            }

            Calculate5taEntradaSignals();

            if (BarsInProgress == 0)
            {
                Processar5taEetrada();
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

        [Browsable(false)]
        [XmlIgnore()]
        [NinjaScriptProperty]       
        public bool Is5taEntradaAlcista
        { get; set; }

        [Browsable(false)]
        [XmlIgnore()]
        [NinjaScriptProperty]        
        public bool Is5taEntradaBajista
        { get; set; }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TR5taEntradaSignal[] cacheTR5taEntradaSignal;
		public TR5taEntradaSignal TR5taEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			return TR5taEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, is5taEntradaAlcista, is5taEntradaBajista);
		}

		public TR5taEntradaSignal TR5taEntradaSignal(ISeries<double> input, bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			if (cacheTR5taEntradaSignal != null)
				for (int idx = 0; idx < cacheTR5taEntradaSignal.Length; idx++)
					if (cacheTR5taEntradaSignal[idx] != null && cacheTR5taEntradaSignal[idx].IsWriteToFile == isWriteToFile && cacheTR5taEntradaSignal[idx].IsVerboseLogs == isVerboseLogs && cacheTR5taEntradaSignal[idx].IsPrintOutput == isPrintOutput && cacheTR5taEntradaSignal[idx].Is5taEntradaAlcista == is5taEntradaAlcista && cacheTR5taEntradaSignal[idx].Is5taEntradaBajista == is5taEntradaBajista && cacheTR5taEntradaSignal[idx].EqualsInput(input))
						return cacheTR5taEntradaSignal[idx];
			return CacheIndicator<TR5taEntradaSignal>(new TR5taEntradaSignal(){ IsWriteToFile = isWriteToFile, IsVerboseLogs = isVerboseLogs, IsPrintOutput = isPrintOutput, Is5taEntradaAlcista = is5taEntradaAlcista, Is5taEntradaBajista = is5taEntradaBajista }, input, ref cacheTR5taEntradaSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TR5taEntradaSignal TR5taEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			return indicator.TR5taEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, is5taEntradaAlcista, is5taEntradaBajista);
		}

		public Indicators.TR5taEntradaSignal TR5taEntradaSignal(ISeries<double> input , bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			return indicator.TR5taEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput, is5taEntradaAlcista, is5taEntradaBajista);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TR5taEntradaSignal TR5taEntradaSignal(bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			return indicator.TR5taEntradaSignal(Input, isWriteToFile, isVerboseLogs, isPrintOutput, is5taEntradaAlcista, is5taEntradaBajista);
		}

		public Indicators.TR5taEntradaSignal TR5taEntradaSignal(ISeries<double> input , bool isWriteToFile, bool isVerboseLogs, bool isPrintOutput, bool is5taEntradaAlcista, bool is5taEntradaBajista)
		{
			return indicator.TR5taEntradaSignal(input, isWriteToFile, isVerboseLogs, isPrintOutput, is5taEntradaAlcista, is5taEntradaBajista);
		}
	}
}

#endregion
