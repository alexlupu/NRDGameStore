<%@ Page Title="Xbox Games Store" Language="C#" MasterPageFile="~/JSCheck.Master" AutoEventWireup="true" CodeBehind="StorePage.aspx.cs" Inherits="NRDStoreFormsUI.StorePage" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadPlaceholder" runat="server">
    <script type="text/javascript">
        function VerifyVoteForGame() {
            return(confirm("Vote for this game?") === true);
        }

        function VerifyNewGameTitle() {
            return (confirm("Add this new title?") === true);
        }
    </script>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="BodyPlaceholder" runat="server">
    <h1>XBOX Game Store</h1>

    <asp:Panel runat="server" id="messagePanel" Visible="False">
        <div runat="server" id="resultMessage"></div>
        <br/>
        <br/>
    </asp:Panel>
    
    <asp:Panel runat="server" id="loadingPanel" Visible="False">
        <div>Loading games...</div>
    </asp:Panel>

    <asp:Panel runat="server" ID="allGamesPanel">
        <asp:Panel runat="server" id="ownedGamesPanel" Visible="False">
            <label>Games we own:</label>
            <select id="listOfOwnedGames" name="listOfOwnedGames" runat="server"></select>
        </asp:Panel>
        <asp:Panel runat="server" id="noOwnedGamesPanel" Visible="False">
            <div>No owned games found. Consider lowering your productivity goals?</div>
        </asp:Panel>
        <br/>
        <hr/>
        <br/>
        <asp:Panel runat="server" id="voteGamesPanel" Visible="False">
            <label>Vote for your favourite game:</label>
            <select id="listOfVotedGames" name="listOfVotedGames" runat="server"></select>
            <asp:Button runat="server" ID="btnVoteForGame" OnClick="VoteForGame_OnClick" OnClientClick="return VerifyVoteForGame();" Text="Vote for this game"/>
        </asp:Panel>
        <asp:Panel runat="server" id="noVotedGamesPanel" Visible="False">
            <div>No voted games found. Add a new game suggestion below!</div>
        </asp:Panel>
    </asp:Panel>    
    <br/>
    <hr/>
    <br/>
    <asp:Panel runat="server" id="addNewTitlePanel" Visible="True">
        <label>Suggest a new game title:</label>
        <input type="text" name="newGameTitleInput" id="newGameTitleInput" runat="server" ValidateRequestMode="Disabled"/>
        <asp:Button runat="server" ID="btnAddNewGameTitle" OnClick="AddNewGameTitle_OnClick" OnClientClick="return VerifyNewGameTitle();" Text="Add this title"/>
    </asp:Panel>
    
</asp:Content>