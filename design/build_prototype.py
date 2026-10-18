"""Generate editable Figcraft commands. Does not launch apps or connect to the network."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
C = dict(bg='#F3F5F7', white='#FFFFFF', ink='#172B3A', muted='#657581', line='#DFE5E9',
         nav='#142C36', navtext='#C1D0D5', teal='#167D80', pale='#E4F3EF', orange='#AC641F',
         amber='#FFF3DF', blue='#EAF0FB', red='#A84949')
commands=[]
frames={}
links=[]
navs=[]

def cmd(command, **params):
    i=len(commands)
    commands.append(dict(command=command,params=params))
    return {'$step':i}

def box(parent,x,y,w,h,fill=None,name='Panel',radius=10,stroke=None):
    props=dict(fill=fill or C['white'],cornerRadius=radius,clip=False)
    if stroke: props.update(stroke=stroke,strokeWeight=1)
    return cmd('node.create',type='frame',parent=parent,x=x,y=y,width=w,height=h,name=name,props=props)

def text(parent,x,y,value,size=14,color=None,width=500,bold=False,name=None):
    return cmd('text.create',parent=parent,x=x,y=y,width=width,text=value,
               props=dict(fontFamily='Segoe UI',fontSize=size,fontStyle='Bold' if bold else 'Regular',
                          fill=color or C['ink'],lineHeight=f'{145}%',textAutoResize='height'))

def button(parent,x,y,label,dest=None,w=180,primary=True):
    b=box(parent,x,y,w,40,C['teal'] if primary else C['white'],label,7,C['teal'] if not primary else None)
    text(b,14,9,label,13,C['white'] if primary else C['teal'],w-26,True)
    if dest: links.append((b,dest))
    return b

def badge(parent,x,y,label,color=None,w=130):
    b=box(parent,x,y,w,26,C['pale'],label,6)
    text(b,10,4,label,11,color or C['teal'],w-18,True)
    return b

def card(parent,x,y,w,h,title,body=None):
    b=box(parent,x,y,w,h,stroke=C['line'],name=title)
    text(b,20,16,title,17,width=w-40,bold=True)
    if body: text(b,20,52,body,13,C['muted'],w-40)
    return b

NAV=[('overview','Visão geral'),('inventory','Agentes e skills'),('conversation','Assistente'),
     ('plugins','Plugins'),('discover','Descobrir'),('history','Histórico')]

def shell(key,title,subtitle,selected=None):
    i=len(frames)
    f=box(None,(i%3)*1460,(i//3)*980,1360,880,C['bg'],f'{i+1:02} · {title}',12)
    frames[key]=f
    side=box(f,0,0,212,880,C['nav'],'Navegação',0)
    text(side,24,25,'h / helper',24,C['white'],170,True)
    text(side,24,65,'SEU AMBIENTE DE IA',10,C['navtext'],170,True)
    project=box(side,16,112,180,64,'#24424D','Projeto atual',8)
    text(project,14,10,'Caltek.Api',16,C['white'],145,True)
    text(project,14,36,'Projeto de demonstração',11,C['navtext'],152)
    for j,(target,label) in enumerate(NAV):
        active=target==(selected or key)
        b=box(side,16,207+j*48,180,40,'#2D535C' if active else C['nav'],label,7)
        text(b,14,9,label,13,C['white'] if active else C['navtext'],155,active)
        navs.append((b,target))
    text(side,24,664,'PRÓXIMAS EVOLUÇÕES',10,C['navtext'],170,True)
    text(side,24,695,'Configurar do zero\nBiblioteca pessoal\nOutros assistentes',12,'#90A8B1',165)
    text(side,24,825,'Windows  •  Claude Code',11,C['navtext'],180)
    header=box(f,212,0,1148,72,C['white'],'Barra do projeto',0)
    text(header,28,23,'Projetos  /  Caltek.Api',15,width=350,bold=True)
    badge(header,650,23,'CLAUDE CODE',w=125)
    text(header,805,27,'PROTÓTIPO · DADOS FICTÍCIOS',10,C['muted'],290)
    text(f,244,105,title,30,width=850,bold=True)
    text(f,244,154,subtitle,13,C['muted'],1050)
    text(f,244,847,'Escopo sempre visível. Alterações somente após revisão.',11,C['muted'],1000)
    return f

def assistant(f,heading='Vamos melhorar seu projeto?'):
    b=card(f,1004,202,324,610,'Assistente do projeto')
    badge(b,20,52,'CONTEXTO SELECIONADO',w=211)
    text(b,20,97,heading,20,width=280,bold=True)
    text(b,20,160,'Use a conversa para entender uma\nsugestão e transformar a ideia em\numa proposta revisável.',14,C['muted'],280)
    box(b,20,259,284,108,C['bg'],'Contexto permitido',8)
    text(b,34,274,'Incluído: configurações + estrutura\nCódigo-fonte: não selecionado\nCredenciais identificadas: omitidas',12,C['muted'],260)
    button(b,20,402,'Abrir conversa','conversation',w=284)
    button(b,20,455,'Revisar contexto','context',w=284,primary=False)
    text(b,20,529,'A IA propõe. Você decide o que aplicar.',12,C['muted'],277)

# 01 Dashboard
f=shell('overview','Um projeto mais claro. Um agente mais útil.',
        'Veja o que existe, entenda as prioridades e escolha o próximo ajuste.')
for x,n,label in [(244,'12','Recursos encontrados'),(492,'3','Sugestões para revisar'),(740,'2','Escopos encontrados')]:
    b=box(f,x,202,232,110,name=label,stroke=C['line'])
    text(b,20,12,n,34,width=160,bold=True); text(b,20,69,label,12,C['muted'],196)
b=card(f,244,334,728,267,'Seu próximo passo','Diagnóstico demonstrativo · confirme as evidências antes de aplicar.')
badge(b,20,86,'ORGANIZAÇÃO',w=130)
text(b,20,127,'O procedimento de revisão está nas instruções gerais.',20,width=670,bold=True)
text(b,20,166,'Uma skill pode guardar os passos e carregar esse conteúdo quando necessário.\nConfira a proposta e mantenha as regras essenciais nas instruções do projeto.',13,C['muted'],674)
button(b,20,214,'Entender sugestão','conversation',w=193)
button(b,228,214,'Ver recursos','inventory',w=170,primary=False)
b=card(f,244,621,728,191,'Origem das configurações')
text(b,20,59,'Pessoal     ~/.claude/                         Preferências entre projetos\nProjeto     Caltek.Api/.claude/             Regras compartilhadas\nLocal        settings.local.json             Ajustes só desta máquina',14,C['muted'],670)
assistant(f)

# 02 Inventory
f=shell('inventory','Agentes, skills e instruções','Cada recurso com sua finalidade, origem e escopo. Sem misturar as regras de prioridade.')
button(f,1133,112,'Criar recurso','editor',w=195)
b=box(f,244,202,1084,56,name='Filtros',stroke=C['line'])
text(b,18,17,'Buscar por nome ou finalidade…',14,C['muted'],420)
text(b,570,18,'Todos os tipos     |     Todos os escopos',13,C['muted'],480)
rows=[('code-reviewer','Agente','Projeto','Revisão técnica com ferramentas de leitura'),
      ('review-api','Skill','Projeto','Passos repetíveis para revisar endpoints'),
      ('CLAUDE.md','Instruções','Projeto','Comandos e convenções essenciais'),
      ('documentar','Skill','Pessoal','Orientação reutilizada entre projetos'),
      ('settings.json','Configuração','Pessoal','Preferências e permissões pessoais')]
b=box(f,244,278,1084,382,name='Inventário',stroke=C['line'])
text(b,20,17,'RECURSO',11,C['muted'],300,True);text(b,350,17,'TIPO / ESCOPO',11,C['muted'],250,True)
text(b,600,17,'FINALIDADE',11,C['muted'],420,True)
for j,(name,kind,scope,desc) in enumerate(rows):
    y=55+j*61
    row=box(b,10,y,1064,55,C['bg'] if j==0 else C['white'],name,5)
    text(row,10,16,name,14,width=310,bold=True)
    text(row,340,16,f'{kind}  ·  {scope}',13,C['muted'],240)
    text(row,590,16,desc,13,C['muted'],460)
    links.append((row,'editor'))
b=card(f,244,682,1084,130,'Por que o escopo importa?')
text(b,20,55,'Agentes, skills e settings não têm a mesma ordem de prioridade. Abra um recurso para consultar sua origem.\nEsta lista representa arquivos encontrados; o estado carregado pelo CLI pode depender da sessão.',13,C['muted'],1025)

# 03 Editor
f=shell('editor','Editar com intenção','Recurso / code-reviewer  ·  proposta ainda não aplicada','inventory')
b=card(f,244,202,728,610,'Definição do agente')
badge(b,20,54,'ESCOPO: PROJETO',w=160)
text(b,20,97,'Destino',12,C['muted'],650,True)
text(b,20,122,'.claude/agents/code-reviewer.md',14,width=665)
text(b,20,171,'Nome',12,C['muted'],200,True)
field=box(b,20,198,688,44,C['bg'],'Nome',6);text(field,13,11,'code-reviewer',14,width=650)
text(b,20,264,'Quando este agente deve atuar?',13,width=650,bold=True)
field=box(b,20,292,688,84,C['bg'],'Descrição',6)
text(field,13,12,'Use ao revisar alterações de uma API .NET. Identifique riscos\ncom evidência no diff e proponha verificações proporcionais.',14,width=650)
text(b,20,399,'Ferramentas permitidas',13,width=650,bold=True)
badge(b,20,431,'Read',w=90);badge(b,122,431,'Grep',w=90);badge(b,224,431,'Glob',w=90)
text(b,20,486,'Modelo: herdar da sessão  ·  Prévia Markdown disponível na revisão.',12,C['muted'],665)
button(b,20,547,'Revisar alteração','review',w=200)
button(b,237,547,'Pedir melhoria à IA','conversation',w=200,primary=False)
assistant(f,'Agente ou skill?')

# 04 Conversation
f=shell('conversation','Pense junto com o assistente','Conversa vinculada ao projeto. Respostas podem gerar propostas; não aplicam alterações.')
b=card(f,244,202,728,610,'Melhorar a revisão da API')
user=box(b,143,61,565,79,C['blue'],'Mensagem do usuário',10)
text(user,16,14,'Quero uma revisão mais consistente sem repetir\num prompt grande a cada tarefa.',15,width=525)
text(b,20,167,'ASSISTENTE  /  EXEMPLO DE RESPOSTA',10,C['teal'],660,True)
text(b,20,198,'Podemos separar responsabilidade e procedimento.',21,width=665,bold=True)
text(b,20,249,'O agente define o foco da revisão e as ferramentas disponíveis.\nA skill guarda os passos reutilizáveis e o formato do resultado.\n\nNeste exemplo, eu começaria ajustando a descrição do agente.\nA economia de tokens precisa ser medida; não é garantida.',14,C['muted'],665)
button(b,20,407,'Preparar proposta','review',w=200)
button(b,235,407,'Explorar ferramentas','discover',w=205,primary=False)
field=box(b,20,485,688,99,C['bg'],'Compositor',8,C['line'])
text(field,15,14,'Pergunte sobre seu projeto…',14,C['muted'],600)
text(field,15,59,'+ Contexto     / Blocos     / Termos',12,C['teal'],430)
button(field,535,49,'Enviar','review',w=135)
assistant(f,'Você controla o contexto')

# 05 Context
f=shell('context','O que o assistente pode consultar','Revise o contexto antes de enviar. O mesmo limite deve valer para as ferramentas da sessão.','conversation')
b=card(f,244,202,1084,610,'Contexto desta conversa')
for y,title,body in [(75,'INCLUÍDO · Configurações','Instruções, agentes, skills e configurações relevantes; valores sensíveis identificados são omitidos.'),
                     (185,'INCLUÍDO · Estrutura','Nomes e organização de pastas, com exclusões respeitadas. Não inclui o conteúdo do código.'),
                     (295,'NÃO INCLUÍDO · Código-fonte','Nenhum arquivo selecionado. Você escolhe explicitamente os arquivos que deseja compartilhar.')]:
    row=box(b,20,y,1044,94,C['bg'],title,8)
    text(row,18,14,title,15,width=990,bold=True);text(row,18,46,body,13,C['muted'],990)
text(b,20,429,'Antes de enviar: confira a prévia do contexto. A detecção de segredos não é perfeita.',13,C['orange'],1000)
button(b,20,503,'Confirmar contexto','conversation',w=210)
button(b,245,503,'Voltar','conversation',w=130,primary=False)

# 06 Review
f=shell('review','Revise antes de aplicar','1 arquivo  ·  escopo de projeto  ·  nenhuma alteração aplicada','conversation')
b=card(f,244,202,1084,610,'Proposta · tornar o agente de revisão mais específico')
text(b,20,55,'.claude/agents/code-reviewer.md',13,C['muted'],1010)
left=box(b,20,100,514,247,'#FFF1EF','Antes',8)
right=box(b,550,100,514,247,'#EAF6EF','Depois',8)
text(left,18,16,'ANTES',11,C['red'],470,True)
text(left,18,57,'name: code-reviewer\ndescription: Revisa código\n\nRevise o código e indique melhorias.',14,width=470)
text(right,18,16,'DEPOIS',11,C['teal'],470,True)
text(right,18,57,'name: code-reviewer\ndescription: Revisa diffs de APIs .NET\ntools: Read, Grep, Glob\n\nPriorize riscos demonstráveis no diff.\nInforme arquivo, impacto e verificação.',14,width=470)
text(b,20,378,'O que acontecerá',17,width=1000,bold=True)
text(b,20,415,'1. Conferir se o arquivo mudou desde a leitura.\n2. Criar backup e gravar a versão aprovada.\n3. Validar o resultado e registrar a operação no histórico.',14,C['muted'],1000)
button(b,20,536,'Aprovar e aplicar','history',w=210)
button(b,246,536,'Voltar para ajustar','editor',w=200,primary=False)
text(b,470,546,'Este botão apenas navega no protótipo.',12,C['muted'],570)

# 07 Plugins
f=shell('plugins','Plugins com origem e escopo claros','Instalar, habilitar e atualizar são operações diferentes. Cada uma terá revisão própria.')
b=card(f,244,202,728,610,'Plugins do projeto')
for y,name,status,body in [(75,'typesafe','Disponível no catálogo','Skill para orientar workflows com TypeSafe.\nExemplo de catálogo, sem instalação nesta máquina.'),
                           (241,'plugin-exemplo','Instalado · desabilitado','Pacote fictício para demonstrar os estados.\nOrigem, versão e componentes devem ser consultáveis.')]:
    row=box(b,20,y,688,146,C['bg'],name,8)
    text(row,17,13,name,19,width=290,bold=True)
    text(row,330,18,status,12,C['teal'],335)
    text(row,17,53,body,13,C['muted'],650)
    button(row,17,97,'Revisar operação','plugin_review',w=186,primary=False)
text(b,20,433,'A instalação usa o CLI oficial. Ações que executam código\nou adicionam ferramentas são apresentadas antes da aprovação.',14,C['muted'],665)
button(b,20,535,'Descobrir plugins','discover',w=200)
assistant(f,'Instalado não é carregado')

# 08 plugin approval
f=shell('plugin_review','Confirme a operação do plugin','Operação demonstrativa · não executa comandos neste protótipo','plugins')
b=card(f,244,202,1084,610,'Instalar TypeSafe no projeto')
for y,k,v in [(72,'Origem','Marketplace typesafe-ai/skills · fornecedor TypeSafe'),(137,'Destino','Projeto Caltek.Api · escopo project'),(202,'Conteúdo','Skill orientadora para construir workflows com TypeSafe'),(267,'Antes de instalar','Verificar origem, versão e componentes atuais pelo CLI')]:
    text(b,20,y,k,12,C['muted'],1000,True);text(b,20,y+25,v,16,width=1010)
note=box(b,20,346,1044,128,C['amber'],'Efeitos da operação',8)
text(note,18,15,'Instalar a skill não ativa roteamento automático por Jev.',17,width=990,bold=True)
text(note,18,51,'Usar uma API externa posteriormente pode exigir conta, credenciais e envio de dados.\nO histórico registrará sucesso ou falha; remover o plugin não desfaz efeitos externos.',13,C['muted'],990)
button(b,20,531,'Aprovar operação','history',w=210)
button(b,246,531,'Cancelar','plugins',w=150,primary=False)

# 09 discovery
f=shell('discover','Ferramentas que resolvem um problema','Catálogo selecionado. Busca no GitHub somente quando você solicitar.')
b=card(f,244,202,728,610,'Jev / TypeSafe')
badge(b,20,53,'ESTUDO DE CASO',w=160)
text(b,20,105,'Quando uma decisão tem opções bem definidas.',22,width=670,bold=True)
text(b,20,158,'Pode valer um experimento em classificação, ranking ou seleção\nentre alternativas. Ter agentes no projeto, por si só, não justifica\nadicionar mais uma ferramenta.',14,C['muted'],665)
for y,label,body in [(260,'ONDE ENTRA','Skill: ensina a construir com a tecnologia.\nAPI ou ferramenta: executa a decisão no fluxo.'),
                     (365,'COMO AVALIAR','Comparar acerto, latência e custo total em exemplos reais.\nDefinir o que fazer nos casos de incerteza.')]:
    text(b,20,y,label,11,C['teal'],665,True);text(b,20,y+28,body,14,C['muted'],665)
button(b,20,524,'Conversar sobre uso','conversation',w=210)
button(b,245,524,'Ver integração','plugin_review',w=185,primary=False)
r=card(f,1004,202,324,610,'Descoberta explicável')
text(r,20,64,'Critérios além de estrelas',19,width=280,bold=True)
text(r,20,112,'• Adequação à tarefa\n• Compatibilidade Windows\n• Manutenção e licença\n• Origem e dependências\n• Dados enviados e custo',14,C['muted'],280)
text(r,20,290,'Busca ao vivo ainda não realizada.\nContagens não são inventadas.',13,C['muted'],280)
button(r,20,376,'Pesquisar no GitHub','search',w=284)
text(r,20,446,'Fonte deste exemplo:\ngithub.com/typesafe-ai/skills',12,C['teal'],280)

# 10 search state
f=shell('search','Pesquisa de ferramentas','Configure a busca. Nenhuma consulta real ocorre no protótipo.','discover')
b=card(f,244,202,1084,610,'Buscar no GitHub')
text(b,20,76,'Necessidade',13,width=1000,bold=True)
inp=box(b,20,109,1044,57,C['bg'],'Busca',8);text(inp,17,17,'Ferramentas para revisar APIs .NET com agentes',16,width=990)
text(b,20,199,'Filtros propostos',15,width=1000,bold=True)
badge(b,20,235,'WINDOWS',w=125);badge(b,159,235,'REPOSITÓRIOS ATIVOS',w=205);badge(b,379,235,'LICENÇA INFORMADA',w=200)
text(b,20,306,'Ordenação: relevância para a tarefa; estrelas como critério adicional.\nResultados deverão mostrar data da consulta, fonte e limitações de compatibilidade.',15,C['muted'],990)
button(b,20,421,'Voltar ao catálogo','discover',w=210)
text(b,20,499,'Estado desenhado: antes da pesquisa. Erro de rede e limite de API terão opção de tentar novamente.',13,C['muted'],1000)

# 11 History
f=shell('history','Histórico e recuperação','Saiba o que mudou, em qual escopo e como recuperar um arquivo.')
b=card(f,244,202,1084,610,'Operação demonstrativa concluída')
badge(b,20,56,'EXEMPLO DE SUCESSO',w=200)
text(b,20,113,'Descrição do agente atualizada',24,width=1000,bold=True)
text(b,20,164,'Arquivo: .claude/agents/code-reviewer.md\nEscopo: projeto Caltek.Api\nVerificação: arquivo gravado e lido novamente\nBackup: versão anterior preservada',16,C['muted'],1000)
note=box(b,20,306,1044,111,C['blue'],'Recuperação',8)
text(note,18,17,'Restaurar também é uma alteração revisável.',18,width=990,bold=True)
text(note,18,57,'Antes de restaurar, compare com o arquivo atual. Backups de arquivos não desfazem efeitos externos de plugins.',13,C['muted'],990)
button(b,20,470,'Revisar restauração','review',w=210,primary=False)
button(b,245,470,'Voltar ao projeto','overview',w=200)
text(b,20,545,'Nenhum arquivo real foi alterado. Esta é uma simulação do fluxo.',13,C['muted'],1000)

# Dedicated flow board, same document and editable nodes.
flow=box(None,2920,2940,1360,880,C['nav'],'12 · Fluxos e estados',12)
text(flow,48,40,'Do diagnóstico à mudança verificada',32,C['white'],1250,True)
text(flow,48,99,'Mapa de navegação · cada etapa pode ser aberta no protótipo',15,C['navtext'],1250)
steps=[('overview','01  Selecionar projeto','Ler sem executar'),('inventory','02  Consultar recursos','Origem e escopo'),
       ('conversation','03  Conversar','Contexto explícito'),('review','04  Revisar proposta','Diff e aprovação'),
       ('history','05  Verificar resultado','Registro e recuperação')]
for j,(dest,title,body) in enumerate(steps):
    b=box(flow,48+j*259,180,235,128,'#24424D',title,10)
    text(b,16,19,title,15,C['white'],205,True);text(b,16,66,body,13,C['navtext'],205)
    links.append((b,dest))
    if j<4: text(flow,288+j*259,224,'→',20,C['navtext'],25)
for j,(title,body) in enumerate([
    ('Falha na leitura','Mostrar o arquivo e a razão.\nNão interpretar ausência como vazio.'),
    ('Arquivo mudou externamente','Interromper a aplicação.\nRecarregar e revisar novo diff.'),
    ('CLI indisponível ou cancelado','Manter o editor utilizável.\nNão aplicar resposta incompleta.'),
    ('Operação parcial de plugin','Registrar etapas concluídas.\nNão prometer rollback universal.')]):
    b=box(flow,48+(j%2)*645,386+(j//2)*176,616,146,'#24424D',title,10)
    text(b,20,17,title,18,C['white'],575,True);text(b,20,60,body,14,C['navtext'],575)
text(flow,48,794,'Fluxos futuros: configurar do zero · biblioteca pessoal · adaptador Codex',14,C['navtext'],1250)

for b,d in navs+links:
    cmd('proto.interaction.add',id=b,dest=frames[d])
cmd('proto.flow.add',start=frames['overview'],name='Diagnosticar → revisar → aplicar')
cmd('proto.flow.add',start=frames['plugins'],name='Plugin → revisão → resultado')

requests=[dict(id=1,method='engine.execute',params=dict(command='file.new',params=dict(name='Harness Helper · Telas e fluxos'))),
          dict(id=2,method='engine.execute',params=dict(command='page.rename',params=dict(name='01 · Produto / protótipo'))),
          dict(id=3,method='engine.execute',params=dict(command='command.batch',params=dict(label='Telas e fluxos Harness Helper',commands=commands))),
          dict(id=4,method='engine.execute',params=dict(command='file.save',params=dict(path=str(ROOT/'Harness-Helper.figcraft')))),
          dict(id=5,method='document.inspect',params=dict(depth=1,allPages=True))]
(ROOT/'build.jsonl').write_text('\n'.join(json.dumps(r,ensure_ascii=False) for r in requests)+'\n',encoding='utf-8')
(ROOT/'frame-steps.json').write_text(json.dumps(frames,indent=2),encoding='utf-8')
assert all(d in frames for _,d in links+navs)
print(f'{len(frames)+1} frames; {len(commands)} commands; {len(links+navs)} prototype links.')
