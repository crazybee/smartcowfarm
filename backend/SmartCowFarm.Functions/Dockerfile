FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gnupg apt-transport-https ca-certificates lsb-release unixodbc-dev unzip \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/debian/12/prod bookworm main" > /etc/apt/sources.list.d/microsoft-prod.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends azure-functions-core-tools-4 \
    && curl -fsSL https://aka.ms/getvsdbgsh -o /tmp/getvsdbg.sh \
    && bash /tmp/getvsdbg.sh -v latest -l /vsdbg \
    && rm -f /tmp/getvsdbg.sh \
    && rm -rf /var/lib/apt/lists/*

COPY SmartCowFarm.Functions.csproj ./
RUN dotnet restore SmartCowFarm.Functions.csproj

COPY . .

RUN dotnet publish SmartCowFarm.Functions.csproj -c Debug --no-restore -o /app

WORKDIR /app

EXPOSE 7071

CMD ["func", "start", "--dotnet-isolated", "--port", "7071", "--no-build"]
