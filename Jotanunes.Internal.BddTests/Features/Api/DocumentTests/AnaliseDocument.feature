# language: pt-BR
Funcionalidade: Document - Análise automática

Como membro da equipe da Jotanunes
Quero consultar o parecer automático de um documento e pedir uma nova análise
Para avaliar os documentos com apoio da leitura automática

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Consultar a análise de um documento
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que o documento cadastrado tem uma análise pendente
    Quando eu consultar a análise do documento cadastrado
    Então eu recebo uma resposta 200 OK
    E a análise retornada deve estar com status "Pending"

Cenário: Consultar a análise de um documento que ainda não foi analisado
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu consultar a análise do documento cadastrado
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Análise do documento não encontrada"

Cenário: Pedir nova análise de um documento já analisado
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que o documento cadastrado tem uma análise concluída
    Quando eu pedir uma nova análise do documento cadastrado
    Então eu recebo uma resposta 202 Accepted
    E a análise retornada deve estar com status "Pending"

Cenário: Pedir análise de um documento enviado antes da análise automática existir
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu pedir uma nova análise do documento cadastrado
    Então eu recebo uma resposta 202 Accepted
    E a análise retornada deve estar com status "Pending"

Cenário: Não permitir pedir nova análise enquanto a anterior está na fila
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que o documento cadastrado tem uma análise pendente
    Quando eu pedir uma nova análise do documento cadastrado
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "A análise deste documento já está na fila."

Cenário: Pedir análise de um documento inexistente
    Quando eu pedir uma nova análise de um documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"

Cenário: Comparar o parecer da análise com a decisão do analista
    Dado que existe um documento de habilitação aprovado enviado pela empresa cadastrada
    E que o documento cadastrado tem uma análise não conforme
    Quando eu consultar as métricas da análise automática
    Então eu recebo uma resposta 200 OK
    E as métricas mostram 1 documento(s) avaliado(s), 0 concordância(s) e 1 alarme(s) falso(s)
