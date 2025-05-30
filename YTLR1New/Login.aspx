<%@ Page Language="VB" AutoEventWireup="false" EnableEventValidation="true" ViewStateEncryption="true"
    Inherits="YTLWebApplication.AVLS.Login" Codebehind="Login.aspx.vb" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>YTL - AVLS</title>
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="robots" content="noindex, nofollow" />
    <meta name="referrer" content="no-referrer" />
    
    <script type="text/javascript" nonce="<%=Request.GetNonce()%>">
        function getWindowWidth() { if (window.self && self.innerWidth) { return self.innerWidth; } if (document.documentElement && document.documentElement.clientWidth) { return document.documentElement.clientWidth; } return document.documentElement.offsetWidth; }
        function getWindowHeight() { if (window.self && self.innerHeight) { return self.innerHeight; } if (document.documentElement && document.documentElement.clientHeight) { return document.documentElement.clientHeight; } return document.documentElement.offsetHeight; }
        function mysubmit() { 
            var username = document.getElementById("uname");
            var password = document.getElementById("password");
            
            if (!username.value.trim()) { 
                alert("Please enter user name"); 
                username.focus();
                return false; 
            } 
            if (!password.value.trim()) { 
                alert("Please enter password"); 
                password.focus();
                return false; 
            }
            
            // Add CSRF token
            document.getElementById("csrf_token").value = '<%=Session("CSRFToken")%>';
            
            document.getElementById("w").value = getWindowWidth(); 
            document.getElementById("h").value = getWindowHeight(); 
            
            // Clear password from memory after submission
            setTimeout(function() {
                password.value = '';
            }, 100);
            
            return true; 
        }
    </script>
    
    <style type="text/css">
        body {
            font-size: 11px;
            font-family: Verdana, Arial, Helvetica, sans-serif;
            margin: 0;
            background: url("images/blurred-bg-1.jpg") no-repeat center center fixed;
            background-size: cover;
        }
        .login-container {
            background: rgba(255, 255, 255, 0.95);
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            max-width: 400px;
            margin: 50px auto;
        }
        .input-field {
            width: 100%;
            padding: 8px;
            margin: 5px 0;
            border: 1px solid #ccc;
            border-radius: 4px;
            box-sizing: border-box;
        }
        .btn {
            background: #4E6ABD;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
        }
        .btn:hover {
            background: #3952F9;
        }
    </style>
</head>
<body>
    <form id="loginform" runat="server" defaultbutton="ImageButton1" autocomplete="off">
        <div class="login-container">
            <div style="text-align: center; margin-bottom: 20px;">
                <img src="images/ytl-logo.png" alt="YTL Logo" style="max-width: 200px;" />
            </div>
            
            <div>
                <label for="uname">Username</label>
                <input id="uname" runat="server" type="text" class="input-field" 
                    maxlength="50" autocomplete="off" />
            </div>
            
            <div>
                <label for="password">Password</label>
                <input id="password" runat="server" type="password" class="input-field" 
                    maxlength="50" autocomplete="off" />
            </div>
            
            <div style="margin-top: 15px; text-align: center;">
                <asp:ImageButton ID="ImageButton1" runat="server" 
                    CssClass="btn" Text="Login" />
            </div>
            
            <!-- Hidden fields -->
            <input type="hidden" name="csrf_token" id="csrf_token" value="" />
            <input type="hidden" name="w" id="w" value="" />
            <input type="hidden" name="h" id="h" value="" />
            <input type="hidden" name="lat" id="lat" value="" />
            <input type="hidden" name="lon" id="lon" value="" />
            <input type="hidden" name="acc" id="acc" value="" />
            
            <asp:Panel ID="ErrorPanel" runat="server" Visible="false" 
                CssClass="error-message" style="color: red; margin-top: 10px; text-align: center;">
                <asp:Literal ID="ErrorMessage" runat="server"></asp:Literal>
            </asp:Panel>
        </div>
    </form>
    
    <script type="text/javascript" nonce="<%=Request.GetNonce()%>">
        // Prevent form resubmission
        if (window.history.replaceState) {
            window.history.replaceState(null, null, window.location.href);
        }
        
        // Get geolocation if available
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function(position) {
                document.getElementById("lat").value = position.coords.latitude;
                document.getElementById("lon").value = position.coords.longitude;
                document.getElementById("acc").value = position.coords.accuracy;
            });
        }
        
        // Focus username field
        document.getElementById("uname").focus();
        
        // Prevent browser password autofill
        document.getElementById("password").value = '';
    </script>
</body>
</html>