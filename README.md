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

Resources
---------

* https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/prometheus-metrics-enable
