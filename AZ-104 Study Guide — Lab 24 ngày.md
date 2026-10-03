# AZ-104 Study Guide — Lab 24 ngày

Oct 3, 2026 · @Khoa

## Tổng quan và lịch 24 ngày

Mục tiêu: qua AZ-104 (700/1000) sau 24 ngày lab, khoảng 2–2,5 giờ mỗi tối, tổng chi phí dưới 200 USD credit. Networking (7 ngày) và Identity (5 ngày) chiếm khoảng một nửa thời lượng.

Cách dùng guide: mỗi phần có lệnh CLI, đoạn JSON/Bicep/KQL cần thiết và mục **Điểm hay thi**. Mỗi lab làm cả trên portal lẫn CLI, vì câu hotspot thường bắt điền tham số lệnh hoặc template.

**Format đề:** khoảng 40–60 câu, 120 phút, điểm quy đổi. Dạng câu gồm multiple choice, drag-and-drop, hotspot, chuỗi Yes/No (không quay lại được) và 1–2 case study. Không có lab thực hành. Được mở Microsoft Learn trong giờ thi, nhưng dùng chung đồng hồ.

| Ngày | Domain | Lab chính |
| --- | --- | --- |
| 1 | Setup | Budget alert, CLI, Bicep, Entra ID P2 trial, 2 user test |
| 2 | Identity | Users, guest, bulk CSV, dynamic group, SSPR |
| 3 | Identity | RBAC 4 scope, custom role JSON |
| 4 | Identity | Azure Policy, initiative, remediation, resource lock |
| 5 | Identity | Management group, move resource, tags, cost |
| 6 | Identity | Ôn + practice khoảng 40 câu |
| 7 | Networking | VNet, subnet, NSG, ASG, IP flow verify |
| 8 | Networking | Hub-spoke peering, UDR, NVA, next hop |
| 9 | Networking | Azure DNS, private DNS, service endpoint, private endpoint |
| 10 | Networking | Standard Load Balancer, Application Gateway |
| 11 | Networking | VPN Gateway P2S, Bastion, flow logs |
| 12 | Networking | Viết lại hub-spoke bằng Bicep, bấm giờ 60 phút |
| 13 | Networking | Ôn + practice khoảng 50 câu |
| 14 | Storage | Redundancy, tier, lifecycle, versioning, soft delete |
| 15 | Storage | SAS, stored access policy, firewall, Azure Files, AzCopy |
| 16 | Compute | VM, disk, availability set/zone, extension, move |
| 17 | Compute | VMSS autoscale, ARM/Bicep, deployment mode |
| 18 | Compute | App Service, slot, scale |
| 19 | Compute | ACR, ACI, Container Apps |
| 20 | Monitor | Alert, action group, Log Analytics, KQL |
| 21 | Monitor | Backup VM, restore, ASR concept |
| 22 | Nước rút | Mock exam 1 (120 phút) |
| 23 | Nước rút | Lab lại chủ đề yếu + mock exam 2 |
| 24 | Nước rút | Ôn bảng tổng hợp, dọn subscription, nghỉ |

## Setup và kiểm soát chi phí

Quy tắc số 1: mỗi ngày dùng một resource group `rg-lab-dNN`, xong lab là xóa. Làm đúng quy tắc này thì cả 24 ngày chỉ tốn khoảng 40–70 USD.

```bash
az login
az account set --subscription "<subscription-id>"
az config set defaults.location=southeastasia
az bicep install

# Mỗi ngày
RG=rg-lab-d07
az group create -n $RG -l southeastasia

# Budget 200 USD (alert 50/80/100% nên tạo trên portal: Cost Management > Budgets)
az consumption budget create --budget-name lab-200 --amount 200 \
  --category cost --time-grain monthly \
  --start-date 2026-10-01 --end-date 2026-12-31
```
## Register bằng file
```bash
SUB=$(az account show --query id -o tsv)
az rest --method put \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets/lab-200?api-version=2023-05-01" \
  --body @budget.json
```
Kiểm tra lại:
```bash
az rest --method get \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets?api-version=2023-05-01" \
  --query "value[].{name:name, amount:properties.amount, spent:properties.currentSpend.amount}" -o table
```

# Dọn dẹp cuối ngày:

```bash
# Xóa mọi resource group lab
az group list --query "[?starts_with(name,'rg-lab')].name" -o tsv \
  | xargs -I {} az group delete -n {} --yes --no-wait

# Kiểm tra còn gì chạy ngầm không
az resource list --query "[].{name:name, type:type, rg:resourceGroup}" -o table
```

**Tài nguyên đốt credit nhanh, phải xóa ngay sau lab:** Azure Firewall, VPN Gateway, Application Gateway, Bastion. VM dùng size B1s/B2s và deallocate khi không dùng. Recovery Services vault có soft delete chặn việc xóa resource group, nên tắt soft delete trước khi lab backup (xem phần Monitor & Backup).

