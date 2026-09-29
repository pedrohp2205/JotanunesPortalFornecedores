# language: pt-BR
Funcionalidade: Document - Avaliação

Como membro da equipe da Jotanunes
Quero aprovar ou rejeitar os documentos enviados
Para validar a conformidade do fornecedor

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Aprovar um documento pendente
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu aprovar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o documento retornado deve estar aprovado

Cenário: Rejeitar um documento pendente informando o motivo
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu rejeitar o documento cadastrado pelo motivo "Documento ilegível"
    Então eu recebo uma resposta 200 OK
    E o documento retornado deve estar rejeitado pelo motivo "Documento ilegível"

Cenário: Não permitir rejeitar um documento sem informar o motivo
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu rejeitar o documento cadastrado pelo motivo ""
    Então eu recebo uma resposta 400 Bad Request

Esquema do Cenário: Não permitir avaliar um documento já avaliado
    Dado que existe um documento de habilitação <situacao> enviado pela empresa cadastrada
    Quando <acao>
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Documento já foi avaliado."

    Exemplos:
        | situacao  | acao                                                        |
        | aprovado  | eu aprovar o documento cadastrado                           |
        | aprovado  | eu rejeitar o documento cadastrado pelo motivo "Vencido"    |
        | rejeitado | eu aprovar o documento cadastrado                           |
        | rejeitado | eu rejeitar o documento cadastrado pelo motivo "Vencido"    |

Cenário: Tornar a empresa apta ao aprovar o último documento de habilitação obrigatório
    Dado que a empresa cadastrada tem todos os documentos de habilitação obrigatórios aprovados exceto um pendente
    Quando eu aprovar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E a empresa cadastrada deve estar apta

Cenário: Manter a empresa aguardando documentação enquanto faltam documentos obrigatórios
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu aprovar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E a empresa cadastrada deve continuar aguardando documentação

Esquema do Cenário: Avaliar um documento inexistente
    Quando <acao>
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"

    Exemplos:
        | acao                                   |
        | eu aprovar um documento inexistente    |
        | eu rejeitar um documento inexistente   |
