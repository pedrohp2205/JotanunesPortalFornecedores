# language: pt-BR
Funcionalidade: Auth Check - SupplyRequest

Como um usuário do sistema
Quero ter a segurança de que o acesso às solicitações de fornecimento está sendo corretamente verificado
Para garantir que apenas fornecedores autenticados, com senha definitiva e da própria empresa possam consultá-las

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas

Esquema do Cenário: Verificar acesso aos endpoints de solicitação de fornecimento
    Dado que eu <descricaoAutenticacao>
    Quando <executarAcao>
    Então eu recebo uma resposta <respostaEsperada>

    Exemplos:
        | executarAcao                               | descricaoAutenticacao                              | respostaEsperada |
        | eu listar as solicitações da minha empresa | não estou autenticado                              | 401 Unauthorized |
        | eu listar as solicitações da minha empresa | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar a solicitação cadastrada      | não estou autenticado                              | 401 Unauthorized |
        | eu consultar a solicitação cadastrada      | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar a solicitação cadastrada      | estou autenticado como fornecedor de outra empresa | 404 Not Found    |

Cenário: Fornecedor com senha provisória é orientado a trocar a senha
    Dado que eu estou autenticado com senha provisória
    Quando eu listar as solicitações da minha empresa
    Então eu recebo uma resposta 403 Forbidden
    E a resposta indica que é preciso trocar a senha provisória