Ngày 1 kích hoạt **Entra ID P2 trial 30 ngày** (cần cho dynamic group và PIM). Tạo 2 user test `lab-reader`, `lab-ops` để thử quyền RBAC.

## Identity & Governance (Ngày 2–6)

Domain này chiếm 20–25% đề. Trọng tâm là chọn đúng role ở đúng scope, và phân biệt các effect của Azure Policy.

### Users và groups

```bash
az ad user create --display-name "Lab Reader" \
  --user-principal-name labreader@minhkhoale2706gmail.onmicrosoft.com \
  --password 'P@$$w0rd!' --force-change-password-next-sign-in true

az ad group create --display-name IT-Team --mail-nickname itteam
az ad group member add --group IT-Team \
  --member-id $(az ad user show --id labreader@minhkhoale2706gmail.onmicrosoft.com --query id -o tsv)
```

Dynamic group (CLI không hỗ trợ tốt, dùng Microsoft Graph PowerShell hoặc portal):

```powershell
New-MgGroup -DisplayName "IT Dynamic" -MailEnabled:$false -MailNickname itdyn `
  -SecurityEnabled -GroupTypes "DynamicMembership" `
  -MembershipRule 'user.department -eq "IT"' -MembershipRuleProcessingState On
```

Cú pháp rule hay gặp: `user.department -eq "IT"`, `user.country -in ["VN","SG"]`, `(user.jobTitle -contains "Engineer") -and (user.accountEnabled -eq true)`, `device.deviceOSType -eq "Windows"`.

**Điểm hay thi:**

- Dynamic group cần Entra ID P1/P2. Thành viên được cập nhật không tức thì; không thể thêm member thủ công vào dynamic group.
- Một group chỉ dynamic cho user hoặc cho device, không trộn cả hai.
- Guest user (B2B) được mời bằng email; quyền mặc định của guest bị giới hạn hơn member.
- Gán license theo group cần P1 trở lên. Bulk create dùng file CSV mẫu tải từ portal.

### RBAC

```bash
SUB=$(az account show --query id -o tsv)
az role assignment create --assignee labreader@minhkhoale2706gmail.onmicrosoft.com \
  --role "Reader" --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03

az role assignment list --assignee labreader@minhkhoale2706gmail.onmicrosoft.com --all -o table
az role definition list --name "Virtual Machine Contributor" --query "[].permissions"
```

Custom role (`vm-operator.json`):

```json
{
  "Name": "VM Operator",
  "Description": "Start, stop, restart VMs only",
  "Actions": [
    "Microsoft.Compute/virtualMachines/read",
    "Microsoft.Compute/virtualMachines/start/action",
    "Microsoft.Compute/virtualMachines/powerOff/action",
    "Microsoft.Compute/virtualMachines/deallocate/action",
    "Microsoft.Compute/virtualMachines/restart/action"
  ],
  "NotActions": [],
  "DataActions": [],
  "NotDataActions": [],
  "AssignableScopes": ["/subscriptions/<subscription-id>"]
}
```

```bash
az role definition create --role-definition @vm-operator.json
```

| Role | Quản lý resource | Gán quyền cho người khác |
| --- | --- | --- |
| Owner | Có | Có |
| Contributor | Có | Không |
| User Access Administrator | Không | Có |
| Reader | Chỉ xem | Không |

**Điểm hay thi:**

- Scope kế thừa từ trên xuống: management group → subscription → resource group → resource. Least privilege = role hẹp nhất ở scope hẹp nhất.
- `Actions` là control plane (quản lý resource); `DataActions` là data plane (đọc blob, message). Owner không tự có quyền đọc blob qua `--auth-mode login`; cần thêm Storage Blob Data Reader/Contributor.
- Entra roles (Global Administrator, User Administrator) quản lý directory; Azure RBAC roles quản lý resource. Hai hệ tách biệt.
- Đăng nhập vào VM bằng Entra ID cần role Virtual Machine User Login hoặc Administrator Login, không phải VM Contributor.

### Azure Policy

```bash
POLICY=$(az policy definition list \
  --query "[?displayName=='Allowed locations'].name" -o tsv)

az policy assignment create --name allowed-locs \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d04 \
  --policy $POLICY \
  --params '{"listOfAllowedLocations":{"value":["southeastasia"]}}'

# Policy có effect modify/deployIfNotExists cần managed identity
az policy assignment create --name inherit-env-tag \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d04 \
  --policy <id-inherit-tag-from-rg> --params '{"tagName":{"value":"env"}}' \
  --mi-system-assigned --location southeastasia

az policy remediation create --name fix-env-tag \
  --policy-assignment inherit-env-tag --resource-group rg-lab-d04

az policy state summarize --resource-group rg-lab-d04
```

