#requires -Version 5.1
# Generise dijagrame za projektnu dokumentaciju (GDI+, 300 ppi).
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$OutDir = Join-Path $PSScriptRoot 'img'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

# ---------- paleta ----------
$cBorder = [System.Drawing.Color]::FromArgb(31, 78, 121)
$cText   = [System.Drawing.Color]::FromArgb(32, 32, 32)
$cMuted  = [System.Drawing.Color]::FromArgb(89, 89, 89)
$fBlue   = [System.Drawing.Color]::FromArgb(222, 235, 247)
$fGray   = [System.Drawing.Color]::FromArgb(242, 242, 242)
$fGreen  = [System.Drawing.Color]::FromArgb(226, 240, 217)
$fOrange = [System.Drawing.Color]::FromArgb(252, 235, 218)
$fPurple = [System.Drawing.Color]::FromArgb(233, 227, 243)

# ---------- helperi ----------
function New-Canvas([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $bmp.SetResolution(300, 300)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.Clear([System.Drawing.Color]::White)
    return @{ Bitmap = $bmp; Graphics = $g; Width = $w; Height = $h }
}

function New-Fnt([single]$size, [string]$style) {
    $st = [System.Drawing.FontStyle]::Regular
    if ($style -eq 'bold')   { $st = [System.Drawing.FontStyle]::Bold }
    if ($style -eq 'italic') { $st = [System.Drawing.FontStyle]::Italic }
    return New-Object System.Drawing.Font('Calibri', $size, $st, [System.Drawing.GraphicsUnit]::Pixel)
}

function New-RoundPath([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc(($x + $w - $d), $y, $d, $d, 270, 90)
    $p.AddArc(($x + $w - $d), ($y + $h - $d), $d, $d, 0, 90)
    $p.AddArc($x, ($y + $h - $d), $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

# Centrirani multi-line tekst u pravougaoniku
function Draw-Centered($g, [string]$text, $font, $color, [int]$x, [int]$y, [int]$w, [int]$h) {
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $br = New-Object System.Drawing.SolidBrush($color)
    $rc = New-Object System.Drawing.RectangleF($x, $y, $w, $h)
    $g.DrawString($text, $font, $br, $rc, $sf)
    $br.Dispose(); $sf.Dispose()
}

function Draw-Left($g, [string]$text, $font, $color, [int]$x, [int]$y) {
    $br = New-Object System.Drawing.SolidBrush($color)
    $g.DrawString($text, $font, $br, [single]$x, [single]$y)
    $br.Dispose()
}

# Kutija: naslov (bold) + do tri linije podteksta
function Draw-Box($g, [int]$x, [int]$y, [int]$w, [int]$h, [string]$title, [string[]]$lines, $fill, [int]$radius = 14, [int]$borderWidth = 3) {
    $path = New-RoundPath $x $y $w $h $radius
    $br = New-Object System.Drawing.SolidBrush($fill)
    $g.FillPath($br, $path)
    $pen = New-Object System.Drawing.Pen($cBorder, $borderWidth)
    $g.DrawPath($pen, $path)
    $br.Dispose(); $pen.Dispose(); $path.Dispose()

    $fT = New-Fnt 30 'bold'
    $fL = New-Fnt 25 'regular'
    if ($null -eq $lines -or $lines.Count -eq 0) {
        Draw-Centered $g $title $fT $cText $x $y $w $h
    } else {
        $titleH = 46
        $pad = 14
        $blockH = $titleH + ($lines.Count * 34)
        $top = $y + [int](($h - $blockH) / 2)
        Draw-Centered $g $title $fT $cText $x $top $w $titleH
        $ly = $top + $titleH - 4
        foreach ($ln in $lines) {
            Draw-Centered $g $ln $fL $cMuted $x $ly $w 34
            $ly += 34
        }
    }
    $fT.Dispose(); $fL.Dispose()
}

# Kontejner (isprekidani okvir) sa labelom u gornjem levom uglu
function Draw-Container($g, [int]$x, [int]$y, [int]$w, [int]$h, [string]$label) {
    $path = New-RoundPath $x $y $w $h 18
    $pen = New-Object System.Drawing.Pen($cMuted, 3)
    $pen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    $g.DrawPath($pen, $path)
    $pen.Dispose(); $path.Dispose()
    $f = New-Fnt 27 'bold'
    Draw-Left $g $label $f $cMuted ($x + 24) ($y + 12)
    $f.Dispose()
}

function New-ArrowPen([bool]$dashed = $false, [single]$width = 3) {
    $pen = New-Object System.Drawing.Pen($cBorder, $width)
    $cap = New-Object System.Drawing.Drawing2D.AdjustableArrowCap(5, 6, $true)
    $pen.CustomEndCap = $cap
    if ($dashed) { $pen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash }
    return $pen
}

function Draw-Arrow($g, [int]$x1, [int]$y1, [int]$x2, [int]$y2, [bool]$dashed = $false) {
    $pen = New-ArrowPen $dashed
    $g.DrawLine($pen, $x1, $y1, $x2, $y2)
    $pen.Dispose()
}

function Save-Canvas($canvas, [string]$name) {
    $path = Join-Path $OutDir $name
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
    Write-Host "  -> $path"
}

# ==================================================================
# SLIKA 1: Arhitektura sistema
# ==================================================================
Write-Host 'Generisem dijagram arhitekture...'
$c = New-Canvas 1890 1400
$g = $c.Graphics

# --- klijent ---
Draw-Box $g 430 20 620 110 'Klijent (web pregledač)' @('HTML / CSS / Bootstrap 3 / jQuery / toastr') $fGray

# strelice klijent -> cloud service
Draw-Arrow $g 620 130 620 330
Draw-Arrow $g 860 130 860 330
$fA = New-Fnt 24 'bold'
Draw-Left $g 'HTTP' $fA $cBorder 500 190
Draw-Left $g 'WebSocket / SSE /' $fA $cBorder 880 178
Draw-Left $g 'long polling (SignalR)' $fA $cBorder 880 210
$fA.Dispose()

# --- Azure Cloud Service kontejner ---
Draw-Container $g 30 250 1440 650 'Microsoft Azure Cloud Service (classic) - RedditCloud'

Draw-Box $g 70 330 650 230 'RedditService (WebRole)' @(
    'ASP.NET MVC 5.2.7  .  HTTP :80  /  TCP :8080',
    'Home / Registration / Chat Controller',
    'Hubs/ChatHub.cs - SignalR 2.4.3 + OWIN'
) $fBlue

Draw-Box $g 760 330 650 230 'HealthStatusService (WebRole)' @(
    'ASP.NET MVC 5.2.7  .  HTTP :8082',
    'HomeSController - uptime 24 h / 3 h',
    'Chart.js vizualizacija'
) $fBlue

Draw-Box $g 70 610 650 250 'HealthMonitoringService (WorkerRole)' @(
    'RoleEntryPoint  .  interval 5 s',
    'WCF ServiceHost + ChannelFactory',
    'NetTcpBinding :8080 / :8081',
    'IHealthMonitoring.IAmAlive()'
) $fGreen

Draw-Box $g 760 610 650 250 'NotificationService (WorkerRole)' @(
    'RoleEntryPoint  .  beskonačna petlja',
    'CloudQueue.GetMessage("notification")',
    'System.Net.Mail.SmtpClient',
    'EmailLogFile.txt - log isporuke'
) $fGreen

# --- eksterni SMTP ---
Draw-Box $g 1520 655 340 160 'Gmail SMTP' @('smtp.gmail.com : 587', 'TLS / SSL') $fOrange
Draw-Arrow $g 1410 735 1515 735
$fA = New-Fnt 23 'bold'
Draw-Left $g 'e-mail' $fA $cBorder 1412 685
$fA.Dispose()

# --- Azure Storage kontejner ---
Draw-Container $g 30 990 1440 340 'Microsoft Azure Storage (Storage Emulator / Azure)'

Draw-Box $g 70 1050 430 250 'Table Storage' @(
    'UsersTable', 'TopicTable', 'MessagesTable', 'HealthCheck'
) $fPurple

Draw-Box $g 530 1050 430 250 'Blob Storage' @(
    'kontejner "photo"', 'slike profila', 'slike tema'
) $fPurple

Draw-Box $g 990 1050 430 250 'Queue Storage' @(
    'red "notification"', 'format: topicId|commentId'
) $fPurple

# strelice cloud service -> storage
Draw-Arrow $g 300 900 300 1045
Draw-Arrow $g 700 900 700 1045
Draw-Arrow $g 1150 900 1150 1045
$fA = New-Fnt 23 'bold'
Draw-Left $g 'Table / Blob' $fA $cBorder 200 940
Draw-Left $g 'Table' $fA $cBorder 660 940
Draw-Left $g 'Queue' $fA $cBorder 1105 940
$fA.Dispose()

Save-Canvas $c 'arhitektura.png'

# ==================================================================
# SLIKA 2: Sekvencni dijagram real-time poruke
# ==================================================================
Write-Host 'Generisem sekvencni dijagram chata...'
$c = New-Canvas 1890 1580
$g = $c.Graphics

$lanes = @(
    @{ C = 185;  T = 'Korisnik A';        S = @('web pregledač') ; F = $fGray },
    @{ C = 565;  T = 'ChatController';    S = @('RedditService') ; F = $fBlue },
    @{ C = 945;  T = 'MessagesTable';     S = @('Azure Table')   ; F = $fPurple },
    @{ C = 1325; T = 'ChatHub';           S = @('SignalR 2.4.3') ; F = $fGreen },
    @{ C = 1705; T = 'Korisnik B';        S = @('web pregledač') ; F = $fGray }
)

$laneW = 340
foreach ($l in $lanes) {
    Draw-Box $g ($l.C - $laneW / 2) 20 $laneW 110 $l.T $l.S $l.F 12 3
}

# lifelines
$penLife = New-Object System.Drawing.Pen($cMuted, 2)
$penLife.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
foreach ($l in $lanes) { $g.DrawLine($penLife, $l.C, 130, $l.C, 1520) }
$penLife.Dispose()

$fPhase = New-Fnt 27 'bold'
$fStep  = New-Fnt 24 'regular'
$fNum   = New-Fnt 24 'bold'

function Draw-Phase($g, [int]$y, [string]$text) {
    $br = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(238, 242, 248))
    $g.FillRectangle($br, 20, $y, 1850, 46)
    $br.Dispose()
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200, 210, 225), 2)
    $g.DrawRectangle($pen, 20, $y, 1850, 46)
    $pen.Dispose()
    Draw-Left $g $text $fPhase $cBorder 36 ($y + 8)
}

# jedan korak sekvence: strelica sa numerisanom labelom iznad
function Draw-Step($g, [int]$from, [int]$to, [int]$y, [string]$num, [string]$label, [bool]$dashed = $false) {
    Draw-Arrow $g $from $y $to $y $dashed
    $mid = [int](($from + $to) / 2)
    $txt = "$num  $label"
    $sz = $g.MeasureString($txt, $fStep)
    $x = $mid - [int]($sz.Width / 2)
    if ($x -lt 12) { $x = 12 }
    if (($x + $sz.Width) -gt 1878) { $x = 1878 - [int]$sz.Width }
    # bela podloga da linija ne prolazi kroz tekst
    $br = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillRectangle($br, $x - 6, ($y - 40), ([int]$sz.Width + 12), 34)
    $br.Dispose()
    Draw-Left $g $txt $fStep $cText $x ($y - 38)
}

$L1 = $lanes[0].C; $L2 = $lanes[1].C; $L3 = $lanes[2].C; $L4 = $lanes[3].C; $L5 = $lanes[4].C

Draw-Phase $g 165 'Faza 1 - uspostavljanje trajne konekcije'
Draw-Step  $g $L1 $L4 285 '1.' '$.connection.hub.start()  .  qs: userEmail=a@primer.rs'
Draw-Step  $g $L5 $L4 375 '2.' 'hub.start()  .  qs: userEmail=b@primer.rs'
$fNote = New-Fnt 23 'italic'
Draw-Centered $g 'OnConnected(): Groups.Add(Context.ConnectionId, userEmail)' $fNote $cMuted 900 400 860 40
$fNote.Dispose()

Draw-Phase $g 470 'Faza 2 - slanje poruke'
Draw-Step  $g $L1 $L2 590 '3.' 'POST /Chat/SendMessage'
Draw-Step  $g $L2 $L3 680 '4.' 'TableOperation.Insert(Message)'
Draw-Step  $g $L3 $L2 770 '5.' 'potvrda upisa' $true
Draw-Step  $g $L2 $L4 860 '6.' 'GetHubContext<ChatHub>().Clients.Group(...)'
Draw-Step  $g $L4 $L5 950 '7.' 'messageReceived(dto)'
Draw-Step  $g $L4 $L1 1040 '8.' 'messageReceived(dto)'
Draw-Step  $g $L2 $L1 1130 '9.' 'JSON odgovor (dto)' $true

Draw-Phase $g 1210 'Faza 3 - izmena i brisanje poruke'
Draw-Step  $g $L1 $L2 1320 '10.' 'POST /Chat/EditMessage  |  /Chat/DeleteMessage'
Draw-Step  $g $L4 $L5 1420 '11.' 'messageEdited(dto)  /  messageDeleted({rowKey})'
Draw-Step  $g $L4 $L1 1510 '12.' 'messageEdited(dto)  /  messageDeleted({rowKey})'

$fPhase.Dispose(); $fStep.Dispose(); $fNum.Dispose()
Save-Canvas $c 'chat-sekvenca.png'

Write-Host 'Gotovo.'
