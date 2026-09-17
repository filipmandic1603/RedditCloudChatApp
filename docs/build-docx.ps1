#requires -Version 5.1
# Gradi .docx projektnu dokumentaciju iz docs\sadrzaj.txt, po uputstvu izrada_dokumentacije.pdf.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root      = $PSScriptRoot
$InFile    = Join-Path $Root 'sadrzaj.txt'
$ImgDir    = Join-Path $Root 'img'
$OutFile   = Join-Path $Root 'Dokumentacija_RedditCloud.docx'

if (Test-Path $OutFile) { Remove-Item $OutFile -Force }

# ---- Word konstante ----
$wdStyleNormal   = -1
$wdStyleHeading1 = -2
$wdStyleHeading2 = -3
$wdStyleHeading3 = -4
$wdStyleTypePara = 1

$alLeft = 0; $alCenter = 1; $alRight = 2; $alJustify = 3
$wdSectionBreakNextPage = 2
$wdPageBreak            = 7
$wdStory                = 6
$wdFieldPage            = 33
$wdHeaderPrimary        = 1
$wdHeaderEven           = 3
$wdLineStyleSingle      = 1
$wdLineStyleNone        = 0
$wdLineWidth050pt       = 4
$wdBorderBottom         = -3
$wdRowHeightExactly     = 2
$wdCellAlignVerticalCenter = 1
$msoTrue = -1

function CmToPt([double]$cm) { return $cm * 28.3464567 }

Write-Host 'Citam sadrzaj...'
$lines = [IO.File]::ReadAllLines($InFile, [Text.Encoding]::UTF8)

Write-Host 'Pokrecem Word...'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$doc = $word.Documents.Add()
$sel = $word.Selection

# ================= PAGE SETUP =================
$ps = $doc.PageSetup
$ps.PageWidth   = CmToPt 21.0
$ps.PageHeight  = CmToPt 29.7
$ps.TopMargin    = CmToPt 2.5
$ps.BottomMargin = CmToPt 2.5
$ps.LeftMargin   = CmToPt 2.5
$ps.RightMargin  = CmToPt 2.5
$ps.HeaderDistance = CmToPt 1.4
$ps.FooterDistance = CmToPt 1.4
$ps.OddAndEvenPagesHeaderFooter = $true
$ps.DifferentFirstPageHeaderFooter = $false

# ================= STILOVI =================
Write-Host 'Podesavam stilove...'

# Normal: Calibri 10 pt, Justify, bez uvlacenja prvog reda
$stN = $doc.Styles.Item($wdStyleNormal)
$stN.Font.Name = 'Calibri'
$stN.Font.Size = 10
$stN.Font.Bold = $false
$stN.Font.Color = 0
$stN.ParagraphFormat.Alignment = $alJustify
$stN.ParagraphFormat.FirstLineIndent = 0
$stN.ParagraphFormat.SpaceBefore = 0
$stN.ParagraphFormat.SpaceAfter  = 8
$stN.ParagraphFormat.LineSpacingRule = 0   # single
$stN.ParagraphFormat.WidowControl = $true

function Set-Heading($style, [double]$size, [int]$before, [int]$after) {
    $style.Font.Name = 'Calibri'
    $style.Font.Size = $size
    $style.Font.Bold = $true
    $style.Font.Italic = $false
    $style.Font.Color = 0
    $style.ParagraphFormat.Alignment = $alLeft
    $style.ParagraphFormat.FirstLineIndent = 0
    $style.ParagraphFormat.LeftIndent = 0
    $style.ParagraphFormat.SpaceBefore = $before
    $style.ParagraphFormat.SpaceAfter  = $after
    $style.ParagraphFormat.KeepWithNext = $true
    $style.ParagraphFormat.LineSpacingRule = 0
}
Set-Heading $doc.Styles.Item($wdStyleHeading1) 16 24 12
Set-Heading $doc.Styles.Item($wdStyleHeading2) 14 16 8
Set-Heading $doc.Styles.Item($wdStyleHeading3) 12 12 6

function New-Style([string]$name, [string]$font, [double]$size, [bool]$bold, [bool]$italic, [int]$align, [int]$before, [int]$after) {
    $st = $doc.Styles.Add($name, $wdStyleTypePara)
    $st.Font.Name = $font
    $st.Font.Size = $size
    $st.Font.Bold = $bold
    $st.Font.Italic = $italic
    $st.Font.Color = 0
    $st.ParagraphFormat.Alignment = $align
    $st.ParagraphFormat.FirstLineIndent = 0
    $st.ParagraphFormat.SpaceBefore = $before
    $st.ParagraphFormat.SpaceAfter  = $after
    $st.ParagraphFormat.LineSpacingRule = 0
    return $st
}

