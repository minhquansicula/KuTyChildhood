param([switch]$Execute)
$ErrorActionPreference = 'Stop'
$root = 'Assets/_Project/Art/Scene2'
$moves = [System.Collections.Generic.List[object]]::new()
function Add-Move($source, $destination) {
    $moves.Add([pscustomobject]@{sourcePath=$source; destinationPath=$destination})
}
# Preserve FBX subasset names and IDs; only AssetDatabase paths are changed.
$models = @{
 '20261018172426_f68c1c55'='Architecture/PlasterPanel_01';
 '20261018172727_4ebdb830'='Architecture/ImportedPanel_02';
 'bepcui1'='Kitchen/FirewoodStove_01'; 'bepcui2'='Kitchen/FirewoodStove_02';
 'BEPCUIIIII'='Kitchen/FirewoodStove_03'; 'beplua_naucom'='Kitchen/RiceCookingStove';
 'buoctuongmoi'='Architecture/WallPanel_01'; 'buoctuongmoi 1'='Architecture/WallPanel_02';
 'cailu'='Kitchen/RiceJar'; 'cotnha'='Architecture/TimberColumn';
 'gianbep'='Kitchen/KitchenStructure_01'; 'gianbep2'='Kitchen/KitchenStructure_02';
 'gocruachen'='Kitchen/DishwashingStation'; 'kecchen'='Kitchen/DishRack';
 'mainha'='Architecture/Roof'; 'moctreo'='Kitchen/WallHook';
 'noibeplua'='Kitchen/StovePot'; 'rembep'='Kitchen/KitchenCurtain';
 'thao'='Kitchen/WallBasin'; 'tivi'='Bedroom/TelevisionStand';
 'tranh'='Bedroom/WallArt'; 'tuchen'='Kitchen/DishCabinet';
 'tudocanhan'='Bedroom/StorageChest';
 'Act2Home/bed'='Bedroom/Bed'; 'Act2Home/house_1'='Architecture/TraditionalHouseInterior';
 'Act2Home/house_2'='Bedroom/RusticBedroom'; 'Act2Home/house-front'='Architecture/HouseFacade'
}
foreach($key in $models.Keys | Sort-Object) {
    $group,$name=$models[$key].Split('/')
    Add-Move "$root/Models/$key.fbx" "$root/Models/$group/SM_$name.fbx"
}
Add-Move "$root/Models/CotNha_Single.asset" "$root/Models/Architecture/SM_TimberColumn_Single.asset"
Add-Move "$root/Models/Act2Home/GroundTerracotta.asset" "$root/Models/Architecture/SM_TerracottaGround.asset"
$materials = @{
 Act2GroundTerracotta='Architecture/TerracottaGround'; Act2Wall_AgedPlaster='Architecture/AgedPlaster';
 Act2Wall_DarkTimber='Architecture/DarkTimber'; Act2Wall_Tripo7Timber='Architecture/HouseTimber';
 Act2Wall_WeatheredBase='Architecture/WeatheredFooting'; Asset20261018='Architecture/PlasterPanel';
 Asset20261018_LightPlaster='Architecture/LightPlaster'; BepCui1='Kitchen/FirewoodStove_01';
 BepCui2='Kitchen/FirewoodStove_02'; BepLuaNauCom='Kitchen/RiceCookingStove';
 BucTuongMoi='Architecture/WallPanel'; CaiLu='Kitchen/RiceJar'; CotNha='Architecture/TimberColumn';
 GianBep_AgedWalls='Kitchen/KitchenStructure_AgedWalls'; GianBep='Kitchen/KitchenStructure_01';
 GianBep2='Kitchen/KitchenStructure_02'; GocRuaChen='Kitchen/DishwashingStation';
 KeChen='Kitchen/DishRack'; MaiNha='Architecture/Roof'; MocTreo='Kitchen/WallHook';
 NoiBepLua='Kitchen/StovePot'; RemBep='Kitchen/KitchenCurtain';
 ThauTreoTuong='Kitchen/WallBasin'; TuChen='Kitchen/DishCabinet'
}
foreach($key in $materials.Keys | Sort-Object) {
    $group,$name=$materials[$key].Split('/')
    Add-Move "$root/Materials/$key.mat" "$root/Materials/$group/MAT_$name.mat"
    $folder="$root/Textures/${key}Textures"
    if(Test-Path -LiteralPath $folder) {
        foreach($file in Get-ChildItem -LiteralPath $folder -File -Filter '*.png') {
            $channel=switch -Regex ($file.BaseName) { '_normal$' {'Normal';break} '_metallic$' {'Metallic';break} '_roughness$' {'Roughness';break} default {'BaseColor'} }
            Add-Move "$folder/$($file.Name)" "$root/Textures/$group/T_${name}_${channel}.png"
        }
    }
}
foreach($pair in @(@('tranhtuong','WallArt'),@('ke_tivi','TelevisionStand'),@('ruongdo','StorageChest'))) {
    $old,$name=$pair
    Add-Move "$root/BedroomDecor/$old/Materials/texture_pbr_20250901.mat" "$root/Materials/Bedroom/MAT_$name.mat"
    foreach($file in Get-ChildItem -LiteralPath "$root/BedroomDecor/$old/Textures" -Filter '*.png' -File) {
        $channel=switch -Regex ($file.BaseName) { '_normal$' {'Normal';break} '_metallic$' {'Metallic';break} '_roughness$' {'Roughness';break} default {'BaseColor'} }
        Add-Move "$root/BedroomDecor/$old/Textures/$($file.Name)" "$root/Textures/Bedroom/T_${name}_${channel}.png"
    }
}
foreach($file in Get-ChildItem -LiteralPath "$root/Models/Act2Home/house_1" -File | Where-Object Extension -ne '.meta') {
    $part=$file.BaseName.Replace('traditional_house_1_3d_model_','').Replace('_basecolor','')
    $part=switch($part) { 'Bàn thờ' {'Altar'} 'Chair' {'Chair'} 'Floor' {'Floor'} 'Máy chơi nhạc' {'MusicPlayer'} 'Tủ đồ' {'Cabinet'} default { $part -replace 'tripo_part_(\d+)', 'Part$1' } }
    Add-Move "$root/Models/Act2Home/house_1/$($file.Name)" "$root/Textures/Architecture/T_TraditionalHouse_${part}_BaseColor.jpeg"
}
Add-Move "$root/Models/Act2Home/house_2/rustic_bedroom_3d_model_basecolor.JPEG" "$root/Textures/Bedroom/T_RusticBedroom_BaseColor.jpeg"
Add-Move "$root/Prefabs/Act2_BedroomWall.prefab" "$root/Prefabs/PF_BedroomWall.prefab"
Add-Move "$root/Prefabs/CotNha_Single.prefab" "$root/Prefabs/PF_TimberColumn_Single.prefab"
Add-Move "$root/Shaders/GianBepAgedWalls.shader" "$root/Shaders/SH_KitchenAgedWalls.shader"

