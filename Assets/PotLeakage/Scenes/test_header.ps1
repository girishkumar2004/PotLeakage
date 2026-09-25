# Generate PotLeakage_Main.unity scene
$scratchDir = "d:\unity cli\PotLeakage\Assets\PotLeakage\Scenes"
if (!(Test-Path $scratchDir)) {
    New-Item -ItemType Directory -Path $scratchDir -Force
}

$sampleScenePath = "d:\unity cli\PotLeakage\Assets\Scenes\SampleScene.unity"
$sampleContent = Get-Content $sampleScenePath -Raw

# Extract header from SampleScene (Occlusion, RenderSettings, LightmapSettings, NavMesh)
$headerEnd = $sampleContent.IndexOf("--- !u!1 &330585543")
if ($headerEnd -lt 0) { $headerEnd = 1000 }
$header = $sampleContent.Substring(0, $headerEnd)

Write-Host "Header length: $($header.Length)"
