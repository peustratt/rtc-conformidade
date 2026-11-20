# Imagens das calculadoras oficiais, compiladas a partir do código-fonte publicado no GitHub.
# Os repositórios oficiais não têm Dockerfile; este segue os passos dos READMEs deles.
# Os testes constroem estas imagens sozinhos (Testcontainers). Para construir à mão:
#
#   docker build --target regime-regular -t rtc-conformidade/regime-regular calculadoras
#   docker build --target simples-nacional -t rtc-conformidade/simples-nacional calculadoras
#
# RR_REF e SN_REF aceitam tag, branch ou commit. RR_REPO e SN_REPO trocam o repositório, por
# exemplo para testar um fork com uma correção: o código vem de <repo>/archive/<ref>.tar.gz.
# RR_JAVA e SN_JAVA escolhem o Java: as versões do regime regular anteriores à 1.7.0 usam o 21.

ARG RR_REPO=https://github.com/ReceitaFederal/calculadora-rtc-regime-regular
ARG RR_REF=1.7.2
ARG SN_REPO=https://github.com/ReceitaFederal/calculadora-rtc-simples-nacional
ARG SN_REF=1.0.2
ARG RR_JAVA=25
ARG SN_JAVA=25

# A compilação usa o mvnw de cada repositório, como nos READMEs. A imagem do Maven entra pelo
# JDK e pelo curl, com que o mvnw baixa a versão do Maven fixada pelo repositório. MAVEN_CONFIG
# fica vazio porque a imagem o define como /root/.m2, e o mvnw o repassaria ao Maven como argumento.
FROM maven:3.9-eclipse-temurin-${RR_JAVA} AS jdk-regime-regular
ENV MAVEN_CONFIG=

# Regime regular: código-fonte e banco. As migrações Flyway do banco usado pelas duas
# calculadoras ficam no repositório do regime regular.
FROM jdk-regime-regular AS banco
ARG RR_REPO RR_REF
ADD ${RR_REPO}/archive/${RR_REF}.tar.gz /tmp/fonte.tar.gz
WORKDIR /src
RUN tar -xzf /tmp/fonte.tar.gz --strip-components=1 && rm /tmp/fonte.tar.gz
RUN mkdir -p calculadora/db \
 && sh mvnw --batch-mode --errors flyway:migrate -Dflyway.configFiles=./flyway/flyway-pro.conf

FROM banco AS build-regime-regular
RUN sh mvnw --batch-mode --errors -DskipTests package

# Perfil offline: o banco fica em ./calculadora/db, relativo à pasta de trabalho.
# VERSAO_URL aponta o comparador de versões, que consulta a instalação online na subida, para
# uma porta local fechada: assim os testes não dependem da rede. O cálculo não usa essa consulta.
# -XX:-UseBASE64Intrinsics é a mitigação indicada no CHANGELOG das duas calculadoras (17/11/2026).
FROM eclipse-temurin:${RR_JAVA}-jre AS regime-regular
WORKDIR /app
COPY --from=build-regime-regular /src/calculadora/db calculadora/db
COPY --from=build-regime-regular /src/target/api-regime-geral.jar app.jar
ENV SPRING_PROFILES_ACTIVE=offline VERSAO_URL=http://127.0.0.1:9/versao
EXPOSE 8080 9101
ENTRYPOINT ["java", "-XX:-UseBASE64Intrinsics", "-jar", "app.jar"]

# Simples Nacional: só o código-fonte; o banco vem do estágio do regime regular.
FROM maven:3.9-eclipse-temurin-${SN_JAVA} AS build-simples-nacional
ENV MAVEN_CONFIG=
ARG SN_REPO SN_REF
ADD ${SN_REPO}/archive/${SN_REF}.tar.gz /tmp/fonte.tar.gz
WORKDIR /src
RUN tar -xzf /tmp/fonte.tar.gz --strip-components=1 && rm /tmp/fonte.tar.gz
RUN sh mvnw --batch-mode --errors -DskipTests package

FROM eclipse-temurin:${SN_JAVA}-jre AS simples-nacional
WORKDIR /app
COPY --from=banco /src/calculadora/db calculadora/db
COPY --from=build-simples-nacional /src/target/api-simples-nacional.jar app.jar
ENV SPRING_PROFILES_ACTIVE=offline VERSAO_URL=http://127.0.0.1:9/versao
EXPOSE 8082 9103
ENTRYPOINT ["java", "-XX:-UseBASE64Intrinsics", "-jar", "app.jar"]
