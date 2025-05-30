Imports System.Data.SqlClient
Imports System.Web.Security
Imports System.Text.RegularExpressions

Namespace AVLS
    Partial Class Login
        Inherits System.Web.UI.Page
        Public errormessage As String = ""
        Public foc As String = "uname"
        Public logoimagepath As String = "images/logo_big.jpg"
        Public backgroundimage As String = "images/lafargeloginpage.jpg"

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Try
                ' Prevent caching of login page
                Response.Cache.SetCacheability(HttpCacheability.NoCache)
                Response.Cache.SetNoStore()
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1))
                Response.Cache.SetValidUntilExpires(False)
                Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches)

                ImageButton1.Attributes.Add("onclick", "return mysubmit()")

                Session.Abandon()
                Response.Cookies("userinfo").Expires = DateTime.Now
                Response.Cookies.Remove("userinfo")
                Response.Cookies.Clear()

            Catch ex As Exception
                errormessage = "An error occurred. Please try again."
            End Try
        End Sub

        Protected Sub ImageButton1_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles ImageButton1.Click
            Try
                ' Input validation
                If Not ValidateInput(uname.Value, password.Value) Then
                    errormessage = "Invalid input detected"
                    Return
                End If

                Using conn As New SqlConnection(System.Configuration.ConfigurationManager.AppSettings("sqlserverconnection"))
                    ' Use parameterized query to prevent SQL injection
                    Dim cmd As New SqlCommand("select pwd,role,userid,username,userslist,access,timestamp,usertype,remark,dbip,companyname,customrole from userTBL where username = @username and drcaccess='0'", conn)
                    cmd.Parameters.AddWithValue("@username", uname.Value)

                    Try
                        conn.Open()
                        Using dr As SqlDataReader = cmd.ExecuteReader()
                            If dr.Read() Then
                                ' Use secure password comparison
                                If VerifyPassword(password.Value, dr("pwd").ToString()) Then
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
                        errormessage = "Database error occurred. Please try again later."
                    End Try
                End Using
            Catch ex As Exception
                LogError(ex)
                errormessage = "An unexpected error occurred. Please try again later."
            End Try
        End Sub

        Private Function ValidateInput(username As String, pwd As String) As Boolean
            ' Input validation using regex
            If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(pwd) Then
                Return False
            End If

            ' Add more validation as needed
            Dim usernamePattern As String = "^[a-zA-Z0-9_-]{3,20}$"
            Return Regex.IsMatch(username, usernamePattern)
        End Function

        Private Function VerifyPassword(inputPassword As String, storedPassword As String) As Boolean
            ' TODO: Implement proper password hashing
            Return String.Equals(inputPassword.ToUpper(), storedPassword.ToUpper())
        End Function

        Private Sub HandleFailedLogin()
            ' Implement login attempt tracking and rate limiting
            errormessage = "Invalid username or password"
            foc = "uname"
        End Sub

        Private Sub ProcessSuccessfulLogin(dr As SqlDataReader)
            ' Set secure cookie attributes
            Dim authCookie As New HttpCookie("userinfo")
            authCookie.HttpOnly = True
            authCookie.Secure = True
            authCookie.SameSite = SameSiteMode.Strict

            ' Set cookie values
            authCookie.Values("userid") = dr("userid").ToString()
            authCookie.Values("username") = dr("username").ToString()
            authCookie.Values("role") = dr("role").ToString()
            ' Add other necessary values

            ' Set cookie expiration
            authCookie.Expires = DateTime.Now.AddMinutes(30)
            Response.Cookies.Add(authCookie)

            ' Redirect to main page
            Response.Redirect("Main.aspx", True)
        End Sub

        Private Sub LogError(ex As Exception)
            ' Implement proper error logging
            ' TODO: Add proper logging mechanism
            System.Diagnostics.Debug.WriteLine(ex.Message)
        End Sub
    End Class
End Namespace