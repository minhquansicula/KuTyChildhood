exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
wood=bpy.data.materials['M_DryWood'];root=empty('SM_RicePaddle');pivot=empty('Grip_Pivot',root,(0,-.105,.009))
profile=[(-.125,.001,.001),(-.122,.007,.005),(-.113,.009,.006),(-.06,.008,.006),(.012,.009,.005),(.035,.023,.004),(.065,.034,.0045),(.09,.033,.004),(.111,.026,.0035),(.124,.012,.0025),(.127,.001,.001)]
vs=[];uv=[];n=16
for j,(y,w,d) in enumerate(profile):
 for i in range(n):
  a=TAU*i/n;vs.append((w*math.cos(a),y+.105,d*math.sin(a)));uv.append((i/n,j/(len(profile)-1)))
fs=[]
for j in range(len(profile)-1):
 for i in range(n):fs.append((j*n+i,(j+1)*n+i,(j+1)*n+(i+1)%n,j*n+(i+1)%n))
fs.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))])
o=mesh('Paddle_Mesh',vs,fs,wood,pivot,uv);root['interaction']='Rotate Grip_Pivot; root is floor-reference only. Paddle length .252m.'
export(root);save('05_RicePaddle');render('12_RicePaddle',[root],(.24,-.36,.29),(0,0,.008),55);save('05_RicePaddle')
