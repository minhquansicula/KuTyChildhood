import argparse, json, socket, pathlib
p=argparse.ArgumentParser();p.add_argument('script',nargs='?');p.add_argument('--code');a=p.parse_args()
code=a.code if a.code is not None else pathlib.Path(a.script).read_text(encoding='utf-8')
s=socket.create_connection(('127.0.0.1',9876),timeout=20);s.settimeout(1800)
s.sendall(json.dumps({'type':'execute_code','params':{'code':code}}).encode())
b=b''
while True:
 d=s.recv(65536)
 if not d:break
 b+=d
 try:
  result=json.loads(b);break
 except (json.JSONDecodeError,UnicodeDecodeError):pass
print(json.dumps(result,ensure_ascii=False,indent=2));s.close()