| Effect | Tác dụng |
| --- | --- |
| deny | Chặn tạo/sửa resource không tuân thủ |
| audit | Cho tạo, đánh dấu non-compliant |
| append | Thêm field khi tạo/sửa |
| modify | Thêm/sửa tag hoặc property; sửa được resource cũ qua remediation |
| deployIfNotExists | Deploy thêm resource liên quan nếu thiếu (ví dụ diagnostic setting) |
| auditIfNotExists | Báo non-compliant nếu thiếu resource liên quan |
| disabled | Tắt policy |

**Điểm hay thi:**

- Policy chỉ chặn ở thời điểm tạo/sửa. Resource đã tồn tại trước đó chỉ bị báo non-compliant, muốn sửa phải chạy remediation (với modify/deployIfNotExists).
- Initiative = nhóm nhiều policy, gán một lần. Có thể dùng exclusion để loại một resource group khỏi scope.
- Tag không tự kế thừa từ resource group xuống resource; muốn vậy phải dùng policy inherit tag (effect modify).

### Resource lock

```bash
az lock create --name no-delete --lock-type CanNotDelete --resource-group rg-lab-d04
az lock create --name read-only --lock-type ReadOnly --resource-group rg-lab-d04
az lock list --resource-group rg-lab-d04 -o table
```

**Điểm hay thi:**

- Lock áp dụng cho mọi người, kể cả Owner. Muốn thao tác thì phải gỡ lock trước (cần quyền `Microsoft.Authorization/locks/*`, như Owner hoặc User Access Administrator).
- ReadOnly chặn cả các thao tác POST: không start/restart VM được, không list key của storage account được.
- Lock kế thừa từ scope cha xuống resource con.

### Management group, move, tags

```bash
az account management-group create --name mg-lab
az account management-group subscription add --name mg-lab --subscription $SUB

az resource move --destination-group rg-lab-d05b \
  --ids $(az resource show -g rg-lab-d05 -n <ten> --resource-type <type> --query id -o tsv)

az group update -n rg-lab-d05 --tags env=lab owner=khoa
az tag update --resource-id <resource-id> --operation Merge --tags costcenter=it
```

**Điểm hay thi:**

- Cây management group sâu tối đa 6 cấp (không tính root). Mỗi subscription chỉ thuộc một management group.
- Move resource không đổi region. Trong lúc move, cả resource group nguồn và đích bị khóa thao tác ghi.
- Một số resource có ràng buộc khi move (ví dụ VM phải move kèm disk, NIC). Kiểm tra bằng nút Validate trên portal trước.

## Networking (Ngày 7–13)

Domain này chiếm 15–20% đề nhưng có nhiều câu hotspot nhất. Trọng tâm: thứ tự đánh giá NSG, peering không bắc cầu, chọn đúng dịch vụ cân bằng tải.

### VNet, subnet, NSG, ASG (Ngày 7)

```bash
RG=rg-lab-d07
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-web --subnet-prefixes 10.0.1.0/24
az network vnet subnet create -g $RG --vnet-name vnet-hub -n snet-app \
  --address-prefixes 10.0.2.0/24

az network nsg create -g $RG -n nsg-web
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-rdp-myip \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 3389 --source-address-prefixes 203.0.113.10
az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-web \
  --network-security-group nsg-web

# ASG: viết rule theo nhóm VM thay vì IP
az network asg create -g $RG -n asg-web
az network nic ip-config update -g $RG --nic-name vm1VMNic -n ipconfigvm1 \
  --application-security-groups asg-web
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-http-asg \
  --priority 200 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 --destination-asgs asg-web

# Kiểm tra
az network nic list-effective-nsg -g $RG -n vm1VMNic
az network watcher test-ip-flow -g $RG --vm vm1 --direction Inbound \
  --protocol TCP --local 10.0.1.4:3389 --remote 203.0.113.10:50000
```

**Điểm hay thi:**

- Azure giữ lại 5 địa chỉ mỗi subnet (4 đầu + 1 cuối). Subnet /29 chỉ còn 3 IP dùng được.
- Priority từ 100 đến 4096, số nhỏ được xét trước; khớp rule đầu tiên là dừng.
- Inbound: NSG subnet xét trước, rồi NSG NIC. Outbound: ngược lại. Phải cả hai cho phép thì traffic mới qua.
- Rule mặc định: AllowVNetInBound, AllowAzureLoadBalancerInBound, DenyAllInBound (priority 65000 trở lên, không xóa được).
- ASG chỉ gồm NIC trong cùng VNet.

### Peering và UDR (Ngày 8)

