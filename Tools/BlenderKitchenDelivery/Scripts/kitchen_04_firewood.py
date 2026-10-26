exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
INDEX=globals().get('INDEX',1);random.seed(400+INDEX)
bark=texmat('M_DryBark',(.125,.068,.028),.94,0,1024,'wood')
cut=texmat('M_DryWood',(.43,.265,.115),.84,0,1024,'wood')
end=texmat('M_WoodEnd',(.48,.32,.17),.88,0,1024,'endgrain')
root=empty(f'SM_Firewood_{INDEX:02d}');L=[.34,.39,.43,.315,.37][INDEX-1];R=[.026,.032,.028,.034,.03][INDEX-1]
root['feed_end']='+Y in Blender; Grip_Pivot at mass center. Align Feed_Anchor direction with WoodSlot local +Z.'
n=14;nr=15;vs=[];uv=[]
for j in range(nr):
 t=j/(nr-1);yy=(t-.5)*L;bend=(.012*math.sin(t*math.pi) if INDEX==3 else .003*math.sin(t*4+INDEX))
 for i in range(n):
  a=TAU*i/n;rr=R*(1+.10*math.sin(a*3+INDEX)+.05*math.cos(j*1.3+i*.6))
  xx=rr*math.cos(a);zz=rr*math.sin(a)
  if INDEX in [2,5]:zz=min(zz,R*.15) if INDEX==2 else min(zz,R*.65)
  if INDEX==4 and j==nr-1:yyi=yy+random.uniform(-.016,.012)
  else:yyi=yy+(.002*math.sin(i*2+INDEX) if j in [0,nr-1] else 0)
  vs.append((xx+bend,yyi,zz+R));uv.append((i/n,t))
faces=[];mats=[]
for j in range(nr-1):
 for i in range(n):
  faces.append((j*n+i,(j+1)*n+i,(j+1)*n+(i+1)%n,j*n+(i+1)%n))
  mats.append(1 if INDEX in [2,5] and math.sin(TAU*(i+.5)/n)>(.15 if INDEX==2 else .65) else 0)
for j in [0,nr-1]:
 ids=[j*n+i for i in range(n)];center=np.mean(np.array([vs[k] for k in ids]),axis=0);c=len(vs);vs.append(tuple(center));uv.append((.5,.5))
 for i in range(n):
  faces.append((c,ids[i],ids[(i+1)%n]) if j==0 else (c,ids[(i+1)%n],ids[i]));mats.append(2)
low=min(p[2] for p in vs);high=max(p[2] for p in vs);cz=(high+low)/2
vs=[(x,y,z-low-cz) for x,y,z in vs]
piv=empty('Grip_Pivot',root,(0,0,cz));o=mesh('Firewood_Mesh',vs,faces,bark,piv,uv);o.data.materials.append(cut);o.data.materials.append(end)
for p,mi in zip(o.data.polygons,mats):
 p.material_index=mi
 if mi==2:
  p.use_smooth=False
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(v.x/(R*2.5)+.5,v.z/(R*2.5)+.5)
feed=empty('Feed_Anchor',piv,(0,L/2,0));feed.rotation_euler.x=-math.pi/2
export(root);save(f'04_Firewood_{INDEX:02d}')
render(f'11_Firewood_{INDEX:02d}',[root],(.29,-.43,.31),(0,0,.025),55)
save(f'04_Firewood_{INDEX:02d}')
