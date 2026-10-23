#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT="$ROOT_DIR/src/UserService/UserService.API/UserService.API.csproj"

cd "$ROOT_DIR"

echo
echo "=================================="
echo "       SwiftOrders - Ambiente"
echo "=================================="
echo "1) Desenvolvimento — executar API"
echo "2) Testes automatizados"
echo "3) Produção — bloqueada localmente"
echo "0) Sair"
echo

read -r -p "Selecione uma opção: " choice

case "$choice" in
    1)
        secrets="$(dotnet user-secrets list --project "$API_PROJECT")"

        dev_connection="$(
            printf '%s\n' "$secrets" |
            sed -n 's/^ConnectionStrings:DevDatabase = //p'
        )"

        if [[ -z "$dev_connection" ]]; then
            echo "Erro: ConnectionStrings:DevDatabase não configurada."
            exit 1
        fi

        if [[ "$dev_connection" != *"Database=swiftorders_dev"* ]]; then
            echo "Erro: a conexão de desenvolvimento não aponta para swiftorders_dev."
            exit 1
        fi

        echo "Iniciando a API no ambiente Development..."
        export ASPNETCORE_ENVIRONMENT=Development
        export ConnectionStrings__UserDatabase="$dev_connection"

        exec dotnet run \
            --project "$API_PROJECT" \
            --launch-profile http
        ;;

    2)
        secrets="$(dotnet user-secrets list --project "$API_PROJECT")"

        test_connection="$(
            printf '%s\n' "$secrets" |
            sed -n 's/^ConnectionStrings:TestDatabase = //p'
        )"

        if [[ -z "$test_connection" ]]; then
            echo "Erro: ConnectionStrings:TestDatabase não configurada."
            exit 1
        fi

        if [[ "$test_connection" != *"Database=swiftorders_test"* ]]; then
            echo "Erro: a conexão de testes não aponta para swiftorders_test."
            exit 1
        fi

        echo "Executando testes automatizados..."
        export SWIFTORDERS_TEST_CONNECTION="$test_connection"

        exec dotnet test SwiftOrders.slnx
        ;;

    3)
        echo "Produção não pode ser selecionada localmente."
        echo "Configure a conexão de produção exclusivamente no ambiente de implantação."
        exit 1
        ;;

    0)
        echo "Operação cancelada."
        exit 0
        ;;

    *)
        echo "Opção inválida."
        exit 1
        ;;
esac
