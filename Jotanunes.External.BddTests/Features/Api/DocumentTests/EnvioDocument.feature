# language: pt-BR
Funcionalidade: Document - Envio

Como fornecedor autenticado
Quero enviar os documentos exigidos pela Jotanunes
Para manter a minha empresa em conformidade

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe outra empresa cadastrada
    E que existe uma obra cadastrada
    E que eu estou autenticado com senha definitiva

Cenário: Enviar um documento de habilitação
    Quando eu enviar o Cartão de CNPJ em PDF
    Então eu recebo uma resposta 200 OK
    E o documento enviado deve estar pendente de avaliação sem vínculo com solicitação

Cenário: Enviar um documento recorrente para uma solicitação aberta
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    Quando eu enviar a Folha de Pagamento para a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E o documento enviado deve estar vinculado à solicitação cadastrada no período de referência atual
    E a solicitação cadastrada deve estar em andamento

Cenário: Enviar um documento de trabalhador alocado
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador "José da Silva" com CPF "529.982.247-25" cadastrado para a empresa
    E que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu enviar a Folha de Ponto do trabalhador cadastrado para a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E o documento enviado deve ser do trabalhador cadastrado com CPF "52998224725"

Cenário: Não permitir enviar documento recorrente de trabalhador que não está alocado na solicitação
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    Quando eu enviar a Folha de Ponto do trabalhador cadastrado para a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Trabalhador não está alocado nesta solicitação."

Cenário: Não permitir enviar documento de trabalhador sem informar o trabalhador
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    Quando eu enviar a Folha de Ponto sem informar o trabalhador para a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Trabalhador é obrigatório para este tipo de documento."

Cenário: Não permitir enviar documento que não se aplica ao fornecimento da solicitação
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    Quando eu enviar a Folha de Pagamento para a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Este tipo de documento não se aplica ao tipo de fornecimento da solicitação."

Cenário: Não permitir enviar documento para uma solicitação encerrada
    Dado que existe uma solicitação de mão de obra cancelada para a empresa e a obra cadastradas
    Quando eu enviar a Folha de Pagamento para a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Solicitação encerrada não aceita alterações nem novos documentos."

Cenário: Não permitir enviar documento para a solicitação de outra empresa
    Dado que existe uma solicitação de material aberta para a outra empresa na obra cadastrada
    Quando eu enviar a Folha de Pagamento para a solicitação da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"

Cenário: Não permitir enviar documento de um tipo inexistente
    Quando eu enviar um documento de um tipo inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Tipo de documento não encontrado"

Esquema do Cenário: Não permitir enviar arquivo inválido
    Quando eu enviar o Cartão de CNPJ com <arquivo>
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "<mensagem>"

    Exemplos:
        | arquivo              | mensagem                                                |
        | um arquivo de texto  | Formato de arquivo não aceito. Envie PDF, PNG ou JPEG.  |
        | um arquivo vazio     | O arquivo enviado está vazio.                           |