```bash
# Hub <-> spoke1 (tạo 2 chiều)
az network vnet peering create -g $RG -n hub-to-spoke1 --vnet-name vnet-hub \
  --remote-vnet vnet-spoke1 --allow-vnet-access --allow-forwarded-traffic \
  --allow-gateway-transit
az network vnet peering create -g $RG -n spoke1-to-hub --vnet-name vnet-spoke1 \
  --remote-vnet vnet-hub --allow-vnet-access --allow-forwarded-traffic \
  --use-remote-gateways   # chỉ chạy được khi hub đã có VPN/ER gateway

# UDR: đẩy traffic spoke1 -> spoke2 qua NVA trong hub
az network route-table create -g $RG -n rt-spoke1
az network route-table route create -g $RG --route-table-name rt-spoke1 \
  -n to-spoke2 --address-prefix 10.2.0.0/16 \
  --next-hop-type VirtualAppliance --next-hop-ip-address 10.0.3.4
az network vnet subnet update -g $RG --vnet-name vnet-spoke1 -n snet-app \
  --route-table rt-spoke1
az network nic update -g $RG -n nva-nic --ip-forwarding true
# Trong OS của NVA (Linux): sudo sysctl -w net.ipv4.ip_forward=1

az network watcher show-next-hop -g $RG --vm vm-spoke1 \
  --source-ip 10.1.1.4 --dest-ip 10.2.1.4
```

**Điểm hay thi:**

- Peering không có tính bắc cầu: spoke1 và spoke2 cùng peer với hub vẫn không nói chuyện được với nhau. Cách xử lý: peer trực tiếp, hoặc UDR qua NVA/Azure Firewall ở hub.
- Address space hai VNet không được chồng nhau. Global peering dùng được giữa các region.
- Gateway transit: Allow gateway transit ở hub, Use remote gateways ở spoke.
- Route table gắn vào subnet, không gắn vào NIC. Next hop types: VirtualAppliance, VirtualNetworkGateway, VnetLocal, Internet, None.
- NVA cần bật IP forwarding ở cả NIC (Azure) lẫn trong hệ điều hành.

### DNS và private connectivity (Ngày 9)

```bash
# Public DNS
az network dns zone create -g $RG -n khoalab.com
az network dns record-set a add-record -g $RG -z khoalab.com -n www -a 20.1.2.3

# Private DNS + autoregistration
az network private-dns zone create -g $RG -n corp.internal
az network private-dns link vnet create -g $RG -z corp.internal -n link-hub \
  -v vnet-hub -e true

# Service endpoint
az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-app \
  --service-endpoints Microsoft.Storage
az storage account network-rule add -g $RG --account-name <sa> \
  --vnet-name vnet-hub --subnet snet-app

# Private endpoint cho blob
SA_ID=$(az storage account show -g $RG -n <sa> --query id -o tsv)
az network private-endpoint create -g $RG -n pe-blob --vnet-name vnet-hub \
  --subnet snet-app --private-connection-resource-id $SA_ID \
  --group-id blob --connection-name pe-blob-conn
az network private-dns zone create -g $RG -n privatelink.blob.core.windows.net
az network private-dns link vnet create -g $RG \
  -z privatelink.blob.core.windows.net -n link-blob -v vnet-hub -e false
az network private-endpoint dns-zone-group create -g $RG \
  --endpoint-name pe-blob -n default \
  --private-dns-zone privatelink.blob.core.windows.net --zone-name blob
```

**Điểm hay thi:**

- Sau khi tạo DNS zone public, phải trỏ NS record ở nhà đăng ký tên miền về 4 name server của Azure.
- Một VNet chỉ link được với một private DNS zone có bật autoregistration, nhưng có thể link resolve-only với nhiều zone.
- Service endpoint: traffic đi backbone Azure nhưng dịch vụ vẫn dùng IP public, không truy cập được từ on-premises. Private endpoint: dịch vụ có IP private trong VNet, dùng được qua VPN/ExpressRoute.

### Load Balancer và Application Gateway (Ngày 10)

```bash
az network public-ip create -g $RG -n pip-lb --sku Standard
az network lb create -g $RG -n lb-web --sku Standard --public-ip-address pip-lb \
  --frontend-ip-name fe --backend-pool-name be
az network lb probe create -g $RG --lb-name lb-web -n hp80 --protocol Tcp --port 80
az network lb rule create -g $RG --lb-name lb-web -n http --protocol Tcp \
  --frontend-port 80 --backend-port 80 --frontend-ip-name fe \
  --backend-pool-name be --probe-name hp80
az network lb inbound-nat-rule create -g $RG --lb-name lb-web -n ssh-vm1 \
  --protocol Tcp --frontend-port 50001 --backend-port 22 --frontend-ip-name fe

# Application Gateway cần subnet riêng. XÓA NGAY SAU LAB.
az network application-gateway create -g $RG -n agw --sku Standard_v2 \
  --capacity 1 --vnet-name vnet-hub --subnet snet-agw \
  --public-ip-address pip-agw --priority 100
# Path-based routing: az network application-gateway url-path-map create ...
```

**Điểm hay thi:**

- Basic Load Balancer đã ngừng hỗ trợ từ 30/9/2025; đề mới tập trung Standard. Standard LB mặc định đóng, cần NSG cho phép traffic vào backend.
- Backend của Standard LB phải cùng VNet. Public IP của frontend phải cùng SKU Standard.
- Session persistence (client IP) cấu hình trong LB rule.

