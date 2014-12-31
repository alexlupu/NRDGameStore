<%@ Page Title="Owned Xbox Games" Language="C#" MasterPageFile="~/JSCheck.Master" AutoEventWireup="true" CodeBehind="OwnedPage.aspx.cs" Inherits="NRDStoreFormsUI.OwnedPage" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadPlaceholder" runat="server">
    <script type="text/javascript">
        function VerifyOwnedGame() {
            return(confirm("Mark game as owned?") === true);
        }
    </script>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="BodyPlaceholder" runat="server">
    <h1>Owned Xbox Games</h1>
    
    <asp:Panel runat="server" id="messagePanel" Visible="False">
        <div runat="server" id="resultMessage"></div>
    </asp:Panel>
    
    <asp:Panel runat="server" id="loadingPanel" Visible="False">
        <div>Loading games...</div>
    </asp:Panel>

    <asp:Panel runat="server" id="ownedGamesPanel" Visible="False">
        <label>Please select the game that we now own:</label>
        <select id="listOfGames" name="listOfGames" runat="server"></select>
        <asp:Button runat="server" ID="btnMarkVotedGameAsOwned" OnClick="MarkVotedGameAsOwned_OnClick" OnClientClick="return VerifyOwnedGame();" Text="Mark As Owned"/>
        <br/>
        <br/>
        <div>Purchased a game that is not on this list? Make sure to add it to the <a href="/StorePage.aspx">Store Page</a> first and then reload this page to continue.</div>
    </asp:Panel>
    
    <asp:Panel runat="server" id="noGamesPanel" Visible="False">
        <div>No more unpurchased games found. Add a new favorite in the <a href="/StorePage.aspx">Store Page</a> ! </div>
    </asp:Panel>
    
</asp:Content>
