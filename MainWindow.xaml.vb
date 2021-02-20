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
        If System.Globalization.CultureInfo.CurrentCulture.Name.ToLower.StartsWith("pt") Then
            CurrentLanguageInt = 2
        End If
        IO.Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
        My.Settings.Reset()
        My.Settings.Save()
        If (My.Settings.ClientVersion = GetClientVersion()) = False Then
            My.Settings.Reset()
            My.Settings.ClientVersion = GetClientVersion()
            My.Settings.Save()
            If CheckHabboProtocol() = False Then
                Dim Result As MessageBoxResult = MessageBox.Show(AppTranslator.ProtocolRegAdvice(CurrentLanguageInt), Me.Title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                If Result = MessageBoxResult.Yes Then
                    If RegisterHabboProtocol() = True Then
                        MsgBox(AppTranslator.ProtocolRegAdviceOK(CurrentLanguageInt), MsgBoxStyle.Information, Me.Title)
                        Environment.Exit(0)
                    End If
                End If
            End If
        End If
        If My.Settings.RenderMode = "cpu" Then
            CPURenderButton.IsChecked = True
        End If
        If My.Settings.RenderMode = "direct" Then
            DirectRenderButton.IsChecked = True
        End If
        If My.Settings.RenderMode = "gpu" Then
            GPURenderButton.IsChecked = True
        End If
        StartNewInstanceButton.Content = AppTranslator.NewInstance(CurrentLanguageInt)
        UpdateProtocolButton()
        FixWindowsTLS()
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

    Function GetClientVersion() As String
        Try
            Dim ClientXMLPath As String = GetClientPath() & "\META-INF\AIR\application.xml"
            Dim OriginalClientXML = New XmlDocument()
            OriginalClientXML.Load(ClientXMLPath)
            Return OriginalClientXML("application")("versionLabel").InnerText
        Catch
            Return "null"
        End Try
    End Function

    Function GetClientPath() As String
        Dim ProgramFilesAppPath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\Sulake\Habbo Launcher\HabboFlash"
        If IO.Directory.Exists("META-INF\AIR") Then
            Return Directory.GetCurrentDirectory
        End If
        If Directory.Exists(ProgramFilesAppPath & "\META-INF\AIR") Then
            Return ProgramFilesAppPath
        End If
        If Directory.Exists(GetClientShortcutTarget() & "\META-INF\AIR") Then
            Return GetClientShortcutTarget()
        End If
        MsgBox(AppTranslator.ClientNotFound(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
        Environment.Exit(0)
    End Function

    Private Function GetClientShortcutTarget() As String
        Try
            Dim FilePath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Microsoft\Windows\Start Menu\Programs\Sulake\Habbo\Habbo Launcher.lnk"
            Dim FileContent As String = IO.File.ReadAllText(FilePath, Text.Encoding.UTF8)
            FileContent = FileContent.Remove(FileContent.LastIndexOf("\HabboLauncher.exe"))
            FileContent = FileContent.Remove(0, FileContent.LastIndexOf(":\") - 1)
            Return FileContent & "\HabboFlash"
        Catch
            Return ""
        End Try
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

    Private Sub DirectRenderButton_Click(sender As Object, e As RoutedEventArgs) Handles DirectRenderButton.Click
        My.Settings.RenderMode = "direct"
        My.Settings.Save()
    End Sub

    Private Sub CPURenderButton_Click(sender As Object, e As RoutedEventArgs) Handles CPURenderButton.Click
        My.Settings.RenderMode = "cpu"
        My.Settings.Save()
    End Sub

    Private Sub StartNewInstanceButton_Click(sender As Object, e As RoutedEventArgs) Handles StartNewInstanceButton.Click
        Try
            Dim ClientXMLPath As String = GetClientPath() & "\META-INF\AIR\application.xml"
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

    Public Function RegisterHabboProtocol() As Boolean
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
            Return True
        Catch
            MsgBox(AppTranslator.ProtocolRegError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
            Return False
        End Try
    End Function

    Public Function UnregisterHabboProtocol() As Boolean
        Try
            Dim UriScheme = "habbo"
            Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\Classes\" & UriScheme)
            Return True
        Catch
            MsgBox(AppTranslator.ProtocolUnregError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
            Return False
        End Try
    End Function

    Public Sub FixWindowsTLS()
        Try
            Using key = Registry.CurrentUser.CreateSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings")
                If key.GetValue("SecureProtocols") < 2048 Then 'johnou implementation
                    key.SetValue("SecureProtocols", key.GetValue("SecureProtocols") + 2048)
                End If
                If String.IsNullOrEmpty(key.GetValue("DefaultSecureProtocols")) = False Then
                    If key.GetValue("DefaultSecureProtocols") < 2048 Then
                        key.SetValue("DefaultSecureProtocols", key.GetValue("DefaultSecureProtocols") + 2048)
                    End If
                End If
            End Using
            Dim NeedExtraSteps As Boolean = False
            Using key = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client")
                If key Is Nothing = False Then
                    If (key.GetValue("DisabledByDefault") = "1") Or (key.GetValue("Enabled") = "0") Then
                        NeedExtraSteps = True
                    End If
                End If
            End Using
            If NeedExtraSteps = True Then
                If UserIsAdmin() Then
                    Using key = Registry.LocalMachine.CreateSubKey("SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client")
                        key.SetValue("DisabledByDefault", 0)
                        key.SetValue("Enabled", 1)
                    End Using
                Else
                    MsgBox(AppTranslator.TLSFixAdminRightsError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
                    Environment.Exit(0)
                End If
            End If
        Catch
            Console.WriteLine("Could not fix Windows TLS.")
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

    Function GetNextInstanceInt() As Integer
        Dim HabboProcessCount As Integer = 0
        For Each HabboProcess In Process.GetProcessesByName("Habbo")
            Try
                If Path.GetDirectoryName(HabboProcess.MainModule.FileName) = GetClientPath() Then
                    HabboProcessCount += 1
                End If
            Catch
                HabboProcessCount += 1
            End Try
        Next
        If HabboProcessCount > 0 Then
            If HabboProcessCount > My.Settings.LastInstance + 1 Then
                My.Settings.LastInstance = HabboProcessCount + 1
            Else
                My.Settings.LastInstance += 1
            End If
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
    '0=English 1=Spanish 2=Portuguese
    Public Shared ClientNotFound As String() = {
        "Habbo Client not found.",
        "Habbo Client no encontrado.",
        "Habbo Client não encontrado."
    }
    Public Shared AdminRightsError As String() = {
        "You need administrator rights.",
        "Necesitas permisos de administrador.",
        "Você precisa de permissões de administrador."
    }
    Public Shared TLSFixAdminRightsError As String() = {
        "You must run the program as an administrator to enable TLS 1.2 on your system.",
        "Debes ejecutar el programa como administrador para habilitar TLS 1.2 en tu sistema.",
        "Você deve executar o programa como administrador para ativar o TLS 1.2 em seu sistema."
    }
    Public Shared ClientOpenError As String() = {
        "Could not open Habbo Client.",
        "No se pudo abrir Habbo Client.",
        "Habbo Client não pôde ser aberto."
    }
    Public Shared ProtocolRegError As String() = {
        "Could not register protocol.",
        "No se pudo registrar el protocolo.",
        "O protocolo não pôde ser registrado."
    }
    Public Shared ProtocolUnregError As String() = {
        "Could not unregister protocol.",
        "No se pudo eliminar el protocolo.",
        "O protocolo não pôde ser removido."
    }
    Public Shared RegisterProtocol As String() = {
        "Register Habbo Protocol",
        "Registrar Habbo Protocol",
        "Registrar Habbo Protocol"
    }
    Public Shared UnregisterProtocol As String() = {
        "Register Habbo Protocol",
        "Eliminar Habbo Protocol",
        "Remover Habbo Protocol"
    }
    Public Shared NewInstance As String() = {
        "Start new instance",
        "Iniciar nueva instancia",
        "Iniciar nova instância"
    }
    Public Shared ProtocolRegAdvice As String() = {
        "Do you want to register Habbo Protocol for easy access from the new Habbo web button?",
        "Quieres registrar Habbo Protocol para poder acceder fácilmente desde el nuevo botón de la web de Habbo?",
        "Deseja registrar o Habbo Protocol para poder acessar facilmente a partir do novo botão no site do Habbo?"
    }
    Public Shared ProtocolRegAdviceOK As String() = {
        "Now you can access from the new Habbo web button without having to open this program." & vbNewLine & "You don't need to do anything else.",
        "Ahora podrás acceder desde el nuevo botón de la web de Habbo sin necesidad de abrir este programa." & vbNewLine & "No necesitas hacer nada mas.",
        "Agora podes aceder desde o novo botão da web do Habbo sem a necessidade de abrir este programa." & vbNewLine & "Você não precisa fazer mais nada."
    }
End Class