# Stilovi iz tabele 1 uputstva
$stFigCap = New-Style 'Potpis slike'   'Calibri' 8   $false $true  $alCenter 4  14
$stTabCap = New-Style 'Potpis tabele'  'Calibri' 8   $true  $false $alLeft   12 4
$stTabTxt = New-Style 'Tekst u tabeli' 'Calibri' 9   $false $false $alLeft   2  2
$stCode   = New-Style 'Kod'            'Consolas' 8.5 $false $false $alLeft  0  0
$stBul    = New-Style 'Nabrajanje'     'Calibri' 10  $false $false $alJustify 0 6
$stLit    = New-Style 'Literatura stil' 'Calibri' 10 $false $false $alLeft   0  8
$stEq     = New-Style 'Formula'        'Cambria Math' 10 $false $false $alCenter 10 10
$stTitleBig = New-Style 'Naslovna veliko' 'Calibri' 18 $true  $false $alCenter 0 0
$stTitleMid = New-Style 'Naslovna srednje' 'Calibri' 12 $false $false $alCenter 0 0
$stH1Plain  = New-Style 'Naslov bez broja' 'Calibri' 16 $true $false $alLeft 24 12

$stCode.ParagraphFormat.LeftIndent = CmToPt 0.4
$stCode.ParagraphFormat.KeepTogether = $true
$stCode.Shading.BackgroundPatternColor = 15790320   # svetlo siva

$stBul.ParagraphFormat.LeftIndent = CmToPt 0.7
$stBul.ParagraphFormat.FirstLineIndent = -(CmToPt 0.45)

$stLit.ParagraphFormat.LeftIndent = CmToPt 0.8
$stLit.ParagraphFormat.FirstLineIndent = -(CmToPt 0.8)

$stTabTxt.ParagraphFormat.KeepWithNext = $false

# ================= HELPERI =================

# Razlaze inline oznake {i}..{/i} (kurziv), {b}..{/b} (bold), {c}..{/c} (kod)
function Get-Segments([string]$text) {
    $text = $text.Replace('{PIPE}', '|')
    $parts = [regex]::Split($text, '(\{/?[ibc]\})')
    $segs = New-Object System.Collections.ArrayList
    $it = $false; $bo = $false; $cd = $false
    foreach ($p in $parts) {
        switch ($p) {
            '{i}'  { $it = $true;  continue }
            '{/i}' { $it = $false; continue }
            '{b}'  { $bo = $true;  continue }
            '{/b}' { $bo = $false; continue }
            '{c}'  { $cd = $true;  continue }
            '{/c}' { $cd = $false; continue }
            default {
                if ($p -ne '') {
                    [void]$segs.Add(@{ Text = $p; Italic = $it; Bold = $bo; Code = $cd })
                }
            }
        }
    }
    return $segs
}

function Write-Styled([string]$text, $style) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $style
    $segs = Get-Segments $text
    foreach ($s in $segs) {
        if ($s.Code) {
            $sel.Font.Name = 'Consolas'
            $sel.Font.Size = 9
        } else {
            $sel.Font.Name = $style.Font.Name
            $sel.Font.Size = $style.Font.Size
        }
        $sel.Font.Italic = [bool]($s.Italic -or $style.Font.Italic)
        $sel.Font.Bold   = [bool]($s.Bold   -or $style.Font.Bold)
        $sel.TypeText($s.Text)
    }
    $sel.Font.Italic = $style.Font.Italic
    $sel.Font.Bold   = $style.Font.Bold
    $sel.Font.Name   = $style.Font.Name
    $sel.Font.Size   = $style.Font.Size
    $sel.TypeParagraph()
}

function Write-Plain([string]$text, $style) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $style
    $sel.Font.Name = $style.Font.Name
    $sel.Font.Size = $style.Font.Size
    $sel.Font.Bold = $style.Font.Bold
    $sel.Font.Italic = $style.Font.Italic
    $sel.TypeText($text)
    $sel.TypeParagraph()
}

