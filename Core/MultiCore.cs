namespace IROM.Core
{
	using System;
	using System.Linq;
	using System.Windows.Forms;
	using System.Threading;
	using System.Reflection;
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
        public Thread TickThread
        {
        	get;
        	protected set;
        }
        
        /// <summary>
        /// The render thread.
        /// </summary>
        public Thread RenderThread
        {
        	get;
        	protected set;
        }
        
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
        protected MultiCore(String title) : this(title, 60)
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
			
			MethodInfo tickMethod = this.GetType().GetMethod("Tick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo fixedTickMethod = this.GetType().GetMethod("FixedTick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo renderMethod = this.GetType().GetMethod("Render", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			
			if((tickMethod != null && tickMethod.DeclaringType != typeof(Core)) ||
			   (fixedTickMethod != null && fixedTickMethod.DeclaringType != typeof(Core)))
					isTicker = true;
			if(renderMethod != null && renderMethod.DeclaringType != typeof(Core))
					isRenderer = true;
			
			//start the threads
			if(isRenderer)
			{
				RenderThread.Start();
			}else
			{
				RenderThread = null;
			}
			if(isTicker)
			{
				TickThread.Start();
			}else
			{
				TickThread = null;
				RenderLock = null;
				//render is no longer optional
				if(RenderThread != null) RenderThread.IsBackground = false;
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
                //unless done running
                while(Running)
                {
                	bool rendered = BaseRender();
                    //wait
                    if(RenderLock != null) 
                    {
                    	lock(RenderLock)
			            {
			            	Monitor.Wait(RenderLock);
			            }
                    }else
                	if(!rendered)
                	{
                    	Thread.Yield();
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

