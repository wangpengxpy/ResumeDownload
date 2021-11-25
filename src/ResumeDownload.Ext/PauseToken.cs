﻿using System.Threading;
using System.Threading.Tasks;

namespace ResumeDownload.Ext
{
    public struct PauseToken
    {
        /// <summary>
        /// The MRE that manages the "pause" logic, or <c>null</c> if this token can never be paused. When the MRE is set, the token is not paused; when the MRE is not set, the token is paused.
        /// </summary>
        private readonly AsyncManualResetEvent _mre;

        internal PauseToken(AsyncManualResetEvent mre)
        {
            _mre = mre;
        }

        /// <summary>
        /// Whether this token can ever possibly be paused.
        /// </summary>
        public bool CanBePaused
        {
            get { return _mre != null; }
        }

        /// <summary>
        /// Whether or not this token is in the paused state.
        /// </summary>
        public bool IsPaused
        {
            get { return _mre != null && !_mre.IsSet; }
        }

        /// <summary>
        /// Asynchronously waits until the pause token is not paused.
        /// </summary>
        public Task WaitWhilePausedAsync()
        {
            if (_mre == null)
            {
                return Task.CompletedTask;
            }

            return _mre.WaitAsync();
        }

        /// <summary>
        /// Asynchronously waits until the pause token is not paused, or until this wait is canceled by the cancellation token.
        /// </summary>
        /// <param name="token">The cancellation token to observe. If the token is already canceled, this method will first check if the pause token is unpaused, and will return without an exception in that case.</param>
        public Task WaitWhilePausedAsync(CancellationToken token)
        {
            if (_mre == null)
            {
                return Task.CompletedTask;
            }

            return _mre.WaitAsync(token);
        }

        /// <summary>
        /// Synchronously waits until the pause token is not paused.
        /// </summary>
        public void WaitWhilePaused()
        {
            if (_mre != null)
            {
                _mre.Wait();
            }
        }

        /// <summary>
        /// Synchronously waits until the pause token is not paused, or until this wait is canceled by the cancellation token.
        /// </summary>
        /// <param name="token">The cancellation token to observe. If the token is already canceled, this method will first check if the pause token is unpaused, and will return without an exception in that case.</param>
        public void WaitWhilePaused(CancellationToken token)
        {
            if (_mre != null)
            {
                _mre.Wait(token);
            }
        }
    }
}