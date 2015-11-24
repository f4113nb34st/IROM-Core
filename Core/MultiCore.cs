namespace IROM.Core
{
	using System;
	using System.Linq;
	using System.Windows.Forms;
	using System.Threading;
	using IROM.Util;
	
    /// <summary>
    /// Abstract base class for a program core.
    /// Handles rendering to a Window and ticking.
    /// Uses separate rendering and ticking threads.
    /// </summary>
    public abstract class MultiCore : Core
    {
        /// <summary>
        /// The ticking thread.
        /// </summary>
        public readonly Thread TickThread;
        
        /// <summary>
        /// The render thread.
        /// </summary>
        public readonly Thread RenderThread;
        
        /// <summary>
        /// Invoked when the tick thread starts.
        /// </summary>
        public event EventHandler OnTickThreadInit;
        
        /// <summary>
        /// Invoked when the render thread starts.
        /// </summary>
        public event EventHandler OnRenderThreadInit;

        /// <summary>
        /// The lock for limiting the rendering rate to ensure constant ticking.
        /// </summary>
        private object RenderLock = new object();

        /// <summary>
        /// Creates a new <see cref="Core"/> with the given name and a 60Hz tickRate.
        /// </summary>
        /// <param name="title">The title.</param>
        protected MultiCore(String title) : base(title)
        {
            
        }

        /// <summary>
        /// Creates a new <see cref="Core"/> with the given title and tick rate.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="tickRate">The tick rate in Hz.</param>
        protected MultiCore(String title, double tickRate) : base(title, tickRate)
        {
            //create the ticking thread, runs tickTask()
            //responsible for keeping the app alive
            TickThread = new Thread(TickTask);
            TickThread.Name = Title + " Tick Thread";
            TickThread.IsBackground = false;
            TickThread.Priority = ThreadPriority.Highest;

            //create the rendering thread, runs renderTask()
            RenderThread = new Thread(RenderTask);
            RenderThread.Name = Title + " Render Thread";
            RenderThread.IsBackground = true;
        }

		protected override void StartTasks()
		{
			bool isTicker = false;
			bool isRenderer = false;
			if(this.GetType().GetMethod("Tick").DeclaringType != typeof(Core) ||
			   this.GetType().GetMethod("FixedTick").DeclaringType != typeof(Core))
				isTicker = true;
			if(this.GetType().GetMethod("Render").DeclaringType != typeof(Core))
				isRenderer = true;
			//start the threads
			if(isRenderer)
			{
				RenderThread.Start();
			}
			if(isTicker)
			{
				TickThread.Start();
			}else
			{
				RenderLock = null;
			}
		}
		
		protected override void TickTask()
		{
			if(OnTickThreadInit != null) OnTickThreadInit(this, EventArgs.Empty);
			base.TickTask();
		}
        
        protected override void YieldRender()
        {
        	lock(RenderLock)
            {
            	Monitor.PulseAll(RenderLock);
            }
        	Thread.Yield();
        }

        /// <summary>
        /// The primary rendering loop.
        /// </summary>
        private void RenderTask()
        {
            //wrap entire task in a try-catch to ensure errors are reported
            try
            {
            	if(OnRenderThreadInit != null) OnRenderThreadInit(this, EventArgs.Empty);
            	
                //never stop rendering, 
                //we're a background thread so 
                //we will be killed automatically
                while (true)
                {
                	BaseRender();
                    //wait
                    if(RenderLock != null) 
                    {
                    	lock(RenderLock)
			            {
			            	Monitor.Wait(RenderLock);
			            }
                    }
                }
            }catch(Exception ex)
            {
                //report exception
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }
    }
}

