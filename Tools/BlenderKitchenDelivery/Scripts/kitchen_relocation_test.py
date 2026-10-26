import pathlib,shutil,subprocess,json,sys,hashlib
out=pathlib.Path('E:/FPT/FALL26/PRU213/KuTy/Tools/BlenderKitchenDelivery')
target=pathlib.Path('C:/Users/LENOVO/AppData/Local/Temp/opencode/kitchen-relocation-check')
target.mkdir(parents=True,exist_ok=True)
shutil.copy2(out/'KitchenAssets.blend',target/'KitchenAssets.blend')
shutil.copytree(out/'Textures',target/'Textures',dirs_exist_ok=True)
probe=target/'probe.py'
probe.write_text('''import bpy,os,json,pathlib
base=pathlib.Path(bpy.data.filepath).parent
result={'file':str(base/'KitchenAssets.blend'),'images':{},'missing':[],'outside_delivery':[]}
for im in bpy.data.images:
 if im.source=='FILE' and im.name.startswith('M_'):
  path=pathlib.Path(bpy.path.abspath(im.filepath))
  exists=path.is_file()
  if exists:im.reload()
  loaded=bool(im.size[0] and im.size[1])
  result['images'][im.name]={'path':im.filepath,'exists':exists,'loaded':loaded,'resolution':list(im.size)}
  if not exists or not loaded:result['missing'].append(im.name)
  if not str(path).lower().startswith(str(base).lower()):result['outside_delivery'].append(im.name)
result['pass']=not result['missing'] and not result['outside_delivery']
(base/'probe_result.json').write_text(json.dumps(result,indent=2))
print('RELOCATION_PASS',result['pass'])
''',encoding='utf8')
p=subprocess.run(['D:/blender.exe','--background','--disable-autoexec',str(target/'KitchenAssets.blend'),'--python',str(probe)],capture_output=True,text=True,timeout=240)
(out/'RelocationValidation.log').write_text(p.stdout+'\n'+p.stderr,encoding='utf8')
if not (target/'probe_result.json').exists():raise RuntimeError(p.stdout+'\n'+p.stderr)
r=json.loads((target/'probe_result.json').read_text());r['process_exit_code']=p.returncode
r['copied_texture_hashes_match']=all(hashlib.sha256(f.read_bytes()).hexdigest()==hashlib.sha256((target/'Textures'/f.name).read_bytes()).hexdigest() for f in (out/'Textures').glob('*.png'))
r['pass']=r['pass'] and p.returncode==0 and r['copied_texture_hashes_match']
(out/'RelocationValidation.json').write_text(json.dumps(r,indent=2),encoding='utf8')
print(json.dumps({'pass':r['pass'],'images_checked':len(r['images']),'missing':r['missing'],'outside_delivery':r['outside_delivery'],'exit_code':p.returncode},indent=2))
if r['pass']:shutil.rmtree(target)
