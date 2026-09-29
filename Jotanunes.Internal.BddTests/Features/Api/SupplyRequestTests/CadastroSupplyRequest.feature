# language: pt-BR
Funcionalidade: SupplyRequest - Cadastro

Como membro da equipe da Jotanunes
Quero abrir uma solicitação de fornecimento para uma empresa e uma obra
Para que o fornecedor saiba quais documentos precisa enviar

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe uma obra cadastrada

Esquema do Cenário: Abrir uma solicitação de fornecimento
    Quando eu abrir uma solicitação de <tipoFornecimento> para a empresa e a obra cadastradas
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve estar aberta para o fornecimento de <tipoFornecimento>

    Exemplos:
        | tipoFornecimento |
        | material         |
        | mão de obra      |

Cenário: Não permitir abrir outra solicitação ativa para o mesmo fornecimento
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    Quando eu abrir uma solicitação de material para a empresa e a obra cadastradas
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe uma solicitação ativa desta empresa para esta obra e este tipo de fornecimento."

Cenário: Permitir abrir nova solicitação quando a anterior foi encerrada
    Dado que existe uma solicitação de material concluída para a empresa e a obra cadastradas
    Quando eu abrir uma solicitação de material para a empresa e a obra cadastradas
    Então eu recebo uma resposta 200 OK

Cenário: Não permitir abrir solicitação de um fornecimento que a empresa não presta
    Dado que existe outra empresa cadastrada
    Quando eu abrir uma solicitação de mão de obra para a outra empresa na obra cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "A empresa não está cadastrada para fornecer este tipo de serviço."

Cenário: Abrir uma solicitação para uma obra inexistente
    Quando eu abrir uma solicitação de material para a empresa cadastrada em uma obra inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Obra não encontrada"
