FROM microsoft/dotnet:2.2-sdk-alpine as build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine as runtime
COPY --from=build /app/publish /app/publish
WORKDIR /app/publish
EXPOSE 80
CMD ["dotnet", "TweetyCore.dll"]