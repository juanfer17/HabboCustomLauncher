Imports System.IO
Imports System.Security.Principal
Imports System.Xml
Imports Microsoft.Win32

Class MainWindow
    Public RequestedURI As String = ""
    Public RequestedServer As String = ""
    Public RequestedTicket As String = ""
    Public CurrentLanguageInt As Integer = 0
    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If System.Globalization.CultureInfo.CurrentCulture.Name.ToLower.StartsWith("es") Then
            CurrentLanguageInt = 1
        End If
        IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
        Dim RenderModeHandled As Boolean = False
        If My.Settings.RenderMode = "gpu" Then
            RenderModeHandled = True
            GPURenderButton.IsChecked = True
        End If
        If My.Settings.RenderMode = "cpu" Then
            RenderModeHandled = True
            CPURenderButton.IsChecked = True
        End If
        If RenderModeHandled = False Then
            My.Settings.RenderMode = "direct"
            My.Settings.Save()
            DefaultRenderButton.IsChecked = True
        End If
        StartNewInstanceButton.Content = AppTranslator.NewInstance(CurrentLanguageInt)
        UpdateProtocolButton()
        If CheckWritePermissions(GetClientPath) = False Then
            If UserIsAdmin() = False Then
                RestartElevated()
            End If
        End If
        If RequestedURI = "" = False Then
            StartNewInstanceButton_Click(Nothing, Nothing)
        End If
    End Sub

    Function CheckWritePermissions(Path As String) As Boolean
        Try
            IO.File.WriteAllText(Path & "\LauncherPermissionTEST", "")
            IO.File.Delete(Path & "\LauncherPermissionTEST")
            Return True
        Catch
            Return False
        End Try
    End Function

    Function GetClientPath() As String
        Dim ProgramFilesAppPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) & "\Sulake\Habbo Launcher\HabboFlash"
        If IO.Directory.Exists("META-INF\AIR") Then
            Return Directory.GetCurrentDirectory
        End If
        If Directory.Exists(ProgramFilesAppPath & "\META-INF\AIR") Then
            Return ProgramFilesAppPath
        End If
        MsgBox(AppTranslator.ClientNotFound(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
        Environment.Exit(0)
    End Function

    Function UserIsAdmin() As Boolean
        Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
        Dim principal As WindowsPrincipal = New WindowsPrincipal(identity)
        Return principal.IsInRole(WindowsBuiltInRole.Administrator)
    End Function

    Private Sub RestartElevated()
        Try
            Dim info As ProcessStartInfo = New ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
            info.UseShellExecute = True
            info.Verb = "runas"
            If RequestedURI = "" = False Then
                info.Arguments = RequestedURI
            End If
            Process.Start(info)
            Environment.Exit(0)
        Catch
            MsgBox(AppTranslator.AdminRightsError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
            Environment.Exit(0)
        End Try
    End Sub

    Private Sub GPURenderButton_Click(sender As Object, e As RoutedEventArgs) Handles GPURenderButton.Click
        My.Settings.RenderMode = "gpu"
        My.Settings.Save()
    End Sub

    Private Sub CPURenderButton_Click(sender As Object, e As RoutedEventArgs) Handles CPURenderButton.Click
        My.Settings.RenderMode = "cpu"
        My.Settings.Save()
    End Sub

    Private Sub DefaultRenderButton_Click(sender As Object, e As RoutedEventArgs) Handles DefaultRenderButton.Click
        My.Settings.RenderMode = "direct"
        My.Settings.Save()
    End Sub

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function ShowWindow(
        ByVal hWnd As System.IntPtr,
        ByVal nCmdShow As Integer) As Integer
    End Function
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function IsZoomed(hWnd As IntPtr) As Boolean
    End Function

    Private Sub StartNewInstanceButton_Click(sender As Object, e As RoutedEventArgs) Handles StartNewInstanceButton.Click
        Try
            Dim ClientXMLPath As String = "META-INF\AIR\application.xml"
            If IO.File.Exists(ClientXMLPath) = False Then
                ClientXMLPath = GetClientPath() & "\" & ClientXMLPath
            End If
            Dim OriginalClientXML = New XmlDocument()
            OriginalClientXML.Load(ClientXMLPath)
            OriginalClientXML("application")("initialWindow")("renderMode").InnerText = My.Settings.RenderMode
            Dim NextInstanceInt = GetNextInstanceInt()
            If NextInstanceInt = 0 Then
                OriginalClientXML("application")("id").InnerText = "com.sulake.habboair"
            Else
                OriginalClientXML("application")("id").InnerText = "com.sulake.habboair" & NextInstanceInt
            End If
            OriginalClientXML.Save(ClientXMLPath)
            Dim ClientProcess As New Process
            ClientProcess.StartInfo.FileName = GetClientPath() & "\Habbo.exe"
            ClientProcess.StartInfo.WorkingDirectory = GetClientPath()
            If RequestedTicket = "" = False Then
                ClientProcess.StartInfo.Arguments = "-server " & RequestedServer & " -ticket " & RequestedTicket
            End If
            ClientProcess.Start()
        Catch ex As Exception
            MsgBox(AppTranslator.ClientOpenError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
        End Try
        If RequestedURI = "" = False Then
            Environment.Exit(0)
        End If
    End Sub

    Public Sub RegisterHabboProtocol()
        Try
            Dim UriScheme = "habbo"
            Dim FriendlyName = "Habbo Custom Launcher"
            Dim applicationLocation As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Using key = Registry.CurrentUser.CreateSubKey("SOFTWARE\Classes\" & UriScheme)
                key.SetValue("", "URL:" & FriendlyName)
                key.SetValue("URL Protocol", "")

                Using defaultIcon = key.CreateSubKey("DefaultIcon")
                    defaultIcon.SetValue("", applicationLocation & ",1")
                End Using

                Using commandKey = key.CreateSubKey("shell\open\command")
                    commandKey.SetValue("", """" & applicationLocation & """ ""%1""")
                End Using
            End Using
        Catch
            MsgBox(AppTranslator.ProtocolRegError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
        End Try
    End Sub

    Function UpdateProtocolButton()
        If CheckHabboProtocol() Then
            RegisterAppProtocolButton.Content = AppTranslator.UnregisterProtocol(CurrentLanguageInt)
        Else
            RegisterAppProtocolButton.Content = AppTranslator.RegisterProtocol(CurrentLanguageInt)
        End If
    End Function

    Public Function CheckHabboProtocol() As Boolean
        Try
            Dim UriScheme = "habbo"
            Dim applicationLocation As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Using key = Registry.CurrentUser.OpenSubKey("SOFTWARE\Classes\" & UriScheme)
                Using commandKey = key.OpenSubKey("shell\open\command")
                    If commandKey.GetValue("").ToString.Contains(applicationLocation) Then
                        Return True
                    Else
                        Return False
                    End If
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    Public Function UnregisterHabboProtocol() As Boolean
        Try
            Dim UriScheme = "habbo"
            Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\Classes\" & UriScheme)
        Catch
            MsgBox(AppTranslator.ProtocolUnregError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
        End Try
    End Function

    Function GetNextInstanceInt() As Integer
        If Process.GetProcessesByName("Habbo").Count > 0 Then
            My.Settings.LastInstance += 1
        Else
            My.Settings.LastInstance = 0
        End If
        My.Settings.Save()
        Return My.Settings.LastInstance
    End Function

    Private Sub RegisterAppProtocolButton_Click(sender As Object, e As RoutedEventArgs) Handles RegisterAppProtocolButton.Click
        If RegisterAppProtocolButton.Content = AppTranslator.RegisterProtocol(CurrentLanguageInt) Then
            RegisterHabboProtocol()
        Else
            UnregisterHabboProtocol()
        End If
        UpdateProtocolButton()
    End Sub
End Class
Public Class AppTranslator
    Public Shared ClientNotFound As String() = {"Habbo Client not found.", "Habbo Client no encontrado."}
    Public Shared AdminRightsError As String() = {"You need administrator rights.", "Necesitas permisos de administrador."}
    Public Shared ClientOpenError As String() = {"Could not open Habbo Client.", "No se pudo abrir Habbo Client."}
    Public Shared ProtocolRegError As String() = {"Could not register protocol.", "No se pudo registrar el protocolo."}
    Public Shared ProtocolUnregError As String() = {"Could not unregister protocol.", "No se pudo eliminar el protocolo."}
    Public Shared RegisterProtocol As String() = {"Register Habbo Protocol", "Registrar Habbo Protocol"}
    Public Shared UnregisterProtocol As String() = {"Register Habbo Protocol", "Eliminar Habbo Protocol"}
    Public Shared NewInstance As String() = {"Start new instance", "Iniciar nueva instancia"}
End Class