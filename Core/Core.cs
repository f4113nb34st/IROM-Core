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
        public double TickRate;

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
        /// True if window dirties itself automatically.
        /// </summary>
        public bool AutoDirty = true;
        
        /// <summary>
        /// The id of the current frame.
        /// </summary>
        private ulong frameID;

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
        protected Core(String title, double tickRate) : this(title, tickRate, typeof(DoubleBufferStrategy))
        {
        	
        }
        
        /// <summary>
        /// Creates a new <see cref="Core"/> with the given title and buffer strategy.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="renderBufferType">The render buffer type.</param>
        protected Core(String title, Type renderBufferType) : this(title, 60, typeof(DoubleBufferStrategy))
        {
        	
        }

        /// <summary>
        /// Creates a new <see cref="Core"/> with the given title, tick rate, and buffer strategy.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="tickRate">The tick rate in Hz.</param>
        /// <param name="renderBufferType">The render buffer type.</param>
        protected Core(String title, double tickRate, Type renderBufferType)
        {
            //set tickRate vars
            TickRate = tickRate;

            //set the title
            Title = title;
            //create the frame
            WindowObj = new Window(renderBufferType);
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
            //restart smoothing on screen resize
            //OBSOLETE
            //WindowObj.OnResize += size => tickDelay = 100;
            //start running
            Running = true;
            //start the window
            WindowObj.Start();
            //init title
            WindowObj.SetTitle(Title);
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
        protected virtual void Tick(double deltaTime)
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
        /// Marks the window as dirty to be re-rendered.
        /// </summary>
        public void MarkDirty()
        {
        	frameID++;
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
                double time = HiResTimer.CurrentTime;
                //the last loop iteration time
                double prevTime;
                //time since last iteration
                double ellapsedTime;
                
                //the time of last tick
                double tickTime = HiResTimer.CurrentTime;
                //number of ticks to perform
                int tickNum = 0;
                
                //last title update time
                double titleTime = 0;

                //loop till time to exit
                while (Running)
                {
                    //update current time
                    prevTime = time;
                    time = HiResTimer.CurrentTime;
                    ellapsedTime = time - prevTime;
                    
                    Tick(ellapsedTime);

                    //get ticks we need to compute
					tickNum = (int)((ellapsedTime) * TickRate);
					//update time
					tickTime += tickNum / TickRate;
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
                    if(!WindowObj.Fullscreen && AutoDirty)
                    {
                    	//update once a second
                        if(time - titleTime > 1)
                        {
                        	titleTime = time;
                        	//update the title
                       	 	UpdateTitle();
                        }
                    }
                    
                    if(tickNum == 0) Thread.Yield();
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
        /// <returns>True if a frame was rendered.</returns>
        /// </summary>
        protected virtual bool BaseRender()
        {
        	if(AutoDirty) frameID++;
        	FrameBuffer buffer = WindowObj.GetRenderBuffer();
        	if(buffer != null && buffer.LastFrameId < frameID)
        	{
        		//update last frame id
        		buffer.LastFrameId = frameID;
                //perform paint
                Render(buffer.Image);
                //poll frame rate
                RenderRate.Poll();
                //refresh window
                WindowObj.Refresh();
                return true;
        	}
        	return false;
        }

        /// <summary>
        /// Updates the title of the frame and appends the frame rate.
        /// </summary>
        private void UpdateTitle()
        {
        	WindowObj.SetTitle(Title + ": " + (int)RenderRate.GetFrameRate());
        }
        
        //OBSOLETE
        /*
        private int tickDelay = 100;
        private readonly double[] tickBuffer = new double[100];
        private int tickIndex = 0;
        private double tickTotal;
        private double tickAverage;
        private double tickDebt;
        private double tickPart;
        
        /// <summary>
        /// Smooths the time between ticks to prevent jittering.
        /// </summary>
        /// <param name="time">The time for the current tick.</param>
        private void SmoothTime(ref double time)
        {
        	if(tickDelay > 0)
        	{
        		tickDelay--;
        		tickBuffer[tickIndex] = time;
        		tickTotal += time;
        		tickIndex = tickIndex++ % 100;
        	}else
        	{
	        	tickTotal -= tickBuffer[tickIndex];
	        	tickBuffer[tickIndex] = time;
	        	tickTotal += tickBuffer[tickIndex];
	        	tickIndex = tickIndex++ % 100;
	        	tickAverage = tickTotal / 100;
	        	if(time > tickAverage * 1.25)
	        	{
	        		tickPart = time - (tickAverage * 1.25);
	        		tickDebt += tickPart;
	        		time -= tickPart;
	        	}else
	        	if(time < tickAverage * 1.1 && tickDebt > 0)
	        	{
	        		tickPart = (tickAverage * 1.1) - time;
	        		tickDebt -= tickPart;
	        		time += tickPart;
	        	}
        	}
        }*/
    }
}

