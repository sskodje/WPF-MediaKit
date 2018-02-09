using System;
using System.Collections.Generic;
using System.Windows;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace WPFMediaKit.DirectShow.Controls
{
    /// <summary>
    /// The arguments that store information about a media position change
    /// </summary>
    public class MediaPositionChangedEventArgs : EventArgs
    {
        public MediaPositionChangedEventArgs(long pos, bool wasExternal)
        {
            MediaPosition = pos;
            SourceWasExternal = wasExternal;
        }
        /// <summary>
        /// True if the media position was altered externally, e.g. by the user seeking.
        /// </summary>
        public bool SourceWasExternal { get; protected set; }
        /// <summary>
        /// The media position in 100 nanosecond units
        /// </summary>
        public long MediaPosition { get; protected set; }
    }


    /// <summary>
    /// The MediaSeekingElement adds media seeking functionality to
    /// the MediaElementBase class.
    /// </summary>
    public abstract class MediaSeekingElement : MediaElementBase
    {
        /// <summary>
        /// This flag is used to ignore PropertyChangedCallbacks
        /// for when a DependencyProperty is needs to be updated
        /// from the media player thread
        /// </summary>
        private bool m_ignorePropertyChangedCallback;
        private bool m_durationChangeWasExternal;
        #region MediaPosition

        /// <summary>
        /// Notifies when the media position changes
        /// </summary>
        public event EventHandler<MediaPositionChangedEventArgs> MediaPositionChanged;

        public static readonly DependencyProperty MediaPositionProperty =
            DependencyProperty.Register("MediaPosition", typeof(long), typeof(MediaSeekingElement),
                                        new FrameworkPropertyMetadata((long)0,
                                                                      new PropertyChangedCallback(OnMediaPositionChangedCallback)));

        /// <summary>
        /// Gets or sets the media position in units of CurrentPositionFormat
        /// </summary>
        public long MediaPosition
        {
            get { return (long)GetValue(MediaPositionProperty); }
            set { SetValue(MediaPositionProperty, value); }
        }

        private static void OnMediaPositionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaSeekingElement)d).OnMediaPositionChanged(e);
        }

        protected virtual void OnMediaPositionChanged(DependencyPropertyChangedEventArgs e)
        {
            /* If the change came from within our class,
             * ignore this callback */
            if (m_ignorePropertyChangedCallback)
            {
                m_ignorePropertyChangedCallback = false;
                return;
            }
            m_durationChangeWasExternal = true;
            PlayerSetMediaPosition();
        }

        /// <summary>
        /// Used to set the MediaPosition without firing the
        /// PropertyChanged callback
        /// </summary>
        /// <param name="value">The value to set the MediaPosition to</param>
        protected void SetMediaPositionInternal(long value)
        {
            if (MediaPosition != value)
            {
                /* Flag that we want to ignore the next
                 * PropertyChangedCallback */
                m_ignorePropertyChangedCallback = true;

                MediaPosition = value;
            }
        }

        private void PlayerSetMediaPosition()
        {
            var position = MediaPosition;
            if (MediaPlayerBase.Dispatcher.ShuttingOrShutDown)
                return;
            //Due to the async nature of the mediaplayer dispatcher, the MediaPlayer won't know that the user have changed 
            //the MediaPosition value in response to e.g the MediaEnded event until after the MediaComplete event have run.
            //Calling SetMediaPositionInternal() fixes this by setting the value before invoking the change to the underlying MediaPlayer async.
            //It feels quite hackish, so should probably find a better solution.
            MediaSeekingPlayer.SetMediaPositionInternal(position);

            MediaPlayerBase.Dispatcher.BeginInvoke((Action)
                                                  (() =>
                                                  {
                                                      MediaSeekingPlayer.MediaPosition = position;
                                                  }));
            System.Diagnostics.Debug.WriteLine("Set media position to: " + position);
        }

        #endregion

        #region MediaDuration
        private static readonly DependencyPropertyKey MediaDurationPropertyKey
           = DependencyProperty.RegisterReadOnly("MediaDuration", typeof(long), typeof(MediaSeekingElement),
                                                 new FrameworkPropertyMetadata((long)0));

        public static readonly DependencyProperty MediaDurationProperty
            = MediaDurationPropertyKey.DependencyProperty;


        /// <summary>
        /// Gets the duration of the media in the units of CurrentPositionFormat
        /// </summary>
        public long MediaDuration
        {
            get { return (long)GetValue(MediaDurationProperty); }
        }

        /// <summary>
        /// Internal method to set the read-only MediaDuration
        /// </summary>
        protected void SetMediaDuration(long value)
        {
            SetValue(MediaDurationPropertyKey, value);
        }

        #endregion

        #region CurrentPositionFormat

        private static readonly DependencyPropertyKey CurrentPositionFormatPropertyKey
            = DependencyProperty.RegisterReadOnly("CurrentPositionFormat", typeof(MediaPositionFormat), typeof(MediaSeekingElement),
                new FrameworkPropertyMetadata(MediaPositionFormat.None));

        public static readonly DependencyProperty CurrentPositionFormatProperty
            = CurrentPositionFormatPropertyKey.DependencyProperty;

        /// <summary>
        /// The current position format that the media is currently using
        /// </summary>
        public MediaPositionFormat CurrentPositionFormat
        {
            get { return (MediaPositionFormat)GetValue(CurrentPositionFormatProperty); }
        }

        protected void SetCurrentPositionFormat(MediaPositionFormat value)
        {
            SetValue(CurrentPositionFormatPropertyKey, value);
        }

        #endregion

        #region PreferedPositionFormat

        public static readonly DependencyProperty PreferedPositionFormatProperty =
            DependencyProperty.Register("PreferedPositionFormat", typeof(MediaPositionFormat), typeof(MediaSeekingElement),
                new FrameworkPropertyMetadata(MediaPositionFormat.MediaTime,
                    new PropertyChangedCallback(OnPreferedPositionFormatChanged)));

        /// <summary>
        /// The MediaPositionFormat that is prefered to be used
        /// </summary>
        public MediaPositionFormat PreferedPositionFormat
        {
            get { return (MediaPositionFormat)GetValue(PreferedPositionFormatProperty); }
            set { SetValue(PreferedPositionFormatProperty, value); }
        }

        private static void OnPreferedPositionFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaSeekingElement)d).OnPreferedPositionFormatChanged(e);
        }

        /// <summary>
        /// Executes when a the prefered position format has changed
        /// </summary>
        protected virtual void OnPreferedPositionFormatChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized)
                PlayerSetPreferedPositionFormat();
        }

        private void PlayerSetPreferedPositionFormat()
        {
            var format = PreferedPositionFormat;
            MediaPositionFormat currentFormat;
            long duration;

            /* We use BeginInvoke here to avoid what seems to be a deadlock */
            MediaSeekingPlayer.Dispatcher.BeginInvoke((Action)delegate
            {
                MediaSeekingPlayer.PreferedPositionFormat = format;
                currentFormat = MediaSeekingPlayer.CurrentPositionFormat;
                duration = MediaSeekingPlayer.Duration;

                Dispatcher.BeginInvoke((Action)delegate
                {
                    SetCurrentPositionFormat(currentFormat);
                    SetMediaDuration(duration);
                });
            });
        }

        #endregion

        #region SpeedRatio

        public static readonly DependencyProperty SpeedRatioProperty =
            DependencyProperty.Register("SpeedRatio", typeof(double), typeof(MediaSeekingElement),
                new FrameworkPropertyMetadata(1.0,
                    new PropertyChangedCallback(OnSpeedRatioChanged)));

        /// <summary>
        /// Gets or sets the rate the media is played back
        /// </summary>
        public double SpeedRatio
        {
            get { return (double)GetValue(SpeedRatioProperty); }
            set { SetValue(SpeedRatioProperty, value); }
        }

        private static void OnSpeedRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaSeekingElement)d).OnSpeedRatioChanged(e);
        }

        protected virtual void OnSpeedRatioChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized)
                PlayerSetSpeedRatio();
        }

        private void PlayerSetSpeedRatio()
        {
            var speedRatio = SpeedRatio;

            MediaSeekingPlayer.Dispatcher.BeginInvoke((Action)delegate
            {
                MediaSeekingPlayer.SpeedRatio = speedRatio;
            });
        }
        #endregion

        public override void EndInit()
        {
            PlayerSetMediaPosition();
            PlayerSetPreferedPositionFormat();
            PlayerSetSpeedRatio();
            base.EndInit();
        }

        /// <summary>
        /// Internal reference to the MediaSeekingPlayer
        /// </summary>
        protected MediaSeekingPlayer MediaSeekingPlayer
        {
            get { return MediaPlayerBase as MediaSeekingPlayer; }
        }

        /// <summary>
        /// Fires when a media operation has failed
        /// </summary>
        /// <param name="e">The failed arguments</param>
        protected override void OnMediaPlayerFailed(MediaFailedEventArgs e)
        {
            /* Reset some values on a failure of the media */
            Dispatcher.BeginInvoke((Action)delegate
            {
                SetMediaDuration(0);
                MediaPosition = 0;
            });

            base.OnMediaPlayerFailed(e);
        }

        /// <summary>
        /// Occurs when the media player is being initialized.  Here
        /// the method is overridden as to attach to media seeking
        /// related functionality
        /// </summary>
        protected override void InitializeMediaPlayer()
        {
            /* Let the base class have its way with it */
            base.InitializeMediaPlayer();

            if (MediaSeekingPlayer == null)
                throw new WPFMediaKitException("MediaSeekingPlayer is null or does not inherit MediaSeekingPlayer");

            /* Let us know when the media position has changed */
            MediaSeekingPlayer.MediaPositionChanged += OnMediaPlayerPositionChangedPrivate;
        }

        /// <summary>
        /// A private handler for the MediaPositionChanged event of the media player
        /// </summary>
        private void OnMediaPlayerPositionChangedPrivate(object sender, EventArgs e)
        {
            OnMediaPlayerPositionChanged();
        }
        /// <summary>
        /// Fires the MediaStateChanged event
        /// </summary>
        /// <param name="e">The failed media arguments</param>
        protected void InvokeMediaPlayerPositionChanged(MediaPositionChangedEventArgs e)
        {
            EventHandler<MediaPositionChangedEventArgs> mediaPositionChangedEventHandler = MediaPositionChanged;
            if (mediaPositionChangedEventHandler != null) mediaPositionChangedEventHandler(this, e);
        }

        /// <summary>
        /// Runs when the media player's position has changed
        /// </summary>
        protected virtual void OnMediaPlayerPositionChanged()
        {
            long position = MediaSeekingPlayer.MediaPosition;
            long duration = MediaSeekingPlayer.Duration;
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (MediaDuration != duration)
                    SetMediaDuration(duration);

                SetMediaPositionInternal(position);
                InvokeMediaPlayerPositionChanged(new MediaPositionChangedEventArgs(MediaPosition, m_durationChangeWasExternal));
                m_durationChangeWasExternal = false;
            });
        }

        /// <summary>
        /// Runs when the MediaPlayer has successfully opened media
        /// </summary>
        protected override void OnMediaPlayerOpened()
        {
            /* Pull out our values of our properties */
            long duration = MediaSeekingPlayer.Duration;
            long position = 0;// MediaSeekingPlayer.MediaPosition;
            double rate = 1; // MediaSeekingPlayer.SpeedRatio;
            double volume = 1;

            var positionFormat = MediaSeekingPlayer.CurrentPositionFormat;

            Dispatcher.BeginInvoke((Action)delegate
            {
                position = MediaPosition;
                /* Set our DP values */
                SetCurrentPositionFormat(positionFormat);
                SetMediaDuration(duration);
                //SetMediaPositionInternal(position);
                //SpeedRatio = rate;
                rate = SpeedRatio;
                volume = Volume;
                MediaSeekingPlayer.Dispatcher.BeginInvoke((Action)delegate
              {
                  MediaSeekingPlayer.MediaPosition = position;
                  MediaSeekingPlayer.SpeedRatio = rate;
                  MediaPlayerBase.Volume = volume;
              });
            });

            base.OnMediaPlayerOpened();
        }
    }
}
