@echo off

cd cmonitor.web
call npm install
call npm run build 
cd ../ 
		
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./cmonitor.install.win/cmonitor.install.win.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./cmonitor.sas.service/cmonitor.sas.service.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./cmonitor.win/cmonitor.win.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./llock.win/llock.win.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./message.win/message.win.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./notify.win/notify.win.csproj
msbuild  /t:build /p:Configuration=Release /p:OutDir="../cmonitor/publish/" ./wallpaper.win/wallpaper.win.csproj
