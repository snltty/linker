@echo off 

git checkout master
git reset --hard origin-github/dev
git pull
git add .
git push origin-github master --tags
git checkout dev