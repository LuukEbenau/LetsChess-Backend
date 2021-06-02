@ECHO OFF
ECHO Lets update the service!
cd ../
docker build -t sacation/letschess-backend  . -f LetsChess-Backend/Dockerfile
docker push sacation/letschess-backend