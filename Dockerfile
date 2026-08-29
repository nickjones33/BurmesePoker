# The browser table as a portable image (BUILD-PLAN P51, docs/HOSTING.md §6 Step 1).
#
# ⚠️ Build stage restores BurmesePoker.Web and its three project references only — the
# console (Spectre), the harness and the tests are not in a hosted table and must not be in
# the layer that builds one. The csproj files are copied before the sources on purpose: a
# source edit then re-uses the restore layer.
#
# 🔥 But the publish below deliberately does NOT pass --no-restore, which is the other half of
# that idiom. A restore performed with only the csproj files present resolves the framework's
# own static web assets incompletely: the published app comes out without
# wwwroot/_framework/blazor.web.js, MapStaticAssets 404s the script that starts the circuit,
# and the page renders perfectly and never moves again — P13.3's exact failure, arriving by a
# different road. Measured either way in the container: with --no-restore the endpoint
# manifest names blazor.web.js zero times, without it once. The second restore is cheap
# (the packages are already in the image's cache); a dead table is not.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BurmesePoker.Domain/BurmesePoker.Domain.csproj       BurmesePoker.Domain/
COPY BurmesePoker.Presentation/BurmesePoker.Presentation.csproj BurmesePoker.Presentation/
COPY BurmesePoker.Server/BurmesePoker.Server.csproj       BurmesePoker.Server/
COPY BurmesePoker.Web/BurmesePoker.Web.csproj             BurmesePoker.Web/
RUN dotnet restore BurmesePoker.Web/BurmesePoker.Web.csproj

COPY BurmesePoker.Domain/       BurmesePoker.Domain/
COPY BurmesePoker.Presentation/ BurmesePoker.Presentation/
COPY BurmesePoker.Server/       BurmesePoker.Server/
COPY BurmesePoker.Web/          BurmesePoker.Web/
RUN dotnet publish BurmesePoker.Web/BurmesePoker.Web.csproj -c Release -o /app

# ⚠️ The runtime image is `aspnet`, not `runtime`: Blazor Server is a hosted ASP.NET Core
# app and the shared framework is what serves _framework/blazor.web.js (P13.3's finding, one
# layer down).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# 0.0.0.0, not localhost: a container's loopback is its own.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# The image ships non-root; $APP_UID is defined by the base image.
USER $APP_UID

ENTRYPOINT ["dotnet", "BurmesePoker.Web.dll"]
