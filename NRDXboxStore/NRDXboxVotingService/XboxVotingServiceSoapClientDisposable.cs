using System;
using System.ServiceModel;

namespace NRDXboxStore.NRDXboxVotingService
{
    /// <summary>
    /// Partial class for our SOAP client to allow for its Disposable use of the service client.
    /// See: http://coding.abel.nu/2012/02/using-and-disposing-of-wcf-clients/ for further info.
    /// </summary>
    public partial class XboxVotingServiceSoapClient : IDisposable
    {
        #region IDisposable implementation
 
        /// <summary>
        /// IDisposable.Dispose implementation, calls Dispose(true).
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
 
        /// <summary>
        /// Dispose worker method. Handles graceful shutdown of the
        /// client even if it is an faulted state.
        /// </summary>
        /// <param name="disposing">Are we disposing (alternative is to be finalizing)</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (State != CommunicationState.Faulted)
                    {
                        Close();
                    }
                }
                finally
                {
                    if (State != CommunicationState.Closed)
                    {
                        Abort();
                    }
                }
            }
        }
 
        /// <summary>
        /// Finalizer.
        /// </summary>
        ~XboxVotingServiceSoapClient()
        {
            Dispose(false);
        }
 
    #endregion
    }
}