# ================= NASLOVNA STRANA =================
function Write-TitlePage([string]$author, [string]$title, [string]$year) {
    Write-Host 'Gradim naslovnu stranu...'
    $sel.EndKey($wdStory) | Out-Null

    Write-Plain 'UNIVERZITET U NOVOM SADU' $stTitleMid
    Write-Plain 'FAKULTET TEHNIČKIH NAUKA U NOVOM SADU' $stTitleMid
    # naslovna zaglavna linija
    $par = $doc.Paragraphs.Item($doc.Paragraphs.Count)
    $par.Range.Borders.Item($wdBorderBottom).LineStyle = $wdLineStyleSingle
    $par.Range.Borders.Item($wdBorderBottom).LineWidth = $wdLineWidth050pt

    Write-Plain '' $stTitleMid
    Write-Plain '[ ovde umetnuti zvanicne logotipe UNS i FTN iz sablona fakulteta ]' $stFigCap
    for ($i = 0; $i -lt 4; $i++) { Write-Plain '' $stTitleMid }

    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stTitleMid
    $sel.ParagraphFormat.Alignment = $alLeft
    $sel.Font.Size = 12
    $sel.TypeText($author)
    $sel.TypeParagraph()

    for ($i = 0; $i -lt 6; $i++) { Write-Plain '' $stTitleMid }
    Write-Plain $title $stTitleBig
    for ($i = 0; $i -lt 6; $i++) { Write-Plain '' $stTitleMid }
    Write-Plain 'PROJEKAT' $stTitleMid

    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stTitleMid
    $sel.TypeText('')
    $sel.TypeParagraph()
    $par = $doc.Paragraphs.Item($doc.Paragraphs.Count - 1)
    $par.Range.Borders.Item($wdBorderBottom).LineStyle = $wdLineStyleSingle
    $par.Range.Borders.Item($wdBorderBottom).LineWidth = $wdLineWidth050pt
    $par.Format.LeftIndent  = CmToPt 5.0
    $par.Format.RightIndent = CmToPt 5.0

    Write-Plain 'Osnovne akademske studije' $stTitleMid
    for ($i = 0; $i -lt 5; $i++) { Write-Plain '' $stTitleMid }
    Write-Plain "Novi Sad, $year" $stTitleMid
}

# ================= TABELE =================
function Write-Table([string[]]$rows, [int]$cols) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stN
    $sel.TypeParagraph()
    $sel.EndKey($wdStory) | Out-Null

    $nRows = $rows.Count
    $tbl = $doc.Tables.Add($sel.Range, $nRows, $cols)
    $tbl.Range.Style = $stTabTxt
    $tbl.Borders.InsideLineStyle  = $wdLineStyleSingle
    $tbl.Borders.OutsideLineStyle = $wdLineStyleSingle
    $tbl.Borders.InsideColor  = 12566463
    $tbl.Borders.OutsideColor = 8421504
    $tbl.Rows.Item(1).HeadingFormat = $true
    $tbl.PreferredWidthType = 2       # procenat
    $tbl.PreferredWidth = 100
    $tbl.Spacing = 0
    $tbl.TopPadding    = CmToPt 0.08
    $tbl.BottomPadding = CmToPt 0.08
    $tbl.LeftPadding   = CmToPt 0.14
    $tbl.RightPadding  = CmToPt 0.14

    for ($r = 0; $r -lt $nRows; $r++) {
        $cells = $rows[$r].Split('|')
        for ($c = 0; $c -lt $cols; $c++) {
            $val = ''
            if ($c -lt $cells.Count) { $val = $cells[$c].Replace('{PIPE}', '|') }
            $cellRange = $tbl.Cell(($r + 1), ($c + 1)).Range
            $cellRange.Text = $val
            if ($r -eq 0) {
                $cellRange.Font.Bold = $true
                $cellRange.Shading.BackgroundPatternColor = 15132390
            } else {
                $cellRange.Font.Bold = $false
            }
        }
    }
    $sel.EndKey($wdStory) | Out-Null
}

# ================= PLACEHOLDER ZA SNIMAK EKRANA =================
function Write-Placeholder([double]$heightCm, [string]$caption) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stN
    $sel.TypeParagraph()
    $sel.EndKey($wdStory) | Out-Null

    $tbl = $doc.Tables.Add($sel.Range, 1, 1)
    $tbl.PreferredWidthType = 2
    $tbl.PreferredWidth = 100
    $tbl.Borders.OutsideLineStyle = $wdLineStyleSingle
    $tbl.Borders.OutsideColor = 12566463
    $tbl.Borders.InsideLineStyle = $wdLineStyleNone
    $tbl.Rows.Item(1).HeightRule = $wdRowHeightExactly
    $tbl.Rows.Item(1).Height = CmToPt $heightCm
    $cell = $tbl.Cell(1, 1)
    $cell.VerticalAlignment = $wdCellAlignVerticalCenter
    $cell.Shading.BackgroundPatternColor = 16382457
    $cr = $cell.Range
    $cr.Text = "[ SNIMAK EKRANA ]`v$caption`v(zameniti snimkom, rezolucija do 300 ppi)"
    $cr.Style = $stTabTxt
    $cr.ParagraphFormat.Alignment = $alCenter
    $cr.Font.Name = 'Calibri'
    $cr.Font.Size = 9
    $cr.Font.Italic = $true
    $cr.Font.Color = 8421504
    $sel.EndKey($wdStory) | Out-Null
}

