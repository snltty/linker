@echo off 

SET comment=%1

git add .
git commit -m "%comment%"
git push origin dev --tags