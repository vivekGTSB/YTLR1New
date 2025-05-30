Imports System.Data.SqlClient
Imports System.Web.Security
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography

Namespace AVLS
    Partial Class Login
        Inherits System.Web.UI.Page
        Private Const MaxLoginAttempts As Integer = 5
        Private Const LockoutDuration As Integer = 15 ' minutes
        
        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Try
                ' Security headers
                Response.Headers.Add("X-Content-Security-Policy", "default-src 'self'")
                Response.Headers.Add("X-Frame-Options", "DENY")
                
                ' Prevent caching
                Response.Cache.SetCacheability(HttpCacheability.NoCache)
                Response.Cache.SetNoStore()
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1))
                
                ' Generate CSRF token
                If Not IsPostBack Then
                    Dim csrfToken As String = GenerateCSRFToken()
                    Session("CSRFToken") = csrfToken
                End If
                
                ' Clear existing authentication
                FormsAuthentication.SignOut()
                Session.Abandon()
                
                ' Clear authentication cookie
                Dim authCookie As New HttpCookie(FormsAuthentication.FormsCookieName, "")
                authCookie.Expires = DateTime.Now.AddYears(-1)
                Response.Cookies.Add(authCookie)
                
                ' Clear session cookie
                Dim sessionCookie As New HttpCookie("ASP.NET_SessionId", "")
                sessionCookie.Expires = DateTime.Now.AddYears(-1)
                Response.Cookies.Add(sessionCookie)

            Catch ex As Exception
                LogError(ex)
                ShowError("An error occurred. Please try again.")
            End Try
        End Sub

        Protected Sub ImageButton1_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles ImageButton1.Click
            Try
                ' Verify CSRF token
                If Not VerifyCSRFToken() Then
                    ShowError("Invalid request. Please try again.")
                    Return
                End If
                
                ' Check for brute force attempts
                If IsAccountLocked() Then
                    ShowError("Account is temporarily locked. Please try again later.")
                    Return
                End If
                
                ' Validate input
                If Not ValidateInput() Then
                    IncrementLoginAttempts()
                    Return
                End If
                
                Using conn As New SqlConnection(GetEncryptedConnectionString())
                    Dim cmd As New SqlCommand("sp_ValidateUser", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    
                    ' Parameters
                    cmd.Parameters.AddWithValue("@Username", SqlHelper.EscapeInput(uname.Value))
                    cmd.Parameters.AddWithValue("@Password", HashPassword(password.Value))
                    cmd.Parameters.AddWithValue("@IPAddress", Request.UserHostAddress)
                    
                    Try
                        conn.Open()
                        Using dr As SqlDataReader = cmd.ExecuteReader()
                            If dr.Read() Then
                                If ValidateUserAccount(dr) Then
                                    ProcessSuccessfulLogin(dr)
                                Else
                                    HandleFailedLogin()
                                End If
                            Else
                                HandleFailedLogin()
                            End If
                        End Using
                    Catch ex As SqlException
                        LogError(ex)
                        ShowError("Database error occurred. Please try again later.")
                    End Try
                End Using
            Catch ex As Exception
                LogError(ex)
                ShowError("An unexpected error occurred. Please try again later.")
            End Try
        End Sub

        Private Function ValidateInput() As Boolean
            ' Input validation
            If String.IsNullOrEmpty(uname.Value) OrElse String.IsNullOrEmpty(password.Value) Then
                ShowError("Please enter both username and password.")
                Return False
            End If
            
            ' Username validation
            If Not Regex.IsMatch(uname.Value, "^[a-zA-Z0-9_-]{3,20}$") Then
                ShowError("Invalid username format.")
                Return False
            End If
            
            ' Password complexity
            If Not Regex.IsMatch(password.Value, "^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$") Then
                ShowError("Invalid password format.")
                Return False
            End If
            
            Return True
        End Function

        Private Function HashPassword(password As String) As String
            Using sha256 As SHA256 = SHA256.Create()
                ' Add salt and pepper here in production
                Dim hashedBytes As Byte() = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password))
                Return Convert.ToBase64String(hashedBytes)
            End Function
        End Function

        Private Function GenerateCSRFToken() As String
            Using rng As New RNGCryptoServiceProvider()
                Dim tokenBytes As Byte() = New Byte(31) {}
                rng.GetBytes(tokenBytes)
                Return Convert.ToBase64String(tokenBytes)
            End Using
        End Function

        Private Function VerifyCSRFToken() As Boolean
            Dim sessionToken As String = Session("CSRFToken")?.ToString()
            Dim requestToken As String = Request.Form("csrf_token")?.ToString()
            Return Not String.IsNullOrEmpty(sessionToken) AndAlso sessionToken.Equals(requestToken)
        End Function

        Private Function IsAccountLocked() As Boolean
            Dim attempts As Integer = TryGetLoginAttempts()
            If attempts >= MaxLoginAttempts Then
                Dim lastAttempt As DateTime = Session("LastLoginAttempt")
                If DateTime.Now.Subtract(lastAttempt).TotalMinutes < LockoutDuration Then
                    Return True
                Else
                    ResetLoginAttempts()
                End If
            End If
            Return False
        End Function

        Private Sub IncrementLoginAttempts()
            Dim attempts As Integer = TryGetLoginAttempts() + 1
            Session("LoginAttempts") = attempts
            Session("LastLoginAttempt") = DateTime.Now
            
            If attempts >= MaxLoginAttempts Then
                ShowError($"Account locked. Please try again after {LockoutDuration} minutes.")
            Else
                ShowError($"Invalid login attempt {attempts} of {MaxLoginAttempts}.")
            End If
        End Sub

        Private Function TryGetLoginAttempts() As Integer
            Dim attempts As Object = Session("LoginAttempts")
            If attempts IsNot Nothing Then
                Return CInt(attempts)
            End If
            Return 0
        End Function

        Private Sub ResetLoginAttempts()
            Session("LoginAttempts") = 0
            Session.Remove("LastLoginAttempt")
        End Sub

        Private Sub ProcessSuccessfulLogin(dr As SqlDataReader)
            ' Reset login attempts
            ResetLoginAttempts()
            
            ' Create new session
            Session.Clear()
            Session.Regenerate()
            
            ' Set authentication ticket
            Dim ticket As New FormsAuthenticationTicket(
                1,
                dr("username").ToString(),
                DateTime.Now,
                DateTime.Now.AddMinutes(30),
                False,
                dr("role").ToString(),
                FormsAuthentication.FormsCookiePath)
            
            ' Encrypt ticket and create cookie
            Dim encryptedTicket As String = FormsAuthentication.Encrypt(ticket)
            Dim authCookie As New HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket) With {
                .HttpOnly = True,
                .Secure = True,
                .SameSite = SameSiteMode.Strict,
                .Path = FormsAuthentication.FormsCookiePath
            }
            
            Response.Cookies.Add(authCookie)
            
            ' Log successful login
            LogSuccessfulLogin(dr("userid").ToString())
            
            ' Redirect to default page
            Response.Redirect(FormsAuthentication.GetRedirectUrl(dr("username").ToString(), False), True)
        End Sub

        Private Sub HandleFailedLogin()
            IncrementLoginAttempts()
            LogFailedLogin(uname.Value, Request.UserHostAddress)
        End Sub

        Private Sub ShowError(message As String)
            ErrorPanel.Visible = True
            ErrorMessage.Text = message
        End Sub

        Private Sub LogError(ex As Exception)
            ' Implement secure error logging
            ' TODO: Add proper logging mechanism
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}")
        End Sub

        Private Sub LogSuccessfulLogin(userId As String)
            ' Implement audit logging
            ' TODO: Add proper audit logging
        End Sub

        Private Sub LogFailedLogin(username As String, ipAddress As String)
            ' Implement failed login logging
            ' TODO: Add proper security logging
        End Sub

        Private Function GetEncryptedConnectionString() As String
            ' TODO: Implement proper connection string encryption
            Return System.Configuration.ConfigurationManager.AppSettings("sqlserverconnection")
        End Function
    End Class
End Namespace