# ================= SLIKA =================
function Write-Image([string]$fileName, [double]$widthCm) {
    $path = Join-Path $ImgDir $fileName
    if (-not (Test-Path $path)) { throw "Nema slike: $path" }
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stN
    $sel.ParagraphFormat.Alignment = $alCenter
    $sel.ParagraphFormat.SpaceBefore = 6
    $sel.ParagraphFormat.SpaceAfter = 0
    $shp = $sel.InlineShapes.AddPicture($path, $false, $true)
    $shp.LockAspectRatio = $msoTrue
    $shp.Width = CmToPt $widthCm
    $sel.EndKey($wdStory) | Out-Null
    $sel.TypeParagraph()
}

# ================= FORMULA =================
function Write-Equation([string]$text, [string]$number) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stEq
    $sel.ParagraphFormat.TabStops.ClearAll()
    [void]$sel.ParagraphFormat.TabStops.Add((CmToPt 16.0), $alRight)
    $sel.Font.Name = 'Cambria Math'
    $sel.Font.Size = 10
    $sel.TypeText($text)
    $sel.TypeText([char]9)
    $sel.TypeText("($number)")
    $sel.TypeParagraph()
}

# ================= SEKCIJE =================
$script:sectionCount = 1
function New-Section([bool]$restartNumbering) {
    $sel.EndKey($wdStory) | Out-Null
    $sel.InsertBreak($wdSectionBreakNextPage)
    $script:sectionCount++
    $sec = $doc.Sections.Item($script:sectionCount)

    $hdrOdd  = $sec.Headers.Item($wdHeaderPrimary)
    $hdrEven = $sec.Headers.Item($wdHeaderEven)
    $ftrOdd  = $sec.Footers.Item($wdHeaderPrimary)
    $ftrEven = $sec.Footers.Item($wdHeaderEven)

    if ($restartNumbering) {
        foreach ($hf in @($hdrOdd, $hdrEven, $ftrOdd, $ftrEven)) { $hf.LinkToPrevious = $false }

        # Zaglavlje: neparne strane desno, parne strane levo (kao u uputstvu)
        $r = $hdrOdd.Range
        $r.Text = 'Projektna dokumentacija'
        $r.Font.Name = 'Calibri'; $r.Font.Size = 10; $r.Font.Bold = $false
        $r.ParagraphFormat.Alignment = $alRight
        $r.Borders.Item($wdBorderBottom).LineStyle = $wdLineStyleSingle

        $r = $hdrEven.Range
        $r.Text = $script:AuthorName
        $r.Font.Name = 'Calibri'; $r.Font.Size = 10; $r.Font.Bold = $false
        $r.ParagraphFormat.Alignment = $alLeft
        $r.Borders.Item($wdBorderBottom).LineStyle = $wdLineStyleSingle

        # Podnozje: broj strane centriran, numeracija pocinje od 1
        foreach ($ftr in @($ftrOdd, $ftrEven)) {
            $fr = $ftr.Range
            $fr.Text = ''
            $fr.ParagraphFormat.Alignment = $alCenter
            $fr.Font.Name = 'Calibri'; $fr.Font.Size = 10
            [void]$fr.Fields.Add($fr, $wdFieldPage)
            $ftr.PageNumbers.RestartNumberingAtSection = $true
            $ftr.PageNumbers.StartingNumber = 1
        }
    }
}

# ================= GLAVNA PETLJA =================
$script:AuthorName = 'Autor'
$h1 = 0; $h2 = 0; $h3 = 0
$tableBuf = $null; $tableCols = 0
$codeBuf = New-Object System.Collections.ArrayList
$tocRange = $null

function Flush-Code {
    if ($codeBuf.Count -eq 0) { return }
    $sel.EndKey($wdStory) | Out-Null
    $sel.Style = $stCode
    for ($i = 0; $i -lt $codeBuf.Count; $i++) {
        $sel.Font.Name = 'Consolas'
        $sel.Font.Size = 8.5
        $sel.TypeText($codeBuf[$i])
        $sel.TypeParagraph()
    }
    # poslednji paragraf koda dobija donji razmak
    $doc.Paragraphs.Item($doc.Paragraphs.Count - 1).Format.SpaceAfter = 10
    $codeBuf.Clear()
    $sel.EndKey($wdStory) | Out-Null
}

