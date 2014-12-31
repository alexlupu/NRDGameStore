using System;
using System.ServiceModel;
using System.Threading.Tasks;
using NRDXboxStore.NRDXboxVotingService;
using NRDXboxStore.Properties;

namespace NRDXboxStore.Managers
{
    /// <summary>
    /// Wrapper around the Web Service connection. For more info about this class and the
    /// design decisions and reasons, please see the Developer Notes included with the project.
    /// </summary>
    public class ServiceManager
    {
        #region Fields
        /// <summary>
        /// Provided API key, stored in Settings file for simple change and redeployment.
        /// If it changed frequently enough, we could refactor it to be set at run-time instead.
        /// </summary>
        private readonly string apiKey = Settings.Default.XboxServiceAPIKey;
        #endregion

        #region Constructor
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ServiceManager()
        {
            // NOTE: We could test the connection here, and throw an exception if it isn't working.
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Test the connection. NOTE: This should be checked before calling any of the other public methods.
        /// </summary>
        /// <returns>False if we have an invalid API key, but valid connection. True if both are valid.</returns>
        /// <Exception cref="Exception">Connection issues encountered</Exception>
        public bool IsConnectionWorking()
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                bool response;

                try
                {
                    response = serviceClient.CheckKey(apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
                    throw;
                }

                return response;
            }
            
        }

        /// <summary>
        /// Fetch a list of games from the service asynchronously
        /// </summary>
        /// <returns>A list of Xbox Games</returns>
        async public Task<XboxGame[]> GetAllGamesAsync()
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                XboxGame[] response;
                try
                {
                    response = await serviceClient.GetGamesAsync(apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
					throw;
                }

                return response;
            }
        }

        /// <summary>
        /// Fetch a list of games from the service (synchronously)
        /// </summary>
        /// <returns>A list of Xbox Games</returns>        
        public XboxGame[] GetAllGames()
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                XboxGame[] response;
                try
                {
                    response = serviceClient.GetGames(apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
					throw;
                }

                return response;
            }
            
        }

        /// <summary>
        /// Increment the vote counter for a specified game
        /// </summary>
        /// <param name="gameId">ID of the game</param>
        /// <returns>True if the operation was successful</returns>
        public bool AddVoteForGame(int gameId)
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                bool response;

                try
                {
                    response = serviceClient.AddVote(gameId, apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
					throw;
                }

                return response;
            }
        }

        /// <summary>
        /// Add a new game title to the voting list
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns>True if the operation was successful</returns>
        public bool AddNewGameTitle(string gameTitle)
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                bool response;

                try
                {
                    response = serviceClient.AddGame(gameTitle, apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
					throw;
                }

                return response;
            }
        }

        /// <summary>
        /// Mark a game as owned
        /// </summary>
        /// <param name="gameId">ID of the game</param>
        /// <returns>True if the operation was successful</returns>
        public bool MarkGameAsOwned(int gameId)
        {
            using (var serviceClient = new XboxVotingServiceSoapClient())
            {
                bool response;

                try
                {
                    response = serviceClient.SetGotIt(gameId, apiKey);
                }
                catch (FaultException faultException)
                {
                    // handle SOAP-exceptions, if desired. allow others to bubble up.
                    HandleServiceExceptions(faultException);
					throw;
                }

                return response;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handle exceptions caught when connecting to the web service.
        /// </summary>
        /// <param name="faultException">exception raised by the service command</param>
        private void HandleServiceExceptions(FaultException faultException)
        {
            // handle any web service-specific exceptions here, if we have a preference.
            // e.g. if we wanted to log this somewhere separate from IIS logs, we could do that now.
            // We could also throw an exception that's not specific to a web service and give the
            // business manager only enough info that we decide it needs.            
        }
        #endregion

   }
}