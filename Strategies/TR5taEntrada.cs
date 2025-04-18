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
using NinjaTrader.Code;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class TR5taEntrada : Strategy
    {
        private bool _initialized = false;
        private string _path;

        private string HeaderText = "Time,Message,TriggerPrice,StopPrice";
        // private string HeaderText = "Time, BarInProgress,Open,Close,High,Low,Guia,GuiaArriba,IsHighGTGuia, IsLowCrossGuia,IsFacturoGuia, IsFacturoLTGuia,sma50, ema2, ema15,IsCloseGTSMA50, IsEma2Rising,IsEma15Rising, IsCloseGTema2,Is5taEntradaAlcista,Is5taEntradaBajista";

        private string BodyText = "{0},{1},{2},{3}";

        private string FileNamePrefix = "TR-5taEntrada-Strat-";
        private string FileName = "";

        private DireccionTR DireccionTR2Min;
        private DireccionTR DireccionTR12Min;
        private GuiaTR GuiaTR1;
        private SMA sma;
        private EMA ema2;
        private EMA ema15;
        private ATR atr;

        private TRProperties trProp = new TRProperties();

        private double StopPrice;
        private double TriggerPrice;
        private int TriggerState;

        private BreakEvenExitStrategy _breakEvenExit;

        private bool GuiaArriba = false;
        private bool PreviousGuiaArriba = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "TR5taEntrada";
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

                StopPrice = 0;
                TriggerPrice = 0;
                TriggerState = 0;
                IsPrintOutput = false;
                IsVerbose = false;
                IsWriteToFile = false;


                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
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
                atr = ATR(BarsArray[1], 5);

                ema2 = EMA(BarsArray[0], 20);
                ema15 = EMA(BarsArray[2], 20);

                GuiaTR1 = GuiaTR(Closes[1], 5, 2.1);

                _breakEvenExit = new BreakEvenExitStrategy(this);

                if (IsVerbose)
                {
                    _breakEvenExit.IsPrintOutput = IsPrintOutput;
                }

                _breakEvenExit.IsWriteToFile = IsWriteToFile;
            }
        }

        private void CalculateSectorSignals()
        {
            //Used to compare a change of Guia
            trProp.PreviousGuiaArriba = trProp.GuiaArriba;

            //Calculate Guia
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

            trProp.IsEma2Rising = (ema2[0] > ema2[1] && ema2[1] > ema2[2]) ? true : false;
            trProp.IsEma15Rising = (ema15[0] > ema15[1] && ema15[1] > ema15[2]) ? true : false;

            trProp.IsCloseGTEma2min = (Close[0] > ema2[0]) ? true : false;
            trProp.IsEma2OverEma15 = (ema2[0] > ema15[0]) ? true : false;


            trProp.IsCambioDeGuia = (trProp.PreviousGuiaArriba != trProp.GuiaArriba) ? true : false;

            trProp.Is5taEntradaAlcista = false;
            trProp.Is5taEntradaBajista = false;


            if (!trProp.GuiaArriba && trProp.IsEma2Rising && trProp.IsEma15Rising && trProp.IsEma2OverEma15 & trProp.IsCambioDeGuia)
            {
                trProp.Is5taEntradaAlcista = true;

            }

            if (trProp.GuiaArriba && !trProp.IsEma2Rising && !trProp.IsEma15Rising && !trProp.IsEma2OverEma15 & trProp.IsCambioDeGuia)
            {
                trProp.Is5taEntradaBajista = true;

            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress == 0 && CurrentBar == 1)
            {
                if (IsWriteToFile)
                {
                    string uniqueIdentifier = DateTime.Now.Ticks.ToString();
                    FileName = FileNamePrefix + uniqueIdentifier + ".csv";
                    _path = NinjaTrader.Core.Globals.UserDataDir + FileName; // Define the Path to our test file
                    TRUtilities.SaveToFile(_path, IsWriteToFile, HeaderText);

                    string fileNameExit = FileNamePrefix + uniqueIdentifier + "-Exit.csv";
                    _breakEvenExit.Path = NinjaTrader.Core.Globals.UserDataDir + fileNameExit;
                    TRUtilities.SaveToFile(_breakEvenExit.Path, IsWriteToFile, _breakEvenExit.HeaderText);

                }

                if (IsPrintOutput)
                {
                    Print(HeaderText);
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
                // BreakEvenExtiStrategy();
            }

        }

        private void ProcessSectorSignals()
        {

            if (trProp.Is5taEntradaAlcista || trProp.Is5taEntradaBajista)
            {

                double riesgo = 0;
                double profitPotential = 0;
                string entradamessage = "";

                if (trProp.IsSectorAlcista == true)
                {
                    entradamessage = "5ta Entrada Alcista";
                    PrintOutput(entradamessage);

                    Draw.Dot(this, @"5taEntradaTR" + CurrentBar, true, 0, Low[0] - 1, Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, entradamessage, 0, Convert.ToInt32(Low[0]) - 10, ChartControl.Properties.ChartText);

                    _breakEvenExit.StopPrice = Low[0];
                    //   _ema15PriceTarget = ema15[0];
                    //   _breakEvenExit.TriggerPrice = _ema15PriceTarget;

                    if (Position.MarketPosition == MarketPosition.Flat)
                    {
                        //riesgo = Close[0] - _breakEvenExit.StopPrice;
                        //profitPotential = _breakEvenExit.TriggerPrice - Close[0];

                        //if (profitPotential > riesgo)
                        //{
                        //    _breakEvenExit.TriggerState = 1;
                        //    PrintOutput("Entering Long");
                        // //   EnterLong(Convert.ToInt32(DefaultQuantity), @"entry");
                        //}
                        //else
                        //{
                        //    PrintOutput("Skipping Long opportunity Risk is not worth the target");
                        //}
                    }
                }
                else
                {
                    entradamessage = "5ta Entrada Bajista";
                    PrintOutput(entradamessage);

                    Draw.Dot(this, @"5taEntradaTR" + CurrentBar, true, 0, High[0] + 1, Brushes.CornflowerBlue);
                    Draw.Text(this, "tag1" + CurrentBar, entradamessage, 0, Convert.ToInt32(High[0]) + 10, ChartControl.Properties.ChartText);

                    _breakEvenExit.StopPrice = High[0];
                    _breakEvenExit.TriggerPrice = ema15[0];

                    //if (Position.MarketPosition == MarketPosition.Flat)
                    //{
                    //    riesgo = _breakEvenExit.StopPrice - Close[0];
                    //    profitPotential = Close[0] - _breakEvenExit.TriggerPrice;

                    //    if (profitPotential > riesgo)
                    //    {
                    //        _breakEvenExit.TriggerState = -1;
                    //        PrintOutput("Entering Short");
                    //      //  EnterShort(Convert.ToInt32(DefaultQuantity), @"entryshort");
                    //    }
                    //    else
                    //    {
                    //        PrintOutput("Skipping Short opportunity Risk is not worth the target");
                    //    }
                    //}

                }

                // _ = Draw.Dot(this, @"SectorTR" + CurrentBar, true, 0, High[0] + 5, Brushes.Fuchsia);

                // Time, BarInProgress,Open,Close,High,Low,Guia,GuiaArriba,IsHighGTGuia, IsLowCrossGuia,IsFacturoGuia, IsFacturoLTGuia,sma50, ema2, ema15,IsCloseGTSMA50, IsEma2Rising,IsEma15Rising, IsCloseGTema2,Is5taEntradaAlcista,Is5taEntradaBajista
                //  Print(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], GuiaArriba, IsHighGTGuia, IsLowCrossGuia, IsFacturoGuia, IsFacturoLTGuia, sma[0], ema2[0], ema15[0], IsCloseGTSMA50Alcista, IsEma2Rising, IsEma15Rising, IsCloseGTEma2min, Is5taEntradaAlcista, Is5taEntradaBajista));

                //   string logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", Time[0], BarsInProgress, Open[0], Close[0], High[0], Low[0], GuiaTR1[0], GuiaArriba, IsHighGTGuia, IsLowCrossGuia, IsFacturoGuia, IsFacturoLTGuia, sma[0], ema2[0], ema15[0], IsCloseGTSMA50Alcista, IsEma2Rising, IsEma15Rising, IsCloseGTEma2min, Is5taEntradaAlcista, Is5taEntradaBajista);
                //   Print(logEntry);


            }

        }
        private void BreakEvenExtiStrategy() { }

        private void PrintOutput(string text)
        {
            string output = string.Format(BodyText, Time[0], text, _breakEvenExit.TriggerPrice, _breakEvenExit.StopPrice);
            if (IsPrintOutput)
            {
                Print(output);
            }

            Write2File(output);
        }
        private void Write2File(string text)
        {
            if (IsWriteToFile)
            {
                TRUtilities.SaveToFile(_path, IsWriteToFile, text);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "IsWriteToFile", Description = "Log entries to a file", Order = 1, GroupName = "Parameters")]
        public bool IsWriteToFile
        { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "IsVerbose", Description = "Print Details for debugging", Order = 2, GroupName = "Parameters")]
        public bool IsVerbose { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "IsPrintOutput", Description = "Print Entries in Output Window", Order = 3, GroupName = "Parameters")]
        public bool IsPrintOutput
        { get; set; }

        #endregion
    }
}
