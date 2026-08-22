# Dólar comercial (venda e compra) - cotações diárias e Taxas de Câmbio - todos os boletins diários - v1
Este documento descreve a versão v1 do serviço de dados Dólar comercial (venda e compra) - cotações diárias e Taxas de Câmbio - todos os boletins diários. Esta versão implementa o protocolo OData e disponibiliza os seguintes subrecursos:

## Moedas
Retorna a lista de moedas que podem ser usadas como parâmetros para este conjunto de dados.

### Parâmetros
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- simbolo	texto	Símbolo	Símbolo de três letras da moeda.
- nomeFormatado	texto	Nome da moeda	Nome da moeda.
- tipoMoeda	texto	Tipo da moeda	Tipo da moeda. As moedas podem ser do tipo A ou B.

## Cotação do Dólar em uma determinada data
Retorna a Cotação de Compra e a Cotação de Venda da moeda Dólar contra a unidade monetária corrente para a data informada.

### Parâmetros
dataCotacao	texto	Data da cotação	Data da cotação - informar no padrão 'MM-DD-AAAA'
- $format texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado

- cotacaoCompra	decimal	Cotação de compra	Cotação de compra do dólar contra a unidade monetária corrente: unidade monetária corrente/dólar americano.
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda do dólar contra a unidade monetária corrente: unidade monetária corrente/dólar americano.
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das cotações de compra e venda.

## Cotação do Dólar por período
Retorna a Cotação de Compra e a Cotação de Venda da moeda Dólar contra a unidade monetária corrente para o período informado.

### Parâmetros
- dataInicial	texto	Data inicial	Data de início do período de cotação - informar no padrão 'MM-DD-AAAA'
- dataFinalCotacao	texto	Data final	Data de fim do período de cotação - informar no padrão 'MM-DD-AAAA'
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- cotacaoCompra	decimal	Cotação de compra	Cotação de compra do dólar contra a unidade monetária corrente: unidade monetária corrente/dólar americano.
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda do dólar contra a unidade monetária corrente: unidade monetária corrente/dólar americano.
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das cotações de compra e venda.

## Boletim - Paridade e Cotação de moeda por data
Retorna os boletins diários com a Paridade de Compra e a Paridade de Venda, a Cotação de Compra e a Cotação de Venda para a data da moeda consultada. São cinco boletins para cada data, um de abertura, três intermediários e um de fechamento.

### Parâmetros
- moeda	texto	Moeda	Código texto da moeda cujas cotações e paridades serão consultadas. Formato - MMM: Três letras. Exemplo: EUR. As moedas que estão disponíveis podem ser consultadas no recurso 'Moedas' deste mesmo conjunto de dados.
- dataCotacao	texto	Data da cotação	Data da cotação - informar no padrão 'MM-DD-AAAA'
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- paridadeCompra	decimal	Paridade de compra	Paridade de compra da moeda consultada contra o dólar: Moedas tipo A: USD/[moeda]. Moedas tipo B: [moeda]/USD.
- paridadeVenda	decimal	Paridade de venda	Paridade de venda da moeda consultada contra o dólar: Moedas tipo A: USD/[moeda]. Moedas tipo B: [moeda]/USD.
- cotacaoCompra	decimal	Cotação de compra	Cotação de compra da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das paridades e cotações.
- tipoBoletim	texto	Tipo do boletim	Tipo das paridades e cotações para aquela data e hora. Podem ser dos tipos: Abertura, Intermediário, Fechamento Interbancário ou Fechamento.

## Boletim - Paridade e Cotação de moeda por período
Retorna os boletins diários com a Paridade de Compra e a Paridade de Venda, a Cotação de Compra e a Cotação de Venda para um período da moeda consultada. São cinco boletins para cada data, um de abertura, três intermediários e um de fechamento.

### Parâmetros
- moeda	texto	Moeda	Código texto da moeda cujas cotações e paridades serão consultadas. Formato - MMM: Três letras. Exemplo: EUR. As moedas que estão disponíveis podem ser consultadas no recurso 'Moedas' deste mesmo conjunto de dados.
- dataInicial	texto	Data inicial	Data de início do período de cotação - informar no padrão 'MM-DD-AAAA'
- dataFinalCotacao	texto	Data final	Data de fim do período de cotação - informar no padrão 'MM-DD-AAAA'
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- paridadeCompra	decimal	Paridade de compra	Paridade de compra da moeda consultada contra o dólar: Moedas tipo A: USD/[moeda]. Moedas tipo B: [moeda]/USD.
- paridadeVenda	decimal	Paridade de venda	Paridade de venda da moeda consultada contra o dólar: Moedas tipo A: USD/[moeda]. Moedas tipo B: [moeda]/USD.
- cotacaoCompra	decimal	Cotação de compra	Cotação de compra da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das paridades e cotações.
- tipoBoletim	texto	Tipo do boletim	Tipo das paridades e cotações para aquela data e hora. Podem ser dos tipos: Abertura, Intermediário, Fechamento Interbancário ou Fechamento.

### Parâmetros
- codigoMoeda	texto		
- dataCotacao	texto		
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- cotacaoCompra	decimal	Cotação de compra	Cotação de compra da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das paridades e cotações.
- tipoBoletim	texto	Tipo do boletim	Tipo das paridades e cotações para aquela data e hora. Podem ser dos tipos: Abertura, Intermediário, Fechamento Interbancário ou Fechamento.

### Parâmetros
- codigoMoeda	texto		
- dataInicialCotacao	texto		
- dataFinalCotacao	texto		
- $format	texto	$format	Tipo de conteúdo que será retornado
- $select	texto	$select	Propriedades que serão retornadas
- $filter	texto	$filter	Filtro de seleção de entidades. e.g. Nome eq 'João'. Clique aqui para as opções de operadores e funções.
- $orderby	texto	$orderby	Propriedades para ordenação das entidades. e.g. Nome asc, Idade desc
- $skip	inteiro	$skip	Índice (maior ou igual a zero) da primeira entidade que será retornada
- $top	inteiro	$top	Número máximo (maior que zero) de entidades que serão retornadas

### Resultado
- cotacaoCompra	decimal	Cotação de compra	Cotação de compra da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- cotacaoVenda	decimal	Cotação de venda	Cotação de venda da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
- dataHoraCotacao	texto	Data e hora da cotação	Data, hora e minuto das paridades e cotações.
- tipoBoletim	texto	Tipo do boletim	Tipo das paridades e cotações para aquela data e hora. Podem ser dos tipos: Abertura, Intermediário, Fechamento Interbancário ou Fechamento.
