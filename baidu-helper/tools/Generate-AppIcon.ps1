param(
    [string]$OutputPath = "src\BaiduShareTool.App\Resources\app.ico"
)

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

$bitmap = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$background = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 22, 54, 92))
$backgroundPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$backgroundPath.AddArc(8, 8, 96, 96, 180, 90)
$backgroundPath.AddArc(152, 8, 96, 96, 270, 90)
$backgroundPath.AddArc(152, 152, 96, 96, 0, 90)
$backgroundPath.AddArc(8, 152, 96, 96, 90, 90)
$backgroundPath.CloseFigure()
$graphics.FillPath($background, $backgroundPath)

$cloudPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 70, 214, 198)), 16
$cloudPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$cloudPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$cloudPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$cloudPath.AddBezier(54, 152, 48, 119, 70, 94, 102, 99)
$cloudPath.AddBezier(102, 99, 110, 66, 162, 66, 174, 111)
$cloudPath.AddBezier(174, 111, 211, 108, 224, 145, 196, 162)
$cloudPath.AddLine(196, 162, 84, 162)
$cloudPath.CloseFigure()
$graphics.DrawPath($cloudPen, $cloudPath)

$arrowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White, 15
)
$arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLine($arrowPen, 86, 183, 167, 183)
$graphics.DrawLine($arrowPen, 167, 183, 143, 159)
$graphics.DrawLine($arrowPen, 167, 183, 143, 207)

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$pngStream = New-Object System.IO.MemoryStream
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()
$writer = New-Object System.IO.BinaryWriter([System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create))
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)
$writer.Write($pngBytes)
$writer.Dispose()
$pngStream.Dispose()
$cloudPath.Dispose()
$backgroundPath.Dispose()
$cloudPen.Dispose()
$arrowPen.Dispose()
$background.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
