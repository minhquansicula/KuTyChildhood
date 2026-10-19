$ErrorActionPreference='Stop'
$names=@{
 'godownbed'='BedExitPlatform'; 'Act2 Systems'='Act2Systems'; 'Point Light'='HouseAmbientLight';
 'HouseLayout/lunuoc'='TraditionalHouseInterior';
 'HouseLayout/lunuoc/tripo_part_10'='WaterJarVisual';
 'HouseLayout/MaiNha'='Roof'; 'HouseLayout/CotNha'='TimberColumns';
 'HouseLayout/CotNha/CotNha_Single'='TimberColumn_01';
 'HouseLayout/KitchenArea/FirewoodStove/NoiBepLua'='StovePotVisual';
 'HouseLayout/KitchenArea/DishCabinet/TuChen'='DishCabinetVisual';
 'HouseLayout/KitchenArea/Sink/GocRuaChen'='DishwashingStationVisual';
 'HouseLayout/KitchenArea/RemBep'='KitchenCurtains';
 'HouseLayout/KitchenArea/BepLuaNauCom'='RiceCookingStove';
 'HouseLayout/KitchenArea/ThauTreoTuong'='WallBasins';
 'HouseLayout/KitchenArea/BepCui1'='FirewoodStove_01';
 'HouseLayout/KitchenArea/BepCui2'='FirewoodStove_02';
 'HouseLayout/KitchenArea/KeChen'='DishRack';
 'HouseLayout/KitchenArea/CaiLu'='RiceJar';
 'HouseLayout/KitchenArea/GianBep2'='KitchenStructure';
 'HouseLayout/KitchenArea/GianBep2/MocTreo'='WallHooks';
 'HouseLayout/KitchenArea/GianBep2/20261018172426_f68c1c55'='PlasterPanels';
 'BedroomArea/Decor_Textured'='BedroomDecor';
 'BedroomArea/Decor_Textured/tranhtuong'='WallArt';
 'BedroomArea/Decor_Textured/ke_tivi'='TelevisionStand';
 'BedroomArea/Decor_Textured/ruongdo'='StorageChest';
 'BedroomArea/BedroomProps_New'='BedroomPropAnchors';
 'BedroomArea/Act2_BedroomWall (1)'='BedroomWall';
 'GianBep'='KitchenStructureAnchor'
}
$hierarchy=Invoke-RestMethod -Method Post 'http://localhost:8090/skill/scene_get_hierarchy' -ContentType 'application/json' -Body '{"maxDepth":12}'
$items=[System.Collections.Generic.List[object]]::new()
function Visit($nodes,$prefix) {
    foreach($node in $nodes) {
        $path=if($prefix){"$prefix/$($node.name)"}else{$node.name}
        $new=$names[$path]
        if($path -like 'HouseLayout/KitchenArea/GianBep2/20261018172426_f68c1c55/*') {
            $new='PlasterPanel_'+('{0:D2}' -f (1+@($items | Where-Object newName -like 'PlasterPanel_*').Count))
        }
        if($new) { $items.Add(@{instanceId=$node.instanceId;newName=$new;oldPath=$path}) }
        if($node.children) { Visit $node.children $path }
    }
}
Visit $hierarchy.result.hierarchy ''
$body=@{items=ConvertTo-Json -InputObject @($items | ForEach-Object { @{instanceId=$_.instanceId;newName=$_.newName} }) -Compress} | ConvertTo-Json -Compress
$dry=Invoke-RestMethod -Method Post 'http://localhost:8090/skill/gameobject_rename_batch?mode=dryRun' -ContentType 'application/json' -Body $body
if(!$dry.valid -or !$dry.authorization.allowed) { throw ($dry | ConvertTo-Json -Depth 12) }
$backup=Get-ChildItem .unity-validation -Directory -Filter 'scene2-organization-*' | Sort-Object Name -Descending | Select-Object -First 1
$items | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$($backup.FullName)/hierarchy-renames.json" -Encoding utf8
$result=Invoke-RestMethod -Method Post 'http://localhost:8090/skill/gameobject_rename_batch' -ContentType 'application/json' -Body $body -TimeoutSec 180
if($result.status -ne 'success' -or $result.result.failCount -gt 0) { throw ($result | ConvertTo-Json -Depth 15) }
$result.result | Select-Object totalItems,successCount,failCount | ConvertTo-Json
Invoke-RestMethod -Method Post 'http://localhost:8090/skill/scene_save' -ContentType 'application/json' -Body '{}' | ConvertTo-Json -Depth 5
