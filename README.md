# LibraryCheckConsoleApp

goto: C:\Users\user\source\repos\LibraryCheckConsoleApp

-> dotnet publish -r linux-arm

goto: C:\Users\user\source\repos\LibraryCheckConsoleApp\bin\Debug\netcoreapp3.1\linux-arm

-> >scp -r publish jan@192.168.0.101:/usr/local/bin/apps/LibraryCheckConsoleApp

(
rights need to be set for folder apps on RPI 
-> cd /usr/local/bin
-> sudo chmod -R 777 apps
)

Now all publish files were copied to RPI.
