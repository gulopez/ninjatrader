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
using System.IO;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SectorTR : Strategy
	{
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
        private string docPath = "";
        //private ATR atr;
        private string path;
        private StreamWriter sw; // a variable for the StreamWriter that will be used 


        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Testing Entrada Sector";
				Name										= "SectorTR";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
                IsWriteToFile = true;
                
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration	= true;

                FileName = FileNamePrefix + DateTime.Now.ToString() + ".csv";
                path = NinjaTrader.Core.Globals.UserDataDir + FileName; // Define the Path to our test file
            }
			else if (State == State.Configure)
			{
                AddDataSeries(Data.BarsPeriodType.Minute, 5);
                AddDataSeries(Data.BarsPeriodType.Minute, 15);

                
                if (IsWriteToFile)
                {
                    //sw = File.AppendText(path);  // Open the path for writing
                    //sw.WriteLine(HeaderText + Environment.NewLine); // Append a new line to the file
                    //sw.Close(); // Close the file to allow future calls to access the file again.
                }
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
                //if (sw != null)
                //{
                //    sw.Close();
                //    sw.Dispose();
                //    sw = null;
                //}
            }
        }

		protected override void OnBarUpdate()
		{
            //Make sure Daily bars are greater than BarsRequiredToTrade
            if (BarsInProgress == 2 && CurrentBar > BarsRequiredToTrade)
                _initialized = true;

            if (!_initialized)
                return;

            //Calculate Guia
            if (!GuiaArriba && GuiaTR1[0] > High[0] && GuiaTR1[0] > GuiaTR1[1])
            {
                GuiaArriba = true;
            }
            else
            {
                if (GuiaArriba && GuiaTR1[0] < Low[0] && GuiaTR1[0] < GuiaTR1[1])
                    GuiaArriba = false;
            }

            bool IsHighGTGuia = GuiaTR1[0] < High[0];
			bool IsLowCrossGuia = GuiaTR1[0] > Low[0];
			bool IsFacturoGuia = Close[0] > GuiaTR1[0];
			bool IsFacturoLTGuia = Close[0] < GuiaTR1[0];

			bool IsCloseGTSMA50Alcista = (Close[0] > sma[0] && Open[0] < sma[0]) ? true : false;
            bool IsCloseGTSMA50Bajista = (Close[0] < sma[0] && Open[0] > sma[0]) ? true : false;

            bool IsEma2Rising = (ema2[0] > ema2[1]) ? true : false;
            bool IsEma15Rising = (ema15[0] > ema15[1]) ? true : false;

			bool IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;

			bool IsSectorAlcista = false;
            bool IsSectorBajista = false;


            if (!GuiaArriba && IsEma2Rising && !IsEma15Rising && IsCloseGTSMA50Alcista)
			{
				IsSectorAlcista = true;

            }

            if (GuiaArriba && !IsEma2Rising && IsEma15Rising && IsCloseGTSMA50Bajista)
            {
                IsSectorBajista = true;

            }

            if(BarsInProgress == 0)
            {

                if (IsSectorAlcista || IsSectorBajista)
                {
                    _ = Draw.Dot(this, @"SectorTR"+CurrentBar, true, 0, High[0] + 5, Brushes.CornflowerBlue);

                    string logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], GuiaArriba, IsHighGTGuia, IsLowCrossGuia, IsFacturoGuia, IsFacturoLTGuia, sma[0], ema2[0], ema15[0], IsCloseGTSMA50Alcista, IsEma2Rising, IsEma15Rising, IsCloseGTEma2min, IsSectorAlcista, IsSectorBajista);
                    Print(logEntry);

                    if (IsWriteToFile)
                    {

                       
                        // Append new lines of text to the file                        //sw = File.AppendText(path);  // Open the path for writing
                        //sw.WriteLine(logEntry); // Append a new line to the file
                        //sw.Close(); // Close the file to allow future calls to access the file again.


                        // Time, BarInProgress,Open,Close,High,Low,Guia,GuiaArriba,IsHighGTGuia, IsLowCrossGuia,IsFacturoGuia, IsFacturoLTGuia,sma50, ema2, ema15,IsCloseGTSMA50, IsEma2Rising,IsEma15Rising, IsCloseGTema2,IsSectorAlcista,IsSectorBajista
                       

                    }
                }
            }
        }


        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "IsWriteToFile", Description = "Log entries to a file", Order = 1, GroupName = "Parameters")]
        public bool IsWriteToFile
        { get; set; }

       
        #endregion
    }
}
