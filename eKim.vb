Option Strict On
Imports System.Runtime.InteropServices
Imports System.Text
Imports IWshRuntimeLibrary

Public Class eKim

#Region "Campuri"
    Private Const WM_HOTKEY As Integer = &H312
    Private Const HOTKEY_ID As Integer = 200
    Private Const MOD_CONTROL As UInteger = &H2UI
    Private Const MOD_NOREPEAT As UInteger = &H4000UI
    Private Const VK_CAPITAL As UInteger = &H14UI

    Private ReadOnly appName As String = Application.ProductName
    Private ReadOnly docsDir As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments
    Private ReadOnly saveDir As String = My.Computer.FileSystem.SpecialDirectories.Desktop & "\" & Application.ProductName
    Private ReadOnly startupLnk As String = Environment.GetFolderPath(Environment.SpecialFolder.Startup) & "\" & Application.ProductName & ".lnk"
    ' UTF-8 cu BOM: diacriticele (ă, ș, ț) se vad corect in Notepad si in browser
    Private ReadOnly utf8Bom As New UTF8Encoding(True)

    Private tJurnal As String = "Jurnal"
    Private tSursa As String = "Sursa"
    Private timeFmt As String = "dd MMMM yyyy, HH:mm:ss"
    Private msgNimic As String = "Nimic selectat"

    Private Activ1 As Boolean       ' monitorizare activa / in pauza
    Private busy As Boolean         ' o captura este in curs
    Private hotkeyOn As Boolean     ' Ctrl+CapsLock inregistrat

    Private ReadOnly Property LogPath As String
        Get
            Return IO.Path.Combine(docsDir, appName & " " & tJurnal & ".htm")
        End Get
    End Property
#End Region

#Region "Meniu / interfata"
#Disable Warning IDE1006 ' Naming Styles
    Private Sub mnustop_Click(sender As Object, e As EventArgs) Handles mnustop.Click
        Application.Exit()
    End Sub

    Private Sub Mnupauza_Click(sender As Object, e As EventArgs) Handles mnupauza.Click
        Activ1 = Not Activ1
        ApplyMonitorState()
    End Sub

    Private Sub mnudeschid_Click(sender As Object, e As EventArgs) Handles mnudeschid.Click
        If IO.File.Exists(LogPath) Then OpenShell(LogPath)
    End Sub

    Private Sub NotifyIcon1_Click(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDown
        If e.Button = MouseButtons.Left Then mnuSettings_Click(sender, e)
    End Sub

    Private Sub mnuSettings_Click(sender As Object, e As EventArgs) Handles mnuSettings.Click
        Visible = True
        WindowState = FormWindowState.Normal
        Icon = My.Resources.circle_red
        ShowIcon = True
        ShowInTaskbar = True
        Activate()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        My.Settings.sLimba = ComboBox1.Text
        My.Settings.sNotificari = CheckBox1.Checked
        My.Settings.sStartup = CheckBox2.Checked
        My.Settings.sTimp = CheckBox3.Checked
        My.Settings.Save()
        ApplyStartup(CheckBox2.Checked)     ' scurtatura se aplica la "Salveaza", nu la bifare
        Application.Restart()
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        OpenShell("http://atis.us.to")
    End Sub

    Private Shared Sub OpenShell(target As String)
        Try
            Process.Start(New ProcessStartInfo(target) With {.UseShellExecute = True})
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Exclamation, Application.ProductName)
        End Try
    End Sub

    ' Seteaza iconita/textul/hotkey-ul in functie de starea Activ1 (folosit la start si la pauza)
    Private Sub ApplyMonitorState()
        Dim ro As Boolean = (My.Settings.sLimba = "ro-RO")
        If Activ1 Then
            SetHotkey(True)
            NotifyIcon1.Text = "Control CapsLock=> .txt"
            NotifyIcon1.Icon = My.Resources.circle_red
            mnupauza.Image = My.Resources.red
            mnupauza.Text = If(ro, "Monitor Activ", "&Monitor On")
        Else
            SetHotkey(False)
            NotifyIcon1.Text = "Control CapsLock=> Ø"
            NotifyIcon1.Icon = My.Resources.circle_green
            mnupauza.Image = My.Resources.green
            mnupauza.Text = If(ro, "Monitor Inactiv", "&Monitor Off")
        End If
    End Sub

    Private Sub ApplyStartup(enable As Boolean)
        Try
            If enable Then
                Dim sh As WshShell = New WshShell()
                Dim lnk As IWshShortcut = CType(sh.CreateShortcut(startupLnk), IWshShortcut)
                lnk.TargetPath = Application.ExecutablePath
                lnk.WorkingDirectory = Application.StartupPath
                lnk.Save()
            ElseIf IO.File.Exists(startupLnk) Then
                IO.File.Delete(startupLnk)
            End If
        Catch ex As Exception
            Notify(ex.Message, ToolTipIcon.Error, True)
        End Try
    End Sub

    ' Balon in tray. Erorile (always:=True) se afiseaza indiferent de setarea "Notificari".
    Private Sub Notify(text As String, icon As ToolTipIcon, Optional always As Boolean = False)
        If Not always AndAlso Not My.Settings.sNotificari Then Return
        NotifyIcon1.BalloonTipTitle = appName
        NotifyIcon1.BalloonTipText = text
        NotifyIcon1.BalloonTipIcon = icon
        NotifyIcon1.ShowBalloonTip(1000)
    End Sub
