# LibraryCheckConsoleApp
```sh
cd C:\Users\user\source\repos\LibraryCheckConsoleApp
```
```sh
dotnet publish -r linux-arm
```
```sh
scp -r bin\Debug\netcoreapp3.1\linux-arm\publish jan@192.168.0.101:/usr/local/bin/apps/LibraryCheckConsoleApp
```

>Prerequest:
>rights need to be set for folder apps on RPI 
> `cd /usr/local/bin`
> `sudo chmod -R 777 apps`

Now all publish files were copied to RPI.

set appsetings.json to chromium driver: **/usr/lib/chromium-browser/**

>Prerequest: webdriver
>rights need to be set for folder apps on RPI 
> Install webdriver `sudo apt-get install chromium-chromedriver`
> After that webdriver is installed: **/usr/lib/chromium-browser/chromedriver**

You can run it with webmin app -> go to scheduled cron jobs and run it (https://192.168.0.101:10000/)
