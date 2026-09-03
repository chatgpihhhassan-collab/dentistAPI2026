Add-Type -AssemblyName System.Drawing

$outDir = "f:\DentistApp_Theme2\public\tooth_textures"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force }

$items = @(
    @{ Src = "C:\Users\haider.ali\.gemini\antigravity-ide\brain\4a9a37fb-a1e7-462e-ab71-34f74624aa20\.user_uploaded\media_1787897944596.png"; Dest = "$outDir\molar.png" },
    @{ Src = "C:\Users\haider.ali\.gemini\antigravity-ide\brain\4a9a37fb-a1e7-462e-ab71-34f74624aa20\premolar_occlusal_texture_1787898001847.jpg"; Dest = "$outDir\premolar.png" },
    @{ Src = "C:\Users\haider.ali\.gemini\antigravity-ide\brain\4a9a37fb-a1e7-462e-ab71-34f74624aa20\canine_occlusal_texture_1787898048009.jpg"; Dest = "$outDir\canine.png" },
    @{ Src = "C:\Users\haider.ali\.gemini\antigravity-ide\brain\4a9a37fb-a1e7-462e-ab71-34f74624aa20\incisor_occlusal_texture_1787898095558.jpg"; Dest = "$outDir\incisor.png" }
)

foreach ($item in $items) {
    if (Test-Path $item.Src) {
        $bmp = [System.Drawing.Bitmap]::FromFile($item.Src)
        $outBmp = New-Object System.Drawing.Bitmap($bmp.Width, $bmp.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        
        for ($y = 0; $y -lt $bmp.Height; $y++) {
            for ($x = 0; $x -lt $bmp.Width; $x++) {
                $c = $bmp.GetPixel($x, $y)
                if ($c.R -gt 248 -and $c.G -gt 248 -and $c.B -gt 248) {
                    $outBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 255, 255, 255))
                } elseif ($c.R -gt 235 -and $c.G -gt 235 -and $c.B -gt 235) {
                    $alpha = [int](255 * (255 - (($c.R + $c.G + $c.B) / 3)) / 20)
                    if ($alpha -lt 0) { $alpha = 0 }
                    if ($alpha -gt 255) { $alpha = 255 }
                    $outBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $c.R, $c.G, $c.B))
                } else {
                    $outBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
                }
            }
        }
        $outBmp.Save($item.Dest, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $outBmp.Dispose()
        Write-Output "Processed: $($item.Dest)"
    }
}
