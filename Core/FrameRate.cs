namespace IROM.Core
{
	using System;
	
	/// <summary>
    /// Simple class for calculating the frame rate of an application.
    /// </summary>
    public sealed class FrameRate
    {
        //the frames since last calc
        private int Frames;
        //the last frame calc time
        private long Start = long.MinValue;
        //current frame rate
        private volatile float Rate;
        //rate to recalc frame rate
        private readonly int RateCalcRate = 1000;

        /// <summary>
        /// Polls a frame.
        /// </summary>
        public void Poll()
        {
            //inc frames
            Frames++;
            long time = Environment.TickCount;
            //if time to recalc
            if(time >= Start + RateCalcRate)
            {
                //recalc
                CalcFrameRate();
                //reset frames
                Frames = 0;
                //set start time
                Start = time;
            }
        }

        /// <summary>
        /// Returns the current frame rate.
        /// </summary>
        /// <returns>The fps.</returns>
        public float GetFrameRate()
        {
            return Rate;
        }

        /// <summary>
        /// Recalculates the frame rate.
        /// </summary>
        private void CalcFrameRate()
        {
            Rate = Frames / ((Environment.TickCount - Start) / 1000F);
            if (Rate > 5)//if >5 round to nearest frame
            {
                Rate = (float)Math.Round(Rate);
            }
        }
    }
}
