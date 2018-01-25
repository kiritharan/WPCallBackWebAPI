<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="MessengerBot.Pages.Hello" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Wlecome to Messanger Bot
    </div>
        <div>
            <asp:Button ID="cleanCache" runat="server" Text="Clean Cache" OnClick="cleanCache_Click" />
        </div>
    </form>
</body>
</html>
