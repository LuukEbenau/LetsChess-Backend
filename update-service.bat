@ECHO OFF
ECHO Lets update the service!
docker build . -t sacation/letschess-backend
docker push sacation/letschess-backend
kubectl delete pods -l letschess.service=letschessbackend -n letschess