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

| Calculadora | Repositório oficial | Versão testada |
| --- | --- | --- |
| Regime regular | [calculadora-rtc-regime-regular](https://github.com/ReceitaFederal/calculadora-rtc-regime-regular) | 1.7.2 |
| Simples Nacional | [calculadora-rtc-simples-nacional](https://github.com/ReceitaFederal/calculadora-rtc-simples-nacional) | 1.0.2 |

As duas usam o banco V0073. Para testar outra versão, veja [Outra versão ou outro endereço](#outra-versão-ou-outro-endereço).

## Invariantes

Todas comparam os valores como aparecem na resposta, já arredondados, que é o que a SEFAZ confere ao autorizar o documento.

- [x] **1. O total da nota é igual à soma dos itens.** No regime regular, `vCBS`, `vIBS`, `vIBSUF` e `vIBSMun` (regime-regular#3, rejeição 1091). No Simples Nacional, todos os totais da operação, de `vRBSNtot` a `totalTributos` (simples-nacional#4).
- [x] **2. Um valor composto é igual à soma das partes.** No regime regular, `vIBS` é `vIBSUF + vIBSMun`, no item e no total (regime-regular#10, regra UB54a-10). No Simples Nacional, o `totalItem` é `vIBSSN + vCBSSN + IS`, e o `totalTributos` é a soma dos totais de cada tributo.
- [ ] **3. Um `cClassTrib` encerrado não aparece como vigente.**

As invariantes rodam sobre a resposta de cada caso em [cases/](cases/). O código fica em `tests/RtcConformidade.Tests/Invariantes.cs`.

## Como rodar

Você precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download) e do Docker. Na raiz do repositório:

```bash
dotnet test
```

A suíte constrói as duas calculadoras a partir do código-fonte oficial e as sobe em containers ([Testcontainers](https://dotnet.testcontainers.org/)), usando [calculadoras/Dockerfile](calculadoras/Dockerfile). A primeira execução leva alguns minutos, por causa da compilação; as seguintes reaproveitam o cache do Docker e levam cerca de um minuto.

Para rodar só os testes que não precisam das calculadoras (formato dos casos e checagens das invariantes):

```bash
dotnet test --filter "Requer!=calculadora"
```

### Outra versão ou outro endereço

As variáveis de ambiente abaixo mudam o que a suíte testa, sem mudar o código. Troque `RTC_REGIME_REGULAR` por `RTC_SIMPLES_NACIONAL` para o Simples Nacional.

| Variável | O que faz | Padrão |
| --- | --- | --- |
| `RTC_REGIME_REGULAR_URL` | Usa a calculadora que já está nesse endereço, sem subir container | (sobe um container) |
| `RTC_REGIME_REGULAR_REF` | Tag, branch ou commit do repositório oficial a compilar | `1.7.2` (Simples Nacional: `1.0.2`) |
| `RTC_REGIME_REGULAR_REPO` | Outro repositório, por exemplo um fork com uma correção | o repositório oficial |
| `RTC_REGIME_REGULAR_JAVA` | Versão do Java da compilação | `25` |

O banco das duas calculadoras é gerado pelas migrações do regime regular, então `RTC_REGIME_REGULAR_REF` também muda o banco do Simples Nacional.

Por exemplo, a 1.6.1 do regime regular, anterior à correção das issues #3 e #10, faz os dois casos do regime regular falharem:

```bash
RTC_REGIME_REGULAR_REF=1.6.1 RTC_REGIME_REGULAR_JAVA=21 dotnet test --filter "DisplayName~regime-regular"
```

Para testar uma calculadora que você subiu à mão, seguindo o README do repositório oficial (no Simples Nacional, o endereço é `http://localhost:8082/api`):

```bash
RTC_REGIME_REGULAR_URL=http://localhost:8080/api/calculadora dotnet test
```

## Estrutura

```
calculadoras/Dockerfile       imagens das calculadoras oficiais, compiladas do código-fonte
cases/                        casos de teste em JSON, por calculadora
  regime-regular/
  simples-nacional/
tests/RtcConformidade.Tests/  invariantes, executor dos casos e subida das calculadoras (xUnit)
```

## Como contribuir

Contribuições de casos são bem-vindas, de qualquer linguagem: basta um arquivo JSON em `cases/` no formato descrito em [cases/README.md](cases/README.md). Todo caso precisa citar a origem do valor esperado.

## Licença

[MIT](LICENSE)
