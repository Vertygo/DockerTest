FROM microsoft/aspnet@sha256:7cdafe834e1c08ed880cd54183ba33d4760c8b19e651ef1cbff0cf3118684e88
ARG source
WORKDIR /inetpub/wwwroot
COPY ${source:-obj/Docker/publish} .