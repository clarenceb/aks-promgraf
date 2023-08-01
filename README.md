AKS using Azure Managed Prometheus and Grafana
==============================================

Environment variables
---------------------

```sh
export RG_NAME="aks-promgraf"
export CLUSTER="aks-promgraf"
export LOCATION="australiaeast"
export PROM_LOCATION="australiasoutheast"
export MONITOR_WORKSPACE="akspromwrksp"
export GRAFANA_INSTANCE="aksgrafana-$(openssl rand -base64 6 | sed 's/[=\/\-\+]+//g')"
export K8S_VERSION="1.27.3"
export CONTAINER_REGISTRY="samples$(openssl rand -base64 6 | sed 's/[=\/\-\+]+//g' | tr A-Z a-z)"
```

Create resource group
---------------------

```sh
az group create -n $RG_NAME -l $LOCATION
```

Create an Azure Monitor workspace
---------------------------------

```sh
az resource create \
    --resource-group $RG_NAME \
    --namespace microsoft.monitor \
    --resource-type accounts \
    --name $MONITOR_WORKSPACE \
    --location $PROM_LOCATION \
    --properties {}
```

Create an Azure Managed Grafana
-------------------------------

```sh
az grafana create --name $GRAFANA_INSTANCE --resource-group $RG_NAME
```

Create AKS cluster with Azure monitor metrics enabled and link to a Grafana instance
------------------------------------------------------------------------------------

```sh
GRAF_ID="$(az grafana show --name $GRAFANA_INSTANCE --resource-group $RG_NAME --query id -o tsv)"
WRKSP_ID="$(az resource show --resource-group $RG_NAME --name $MONITOR_WORKSPACE --resource-type accounts  --namespace microsoft.monitor --query id -o tsv)"

az aks create \
    --enable-azure-monitor-metrics \
    -n $CLUSTER \
    -g $RG_NAME \
    --network-plugin azure \
    --node-count 3 \
    --azure-monitor-workspace-resource-id $WRKSP_ID \
    --grafana-resource-id  $GRAF_ID \
    --kubernetes-version $K8S_VERSION \
    --enable-managed-identity \
    --generate-ssh-keys
```

(Optional) Test sample application locally
------------------------------------------

```sh
cd prometheus-net/Sample.Web

# Requires .NET 6.0
dotnet restore
dotnet run
```

(Optional) Test sample application locally with Docker
------------------------------------------------------

```sh
docker build -t sampleweb .
docker run --rm -p 8080:80 sampleweb
```

Browse to: http://localhost:8080/

Check Prometheus metrics at: http://localhost:8080/metrics

Create Azure Container Registry
-------------------------------

```sh
az acr create -g $RG_NAME -n $CONTAINER_REGISTRY --sku Standard
```

Publish container image
-----------------------

```sh
az acr build -t sampleweb -r $CONTAINER_REGISTRY .
```

Deploy sample application to AKS
--------------------------------

```sh
az aks update -n $CLUSTER -g $RG_NAME --attach-acr $CONTAINER_REGISTRY

kubectl create ns sampleweb

export REGISTRY_LOGIN_SERVER="$(az acr show -n $CONTAINER_REGISTRY --query loginServer -o tsv)"
envsubst < kubernetes/sampleweb.yaml | kubectl apply -n sampleweb -f -
```

Enable pod-annotation based scraping for the app
------------------------------------------------

```sh
wget https://raw.githubusercontent.com/Azure/prometheus-collector/main/otelcollector/configmaps/ama-metrics-settings-configmap.yaml
```

Change:

```yaml
podannotationnamespaceregex = ""
```

To:

```yaml
podannotationnamespaceregex = ".*"
```

Apply the changes, wait 2-3 mins.

```sh
kubectl create configmap ama-metrics-settings-configmap --from-file=ama-metrics-settings-configmap.yaml -n kube-system
```

Define the Kubernetes pod scrape job
------------------------------------

```sh
wget https://raw.githubusercontent.com/Azure/prometheus-collector/main/otelcollector/configmaps/ama-metrics-prometheus-config-configmap.yaml
```

Copy the contents of the file `prometheus-config` into the `scrape_configs` section of the config map template just downloaded.
Also, see: https://learn.microsoft.com/en-Us/azure/azure-monitor/essentials/prometheus-metrics-scrape-configuration#pod-annotation-based-scraping

```sh
kubectl apply -f ama-metrics-prometheus-config-configmap.yaml
```

Check prometheus metrics scraping is working
--------------------------------------------

```sh
kubectl port-forward ama-metrics-7b6898666-8dnvp -n kube-system 9090
```

Check:

- http://localhost:9090/config
- http://localhost:9090/targets

You should see you pod(s) in the endpoints and also the scrape job in the configuration.

Generate load on the sample app
-------------------------------

```sh
kubectl port-forward svc/sampleweb 8080:80 -n sampleweb
k6 run k6-script.js
```

Run a PromQL query in Azure portal
----------------------------------

Either use Azure Monitoring workspace Prometheus Explorer or create an Azure Workbook:

```promql
sum(rate(http_request_duration_seconds_count[30s]))
```

Create a dashboard in Grafana
-----------------------------

Import the dashboard `sampleweb-dashboard.json` and adjust the data source to match the name of your prometheus data source.

Resources
---------

* https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/prometheus-metrics-enable

Credits
-------

* Sample.Web app is from: https://github.com/prometheus-net/prometheus-net/blob/master/Sample.Web
* Additional dependencies from https://github.com/prometheus-net/prometheus-net and https://github.com/prometheus-net/grafana-dashboards

TODO
----

* Install Istio add-on
* Introduce HTTP faults
