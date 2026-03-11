# language: pt
# encoding: utf-8

Funcionalidade: Gestão de Check-in e Check-out de Vendedores
  Como Gestor do Sistema de Pontos de Apoio
  Desejo controlar com precisão as visitas dos vendedores aos clientes
  Para garantir presença física confirmada e registros auditáveis de duração de visita

  Contexto:
    Dado que o sistema aplica um raio máximo de check-in de 100 metros
    E que a autenticação utiliza JWT Bearer Token com validade de 8 horas
    E que o cálculo de distância utiliza a fórmula de Haversine sobre coordenadas WGS-84

  # ===========================================================
  # ÉPICO 1: AUTENTICAÇÃO
  # ===========================================================

  Cenário: Login bem-sucedido de Administrador por CPF
    Dado que existe um Administrador cadastrado com o CPF "529.982.247-25"
    E a senha "admin123!" está registrada como hash BCrypt
    Quando ele solicitar autenticação com CPF "529.982.247-25" e senha "admin123!"
    Então o sistema deve retornar HTTP 200
    E o corpo deve conter um campo "token" com um JWT válido
    E o campo "userRole" deve ser "ADMIN"
    E o campo "expiresAt" deve representar 8 horas a partir de agora

  Cenário: Login bem-sucedido de Vendedor por CPF
    Dado que existe um Vendedor ativo com o CPF "222.333.444-55"
    Quando ele autenticar com credenciais corretas
    Então o token JWT deve conter a claim "sellerId" com o Id do vendedor
    E a claim "role" deve ser "SELLER"

  Cenário: Tentativa de login com CPF inexistente
    Dado que o CPF "000.000.000-00" não existe na base de dados
    Quando tentar autenticar
    Então o sistema deve retornar HTTP 401 Unauthorized
    E o corpo deve conter o código de erro "UNAUTHORIZED"

  Cenário: Tentativa de login com senha incorreta
    Dado que existe um usuário com o CPF "529.982.247-25"
    Quando tentar autenticar com a senha errada "senhaerrada"
    Então o sistema deve retornar HTTP 401 Unauthorized

  # ===========================================================
  # ÉPICO 2: GESTÃO ADMINISTRATIVA
  # ===========================================================

  Cenário: Administrador cadastra novo Vendedor
    Dado que o Administrador está autenticado com perfil "ADMIN"
    Quando solicitar o cadastro de "João Silva" com CPF "987.654.321-00" e senha "vendedor123!"
    Então o sistema deve validar a unicidade do CPF no banco de dados
    E criar o registro de User com perfil "SELLER"
    E criar o registro de Seller vinculado com status ativo
    E retornar HTTP 201 Created com o SellerId e UserId gerados

  Cenário: Tentativa de cadastrar Vendedor com CPF duplicado
    Dado que já existe um usuário com o CPF "987.654.321-00"
    Quando o Administrador tentar cadastrar outro vendedor com o mesmo CPF
    Então o sistema deve retornar HTTP 409 Conflict
    E o código de erro deve ser "CPF_ALREADY_EXISTS"

  Cenário: Vendedor tenta acessar rota exclusiva de Admin
    Dado que um usuário com perfil "SELLER" está autenticado
    Quando tentar realizar uma requisição "POST /api/sellers"
    Então o sistema deve retornar HTTP 403 Forbidden
    E o código de erro deve ser "FORBIDDEN"

  Cenário: Administrador cadastra Cliente com coordenadas alvo
    Dado que o Administrador está autenticado
    E informa a Razão Social "Padaria Silva", CNPJ "11.222.333/0001-81"
    E as coordenadas Latitude "-23.550520" e Longitude "-46.633308"
    Quando salvar o registro via "POST /api/customers"
    Então o sistema deve retornar HTTP 201 Created
    E armazenar os pontos como ponto alvo para validações futuras

  Cenário: Administrador atualiza Cliente existente (Upsert por CNPJ)
    Dado que já existe um Cliente com CNPJ "11.222.333/0001-81"
    Quando o Administrador enviar novos dados com o mesmo CNPJ
    Então o sistema deve atualizar o registro existente
    E retornar HTTP 200 com o campo "isNew" igual a false

  # ===========================================================
  # ÉPICO 3: OPERAÇÕES DE CHECK-IN
  # ===========================================================

  Cenário: Vendedor realiza check-in dentro do raio permitido (caminho feliz)
    Dado que o Vendedor "Carlos" está autenticado com perfil "SELLER"
    E o Cliente "Padaria Silva" está registrado com coordenadas Lat -23.550520 / Long -46.633308
    E o Vendedor está a 50 metros do Cliente (Lat -23.550600 / Long -46.633400)
    Quando enviar a requisição "POST /api/visits/checkin" com as coordenadas atuais
    Então o sistema deve calcular a distância usando a fórmula de Haversine
    E registrar o check-in com distânciaMétros <= 100
    E o timestamp do check-in deve ser definido com data/hora UTC atual
    E retornar HTTP 201 Created com o VisitId

  Cenário: Vendedor tenta check-in fora do raio permitido
    Dado que o Vendedor está a 500 metros de distância do Cliente
    Quando tentar realizar o check-in
    Então o sistema deve calcular a distância como ~500 metros
    E retornar HTTP 403 Forbidden
    E o código de erro deve ser "OUTSIDE_RADIUS"
    E a descrição deve conter "Fora do raio permitido"

  Cenário: Bloqueio de múltiplos check-ins simultâneos
    Dado que o Vendedor "Carlos" já possui um check-in aberto no "Cliente A"
    Quando ele tentar realizar um novo check-in no "Cliente B" sem encerrar o anterior
    Então o sistema deve retornar HTTP 409 Conflict
    E o código de erro deve ser "CONFLICT_CHECKIN"
    E a mensagem deve informar que ele precisa encerrar a visita atual

  Cenário: Check-in com token ausente
    Dado que nenhum token JWT foi enviado na requisição
    Quando tentar realizar um check-in
    Então o sistema deve retornar HTTP 401 Unauthorized

  # ===========================================================
  # ÉPICO 4: OPERAÇÕES DE CHECK-OUT
  # ===========================================================

  Cenário: Vendedor realiza check-out com sucesso
    Dado que o Vendedor "Carlos" realizou check-in na "Padaria Silva" às 10:00
    E está a 60 metros do Cliente no momento do check-out
    Quando solicitar o check-out às 10:45 via "POST /api/visits/checkout"
    Então o sistema deve registrar o horário de saída
    E calcular o campo "durationMinutes" como 45
    E retornar HTTP 200 com a duração calculada

  Cenário: Vendedor tenta check-out fora do raio
    Dado que o Vendedor possui um check-in ativo
    E está a 500 metros de distância do Cliente no momento do checkout
    Quando tentar realizar o check-out
    Então o sistema deve retornar HTTP 403 Forbidden
    E o código de erro deve ser "OUTSIDE_RADIUS"

  Cenário: Tentativa de check-out sem check-in prévio
    Dado que o Vendedor não possui nenhuma visita ativa
    Quando enviar uma requisição de check-out
    Então o sistema deve retornar HTTP 400 Bad Request
    E o código de erro deve ser "NO_ACTIVE_VISIT"
    E a descrição deve ser "Não existe uma visita ativa para este vendedor"

  Cenário: Tentativa de duplo check-out na mesma visita
    Dado que o Vendedor "Carlos" já realizou check-out na visita atual
    Quando tentar realizar um segundo check-out para a mesma visita
    Então o sistema deve retornar HTTP 422
    E o código de erro deve ser "VISIT_CLOSED"

  # ===========================================================
  # ÉPICO 5: HISTÓRICO DE VISITAS (QUERY)
  # ===========================================================

  Cenário: Vendedor consulta seu histórico de visitas paginado
    Dado que o Vendedor "Carlos" possui 25 visitas registradas
    Quando solicitar "GET /api/visits/history?page=1&pageSize=20"
    Então o sistema deve retornar HTTP 200
    E a lista deve conter exatamente 20 registros
    E os registros devem estar ordenados do mais recente para o mais antigo

  Cenário: Consulta de histórico com paginação na segunda página
    Dado que o Vendedor "Carlos" possui 25 visitas registradas
    Quando solicitar "GET /api/visits/history?page=2&pageSize=20"
    Então o sistema deve retornar HTTP 200
    E a lista deve conter exatamente 5 registros

  # ===========================================================
  # ÉPICO 6: VALIDAÇÕES DE DADOS DE ENTRADA
  # ===========================================================

  Cenário: Check-in com latitude inválida
    Dado que o Vendedor está autenticado
    Quando enviar check-in com latitude 95.0 (fora do intervalo -90 a 90)
    Então o sistema deve retornar HTTP 422 Unprocessable Entity
    E a resposta deve indicar o campo "Latitude" com erro de validação

  Cenário: Cadastro de Vendedor com CPF estruturalmente inválido
    Dado que o Administrador está autenticado
    Quando tentar cadastrar vendedor com CPF "111.111.111-11"
    Então o sistema deve retornar erro indicando CPF inválido

  Cenário: Cadastro de Cliente com CNPJ inválido
    Dado que o Administrador está autenticado
    Quando tentar cadastrar cliente com CNPJ "00.000.000/0000-00"
    Então o sistema deve retornar erro indicando CNPJ inválido
