"""Add a native component library to the existing Figcraft document."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent
commands = []
def cmd(command, **params):
    ref = {'$step': len(commands)}
    commands.append(dict(command=command, params=params))
    return ref

palette = dict(Canvas='#F3F5F7', Surface='#FFFFFF', Ink='#172B3A', Muted='#657581',
               Border='#DFE5E9', Accent='#167D80', AccentHover='#106569',
               AccentPressed='#0C5155', Subtle='#E4F3EF', Sidebar='#142C36',
               Selected='#2D535C', NavText='#C1D0D5', Warning='#FFF3DF',
               Danger='#A84949', DangerSubtle='#FFF0F0', Disabled='#E1E7EA')
collection = cmd('variable.createCollection', name='Harness Helper', modes=['Light'])
tokens = {k: cmd('variable.create', collection=collection, name='Color/'+k,
                 type='color', value=v) for k,v in palette.items()}
for n,v in [('Space/XS',4),('Space/S',8),('Space/M',16),('Space/L',24),
            ('Radius/Control',7),('Radius/Card',10),('Size/Control',40)]:
    tokens[n] = cmd('variable.create', collection=collection,name=n,type='number',value=v)

def bind(node, field, token):
    cmd('variable.bind', ids=[node], field=field, variable=tokens[token])
def box(parent,x,y,w,h,fill='Surface',name='Container',radius=10,stroke=None):
    p=dict(fill=palette[fill],cornerRadius=radius,clip=False)
    if stroke: p.update(stroke=palette[stroke],strokeWeight=1)
    node=cmd('node.create',type='frame',parent=parent,x=x,y=y,width=w,height=h,name=name,props=p)
    bind(node,'fill',fill)
    if stroke: bind(node,'stroke',stroke)
    return node
def text(parent,x,y,value,size=14,color='Ink',width=500,bold=False):
    node=cmd('text.create',parent=parent,x=x,y=y,width=width,text=value,
             props=dict(fontFamily='Segoe UI',fontSize=size,fontStyle='Bold' if bold else 'Regular',
                        fill=palette[color],lineHeight='145%',textAutoResize='height'))
    bind(node,'fill',color)
    return node
def component(node,name,label=None):
    cmd('component.create',ids=[node])
    cmd('node.set',id=node,name=name)
    if label:
        cmd('component.addProperty',id=node,name='Label',type='text',default=label[1],layer=label[0])
    return node
def board(x,title,subtitle,height=1000):
    b=box(None,x,0,1440,height,'Canvas',title,12)
    text(b,40,30,title,30,width=1350,bold=True)
    text(b,40,82,subtitle,14,'Muted',1340)
    return b
def note(b,y,title,body):
    text(b,40,y,title,20,width=1320,bold=True)
    text(b,40,y+34,body,13,'Muted',1330)

b=board(0,'01 / Fundamentos','Uma linguagem visual compartilhada entre Figcraft e WPF.')
for i,(name,value) in enumerate(palette.items()):
    x=40+(i%4)*345; y=144+(i//4)*115
    swatch=box(b,x,y,56,56,name,name,10,'Border')
    text(b,x+70,y,name,15,width=255,bold=True)
    text(b,x+70,y+27,value,13,'Muted',255)
note(b,630,'Tipografia / Segoe UI','Título 30 · Seção 20 · Cartão 17 · Corpo 14 · Auxiliar 12. Hierarquia antes de decoração.')
text(b,40,710,'Uma configuração mais clara',30,width=700,bold=True)
text(b,40,764,'Orientação contextual e decisões revisáveis.',17,'Muted',850)
note(b,835,'Espaçamento e forma','Escala 4 / 8 / 16 / 24 px · Controle 40 px · Raio 7 px em controles e 10 px em cartões.')
text(b,40,930,'WPF: ResourceDictionary → SolidColorBrush, medidas, estilos de texto e templates.',14,'Accent',1320)

b=board(1540,'02 / Controles e estados','Componentes nativos reutilizáveis. Estados desenhados explicitamente; comportamento será implementado no WPF.',1160)
note(b,130,'Button / ação principal','Use uma ação principal por grupo. Texto e tokens podem ser reutilizados nas instâncias.')
buttons=[]
states=[('Default','Accent','Surface'),('Hover','AccentHover','Surface'),('Pressed','AccentPressed','Surface'),
        ('Disabled','Disabled','Muted'),('Focus','Accent','Surface')]
for i,(state,fill,fg) in enumerate(states):
    x=40+i*275
    text(b,x,205,state,12,'Muted',240)
    bt=box(b,x,236,240,40,fill,'Button / Primary / '+state,7,'Ink' if state=='Focus' else None)
    if state=='Focus': cmd('node.set',id=bt,strokeWeight=3)
    label=text(bt,16,9,'Revisar proposta',13,fg,210,True)
    bind(bt,'height','Size/Control'); bind(bt,'cornerRadius','Radius/Control')
    buttons.append(component(bt,'Button/Primary/'+state,(label,'Revisar proposta')))
for i,(kind,fill,fg,stroke,label) in enumerate([
    ('Secondary','Surface','Accent','Accent','Cancelar'),
    ('Danger','Danger','Surface',None,'Remover plugin'),
    ('Busy','Accent','Surface',None,'Analisando…')]):
    bt=box(b,40+i*275,315,240,40,fill,kind,7,stroke)
    t=text(bt,16,9,label,13,fg,210,True)
    component(bt,'Button/'+kind,(t,label))
note(b,405,'TextField / conteúdo e validação','Label sempre visível. Mensagem de erro explica o que corrigir; cor não é o único indicador.')
for i,state in enumerate(['Default','Focus','Error']):
    f=box(b,40+i*455,478,430,128,'Surface',state,7)
    text(f,0,0,'Nome do agente',13,width=420,bold=True)
    inner=box(f,0,30,430,44,'Surface','Input',7,'Danger' if state=='Error' else 'Accent' if state=='Focus' else 'Border')
    label=text(inner,14,11,'code-reviewer' if state!='Error' else 'nome inválido!',14,width=396)
    text(f,0,84,'Erro: use um nome compatível com o harness.' if state=='Error' else 'Nome usado para identificar o recurso.',12,'Danger' if state=='Error' else 'Muted',420)
    component(f,'TextField/'+state,(label,'code-reviewer' if state!='Error' else 'nome inválido!'))
note(b,655,'Badge / origem e estado','Texto explícito. Use estado junto à origem; habilitado não significa carregado.')
for i,(label,fill,fg) in enumerate([('PROJETO','Subtle','Accent'),('PESSOAL','Canvas','Muted'),('ATENÇÃO','Warning','Ink'),('ERRO','DangerSubtle','Danger')]):
    q=box(b,40+i*275,729,240,28,fill,label,6)
    t=text(q,10,5,label,11,fg,218,True)
    component(q,'Badge/'+label,(t,label))
note(b,804,'NavigationItem / orientação','Selecionado deve ter contraste e peso; o foco de teclado é independente da seleção.')
for i,(state,fill) in enumerate([('Default','Sidebar'),('Selected','Selected')]):
    q=box(b,40+i*350,882,320,48,fill,state,7)
    t=text(q,16,13,'Agentes e skills',14,'Surface',284,state=='Selected')
    component(q,'NavigationItem/'+state,(t,'Agentes e skills'))
text(b,40,1025,'WPF: Button + ControlTemplate / TextBox + validação / DataTemplate de Badge / ListBoxItem.',14,'Accent',1340)
text(b,40,1060,'Hover, Pressed, Disabled e Focus: triggers ou VisualStateManager. Busy: comando assíncrono + IsBusy.',13,'Muted',1340)

b=board(3080,'03 / Blocos compostos','Componentes maiores combinam os controles e mantêm o contexto da tarefa.',1130)
card=box(b,40,150,650,230,'Surface','ResourceCard',10,'Border')
t=text(card,24,20,'code-reviewer',20,width=590,bold=True)
text(card,24,62,'Agente · Projeto · .claude/agents/code-reviewer.md',13,'Muted',598)
text(card,24,101,'Especialista em revisão de código e riscos de execução.',14,width=598)
cmd('instance.create',component=buttons[0],parent=card,x=24,y=165)
resource=component(card,'Card/Resource',(t,'code-reviewer'))
alert=box(b,730,150,670,230,'Warning','Alert',10)
t=text(alert,24,20,'Arquivo alterado externamente',20,width=620,bold=True)
text(alert,24,66,'Recarregue o conteúdo e revise o novo diff antes de aplicar.\nA versão atual deve ser preservada.',14,width=620)
cmd('instance.create',component=buttons[0],parent=alert,x=24,y=165)
component(alert,'Alert/Warning',(t,'Arquivo alterado externamente'))
context=box(b,40,430,650,220,'Surface','ContextSummary',10,'Border')
t=text(context,24,20,'Contexto da conversa',20,width=600,bold=True)
text(context,24,70,'Incluído: configurações + estrutura\nCódigo-fonte: somente arquivos selecionados\nCredenciais identificadas: omitidas',14,'Muted',600)
component(context,'Panel/ContextSummary',(t,'Contexto da conversa'))
empty=box(b,730,430,670,220,'Surface','EmptyState',10,'Border')
t=text(empty,24,20,'Nenhum recurso encontrado',20,width=620,bold=True)
text(empty,24,70,'Você pode criar um agente ou uma skill para este projeto.\nFalha de leitura deve aparecer como erro, não como vazio.',14,'Muted',620)
cmd('instance.create',component=buttons[0],parent=empty,x=24,y=155)
component(empty,'State/Empty',(t,'Nenhum recurso encontrado'))
note(b,710,'Instância de exemplo / mesmo componente, outro conteúdo','Abaixo: instância vinculada ao Card/Resource, com override da propriedade Label.')
example=cmd('instance.create',component=resource,parent=b,x=40,y=790)
cmd('instance.setProperties',ids=[example],properties={'Label':'security-reviewer'})
text(b,730,800,'WPF: composição por UserControl ou ContentControl.\nDados vêm do binding; ações vêm de ICommand.\nNão colocar parsing, CLI ou escrita de arquivos no controle.',16,'Muted',635)
text(b,730,935,'O desenho define aparência e estados.\nAcessibilidade, teclado e layout adaptável\nprecisam ser implementados e testados no WPF.',14,'Muted',635)

requests=[dict(id=1,method='engine.execute',params=dict(command='file.save',params=dict(path=str(root/'Harness-Helper.figcraft')))),
          dict(id=2,method='engine.execute',params=dict(command='page.add',params=dict(name='02 · Componentes reutilizáveis'))),
          dict(id=3,method='engine.execute',params=dict(command='command.batch',params=dict(label='Biblioteca reutilizável',commands=commands))),
          dict(id=4,method='engine.execute',params=dict(command='file.save',params=dict(path=str(root/'Harness-Helper.figcraft')))),
          dict(id=5,method='document.inspect',params=dict(depth=2,allPages=True)),
          dict(id=6,method='engine.execute',params=dict(command='component.list',params={})),
          dict(id=7,method='engine.execute',params=dict(command='view.zoomAt',params=dict(x=1540,y=0,width=1440,height=1160)))]
(root/'components.jsonl').write_text('\n'.join(json.dumps(r,ensure_ascii=False) for r in requests)+'\n',encoding='utf-8')
print(len(commands),'commands prepared')
