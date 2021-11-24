namespace ResumeDownload.Ext
{
    /// <summary>
    /// 暂停源
    /// </summary>
    public sealed class PauseTokenSource
    {
        /// <summary>
        /// The MRE that manages the "pause" logic. When the MRE is set, the token is not paused; when the MRE is not set, the token is paused.
        /// </summary>
        private readonly AsyncManualResetEvent _mre = new AsyncManualResetEvent(true);

        /// <summary>
        /// Whether or not this source (and its tokens) are in the paused state. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public bool IsPaused
        {
            get { return !_mre.IsSet; }
            set
            {
                if (value)
                {
                    _mre.Reset();
                }
                else
                {
                    _mre.Set();
                }
            }
        }

        /// <summary>
        /// Gets a pause token controlled by this source.
        /// </summary>
        public PauseToken Token
        {
            get { return new PauseToken(_mre); }
        }
    }
}