#End Region

#Region "Start / inchidere"
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim en As Boolean = (My.Settings.sLimba = "en-US")
        If Not en Then My.Settings.sLimba = "ro-RO"
        ComboBox1.Text = If(en, "en-US", "ro-RO")

        CheckBox1.Checked = My.Settings.sNotificari
        CheckBox2.Checked = My.Settings.sStartup
        CheckBox3.Checked = My.Settings.sTimp
        Activ1 = My.Settings.sAuto

        If en Then
            mnudeschid.Text = "&Open log" : mnustop.Text = "E&xit" : mnuSettings.Text = "&Settings"
            tJurnal = "Log" : tSursa = "Source" : msgNimic = "Nothing selected"
            CheckBox1.Text = "Show notification?"
            CheckBox2.Text = "Start with Windows?"
            CheckBox3.Text = "Insert time in text?"
            Label1.Text = "Change language:"
            Button1.Text = "Save"
            timeFmt = "MMM d yyyy H:mm:ss"
        End If

        If My.Settings.sStartup Then ApplyStartup(True)     ' reimprospateaza calea scurtaturii
        ApplyMonitorState()                                 ' iconita + meniu + hotkey

        Try
            RotateLogIfNeeded()
            EnsureLogExists()
        Catch ex As Exception
            Notify(ex.Message, ToolTipIcon.Error, True)
        End Try

        mnudeschid.Enabled = True
        Notify("Control CapsLock => .txt", ToolTipIcon.Info, True)
        Hide()
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' X-ul ferestrei de setari doar o ascunde; iesirea se face din meniu (Exit)
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Hide()
            Return
        End If
        SetHotkey(False)
        NotifyIcon1.Visible = False
        NotifyIcon1.Dispose()
    End Sub

    Private Sub SetHotkey(enable As Boolean)
        If enable = hotkeyOn Then Return
        If enable Then
            hotkeyOn = RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL Or MOD_NOREPEAT, VK_CAPITAL)
            If Not hotkeyOn Then
                Notify(If(My.Settings.sLimba = "ro-RO",
                          "Ctrl+CapsLock este folosit de alt program.",
                          "Ctrl+CapsLock is used by another program."), ToolTipIcon.Warning, True)
            End If
        Else
            UnregisterHotKey(Handle, HOTKEY_ID)
            hotkeyOn = False
        End If
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_HOTKEY AndAlso m.WParam.ToInt32() = HOTKEY_ID AndAlso Activ1 Then
            CaptureClipboardAsync()
        End If
        MyBase.WndProc(m)
    End Sub
#End Region

#Region "Captura"
    Private Async Sub CaptureClipboardAsync()
        If busy Then Return
        busy = True
        Try
            Dim hWnd As IntPtr = GetForegroundWindow()
            If hWnd = IntPtr.Zero OrElse hWnd = Handle Then Return

            ' numele ferestrei (sau al procesului, daca fereastra nu are titlu)
            Dim title As String = GetWindowTitle(hWnd)
            If title.Length = 0 Then title = GetProcessName(hWnd)
            Dim fileName As String = SafeFileName(title)

            ' copiem selectia; Ctrl+CapsLock a schimbat starea CapsLock, o readucem
            Dim seqBefore As UInteger = GetClipboardSequenceNumber()
            SendKeys.SendWait("^c")
            SendKeys.Send("{CAPSLOCK}")

            ' asteptam (max ~0,6 s) sa se schimbe clipboardul, fara sa blocam interfata
            Dim waited As Integer = 0
            Do While GetClipboardSequenceNumber() = seqBefore AndAlso waited < 600
                Await Task.Delay(25)
                waited += 25
            Loop

            Dim text As String = String.Empty
            If GetClipboardSequenceNumber() <> seqBefore Then
                Await Task.Delay(30)    ' lasam aplicatia sursa sa termine scrierea
                text = Await ReadClipboardTextAsync()
            End If

            If text.Length = 0 Then
                Notify(msgNimic & Environment.NewLine & ":-(", ToolTipIcon.Error)
                Return
            End If

            If Not IO.Directory.Exists(saveDir) Then IO.Directory.CreateDirectory(saveDir)
            Dim filePath As String = IO.Path.Combine(saveDir, fileName & ".txt")
            Dim header As String = If(My.Settings.sTimp,
                                      "--------------" & Now.ToString(timeFmt) & "--------------" & Environment.NewLine,
                                      String.Empty)
            IO.File.AppendAllText(filePath, header & text & Environment.NewLine, utf8Bom)

            WriteLog(fileName, text, filePath)
            Notify("+ in " & tJurnal & " = " & text.Length & Environment.NewLine & ":-)", ToolTipIcon.Info)
        Catch ex As Exception
            Notify(ex.Message, ToolTipIcon.Error, True)
        Finally
            busy = False
        End Try
    End Sub

    ' Citeste textul din clipboard; reincearca daca e blocat de alt proces
    Private Async Function ReadClipboardTextAsync() As Task(Of String)
        For i As Integer = 1 To 8
            Try
                Return If(Clipboard.ContainsText(), Clipboard.GetText(), String.Empty)
            Catch ex As ExternalException
                ' clipboard ocupat; mai incercam
            End Try
            Await Task.Delay(40)
        Next
        Return String.Empty
    End Function

    Private Shared Function SafeFileName(name As String) As String
        Dim invalid As Char() = IO.Path.GetInvalidFileNameChars()
        Dim chars As Char() = name.ToCharArray()
        For i As Integer = 0 To chars.Length - 1
            If chars(i) = "#"c OrElse Array.IndexOf(invalid, chars(i)) >= 0 Then chars(i) = " "c
        Next
        Dim s As String = New String(chars).Trim().TrimEnd("."c).Trim()
        If s.Length > 100 Then s = s.Substring(0, 100).TrimEnd()      ' evita caile > 260 caractere
        Return If(s.Length = 0, "Clipboard", s)
    End Function
