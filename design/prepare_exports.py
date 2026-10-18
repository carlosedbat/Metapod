import json
from pathlib import Path
p=Path(__file__).resolve().parent
r=json.loads((p/'build-result.json').read_text(encoding='utf-8-sig'))
frames=r[4]['result']['pages'][0]['children']
req=[]
for n,f in enumerate(frames):
    req.append(dict(id=n+1,method='engine.execute',params=dict(command='file.export',params=dict(ids=[f['id']],format='png',scale=1,path=str(p/f"screen-{n+1:02}.png")))))
req.append(dict(id=20,method='document.inspect',params=dict(depth=12,allPages=True)))
(p/'export.jsonl').write_text('\n'.join(json.dumps(x,ensure_ascii=False) for x in req)+'\n',encoding='utf-8')
print('Prepared exports:',len(frames))