### VPN Gateway, Bastion, Network Watcher (Ngày 11)

```bash
az network vnet subnet create -g $RG --vnet-name vnet-hub -n GatewaySubnet \
  --address-prefixes 10.0.255.0/27
az network public-ip create -g $RG -n pip-vpngw --sku Standard
az network vnet-gateway create -g $RG -n vpngw --vnet vnet-hub \
  --public-ip-addresses pip-vpngw --gateway-type Vpn --vpn-type RouteBased \
  --sku VpnGw1 --no-wait   # mất 30-45 phút

az network vnet subnet create -g $RG --vnet-name vnet-hub -n AzureBastionSubnet \
  --address-prefixes 10.0.254.0/26
az network public-ip create -g $RG -n pip-bas --sku Standard
az network bastion create -g $RG -n bas --vnet-name vnet-hub \
  --public-ip-address pip-bas --sku Basic

az network watcher test-connectivity -g $RG --source-resource vm1 \
  --dest-address 10.1.1.4 --dest-port 443
```

**Điểm hay thi:**

- Tên subnet bắt buộc: `GatewaySubnet` (khuyến nghị /27), `AzureBastionSubnet` (tối thiểu /26), `AzureFirewallSubnet` (/26).
- Point-to-Site: từng máy client kết nối. Site-to-Site: cả mạng on-prem, cần local network gateway + thiết bị VPN có IP public.
- Bastion cho RDP/SSH qua trình duyệt, VM không cần public IP.

| Công cụ Network Watcher | Dùng khi |
| --- | --- |
| IP flow verify | NSG có cho phép một gói tin cụ thể không |
| Next hop | Traffic đi theo route nào |
| Connection troubleshoot | Kiểm tra kết nối từ VM đến đích |
| Effective security rules | Xem rule NSG thực tế trên NIC |
| Flow logs | Ghi log traffic qua NSG/VNet |
| Packet capture | Bắt gói tin trên VM |

## Storage (Ngày 14–15)

Domain này chiếm 15–20% đề. Trọng tâm: chọn redundancy theo yêu cầu, các loại SAS và cách thu hồi SAS.

### Storage account, tier, lifecycle (Ngày 14)

```bash
SA=stkhoalab$RANDOM
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --access-tier Hot --min-tls-version TLS1_2 --allow-blob-public-access false
az storage account update -g $RG -n $SA --sku Standard_GRS   # đổi redundancy

# --auth-mode login cần role Storage Blob Data Contributor cho chính bạn
az storage container create --account-name $SA -n data --auth-mode login
az storage blob upload --account-name $SA -c data -f ./a.txt -n logs/a.txt --auth-mode login
az storage blob set-tier --account-name $SA -c data -n logs/a.txt --tier Archive --auth-mode login
az storage blob set-tier --account-name $SA -c data -n logs/a.txt --tier Hot \
  --rehydrate-priority High --auth-mode login

az storage account blob-service-properties update -g $RG --account-name $SA \
  --enable-versioning true --enable-change-feed true \
  --enable-delete-retention true --delete-retention-days 7 \
  --enable-container-delete-retention true --container-delete-retention-days 7

az storage account management-policy create -g $RG --account-name $SA \
  --policy @policy.json
```

`policy.json`:

```json
{
  "rules": [{
    "enabled": true,
    "name": "tiering",
    "type": "Lifecycle",
    "definition": {
      "actions": {
        "baseBlob": {
          "tierToCool":    { "daysAfterModificationGreaterThan": 30 },
          "tierToArchive": { "daysAfterModificationGreaterThan": 90 },
          "delete":        { "daysAfterModificationGreaterThan": 365 }
        }
      },
      "filters": { "blobTypes": ["blockBlob"], "prefixMatch": ["data/logs"] }
    }
  }]
}
```

| Redundancy | Bản sao | Chịu được |
| --- | --- | --- |
| LRS | 3 bản trong 1 datacenter | Hỏng ổ đĩa, rack |
| ZRS | 3 bản ở 3 availability zone | Mất một zone |
| GRS | LRS chính + LRS ở region cặp | Mất cả region |
| GZRS | ZRS chính + LRS ở region cặp | Mất zone và mất region |
| RA-GRS / RA-GZRS | Như trên, đọc được region phụ | Cần đọc khi region chính lỗi |

**Điểm hay thi:**

- Archive là offline, phải rehydrate (Standard tối đa khoảng 15 giờ, High nhanh hơn) mới đọc được.
- Thời gian lưu tối thiểu: Cool 30 ngày, Cold 90 ngày, Archive 180 ngày; chuyển hoặc xóa sớm vẫn bị tính phí.
- Object replication cần bật versioning ở cả hai account và change feed ở account nguồn.
- Premium account không có access tier Hot/Cool/Archive.