Write-Host 'Gradim dokument...'
$n = 0
foreach ($line in $lines) {
    $n++
    if ($line -eq '') { continue }
    $idx = $line.IndexOf('|')
    if ($idx -lt 0) { continue }
    $tag  = $line.Substring(0, $idx)
    $body = $line.Substring($idx + 1)

    if ($tag -ne 'CODE') { Flush-Code }
    if ($tag -ne 'TR' -and $tag -ne 'TABLE' -and $null -ne $tableBuf -and $tag -ne 'ENDTABLE') {
        throw "Tabela nije zatvorena pre linije $n"
    }

    switch ($tag) {
        'TITLEPAGE' {
            $a = $body.Split('|')
            $script:AuthorName = $a[0]
            Write-TitlePage $a[0] $a[1] $a[2]
        }
        'TOC' {
            $sel.EndKey($wdStory) | Out-Null
            $sel.InsertBreak($wdSectionBreakNextPage)
            $script:sectionCount++
            Write-Plain 'Sadržaj' $stH1Plain
            $sel.EndKey($wdStory) | Out-Null
            $sel.Style = $stN
            $sel.TypeParagraph()
            $script:tocRange = $sel.Range
        }
        'PB'  { $sel.EndKey($wdStory) | Out-Null; $sel.InsertBreak($wdPageBreak) }
        'SB'  {
            if ($script:sectionCount -eq 2) { New-Section $true } else { New-Section $false }
        }
        'H1'  {
            $h1++; $h2 = 0; $h3 = 0
            Write-Styled "$h1. $body" $doc.Styles.Item($wdStyleHeading1)
        }
        'H2'  {
            $h2++; $h3 = 0
            Write-Styled "$h1.$h2. $body" $doc.Styles.Item($wdStyleHeading2)
        }
        'H3'  {
            $h3++
            Write-Styled "$h1.$h2.$h3. $body" $doc.Styles.Item($wdStyleHeading3)
        }
        'H1NN' { Write-Styled $body $doc.Styles.Item($wdStyleHeading1) }
        'H1X'  { Write-Plain $body $stH1Plain }
        'P'   { Write-Styled $body $stN }
        'BUL' { Write-Styled ([char]0x2022 + [char]9 + $body) $stBul }
        'LIT' { Write-Styled $body $stLit }
        'CODE' { [void]$codeBuf.Add($body) }
        'EQ'  {
            $a = $body.Split('|')
            Write-Equation $a[0] $a[1]
        }
        'IMG' {
            $a = $body.Split('|')
            Write-Image $a[0] ([double]$a[1])
        }
        'FIGPH' {
            $a = $body.Split('|')
            Write-Placeholder ([double]$a[0]) $a[1]
        }
        'FIGCAP' { Write-Styled $body $stFigCap }
        'TABCAP' { Write-Styled $body $stTabCap }
        'TABLE' {
            $tableCols = [int]$body
            $tableBuf = New-Object System.Collections.ArrayList
        }
        'TR' {
            if ($null -eq $tableBuf) { throw "TR bez TABLE na liniji $n" }
            [void]$tableBuf.Add($body)
        }
        'ENDTABLE' {
            Write-Table $tableBuf.ToArray() $tableCols
            $tableBuf = $null
        }
        default { throw "Nepoznata oznaka '$tag' na liniji $n" }
    }
}
Flush-Code

# ================= SADRZAJ (TOC) =================
Write-Host 'Generisem sadrzaj...'
$toc = $doc.TablesOfContents.Add($script:tocRange, $true, 1, 3)
$toc.TabLeader = 1     # tacke
$toc.RightAlignPageNumbers = $true
$toc.Update()
$toc.Range.Font.Name = 'Calibri'

# TOC ne treba da ima kurziv nasledjen iz stilova
foreach ($i in 1..3) {
    try {
        $tocStyle = $doc.Styles.Item("TOC $i")
        $tocStyle.Font.Name = 'Calibri'
        $tocStyle.Font.Size = 11
        $tocStyle.ParagraphFormat.SpaceAfter = 6
    } catch { }
}
$toc.Update()

# ================= CUVANJE =================
Write-Host 'Cuvam dokument...'
$doc.Fields.Update() | Out-Null
$doc.SaveAs2($OutFile, 16)   # wdFormatDocumentDefault = .docx
$pages = $doc.ComputeStatistics(2)   # wdStatisticPages
$words = $doc.ComputeStatistics(0)   # wdStatisticWords
$doc.Close($false)
$word.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($sel)  | Out-Null
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($doc)  | Out-Null
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null

Write-Host ''
Write-Host "Gotovo: $OutFile"
Write-Host "Strana: $pages   Reci: $words"