$destinations=@{}
foreach($move in $moves) {
    if($destinations.ContainsKey($move.destinationPath)) { throw "Duplicate destination: $($move.destinationPath)" }
    $destinations[$move.destinationPath]=$true
    if(!(Test-Path -LiteralPath $move.sourcePath)) { throw "Missing source: $($move.sourcePath)" }
    if(Test-Path -LiteralPath $move.destinationPath) { throw "Destination already exists: $($move.destinationPath)" }
}
if(!$Execute) { $moves | ConvertTo-Json -Depth 5; return }

function Call-Unity($skill,$body) {
    $response=Invoke-RestMethod -Method Post -Uri "http://localhost:8090/skill/$skill" -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Compress -Depth 20))) -TimeoutSec 180
    if($response.status -ne 'success' -or $response.result.success -eq $false -or $response.result.failCount -gt 0) { throw ($response | ConvertTo-Json -Depth 20) }
    return $response.result
}
$health=Invoke-RestMethod 'http://localhost:8090/health'
if($health.projectName -ne 'Chia Khoa Ky Uc' -or $health.surfaceProfile -ne 'full') { throw 'Wrong project or restricted Unity surface.' }
$scene=Call-Unity 'scene_get_info' @{}
if($scene.scenePath -ne 'Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity') { throw 'Open Scene 2 first.' }
Call-Unity 'scene_save' @{} | Out-Null
$backup=Join-Path '.unity-validation' ('scene2-organization-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath $scene.scenePath -Destination "$backup/Before.unity"
$moves | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$backup/asset-moves.json" -Encoding utf8
$guidBefore=@{}
foreach($move in $moves) { $guidBefore[$move.destinationPath]=(Select-String -LiteralPath ($move.sourcePath+'.meta') -Pattern '^guid: ').Line }
$folders=@($moves.destinationPath | ForEach-Object { Split-Path $_ -Parent } | ForEach-Object { $_.Replace('\','/') } | Sort-Object -Unique | Where-Object { !(Test-Path -LiteralPath $_) } | ForEach-Object { @{folderPath=$_} })
if($folders.Count) { Call-Unity 'asset_create_folder_batch' @{items=ConvertTo-Json -InputObject $folders -Compress} | Out-Null }
for($i=0;$i -lt $moves.Count;$i+=40) {
    $chunk=@($moves | Select-Object -Skip $i -First 40)
    Call-Unity 'asset_move_batch' @{items=ConvertTo-Json -InputObject $chunk -Compress} | Out-Null
    Write-Output "Moved $([Math]::Min($i+40,$moves.Count))/$($moves.Count) assets"
}
foreach($move in $moves) {
    $guidAfter=(Select-String -LiteralPath ($move.destinationPath+'.meta') -Pattern '^guid: ').Line
    if($guidAfter -ne $guidBefore[$move.destinationPath]) { throw "GUID changed: $($move.destinationPath)" }
}
Call-Unity 'scene_save' @{} | Out-Null
Write-Output "SUCCESS: $($moves.Count) asset paths organized; all GUIDs preserved. Backup: $backup"