### SAS, firewall, Azure Files, AzCopy (Ngày 15)

```bash
# User delegation SAS (ký bằng Entra ID, an toàn nhất)
az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry 2026-10-31T00:00Z --auth-mode login --as-user

# Stored access policy + SAS tham chiếu policy
KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage container policy create --account-name $SA -c data -n read-policy \
  --permissions rl --expiry 2026-10-31T00:00Z --account-key $KEY
az storage container generate-sas --account-name $SA -n data \
  --policy-name read-policy --account-key $KEY

# Thu hồi
az storage container policy delete --account-name $SA -c data -n read-policy --account-key $KEY
az storage account keys renew -g $RG -n $SA --key primary
az storage account revoke-delegation-keys -g $RG -n $SA

# Firewall
az storage account update -g $RG -n $SA --default-action Deny
az storage account network-rule add -g $RG --account-name $SA --ip-address 203.0.113.10

# Azure Files
az storage share-rm create -g $RG --storage-account $SA -n share1 --quota 100

# AzCopy
azcopy login
azcopy copy "./data" "https://$SA.blob.core.windows.net/data" --recursive
azcopy sync "./data" "https://$SA.blob.core.windows.net/data" --delete-destination=true
```

| Loại SAS | Ký bằng | Cách thu hồi |
| --- | --- | --- |
| Account SAS | Account key | Rotate key |
| Service SAS (ad-hoc) | Account key | Rotate key |
| Service SAS + stored access policy | Account key | Xóa hoặc sửa policy, không cần rotate key |
| User delegation SAS | Entra ID (user delegation key) | Revoke delegation keys; hiệu lực tối đa 7 ngày |

**Điểm hay thi:**

- Mỗi container có tối đa 5 stored access policy.
- Azure Files dùng SMB port 445; nhiều ISP chặn port này nên mount từ máy nhà có thể lỗi. Azure File Sync để đồng bộ file server on-prem lên share.
- Storage Explorer và AzCopy hỗ trợ cả Entra ID lẫn SAS.

## Compute (Ngày 16–19)

Domain này chiếm 20–25% đề. Trọng tâm: availability, ARM/Bicep template, tier nào của App Service có tính năng gì.

### Virtual Machine (Ngày 16)

```bash
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --zone 1 \
  --vnet-name vnet-hub --subnet snet-web --public-ip-address "" --nsg ""

az vm list-vm-resize-options -g $RG -n vm1 -o table
az vm resize -g $RG -n vm1 --size Standard_B2s
az vm disk attach -g $RG --vm-name vm1 --name data1 --new --size-gb 32 \
  --sku StandardSSD_LRS
az vm extension set -g $RG --vm-name vm1 --name CustomScript \
  --publisher Microsoft.Azure.Extensions \
  --settings '{"commandToExecute":"apt-get -y install nginx"}'
az vm deallocate -g $RG -n vm1

az vm availability-set create -g $RG -n avset1 \
  --platform-fault-domain-count 2 --platform-update-domain-count 5
```

**Điểm hay thi:**

- Availability set: tối đa 3 fault domain, 20 update domain, SLA 99,95%. Availability zone: SLA 99,99%.
- Không thêm VM đang có vào availability set được; phải tạo lại VM.
- Resize sang size không có trên cluster hiện tại phải deallocate trước. Stop trong OS không dừng tính phí compute; phải deallocate.
- Move VM sang region khác dùng Azure Resource Mover hoặc Site Recovery, không dùng `az resource move`.
- Ổ temporary (D: trên Windows) mất dữ liệu khi deallocate/redeploy.

### VMSS và ARM/Bicep (Ngày 17)

```bash
az vmss create -g $RG -n vmss1 --image Ubuntu2204 --vm-sku Standard_B1s \
  --instance-count 2 --admin-username azureuser --generate-ssh-keys \
  --orchestration-mode Uniform

az monitor autoscale create -g $RG --resource vmss1 \
  --resource-type Microsoft.Compute/virtualMachineScaleSets \
  --name as-vmss1 --min-count 1 --max-count 4 --count 2
az monitor autoscale rule create -g $RG --autoscale-name as-vmss1 \
  --condition "Percentage CPU > 70 avg 5m" --scale out 1
az monitor autoscale rule create -g $RG --autoscale-name as-vmss1 \
  --condition "Percentage CPU < 30 avg 5m" --scale in 1
```

`main.bicep` mẫu:

```bicep
param location string = resourceGroup().location
param prefix string = 'lab'

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: '${prefix}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.0.0.0/16'] }
    subnets: [
      { name: 'snet-web', properties: { addressPrefix: '10.0.1.0/24' } }
    ]
  }
}

resource sa 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${prefix}st${uniqueString(resourceGroup().id)}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { accessTier: 'Hot', minimumTlsVersion: 'TLS1_2' }
}

output saName string = sa.name
```

