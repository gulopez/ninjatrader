namespace NinjaTrader.NinjaScript.Indicators
{
    public class TRProperties
    {
        //Sector
        public bool IsSectorAlcista { get; set; } = false;
        public bool IsSectorBajista { get; set; } = false;
        public bool IsHighGTGuia { get; set; } = false;
        public bool IsLowCrossGuia { get; set; } = false;
        public bool IsFacturoGuia { get; set; } = false;
        public bool IsFacturoLTGuia { get; set; } = false;
        public bool IsCloseGTSMA50Alcista { get; set; } = false;
        public bool IsCloseGTSMA50Bajista { get; set; } = false;
        public bool IsEma2Rising { get; set; } = false;
        public bool IsEma15Rising { get; set; } = false;
        public bool IsCloseGTEma2min { get; set; } = false;

        //5ta Entrada
        public bool PreviousGuiaArriba { get; set; } = false;
        public bool IsEma2OverEma15 { get; set; } = false;
        public bool IsCambioDeGuia { get; set; } = false;

        //Tarde
        public bool IsCruzoPorDebajo { get; set; }
        public bool IsCruzoPorArriba { get; set; }
        public bool IsEMA2CrossAbove { get; set; }
        public bool IsEMA2CrossBelow { get; set; }
        public bool GuiaArriba { get; set; }        
        public int Steps { get; set; } = 0;
        public bool IsTardeEntradaAlcista { get; set; } = false;

        public bool  IsTardeEntradaBajista { get; set; } = false;
    }
}
