import urllib.request, pathlib, json, concurrent.futures
R=pathlib.Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery/Source/Resources');R.mkdir(parents=True,exist_ok=True)
paths=['makehuman/data/3dobjs/base.obj','makehuman/data/rigs/default.mhskel','makehuman/data/rigs/default_weights.mhw','makehuman/data/targets/macrodetails/asian-female-young.target','makehuman/data/targets/macrodetails/asian-female-old.target','makehuman/data/targets/macrodetails/universal-female-young-averagemuscle-averageweight.target','makehuman/data/targets/macrodetails/universal-female-old-averagemuscle-averageweight.target','makehuman/data/targets/expression/units/asian/eye-left-closure.target','makehuman/data/targets/expression/units/asian/eye-right-closure.target','makehuman/data/targets/expression/units/asian/mouth-corner-puller.target','makehuman/data/targets/expression/units/asian/mouth-open.target','makehuman/data/eyes/low-poly/low-poly.obj','makehuman/data/eyes/low-poly/low-poly.mhclo','LICENSE.CC0']
def get(path):
 try:
  data=urllib.request.urlopen('https://raw.githubusercontent.com/makehumancommunity/makehuman/master/'+path,timeout=60).read();(R/path.split('/')[-1]).write_bytes(data);return (path,len(data))
 except Exception as e:return(path,str(e))
print(json.dumps(list(concurrent.futures.ThreadPoolExecutor(6).map(get,paths)),indent=2))
