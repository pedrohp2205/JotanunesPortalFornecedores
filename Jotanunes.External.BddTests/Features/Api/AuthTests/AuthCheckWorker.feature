# language: pt-BR
Funcionalidade: Auth Check - Worker

Como um usuário do sistema
Quero ter a segurança de que o acesso ao cadastro de trabalhadores está sendo corretamente verificado
Para garantir que apenas fornecedores autenticados, com senha definitiva e da própria empresa possam mantê-lo

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece mão de obra
    E que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa

Esquema do Cenário: Verificar acesso aos endpoints de trabalhador
    Dado que eu <descricaoAutenticacao>
    Quando <executarAcao>
    Então eu recebo uma resposta <respostaEsperada>

    Exemplos:
        | executarAcao                                                 | descricaoAutenticacao                              | respostaEsperada |
        | eu listar os trabalhadores da minha empresa                  | não estou autenticado                              | 401 Unauthorized |
        | eu listar os trabalhadores da minha empresa                  | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar o trabalhador cadastrado                        | não estou autenticado                              | 401 Unauthorized |
        | eu consultar o trabalhador cadastrado                        | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar o trabalhador cadastrado                        | estou autenticado como fornecedor de outra empresa | 404 Not Found    |
        | eu desativar o trabalhador cadastrado                        | estou autenticado como fornecedor de outra empresa | 404 Not Found    |
        | eu alocar o trabalhador cadastrado na solicitação cadastrada | não estou autenticado                              | 401 Unauthorized |
        | eu alocar o trabalhador cadastrado na solicitação cadastrada | estou autenticado como fornecedor de outra empresa | 404 Not Found    |
        | eu listar os trabalhadores alocados na solicitação cadastrada | estou autenticado como fornecedor de outra empresa | 404 Not Found   |
