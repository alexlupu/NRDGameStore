using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using NRDXboxStore.Managers;

namespace NRDStoreFormsUI
{
    public partial class StorePage : System.Web.UI.Page
    {
        #region Fields
        /// <summary>
        /// Local business manager instance
        /// </summary>
        private readonly BusinessManager businessManager = new BusinessManager();

        /// <summary>
        /// Default date of last entry for those that don't have a history in our system. 
        /// If set it to today, it would force them to wait a day before being able to vote.
        /// If we default to yesterday, they can vote right away, and clear the cookies any time. 
        /// </summary>
        private readonly DateTime defaultLastEntryDateUtc = DateTime.UtcNow.AddDays(-1);

        /// <summary>
        /// Title of the parent name of a browser cookie
        /// </summary>
        private const string COOKIE_PARENT = "NRDXboxStore_UserSettings";

        /// <summary>
        /// Title of the Last User Entry cookie attribute
        /// </summary>
        private const string COOKIE_LAST_ENTRY_UTC = "LastUserEntryUTC";

        /// <summary>
        /// Error connecting to service
        /// </summary>
        private const string ERROR_MESSAGE_CONNECTION = "Error connecting to the service. Please check the connection strings and try again.";

        /// <summary>
        /// Error message when failing to mark game as owned
        /// </summary>
        private const string ERROR_MESSAGE_VOTE_GAME = "Error casting your vote. Refresh the page and try again.";

        /// <summary>
        /// Error message when failing to mark game as owned
        /// </summary>
        private const string ERROR_MESSAGE_ADD_NEW_TITLE = "Error adding new title. Refresh the page and try again.";

        /// <summary>
        /// Error when fetching the list of owned games
        /// </summary>
        private const string ERROR_MESSAGE_FETCH_GAMES = "Error fetching the list of owned games. Please try again.";

        /// <summary>
        /// Error choosing a game from dropdown
        /// </summary>
        private const string ERROR_MESSAGE_CHOOSE_GAME = "Error selecting a game. Choose a game from the drop-down and try again.";
        
        /// <summary>
        /// Error message when vote already cast
        /// </summary>
        private const string ERROR_MESSAGE_VOTE_CAST = "You have already submitted a title or cast a vote for the day! Come back the next working day!";
        
        /// <summary>
        /// Error message when voting not allowed today
        /// </summary>
        private const string ERROR_MESSAGE_NO_VOTES_TODAY = "No voting or new entries are allowed today!";
        #endregion

        #region Form Events
        protected void Page_Load(object sender, EventArgs e)
        {
            messagePanel.Visible = false;

            if (IsPostBack)
            {
                return; // no need to reload the game list
            }

            SetUpPage();
        }

        protected void VoteForGame_OnClick(object sender, EventArgs e)
        {
            VoteForAGame();
        }

        protected void AddNewGameTitle_OnClick(object sender, EventArgs e)
        {
            AddNewTitle();
        }
        #endregion

        /// <summary>
        /// Set up the page for initial load and refresh 
        /// </summary>
        private void SetUpPage()
        {
            // deal with bad connection.
            try
            {
                if (!businessManager.IsConnectionWorking())
                {
                    ShowMessage(ERROR_MESSAGE_CONNECTION, true);
                    return;
                }

            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_CONNECTION, true);
                return;
            }

            // populate the page fields.
            RefreshAndPopulateListOfGames();
        }

        /// <summary>
        /// Refresh the list of voted-for games
        /// </summary>
        private void RefreshAndPopulateListOfGames()
        {
            // show the loading sign, and hide the grid.
            loadingPanel.Visible = true;
            ownedGamesPanel.Visible = false;
            noOwnedGamesPanel.Visible = false;

            try
            {
                // carry out only ONE fetch from the service, and apply filters here. 
                var allGames = businessManager.GetAllGames();

                // bind data to owned list
                var ownedGames = allGames.Where(game => game.Status == BusinessManager.GAME_STATUS_OWNED);

                listOfOwnedGames.DataTextField = "Title";
                listOfOwnedGames.DataValueField = "Id";
                listOfOwnedGames.DataSource = ownedGames;
                listOfOwnedGames.DataBind();

                // for voted games, we need to display the count as well, so we'll use a loop instead of data binding.
                var wantedGames = allGames.Where(game => game.Status == BusinessManager.GAME_STATUS_WANTED);

                listOfVotedGames.Items.Clear();
                foreach (var thisGame in wantedGames)
                {
                    listOfVotedGames.Items.Add(new ListItem(string.Format("{0} [votes: {1}]", thisGame.Title, thisGame.Votes), thisGame.Id.ToString()));
                }

            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_FETCH_GAMES, true);
            }
            finally
            {
                // show the correct panels again after data is fetched.
                loadingPanel.Visible = false;
                ownedGamesPanel.Visible = (listOfOwnedGames.Items.Count > 0);
                voteGamesPanel.Visible = (listOfVotedGames.Items.Count > 0);
                noOwnedGamesPanel.Visible = !ownedGamesPanel.Visible;
                noVotedGamesPanel.Visible = !voteGamesPanel.Visible;
            }
            