```bash
az deployment group what-if -g $RG -f main.bicep -p prefix=lab
az deployment group create -g $RG -f main.bicep -p prefix=lab --mode Incremental
az group export -n $RG > exported.json
az bicep decompile --file exported.json
```

**Điểm hay thi:**

- Mode Incremental (mặc định) giữ resource không có trong template. Mode Complete xóa chúng.
- ARM JSON gồm: `$schema`, `contentVersion`, `parameters`, `variables`, `resources`, `outputs`. Hotspot hay hỏi `dependsOn`, `copy`, `[resourceGroup().location]`, `[parameters('x')]`.
- Bicep tự suy ra phụ thuộc khi resource này tham chiếu property của resource kia; không cần `dependsOn`.

### App Service (Ngày 18)

```bash
az appservice plan create -g $RG -n plan1 --sku S1 --is-linux
az webapp create -g $RG -p plan1 -n khoalab-api --runtime "DOTNETCORE:8.0"
az webapp deployment slot create -g $RG -n khoalab-api --slot staging
az webapp deployment slot swap -g $RG -n khoalab-api --slot staging --target-slot production
az appservice plan update -g $RG -n plan1 --number-of-workers 2   # scale out
az appservice plan update -g $RG -n plan1 --sku P1V3              # scale up
```

| Tier | Custom domain | Slot | Autoscale |
| --- | --- | --- | --- |
| Free / Shared | Shared có | Không | Không |
| Basic | Có | Không | Không (chỉ scale thủ công) |
| Standard | Có | 5 | Có |
| Premium | Có | 20 | Có |

**Điểm hay thi:**

- Tất cả app trong cùng plan dùng chung tài nguyên; scale là scale cả plan.
- App setting đánh dấu "deployment slot setting" ở lại slot, không đi theo khi swap.
- Muốn có deployment slot hoặc autoscale mà đang ở Basic → nâng lên Standard (đáp án "minimize cost").

### Containers (Ngày 19)

```bash
az acr create -g $RG -n khoalabacr --sku Basic
az acr build -r khoalabacr -t api:v1 .

az container create -g $RG -n aci-api --image khoalabacr.azurecr.io/api:v1 \
  --os-type Linux --cpu 1 --memory 1.5 --ports 8080 --ip-address Public \
  --registry-login-server khoalabacr.azurecr.io \
  --registry-username <acr-user> --registry-password <acr-password> \
  --restart-policy OnFailure

az containerapp up -n ca-api -g $RG --image khoalabacr.azurecr.io/api:v1 \
  --ingress external --target-port 8080
```

**Điểm hay thi:**

- ACR SKU: Basic, Standard, Premium. Geo-replication và private endpoint chỉ có ở Premium.
- ACI restart policy: Always (mặc định), OnFailure, Never. Container group = các container chạy chung host, chung IP.
- Container Apps hỗ trợ scale to zero và scale theo HTTP/event; ACI thì không tự scale.

## Monitor & Backup (Ngày 20–21)

Domain này chiếm 10–15% đề. Trọng tâm: alert kèm action group, KQL cơ bản, loại vault nào backup được gì.

### Alert và Log Analytics (Ngày 20)

```bash
az monitor action-group create -g $RG -n ag-mail --short-name agmail \
  --action email khoa khoa@example.com

VM_ID=$(az vm show -g $RG -n vm1 --query id -o tsv)
az monitor metrics alert create -g $RG -n cpu-high --scopes $VM_ID \
  --condition "avg Percentage CPU > 80" --window-size 5m \
  --evaluation-frequency 1m --action ag-mail

az monitor log-analytics workspace create -g $RG -n law-lab
LAW_ID=$(az monitor log-analytics workspace show -g $RG -n law-lab --query id -o tsv)
az monitor diagnostic-settings create --name to-law --resource <resource-id> \
  --workspace $LAW_ID --metrics '[{"category":"AllMetrics","enabled":true}]'
```

KQL hay gặp:

```kusto
// VM mất heartbeat hơn 5 phút
Heartbeat
| summarize LastSeen = max(TimeGenerated) by Computer
| where LastSeen < ago(5m)

// CPU trung bình theo 5 phút (cần DCR thu perf counter)
Perf
| where ObjectName == "Processor" and CounterName == "% Processor Time"
| summarize avg(CounterValue) by Computer, bin(TimeGenerated, 5m)
| render timechart

// Ai đã xóa resource
AzureActivity
| where OperationNameValue has "delete"
| project TimeGenerated, Caller, ResourceGroup, OperationNameValue
| order by TimeGenerated desc

// 20 event lỗi gần nhất
Event
| where EventLevelName == "Error"
| take 20
```

**Điểm hay thi:**

