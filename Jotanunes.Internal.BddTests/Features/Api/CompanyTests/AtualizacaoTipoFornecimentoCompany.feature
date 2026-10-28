# language: pt-BR
Funcionalidade: Company - Atualização do tipo de fornecimento

Como membro da equipe da Jotanunes
Quero atualizar o tipo de fornecimento de uma empresa
Para refletir se ela fornece material, mão de obra ou os dois

Cenário: Incluir mão de obra no tipo de fornecimento da empresa
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material
    Quando eu atualizar a empresa cadastrada para fornecer material e mão de obra
    Então eu recebo uma resposta 200 OK
    E a empresa retornada deve fornecer material e mão de obra

Cenário: Não permitir remover um tipo de fornecimento com solicitação ativa
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    Quando eu atualizar a empresa cadastrada para fornecer material
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Existem solicitações ativas do tipo de fornecimento que está sendo removido. Conclua ou cancele antes de alterar."

Cenário: Permitir remover um tipo de fornecimento cuja solicitação já foi encerrada
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra cancelada para a empresa e a obra cadastradas
    Quando eu atualizar a empresa cadastrada para fornecer material
    Então eu recebo uma resposta 200 OK
    E a empresa retornada deve fornecer material