            // disable the buttons if it's not a Voting day
            btnAddNewGameTitle.Enabled = IsVotingAllowedToday() && CanUserVoteToday();
            btnVoteForGame.Enabled = IsVotingAllowedToday() && CanUserVoteToday();

            if (!IsVotingAllowedToday())
            {
                ShowMessage(ERROR_MESSAGE_NO_VOTES_TODAY, true);
            }
            else if (!CanUserVoteToday())
            {
                ShowMessage(ERROR_MESSAGE_VOTE_CAST, true);
            }

        }

        /// <summary>
        /// Check that votes / submissions are allowed today
        /// </summary>
        /// <returns>True if votes are allowed</returns>
        private bool IsVotingAllowedToday()
        {
            return businessManager.AreEntriesAllowedRightNow();
        }

        /// <summary>
        /// Check the user's last vote to see if they could vote again.
        /// </summary>
        /// <returns>True if user can vote today</returns>
        private bool CanUserVoteToday()
        {
            DateTime usersLastEntryDateUtc;
            DateTime parsedDate;

            HttpCookie storeCookie = Request.Cookies[COOKIE_PARENT];
            if (storeCookie != null && (DateTime.TryParse(storeCookie.Values[COOKIE_LAST_ENTRY_UTC], out parsedDate)))
            {
                // found the cookie and the value!
                usersLastEntryDateUtc = parsedDate;
            }
            else
            {
                // no cookie found, this might be the user's first time to the site                
                // assign the default date of last entry, easily changed depending on how trusting we want to be.
                usersLastEntryDateUtc = defaultLastEntryDateUtc;
            }

            return businessManager.IsThisUserAbleToAddNewEntry(usersLastEntryDateUtc);            
        }

        /// <summary>
        /// Store the last vote / submission time for this user.
        /// </summary>
        private void UpdateUserVotingHistory()
        {
            // wipe any old cookies
            Response.Cookies.Remove(COOKIE_PARENT);

            // Add new cookie of last entry
            HttpCookie storeCookie = new HttpCookie(COOKIE_PARENT)
            {
                Expires = DateTime.Now.AddDays(7),
                HttpOnly = true
            };

            // add the last entry date as an attribute
            storeCookie.Values[COOKIE_LAST_ENTRY_UTC] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            Response.Cookies.Add(storeCookie);
        }


        /// <summary>
        /// Show a message to the user.
        /// </summary>
        /// <param name="messageText">Text of the message body</param>
        /// <param name="isErrorMessage">highlight if it's an error message</param>
        private void ShowMessage(string messageText, bool isErrorMessage)
        {
            resultMessage.InnerText = messageText;
            resultMessage.Style["color"] = isErrorMessage ? "red" : "green";
            messagePanel.Visible = true;
        }

        /// <summary>
        /// Add a new game title.
        /// </summary>
        private void AddNewTitle()
        {
            // NOTE: at this point, the button could only be clicked if the user was able to vote,
            // and voting was also allowed that day. If we're not certain this is true, re-apply the checks here
            
            // we can add a new title today. fetch the string, cleanly.
            string newGameTitle = HttpUtility.HtmlEncode(newGameTitleInput.Value.Trim());
            string successMessage = string.Format("Game [{0}] successfully added!", newGameTitle);

            try
            {
                var successfullyAdded = businessManager.AddNewGameToVotingList(newGameTitle);

                ShowMessage(successMessage, !successfullyAdded);

                if (successfullyAdded)
                {
                    UpdateUserVotingHistory();
                }

                RefreshAndPopulateListOfGames();
            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_ADD_NEW_TITLE, true);
            }

        }

        /// <summary>
        /// Vote for a game.
        /// </summary>
        private void VoteForAGame()
        {
            // NOTE: at this point, the button could only be clicked if the user was able to vote,
            // and voting was also allowed that day. If we're not certain this is true, re-apply the checks here

            // we can vote today. fetch the details needed to vote
            int selectedIndex = listOfVotedGames.SelectedIndex;
            int gameId;
            if (!int.TryParse(listOfVotedGames.Items[selectedIndex].Value, out gameId) || gameId < 0)
            {
                ShowMessage(ERROR_MESSAGE_CHOOSE_GAME, true);
                return;
            }

            try
            {
                var successfullyVoted = businessManager.VoteForGame(gameId);
                string message = successfullyVoted ? "Voted successfully!" : ERROR_MESSAGE_VOTE_GAME;
                ShowMessage(message, !successfullyVoted);
                
                if (successfullyVoted)
                {
                    UpdateUserVotingHistory();
                }

                RefreshAndPopulateListOfGames();
            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_VOTE_GAME, true);
            }
        }

    }
}