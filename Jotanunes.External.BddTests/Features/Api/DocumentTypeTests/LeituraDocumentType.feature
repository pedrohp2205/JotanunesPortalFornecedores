# language: pt-BR
Funcionalidade: DocumentType - Leitura

Como fornecedor autenticado
Quero consultar os tipos de documento que se aplicam à minha empresa
Para saber o que preciso enviar

Esquema do Cenário: Listar os tipos de documento conforme o fornecimento da empresa
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece <tipoFornecimento>
    E que eu estou autenticado com senha definitiva
    Quando eu listar os tipos de documento da minha empresa
    Então eu recebo uma resposta 200 OK
    E a listagem de tipos de documento deve ter <quantidade> itens

    Exemplos:
        | tipoFornecimento | quantidade |
        | material         | 10         |
        | mão de obra      | 20         |

Cenário: Filtrar por um fornecimento que a empresa não presta
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece mão de obra
    E que eu estou autenticado com senha definitiva
    Quando eu listar os tipos de documento da minha empresa para o fornecimento de material
    Então eu recebo uma resposta 200 OK
    E a listagem de tipos de documento deve ter 0 itens
