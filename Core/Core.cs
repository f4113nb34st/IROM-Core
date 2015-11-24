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
    /// </summary>
    public abstract class Core
    {
        /// <summary>
        /// The number of ticks per second for FixedTick.
        /// </summary>
        public double TickRate
        {
        	get
        	{
        		return 1000 / InvTickRateMillis;
        	}
        	set
        	{
        		InvTickRateMillis = 1000 / value;
        	}
        }
        
        /// <summary>
        /// Inv of tickRate. (milliseconds per tick)
        /// </summary>
        protected double InvTickRateMillis;

        /// <summary>
        /// True if currently running.
        /// </summary>
        public volatile bool Running = false;

        /// <summary>
        /// The base <see cref="Window"/>.
        /// </summary>
        public readonly Window WindowObj;

        /// <summary>
        /// The title.
        /// </summary>
        public String Title;
        
        /// <summary>
        /// The <see cref="FrameRate"/> instance.
        /// </summary>
        protected readonly FrameRate RenderRate;

        /// <summary>
        /// Creates a new <see cref="Core"/> with the given name and a 60Hz tickRate.
        /// </summary>
        /// <param name="title">The title.</param>
        protected Core(String title) : this(title, 60)
        {
            
        }

        /// <summary>
        /// Creates a new <see cref="Core"/> with the given title and tick rate.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="tickRate">The tick rate in Hz.</param>
        protected Core(String title, double tickRate)
        {
            //set tickRate vars
            TickRate = tickRate;

            //set the title
            Title = title;
            //create the frame
            WindowObj = new Window();
            WindowObj.Bounds = GetScreenBounds() / 2;
            
            //create the frame rate counter
            RenderRate = new FrameRate();
        }

        /// <summary>
        /// Starts this Core instance.
        /// </summary>
        public void Start()
        {
        	//perform initialization work
            Init();
            //start running
            Running = true;
            //start the window
            WindowObj.Start();
            //start rendering/ticking tasks
            StartTasks();
        }
        
        protected virtual void StartTasks()
        {
        	TickTask();
        }
        
        /// <summary>
        /// Returns the bounds of the primary screen.
        /// </summary>
        public Rectangle GetScreenBounds()
        {
        	return Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// Performs any initialization work required by the application.
        /// </summary>
        protected virtual void Init()
        {
        	
        }
        
        /// <summary>
        /// Called frequently. Perform update work here.
        /// </summary>
        protected virtual void Tick(double time)
        {
        	
        }

        /// <summary>
        /// Called at tickRate. 
        /// Timing may not exactly align to tick bounds,
        /// but number of calls is guaranteed to be correct
        /// over a period of time.
        /// </summary>
        protected virtual void FixedTick()
        {
        	
        }

        /// <summary>
        /// Renders the screen.
        /// </summary>
        /// <param name="image">The screen to paint on.</param>
        protected virtual void Render(Image image)
        {
        	
        }

        /// <summary>
        /// The primary ticking/rendering loop.
        /// </summary>
        protected virtual void TickTask()
        {
            //wrap entire task in a try-catch to ensure errors are reported
            try
            {
                //the current time
                int time = Environment.TickCount;
                //the last loop iteration time
                int prevTime;
                
                //the time of last tick
                double tickTime = Environment.TickCount;
                //number of ticks to perform
                int tickNum = 0;
                
                //last title update time
                int titleTime = 0;

                //loop till time to exit
                while (Running)
                {
                    //update current time
                    prevTime = time;
                    time = Environment.TickCount;
                    
                    Tick(time - prevTime);

                    //get ticks we need to compute
					tickNum = (int)((time - tickTime) / InvTickRateMillis);
					//update time
					tickTime += tickNum * InvTickRateMillis;
					//don't allow us to get more than 5 behind
					tickNum = Math.Min(tickNum, 5);
					//perform ticks
					for(int i = 0; i < tickNum; i++)
					{
						FixedTick();
					}
                    
                    //render a frame
                    YieldRender();
                    
                    //update title
                    if(!WindowObj.Fullscreen)
                    {
                    	//update once a second
                        if(time - titleTime > 1000)
                        {
                        	titleTime = time;
                        	//update the title
                       	 	UpdateTitle();
                        }
                    }
                }
            }catch (Exception ex)
            {
                //report exception
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// Ticking yielding to rendering. 
        /// On single threaded applications performs a frame render.
        /// On multi threaded applications trips render barrier and yield processor.
        /// </summary>
        protected virtual void YieldRender()
        {
        	BaseRender();
        }
        
        /// <summary>
        /// Performs rendering on the window.
        /// </summary>
        protected virtual void BaseRender()
        {
        	Image buffer = WindowObj.GetRenderBuffer();
        	if(buffer != null)
        	{
                //perform paint
                Render(buffer);
                //poll frame rate
                RenderRate.Poll();
                //refresh window
                WindowObj.Refresh();
        	}
        }

        /// <summary>
        /// Updates the title of the frame and appends the frame rate.
        /// </summary>
        private void UpdateTitle()
        {
        	WindowObj.SetTitle(Title + ": " + (int)RenderRate.GetFrameRate());
        }
    }
}

