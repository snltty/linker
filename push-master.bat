@echo off 

cd linker.updater.gen
node index.js
cd ..

git checkout master
git reset --hard origin-github/dev
git pull origin-github
git add .
git push origin-github master --tags 
git checkout dev