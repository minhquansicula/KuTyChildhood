"""Create the house-05 installer from the proven house-04 editor workflow."""
from pathlib import Path
import uuid
project=Path(__file__).resolve().parents[2]
template=project/'Assets/_Project/Editor/Scene3VillageComposition.TencentHouse04.cs'
text=template.read_text(encoding='utf-8')
for before,after in [('House04','House05'),('house04','house05'),('house 04','house 05'),('House 04','House 05'),
    ('Nha04','Nha05'),('Tencent_04','Tencent_05'),('SanNha_Gon_04','SanNha_Gon_05'),
    ('HomeNames[3]','HomeNames[4]'),('fourth','fifth'),('Fourth','Fifth'),
    ('with wood kitchen 04','05'),('with wood kitchen','05'),
    ('new Vector3(18,.1f,66)','new Vector3(-23,.1f,65)'),('Quaternion.Euler(0,270,0)','Quaternion.Euler(0,90,0)'),
    ('house.transform.localScale=Vector3.one;','house.transform.localScale=Vector3.one*.8f;'),
    ('bounds.min.x<10 || bounds.max.x>26 || bounds.min.z<58 || bounds.max.z>74','bounds.min.x<-31 || bounds.max.x>-15 || bounds.min.z<58 || bounds.max.z>72'),
    ('(18, 0.1, 66)','(-23, 0.1, 65)')]:
    text=text.replace(before,after)
start=text.index('    private static void FinalizeHouse05Shading()')
end=text.index('    private static Material CreateHouse05Material',start)
text=text[:start]+text[end:]
text=text.replace('VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateScene();AssetDatabase.SaveAssets();',
    'VerifyGameplay(gameplay);Physics.SyncTransforms();VerifyHouse05Collision(house.transform);ValidateScene();AssetDatabase.SaveAssets();')
helper='''    private static void VerifyHouse05Collision(Transform house)
    {
        var group=house.GetComponent<LODGroup>();
        var collider=house.GetComponentInChildren<MeshCollider>();
        if(!collider || collider.convex || collider.isTrigger || collider.GetComponent<Rigidbody>())
            throw new Exception("House 05 requires a static solid mesh collider.");
        var bounds=RendererBounds(house);var center=bounds.center;center.y=bounds.min.y+1;
        var rays=new[]{new Ray(new Vector3(bounds.max.x+2,center.y,center.z),Vector3.left),
            new Ray(new Vector3(bounds.min.x-2,center.y,center.z),Vector3.right),
            new Ray(new Vector3(center.x,center.y,bounds.max.z+2),Vector3.back),
            new Ray(new Vector3(center.x,center.y,bounds.min.z-2),Vector3.forward),
            new Ray(new Vector3(center.x,bounds.max.y+2,center.z),Vector3.down)};
        try
        {
            group.ForceLOD(0);Physics.SyncTransforms();
            if(!collider.enabled || !collider.gameObject.activeInHierarchy) throw new Exception("House collider disabled with near LOD.");
            for(var i=0;i<rays.Length;i++)
                if(!collider.Raycast(rays[i],out var hit,20)) throw new Exception("House 05 collision ray missed wall/roof: "+i);
            group.ForceLOD(2);Physics.SyncTransforms();
            if(!collider.enabled || !collider.gameObject.activeInHierarchy) throw new Exception("House collider disabled with far LOD.");
            var approach=new Vector3(bounds.max.x+.8f,2,center.z);
            var ground=Physics.RaycastAll(approach,Vector3.down,3,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.rigidbody && !h.collider.GetComponent<CharacterController>()).OrderBy(h=>h.distance).ToArray();
            if(ground.Length==0 || ground[0].point.y>.5f || ground[0].point.y<-.1f)
                throw new Exception("House 05 front approach is not walkable.");
        }
        finally {group.ForceLOD(-1);}
    }

'''
text=text.replace('    private static Material CreateHouse05Material',helper+'    private static Material CreateHouse05Material')
text=text.replace('Low-LOD static mesh collider.','Low-LOD static mesh collider; four walls/roof raycasts and LOD collision persistence verified. Front approach walkable.')
output=project/'Assets/_Project/Editor/Scene3VillageComposition.TencentHouse05.cs'
meta=Path(str(output)+'.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
output.write_text(text,encoding='utf-8')
