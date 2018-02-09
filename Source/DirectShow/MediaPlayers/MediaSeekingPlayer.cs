using System;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace WPFMediaKit.DirectShow.MediaPlayers
{
    //internal enum PlaybackStateInternal
    //{
    //    Playing,
    //    Paused,
    //    Stopped
    //}
    /// <summary>
    /// The arguments that store information about a media position change
    /// </summary>
    internal class MediaStateChangedInternalEventArgs : EventArgs
    {
        public MediaStateChangedInternalEventArgs(MediaState state)
        {
            MediaState = state;
        }

        public MediaState MediaState { get; protected set; }
    }
    /// <summary>
    /// The MediaSeekingPlayer adds media seeking functionality to
    /// to the MediaPlayerBase class
    /// </summary>
    public abstract class MediaSeekingPlayer : MediaPlayerBase
    {
        /// <summary>
        /// Local cache of the current position
        /// </summary>
        private long m_currentPosition;

        /// <summary>
        /// The DirectShow media seeking interface
        /// </summary>
        private IMediaSeeking m_mediaSeeking;

        /// <summary>
        /// Gets the duration in miliseconds, of the media that is opened
        /// </summary>
        public virtual long Duration { get; protected set; }

        /// <summary>
        /// Sets the rate at which the media plays back
        /// </summary>
        public virtual double SpeedRatio
        {
            get
            {
                if (m_mediaSeeking == null)
                    return 1;

                double rate;

                m_mediaSeeking.GetRate(out rate);
                return rate;
            }
            set
            {
                if (m_mediaSeeking == null)
                    return;

                double rate = value;

                int hr = m_mediaSeeking.SetRate(rate);
            }
        }

        /// <summary>
        /// Gets or sets the position in miliseconds of the media
        /// </summary>
        public virtual long MediaPosition
        {
            get
            {
                VerifyAccess();
                return m_currentPosition;
            }
            set
            {
                SetMediaPosionOnMediaSeekingElement(value);
            }
        }

        private void SetMediaPosionOnMediaSeekingElement(long value)
        {
            VerifyAccess();
            m_currentPosition = value;

            if (m_mediaSeeking != null)
            {
                /* Try to set the media time */
                m_mediaSeeking.SetPositions(m_currentPosition,
                                            AMSeekingSeekingFlags.AbsolutePositioning,
                                            0,
                                            AMSeekingSeekingFlags.NoPositioning);
            }
        }

        /// <summary>
        /// Sets the MediaPosition value without notifying the underlying media player.
        /// </summary>
        internal void SetMediaPositionInternal(long position)
        {
            m_currentPosition = position;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                InvokeMediaPositionChanged(null);
            }));
        }

        /// <summary>
        /// The current position format the media is using
        /// </summary>
        private MediaPositionFormat m_currentPositionFormat;

        /// <summary>
        /// The prefered position format to use with the media
        /// </summary>
        private MediaPositionFormat m_preferedPositionFormat;

        /// <summary>
        /// The current media positioning format
        /// </summary>
        public virtual MediaPositionFormat CurrentPositionFormat
        {
            get { return m_currentPositionFormat; }
            protected set { m_currentPositionFormat = value; }
        }

        /// <summary>
        /// The prefered media positioning format
        /// </summary>
        public virtual MediaPositionFormat PreferedPositionFormat
        {
            get { return m_preferedPositionFormat; }
            set
            {
                m_preferedPositionFormat = value;
                SetMediaSeekingInterface(m_mediaSeeking);
            }
        }

        /// <summary>
        /// Notifies when the position of the media has changed
        /// </summary>
        public event EventHandler MediaPositionChanged;

        protected void InvokeMediaPositionChanged(EventArgs e)
        {
            EventHandler mediaPositionChangedHandler = MediaPositionChanged;
            if (mediaPositionChangedHandler != null) mediaPositionChangedHandler(this, e);
        }

        /// <summary>
        /// This method is overriden to get out the seeking
        /// interfaces from the DirectShow graph
        /// </summary>
        protected override void SetupFilterGraph(IFilterGraph graph)
        {
            SetMediaSeekingInterface(graph as IMediaSeeking);
            base.SetupFilterGraph(graph);
        }

        /// <summary>
        /// Frees any allocated or unmanaged resources
        /// </summary>
        protected override void FreeResources()
        {
            base.FreeResources();

            if (m_mediaSeeking != null)
                Marshal.ReleaseComObject(m_mediaSeeking);

            m_mediaSeeking = null;
            m_currentPosition = 0;
        }

        /// <summary>
        /// Is ran everytime a new media event occurs on the graph
        /// </summary>
        /// <param name="code">The Event code that occured</param>
        /// <param name="lparam1">The first event parameter sent by the graph</param>
        /// <param name="lparam2">The second event parameter sent by the graph</param>
        protected override void OnMediaEvent(EventCode code, IntPtr lparam1, IntPtr lparam2)
        {
            base.OnMediaEvent(code, lparam1, lparam2);
            switch (code)
            {
                case EventCode.Complete:
                    {
                        if (MediaPosition == Duration)
                        {
                            Pause();
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Polls the graph for various data about the media that is playing
        /// </summary>
        protected override void OnGraphTimerTick()
        {
            /* Polls the current position */
            if (m_mediaSeeking != null)
            {
                long lCurrentPos;

                int hr = m_mediaSeeking.GetCurrentPosition(out lCurrentPos);

                if (hr == 0)
                {
                    if (lCurrentPos != m_currentPosition)
                    {
                        m_currentPosition = lCurrentPos;
                        InvokeMediaPositionChanged(null);
                    }
                }
            }

            base.OnGraphTimerTick();
        }

        /// <summary>
        /// Converts a MediaPositionFormat enum to a DShow TimeFormat GUID
        /// </summary>
        protected static Guid ConvertPositionFormat(MediaPositionFormat positionFormat)
        {
            Guid timeFormat;

            switch (positionFormat)
            {
                case MediaPositionFormat.MediaTime:
                    timeFormat = TimeFormat.MediaTime;
                    break;
                case MediaPositionFormat.Frame:
                    timeFormat = TimeFormat.Frame;
                    break;
                case MediaPositionFormat.Byte:
                    timeFormat = TimeFormat.Byte;
                    break;
                case MediaPositionFormat.Field:
                    timeFormat = TimeFormat.Field;
                    break;
                case MediaPositionFormat.Sample:
                    timeFormat = TimeFormat.Sample;
                    break;
                default:
                    timeFormat = TimeFormat.None;
                    break;
            }

            return timeFormat;
        }

        /// <summary>
        /// Converts a DirectShow TimeFormat GUID to a MediaPositionFormat enum
        /// </summary>
        protected static MediaPositionFormat ConvertPositionFormat(Guid positionFormat)
        {
            MediaPositionFormat format;

            if (positionFormat == TimeFormat.Byte)
                format = MediaPositionFormat.Byte;
            else if (positionFormat == TimeFormat.Field)
                format = MediaPositionFormat.Field;
            else if (positionFormat == TimeFormat.Frame)
                format = MediaPositionFormat.Frame;
            else if (positionFormat == TimeFormat.MediaTime)
                format = MediaPositionFormat.MediaTime;
            else if (positionFormat == TimeFormat.Sample)
                format = MediaPositionFormat.Sample;
            else
                format = MediaPositionFormat.None;

            return format;
        }

        protected void SetDuration()
        {
            if (m_mediaSeeking == null)
                return;

            long duration;

            /* Get the duration of the media.  This value will
             * be in whatever format that was set. ie Frame, MediaTime */
            m_mediaSeeking.GetDuration(out duration);

            Duration = duration;
        }

        /// <summary>
        /// Setup the IMediaSeeking interface
        /// </summary>
        protected void SetMediaSeekingInterface(IMediaSeeking mediaSeeking)
        {
            m_mediaSeeking = mediaSeeking;

            if (mediaSeeking == null)
            {
                CurrentPositionFormat = MediaPositionFormat.None;
                Duration = 0;
                return;
            }

            /* Get our prefered DirectShow TimeFormat */
            Guid preferedFormat = ConvertPositionFormat(PreferedPositionFormat);

            /* Attempt to set the time format */
            mediaSeeking.SetTimeFormat(preferedFormat);

            Guid currentFormat;

            /* Gets the current time format
             * we may not have been successful
             * setting our prefered format */
            mediaSeeking.GetTimeFormat(out currentFormat);

            /* Set our property up with the right format */
            CurrentPositionFormat = ConvertPositionFormat(currentFormat);

            if (MediaPosition > 0)
            {
                SetMediaPosionOnMediaSeekingElement(MediaPosition);
            }

            SetDuration();
        }
        public override bool Play()
        {
            if (MediaPosition == Duration)
            {
                MediaPosition = 0;
            }
          return  base.Play();
        }

        public override void Stop()
        {
            base.Stop();
            MediaPosition = 0;
            InvokeMediaPositionChanged(null);
        }
    }
}
