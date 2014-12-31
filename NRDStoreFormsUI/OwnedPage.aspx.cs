using System;
using System.Linq;
using System.Web;
using NRDXboxStore.Managers;

namespace NRDStoreFormsUI
{
    public partial class OwnedPage : System.Web.UI.Page
    {
        #region Fields
        /// <summary>
        /// local business manager instance
        /// </summary>
        private readonly BusinessManager businessManager = new BusinessManager();

        /// <summary>
        /// Error connecting to service
        /// </summary>
        private const string ERROR_MESSAGE_CONNECTION = "Error connecting to the service. Please check the connection strings and try again.";

        /// <summary>
        /// Error message when failing to mark game as owned
        /// </summary>
        private const string ERROR_MESSAGE_MARK_OWNED = "Error marking game as owned. Please refresh the page and try again.";

        /// <summary>
        /// Error when fetching the list of owned games
        /// </summary>
        private const string ERROR_MESSAGE_FETCH_GAMES = "Error fetching the list of owned games. Please try again.";

        /// <summary>
        /// Error choosing a game from dropdown
        /// </summary>
        private const string ERROR_MESSAGE_CHOOSE_GAME = "Error selecting a game. Choose a game from the drop-down and try again.";

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

        protected void MarkVotedGameAsOwned_OnClick(object sender, EventArgs e)
        {
            MarkGameAsOwned();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Set up the page for initial load and refresh 
        /// </summary>
        private void SetUpPage()
        {
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
            noGamesPanel.Visible = false;

            try
            {
                // fetch the list of wanted games
                var allVotedGames = businessManager.GetAllGames()
                                                   .Where(game => game.Status == BusinessManager.GAME_STATUS_WANTED);

                // bind data to list
                listOfGames.DataTextField = "Title";
                listOfGames.DataValueField = "Id";
                listOfGames.DataSource = allVotedGames;
                listOfGames.DataBind();
            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_FETCH_GAMES, true);
            }
            finally
            {
                // show the correct panels again after data is fetched.
                loadingPanel.Visible = false;
                ownedGamesPanel.Visible = (listOfGames.Items.Count > 0);
                noGamesPanel.Visible = !ownedGamesPanel.Visible;
            }

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
        /// Mark the selected game as owned.
        /// </summary>
        private void MarkGameAsOwned()
        {
            int selectedIndex = listOfGames.SelectedIndex;
            int gameId;
            if (!int.TryParse(listOfGames.Items[selectedIndex].Value, out gameId)|| gameId < 0)
            {
                ShowMessage(ERROR_MESSAGE_CHOOSE_GAME, true);
                return;
            }
            
            // store and HTML-encode the name, since we're rendering it on the page.
            string gameTitle =  HttpUtility.HtmlEncode(listOfGames.Items[selectedIndex].Text);
            string successMessage = string.Format("Game [{0}] successfully marked as owned!", gameTitle);
            
            try
            {
                bool successfullyAdded = businessManager.MarkExistingGameAsOwned(gameId);
                string thisMessage = successfullyAdded ? successMessage : ERROR_MESSAGE_MARK_OWNED;
                ShowMessage(thisMessage, !successfullyAdded);
            }
            catch (Exception)
            {
                ShowMessage(ERROR_MESSAGE_MARK_OWNED, true);
                return;
            }

            RefreshAndPopulateListOfGames();

        }

        #endregion

    }
}