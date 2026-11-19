# rtc-conformidade

Suíte comunitária de testes de conformidade para as calculadoras da Reforma Tributária do Consumo (CBS, IBS e IS) publicadas pela Receita Federal.

> **Projeto não oficial.** Não tem vínculo com a Receita Federal nem com o SERPRO. Os resultados daqui não substituem a documentação oficial nem a orientação de um contador.

## O que a suíte faz

A suíte acessa as calculadoras por HTTP, então roda contra qualquer versão delas. Ela verifica três coisas:

- **Invariantes:** regras que valem sem precisar saber o valor certo, por exemplo "o total da nota é igual à soma dos itens arredondados".
- **Casos de teste:** arquivos JSON com entrada e saída esperada, cada um com a base legal (formato em [cases/README.md](cases/README.md)).
- **Comparação entre versões** (planejada): o que mudou de uma versão da calculadora para outra.

Os repositórios oficiais não aceitam pull requests de fora. Por isso, cada invariante que falhar vira uma issue com reprodução mínima no repositório oficial.

## Calculadoras cobertas

| Calculadora | Repositório oficial | URL base padrão | Variável para trocar |
| --- | --- | --- | --- |
| Regime regular | [calculadora-rtc-regime-regular](https://github.com/ReceitaFederal/calculadora-rtc-regime-regular) | `http://localhost:8080/api/calculadora` | `RTC_REGIME_REGULAR_URL` |
| Simples Nacional | [calculadora-rtc-simples-nacional](https://github.com/ReceitaFederal/calculadora-rtc-simples-nacional) | `http://localhost:8082/api` | `RTC_SIMPLES_NACIONAL_URL` |

## Invariantes planejadas

- [ ] O total da nota é igual à soma dos itens arredondados (regime-regular#3 e #10, simples-nacional#4). Em andamento: `Invariantes.TotalIgualASomaDosItens` já confere `vCBS`, `vIBS`, `vIBSUF` e `vIBSMun` do regime regular
- [ ] `vIBS` é igual a `vIBSUF + vIBSMun`
- [ ] Um `cClassTrib` encerrado não aparece como vigente

## Como rodar

Você precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download) e, para subir as calculadoras, do Java 21.

1. Suba a calculadora no perfil `offline`, seguindo o README do repositório oficial. No regime regular, a partir da raiz dele:

   ```bash
   mkdir -p ./calculadora/db
   ./mvnw flyway:migrate -Dflyway.configFiles=./flyway/flyway-pro.conf --batch-mode --errors
   ./mvnw spring-boot:run -Dspring-boot.run.profiles=offline
   ```

   No Windows, use o WSL2.

2. Na raiz deste repositório, rode a suíte:

   ```bash
   dotnet test
   ```

   Para apontar para outro endereço ou outra versão, defina as variáveis da tabela acima, por exemplo `RTC_REGIME_REGULAR_URL=http://localhost:9090/api/calculadora dotnet test`.

   Sem a calculadora no ar, rode só os testes que não precisam dela (o CI faz isso por enquanto):

   ```bash
   dotnet test --filter "Requer!=calculadora"
   ```

## Estrutura

```
cases/                        casos de teste em JSON, por calculadora
  regime-regular/
  simples-nacional/
tests/RtcConformidade.Tests/  invariantes e executor dos casos (xUnit)
```

## Como contribuir

Contribuições de casos são bem-vindas, de qualquer linguagem: basta um arquivo JSON em `cases/` no formato descrito em [cases/README.md](cases/README.md). Todo caso precisa citar a origem do valor esperado.

## Licença

[MIT](LICENSE)
