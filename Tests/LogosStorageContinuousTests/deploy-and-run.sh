set -e

replication=$DNR_REP
name=$DNR_NAME
filter=$DNR_FILTER
duration=$DNR_DURATION

echo "Starting tests..."
cd ../../Tests/LogosStorageContinuousTests
for i in $( seq 0 $replication)
do
    screen -d -m dotnet run \
    --kube-config=/opt/kubeconfig.yaml \
    --storage-deployment=storage-deployment-$name-$i.json \
    --log-path=/var/log/storage-continuous-tests/logs-$name-$i \
    --data-path=data-$name-$i \
    --keep=1 \
    --stop=1 \
    --filter=$filter \
    --cleanup=1 \
    --full-container-logs=1 \
    --target-duration=$duration

    sleep 30
done

echo "Done! Sleeping indefinitely..."
while true; do sleep 1d; done
