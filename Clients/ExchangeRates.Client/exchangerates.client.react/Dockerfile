services:
    client:
        build:
            context: ./Clients/ExchangeRates.Client
            dockerfile: Dockerfile
        ports:
            - "80:80"
        depends_on:
               - server

    server:
        build:
            context: .
            dockerfile: ExchangeRates.Server/Dockerfile
        ports:
            - "8080:8080"