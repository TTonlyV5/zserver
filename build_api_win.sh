dotnet publish -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=yes  src/ZServer.API