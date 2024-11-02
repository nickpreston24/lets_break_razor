
dotnet clean
rm -rf bin/
rm -rf obj/
dotnet build 

dotnet pack
dotnet tool install --add-source ./nupkg lets_break_razor --global