- Alert gồm 3 phần: scope (resource), condition (signal), action group (email, SMS, webhook, Logic App, Azure Function, runbook).
- Activity log giữ 90 ngày; muốn lâu hơn phải đẩy sang Log Analytics hoặc storage qua diagnostic settings.
- Thứ tự toán tử KQL: lọc (`where`) trước, rồi `summarize`/`project`, cuối cùng `render`.
- VM insights và thu log VM hiện dùng Azure Monitor Agent + Data Collection Rule.

### Backup (Ngày 21)

```bash
az backup vault create -g $RG -n rsv-lab -l southeastasia
# Tắt soft delete trong lab để xóa được resource group
az backup vault backup-properties set -g $RG -n rsv-lab --soft-delete-feature-state Disable

az backup protection enable-for-vm -g $RG --vault-name rsv-lab --vm vm1 \
  --policy-name DefaultPolicy
az backup protection backup-now -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --retain-until 10-11-2026
az backup recoverypoint list -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM -o table
```

| Vault | Backup được |
| --- | --- |
| Recovery Services vault | Azure VM, SQL/SAP HANA trong VM, Azure Files, máy on-prem qua MARS agent |
| Backup vault | Azure Disks, Azure Blobs, Azure Database for PostgreSQL, AKS |

**Điểm hay thi:**

- Vault phải cùng region với VM được backup.
- Restore VM có 3 kiểu: tạo VM mới, restore disk, replace existing. Restore từng file dùng File Recovery (mount recovery point dạng ổ đĩa).
- Muốn xóa vault: dừng protection, xóa backup data, chờ hết soft delete (14 ngày) hoặc tắt soft delete trước.
- Azure Site Recovery dùng cho DR: replicate VM sang region khác, failover/failback. Backup dùng cho khôi phục dữ liệu theo thời điểm.

## Bảng tổng hợp và checklist nước rút

Đây là phần đọc lại vào ngày 24. Khi gặp câu "chọn dịch vụ nào", hãy xác định trước: tầng mạng (L4 hay L7), phạm vi (một region hay toàn cầu), giao thức (HTTP hay không).

### Chọn dịch vụ cân bằng tải

| Dịch vụ | Tầng | Phạm vi | Dùng khi |
| --- | --- | --- | --- |
| Load Balancer | L4 (TCP/UDP) | Region | Cân bằng TCP/UDP cho VM, không cần hiểu HTTP |
| Application Gateway | L7 (HTTP/S) | Region | Path-based routing, SSL termination, WAF |
| Front Door | L7 (HTTP/S) | Toàn cầu | Web app nhiều region, CDN, WAF toàn cầu |
| Traffic Manager | DNS | Toàn cầu | Định tuyến theo DNS, mọi giao thức |

### Các con số hay thi

| Mục | Giá trị |
| --- | --- |
| IP Azure giữ lại mỗi subnet | 5 |
| NSG priority | 100–4096 |
| `AzureBastionSubnet` tối thiểu | /26 |
| `GatewaySubnet` khuyến nghị | /27 |
| Fault domain / update domain tối đa | 3 / 20 |
| SLA availability set / zone | 99,95% / 99,99% |
| Cấp management group tối đa | 6 (không tính root) |
| Stored access policy mỗi container | 5 |
| User delegation SAS tối đa | 7 ngày |
| Lưu tối thiểu Cool / Cold / Archive | 30 / 90 / 180 ngày |
| Activity log giữ mặc định | 90 ngày |
| Backup soft delete | 14 ngày |
| Deployment slot Standard / Premium | 5 / 20 |

### Từ khóa trong đề và hướng chọn

| Đề nói | Thường chọn |
| --- | --- |
| minimize administrative effort | Dịch vụ managed, policy, built-in role thay vì script hoặc custom |
| minimize cost | SKU/tier thấp nhất vẫn đáp ứng yêu cầu |
| least privilege | Role hẹp nhất ở scope hẹp nhất |
| truy cập từ on-premises bằng IP private | Private endpoint, không phải service endpoint |
| VM không có public IP mà vẫn RDP/SSH | Azure Bastion |
| ngăn xóa nhầm, kể cả Owner | Resource lock CanNotDelete |
| bắt buộc tag/region cho resource mới | Azure Policy (deny hoặc modify) |

### Checklist nước rút

- [ ] Ngày 22: mock exam 1, bấm giờ 120 phút, có ít nhất một case study
- [ ] Ngày 22: ghi lại câu sai theo domain
- [ ] Ngày 23: lab lại 2–3 chủ đề sai nhiều nhất
- [ ] Ngày 23: mock exam 2, mục tiêu trên 80%
- [ ] Ngày 24: đọc lại các bảng trong phần này và mục "Điểm hay thi" từng domain
- [ ] Ngày 24: chạy `az resource list -o table`, xóa mọi resource còn sót
- [ ] Ngày 24: kiểm tra lịch thi, giấy tờ tùy thân, phòng thi hoặc máy thi online
- [ ] Trong phòng thi: đọc dòng "You need to…" trước, gạch chân ràng buộc, case study đọc câu hỏi rồi mới mở tài liệu