#End Region

#Region "Jurnal HTML"
    Private Sub EnsureLogExists()
        If IO.File.Exists(LogPath) Then Return
        IO.File.WriteAllText(LogPath,
            "<!DOCTYPE html><html><head><meta charset=""utf-8""><title>" & appName & " " & tJurnal &
            "</title></head><body style='margin:8px'>Start (#" & appName & "#)", utf8Bom)
    End Sub

    ' Jurnalul mai vechi de 30 de zile se muta intr-un fisier Backup
    Private Sub RotateLogIfNeeded()
        If Not IO.File.Exists(LogPath) Then Return
        Dim created As Date = IO.File.GetCreationTime(LogPath)
        If (Now - created).TotalDays <= 30 Then Return

        Dim baseName As String = IO.Path.Combine(docsDir,
            "Backup " & appName & " " & tJurnal & " " & created.ToString("yyyy-MM-dd") & "_" & Today.ToString("yyyy-MM-dd"))
        Dim backup As String = baseName & ".htm"
        If IO.File.Exists(backup) Then backup = baseName & "_" & Now.ToString("HHmmss") & ".htm"
        IO.File.Move(LogPath, backup)
    End Sub

    Private Sub WriteLog(source As String, copiedText As String, filePath As String)
        RotateLogIfNeeded()
        EnsureLogExists()

        Const cell As String = "border:1px solid #ccc"
        Const head As String = "background-color:#B8DBFD;border:1px solid #ccc"
        Dim html As New StringBuilder()
        html.Append("<table cellpadding='5' cellspacing='0' style='border:1px solid #ccc;word-break:break-all;font-size:9pt;font-family:arial;table-layout:fixed;width:100%'><tr>")
        html.Append("<td style='width:12%;").Append(cell).Append("'>").Append(Now.ToString("dd.MM.yyyy HH:mm:ss")).Append("</td>")
        html.Append("<th style='width:6%;").Append(head).Append("'>").Append(tSursa).Append("</th>")
        html.Append("<td style='width:18%;").Append(cell).Append("'><a href='").Append(New Uri(filePath).AbsoluteUri).Append("'>")
        html.Append(System.Net.WebUtility.HtmlEncode(source)).Append("</a></td>")
        html.Append("<th style='width:6%;").Append(head).Append("'>").Append(tJurnal).Append("</th>")
        html.Append("<td style='width:58%;white-space:pre-wrap;").Append(cell).Append("'>")
        html.Append(System.Net.WebUtility.HtmlEncode(copiedText)).Append("</td></tr></table>")

        IO.File.AppendAllText(LogPath, html.ToString(), utf8Bom)
    End Sub
#End Region

#Region "API"
    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function GetWindowTextLength(hWnd As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function GetWindowText(hWnd As IntPtr, lpString As StringBuilder, nMaxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function

    ' Creste de fiecare data cand se schimba clipboardul (chiar daca textul e identic)
    <DllImport("user32.dll")>
    Private Shared Function GetClipboardSequenceNumber() As UInteger
    End Function

    Private Shared Function GetWindowTitle(hWnd As IntPtr) As String
        Dim n As Integer = GetWindowTextLength(hWnd)
        If n <= 0 Then Return String.Empty
        Dim sb As New StringBuilder(n + 1)
        GetWindowText(hWnd, sb, sb.Capacity)
        Return sb.ToString()
    End Function

    Private Shared Function GetProcessName(hWnd As IntPtr) As String
        Try
            Dim pid As UInteger = 0UI
            GetWindowThreadProcessId(hWnd, pid)
            Return Process.GetProcessById(CInt(pid)).ProcessName
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function
#End Region

End Class
