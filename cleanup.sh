#!/bin/bash
az group list --query "[?starts_with(name,'rg-lab')].name" -o tsv \
  | xargs -I {} az group delete -n {} --yes --no-wait
az resource list --query "[].{name:name, type:type, rg:resourceGroup}" -o table
