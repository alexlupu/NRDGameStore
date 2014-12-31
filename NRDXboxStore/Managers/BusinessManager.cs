using System;
using System.Linq;
using System.Threading.Tasks;
using NRDXboxStore.NRDXboxVotingService;

namespace NRDXboxStore.Managers
{
    /// <summary>
    /// Separated Business rules from interaction with the service.
    /// This allows the service to point to the web service or a DB directly, 
    /// without interfering with the business logic.
    /// </summary>
    public class BusinessManager
    {
        #region Fields

        /// <summary>
        /// Local instance of our service manager to connect to our storage.
        /// </summary>
        private readonly ServiceManager serviceManager;

        /// <summary>
        /// TimeZoneId of the MSP office.
        /// </summary>
        private readonly string officeTimeZoneId = Properties.Settings.Default.MSPOfficeTimeZoneId;

        /// <summary>
        /// List of the days in the week where voting is allowed.
        /// Note: Sunday is 0, Monday is 1. Not sure if that changes in timezones where Monday is the first day of the week.
        /// </summary>
        private readonly int[] daysOfWeekForVoting = Properties.Settings.Default.DaysOfWeekForVoting;

        /// <summary>
        /// Status of a particular game as wanted, and not owned
        /// </summary>
        public const string GAME_STATUS_WANTED = "wantit";

        /// <summary>
        /// Status of a particular game as owned
        /// </summary>
        public const string GAME_STATUS_OWNED = "gotit";

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BusinessManager()
        {
            serviceManager = new ServiceManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Check that the connection to our permanent store is working.
        /// </summary>
        /// <returns>True if we have successfully connected to the store</returns>
        public bool IsConnectionWorking()
        {
            // NOTE: we could capture all connection exceptions here and just return false. 
            // This would work for "simple" UIs who might not want to show specific error 
            // messages to their users, but if we wanted to extend this to an admin screen
            // they could make use of the exception details.
            return serviceManager.IsConnectionWorking();
        }

        /// <summary>
        /// Business logic checking if a user can add an entry based off their last entry and the current environment time.
        /// </summary>
        /// <param name="lastUserEntryDateTimeUtc">Last Vote or New Game suggestion given by the user in UTC</param>
        /// <returns></returns>
        public bool IsThisUserAbleToAddNewEntry(DateTime lastUserEntryDateTimeUtc)
        {            
            // was the user's last entry the yesterday at the latest?                        
            return lastUserEntryDateTimeUtc.Date <= DateTime.UtcNow.AddDays(-1).Date;
        }

        /// <summary>
        /// Check to see if any votes / new game suggestions are allowed.
        /// This is based around a specified office timezone (in settings), 
        /// since we assume this service will be run anywhere.
        /// </summary>
        /// <returns>True if votes are allowed for this day</returns>
        public bool AreEntriesAllowedRightNow()
        {
            // convert from utc to the office's timezone to figure out the day of week.
            // this also takes into account Daylight Savings, where applicable.
            DateTime officeDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "UTC", officeTimeZoneId);
            int currentDayOfWeekInOffice = (int)officeDateTime.DayOfWeek;
            
            return daysOfWeekForVoting.Any(thisValidDay => currentDayOfWeekInOffice == thisValidDay);            
        }

        /// <summary>
        /// Fetch the complete list of Xbox Games
        /// No restrictions placed on calling this service
        /// </summary>
        /// <returns>A list of Xbox Games</returns>
        public XboxGame[] GetAllGames()
        {
            // load the xbox games synchronously.
            return serviceManager.GetAllGames();
        }

        /// <summary>
        /// Fetch the complete list of Xbox Games asynchronously
        /// No restrictions placed on calling this service
        /// </summary>
        /// <returns>A list of Xbox Games, eventually</returns>
        async public Task<XboxGame[]> GetAllGamesAsync()
        {
            return await serviceManager.GetAllGamesAsync();
        }

        /// <summary>
        /// Mark an existing game as owned.
        /// No user restrictions placed on calling this service
        /// </summary>
        /// <param name="gameId">ID of the game</param>
        /// <returns>True if successfully marked as owned</returns>
        public bool MarkExistingGameAsOwned(int gameId)
        {
            // Due to lack of caching, and quirks in data store restrictions (see dev notes), the only way we're able to 
            // minimize issues with stale data on different machines is to fetch the entire library right before writing 
            // to the store, and apply business logic here.

            // fetch all of the wanted games (we can't purchase owned games)
            var allGames = GetAllGames().Where(game => game.Status == GAME_STATUS_WANTED);

            // check business case of only voting for titles in our system.
            var isAbleToBeOwned = allGames.Any(game => game.Id == gameId);

            return isAbleToBeOwned && serviceManager.MarkGameAsOwned(gameId);
        }

        /// <summary>
        /// Add a new game to the voting list
        /// Restrictions are based on whether user can vote and if the game is owned.
        /// The first restriction should be taken before calling this method.
        /// </summary>
        /// <param name="gameTitle">new game title</param>
        /// <returns>True if game title was added successfully</returns>
        public bool AddNewGameToVotingList(string gameTitle)
        {
            // Due to lack of caching, and quirks in data store restrictions (see dev notes), the only way we're able to 
            // minimize issues with stale data on different machines is to fetch the entire library right before writing 
            // to the store, and apply business logic here.

            // fetch all of the games
            var allGames = GetAllGames();

            // check business case of no duplicate titles.
            var isExistingTitle = allGames.Any(game => game.Title == gameTitle);

            return !isExistingTitle && serviceManager.AddNewGameTitle(gameTitle);
        }

        /// <summary>
        /// Vote for an existing game
        /// Restrictions are based on whether user can vote and if the game is owned.
        /// The first restriction should be taken before calling this method.
        /// </summary>
        /// <param name="gameId">ID of the game</param>
        /// <returns>True if game was successfully voted for</returns>
        public bool VoteForGame(int gameId)
        {
            // Due to lack of caching, and quirks in data store restrictions (see dev notes), the only way we're able to 
            // minimize issues with stale data on different machines is to fetch the entire library right before writing 
            // to the store, and apply business logic here.

            // fetch all of the wanted games (we shouldn't be voting for owned games)
            var allGames = GetAllGames().Where(game => game.Status == GAME_STATUS_WANTED);

            // check business case of only voting for titles in our system.
            var isAbleToBeVoted = allGames.Any(game => game.Id == gameId);

            return isAbleToBeVoted && serviceManager.AddVoteForGame(gameId);
        }

        #endregion

    }
}