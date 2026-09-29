# language: pt-BR
Funcionalidade: Auth Check - Document

Como um usuário do sistema
Quero ter a segurança de que o acesso aos documentos está sendo corretamente verificado
Para garantir que apenas fornecedores autenticados, com senha definitiva e da própria empresa possam consultá-los e enviá-los

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    E que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que o arquivo do documento está disponível no armazenamento

Esquema do Cenário: Verificar acesso aos endpoints de documento
    Dado que eu <descricaoAutenticacao>
    Quando <executarAcao>
    Então eu recebo uma resposta <respostaEsperada>

    Exemplos:
        | executarAcao                                     | descricaoAutenticacao                              | respostaEsperada |
        | eu listar os documentos da minha empresa         | não estou autenticado                              | 401 Unauthorized |
        | eu listar os documentos da minha empresa         | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar o documento cadastrado              | não estou autenticado                              | 401 Unauthorized |
        | eu consultar o documento cadastrado              | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar o documento cadastrado              | estou autenticado como fornecedor de outra empresa | 404 Not Found    |
        | eu baixar o arquivo do documento cadastrado      | não estou autenticado                              | 401 Unauthorized |
        | eu baixar o arquivo do documento cadastrado      | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu baixar o arquivo do documento cadastrado      | estou autenticado como fornecedor de outra empresa | 404 Not Found    |
        | eu enviar o Cartão de CNPJ em PDF                | não estou autenticado                              | 401 Unauthorized |
        | eu enviar o Cartão de CNPJ em PDF                | estou autenticado com senha provisória             | 403 Forbidden    |
        | eu consultar o checklist da solicitação cadastrada | não estou autenticado                            | 401 Unauthorized |
        | eu consultar o checklist da solicitação cadastrada | estou autenticado com senha provisória           | 403 Forbidden    |
        | eu consultar o checklist da solicitação cadastrada | estou autenticado como fornecedor de outra empresa | 404 Not Found  |
