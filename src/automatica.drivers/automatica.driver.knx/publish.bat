dotnet publish -r linux-arm /p:ShowLinkerSizeComparison=true P3.Knx.Core.Baos.Console/P3.Knx.Core.Baos.Console.csproj
pushd .\P3.Knx.Core.Baos.Console\bin\Debug\netcoreapp2.2\linux-arm\publish
pscp -pw pi -v -r .\P3.* pi@192.168.8.110:/home/pi/bin
popd