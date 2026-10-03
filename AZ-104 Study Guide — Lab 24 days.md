# AZ-104 Study Guide — Lab 24 ngày

Oct 3, 2026 · @Khoa

## Tổng quan và lịch 24 ngày

Mục tiêu: qua AZ-104 (700/1000) sau 24 ngày lab, khoảng 2–2,5 giờ mỗi tối, tổng chi phí dưới 200 USD credit. Networking (7 ngày) và Identity (5 ngày) chiếm khoảng một nửa thời lượng.

Cách dùng guide: mỗi ngày gồm mục tiêu, các bước lab, lệnh CLI/JSON/Bicep/KQL, câu tự kiểm tra và mục **Điểm hay thi**. Mỗi lab làm cả trên portal lẫn CLI, vì câu hotspot thường bắt điền tham số lệnh hoặc template.

**Format đề:** khoảng 40–60 câu, 120 phút, điểm quy đổi. Dạng câu gồm multiple choice, drag-and-drop, hotspot, chuỗi Yes/No (không quay lại được) và 1–2 case study. Không có lab thực hành. Được mở Microsoft Learn trong giờ thi, nhưng dùng chung đồng hồ.

| Ngày | Domain | Lab chính |
| --- | --- | --- |
| 1 | Setup | Budget alert, CLI, Bicep, 2 user test |
| 2 | Identity | Users, guest, bulk CSV, assigned group; tính năng cần P1/P2 học lý thuyết |
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

## Ngày 1 — Setup và kiểm soát chi phí

**Mục tiêu:** CLI trỏ đúng subscription, có budget alert, có thói quen xóa resource group mỗi tối. Làm đúng quy tắc xóa hằng ngày thì cả 24 ngày tốn khoảng 40–70 USD.

**Các bước lab:**

1. Đăng nhập, kiểm tra đúng tenant và subscription.
2. Đặt location mặc định, cài Bicep.
3. Tạo budget 200 USD kèm alert 50/80/100% và alert dự báo.
4. Tạo 2 user test `lab-reader`, `lab-ops` (dùng cho RBAC ngày 3).
5. Tập chạy script dọn dẹp.

```bash
az login
az account show --query "{sub:name, id:id, tenant:tenantId, user:user.name}" -o table
# Sai tenant: az login --tenant <tenant-id>
az account set --subscription "<subscription-id>"

az configure --defaults location=southeastasia   # không bị cảnh báo experimental như az config
az bicep install
```

Tạo budget bằng REST API (`az consumption budget create` đang lỗi API version cũ). File `budget.json`:

```json
{
  "properties": {
    "category": "Cost",
    "amount": 200,
    "timeGrain": "Monthly",
    "timePeriod": { "startDate": "2026-10-01T00:00:00Z", "endDate": "2026-12-31T00:00:00Z" },
    "notifications": {
      "Actual_50":    { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 50,  "thresholdType": "Actual",     "contactEmails": ["ban@example.com"] },
      "Actual_80":    { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 80,  "thresholdType": "Actual",     "contactEmails": ["ban@example.com"] },
      "Actual_100":   { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 100, "thresholdType": "Actual",     "contactEmails": ["ban@example.com"] },
      "Forecast_100": { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 100, "thresholdType": "Forecasted", "contactEmails": ["ban@example.com"] }
    }
  }
}
```

```bash
SUB=$(az account show --query id -o tsv)
az rest --method put \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets/lab-200?api-version=2023-05-01" \
  --body @budget.json

# Gặp RBACAccessDenied: kiểm tra role và đăng ký provider
az role assignment list --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope /subscriptions/$SUB --include-inherited -o table
az provider register --namespace Microsoft.Consumption
az provider register --namespace Microsoft.CostManagement
# Vẫn lỗi: tạo trên portal, Cost Management > Budgets > Add
```

Tạo user test:

```bash
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)
az ad user create --display-name "Lab Reader" --user-principal-name lab-reader@$DOMAIN \
  --password '<mat-khau-tam>' --force-change-password-next-sign-in true
az ad user create --display-name "Lab Ops" --user-principal-name lab-ops@$DOMAIN \
  --password '<mat-khau-tam>' --force-change-password-next-sign-in true
```

Dọn dẹp cuối mỗi ngày:

```bash
az group list --query "[?starts_with(name,'rg-lab')].name" -o tsv \
  | xargs -I {} az group delete -n {} --yes --no-wait
az resource list --query "[].{name:name, type:type, rg:resourceGroup}" -o table
```

**Điểm hay thi:**

- Budget chỉ gửi cảnh báo (email hoặc action group), không tự dừng resource.
- Role least privilege để tạo và quản lý budget mà không sửa resource: **Cost Management Contributor**. Chỉ xem chi phí: Cost Management Reader.
- Tài nguyên đốt credit nhanh nhất: Azure Firewall, VPN Gateway, Application Gateway, Bastion. Xóa ngay sau lab.

**Tự kiểm tra:** Alert `Forecasted` khác `Actual` thế nào? Vì sao `--auth-mode login` đọc blob vẫn lỗi dù bạn là Owner? (đáp án ở Ngày 3)

## Ngày 2 — Users, groups, guest

**Mục tiêu:** làm thành thạo mọi thao tác user/group mà Entra ID Free cho phép, và học kỹ lý thuyết các tính năng cần P1/P2 vì đề vẫn hỏi.

**Các bước lab:**

1. Tạo user bằng CLI và portal; sửa thuộc tính `department`, `jobTitle`, `usageLocation`.
2. Bulk create 5 user bằng file CSV mẫu (portal: Users > Bulk operations > Bulk create).
3. Mời 1 guest user (B2B) bằng email cá nhân, đăng nhập thử bằng guest.
4. Tạo security group và Microsoft 365 group dạng **Assigned**; thêm member và owner.
5. Bulk add member vào group bằng CSV.
6. Đọc phần dynamic group bên dưới và tự viết 5 rule ra giấy.

```bash
az ad user update --id lab-reader@$DOMAIN --department IT --job-title Engineer
az ad user list --filter "department eq 'IT'" --query "[].userPrincipalName" -o tsv

az ad group create --display-name IT-Team --mail-nickname itteam
az ad group member add --group IT-Team \
  --member-id $(az ad user show --id lab-reader@$DOMAIN --query id -o tsv)
az ad group owner add --group IT-Team \
  --owner-object-id $(az ad user show --id lab-ops@$DOMAIN --query id -o tsv)
az ad group member list --group IT-Team --query "[].displayName" -o tsv
```

### Giới hạn của Entra ID Free

Tenant của tài khoản Azure free chỉ có Entra ID Free. Các tính năng dưới đây không tạo được, nhưng vẫn xuất hiện trong đề.

| Tính năng | License cần | Trên Free làm gì thay |
| --- | --- | --- |
| Dynamic group (user hoặc device) | P1 | Assigned group + bulk add member bằng CSV; học cú pháp rule |
| Gán license theo group | P1 | Học lý thuyết: lỗi thường gặp là thiếu `usageLocation` |
| SSPR cho user thường | P1 | Admin vẫn tự reset được; học số phương thức xác thực, registration, writeback |
| Conditional Access | P1 | Security defaults (bật MFA cơ bản, miễn phí) |
| Custom Entra role | P1 | Dùng built-in Entra role; custom **Azure RBAC** role vẫn miễn phí (Ngày 3) |
| Gán admin cho administrative unit, dynamic AU | P1 | Tạo AU thường, học lý thuyết phần gán quyền |
| PIM, Access reviews, Identity Protection | P2 | Học lý thuyết |

Muốn lab thật các tính năng trên thì cần tenant có license, ví dụ sandbox của Microsoft 365 Developer Program (hiện chỉ cấp cho một số đối tượng đủ điều kiện) hoặc tenant công ty cho phép.

### Dynamic group: học lý thuyết

Cú pháp rule (đề hay bắt chọn rule đúng hoặc điền toán tử):

```text
user.department -eq "IT"
user.country -in ["VN","SG"]
(user.jobTitle -contains "Engineer") -and (user.accountEnabled -eq true)
user.userPrincipalName -endsWith "@contoso.com"
device.deviceOSType -eq "Windows"
user.memberOf -any (group.objectId -in ["<group-id>"])
```

Nếu có tenant P1, lệnh tạo bằng Microsoft Graph PowerShell:

```powershell
New-MgGroup -DisplayName "IT Dynamic" -MailEnabled:$false -MailNickname itdyn `
  -SecurityEnabled -GroupTypes "DynamicMembership" `
  -MembershipRule 'user.department -eq "IT"' -MembershipRuleProcessingState On
```

**Điểm hay thi:**

- Dynamic group không thêm/xóa member thủ công được; member cập nhật không tức thì.
- Một dynamic group chỉ chứa user hoặc chỉ chứa device, không trộn.
- Microsoft 365 group chỉ chứa user; security group chứa user, device, group khác.
- Guest (B2B) đăng nhập bằng tài khoản của họ; quyền mặc định hạn chế hơn member. Có thể chặn mời guest trong External collaboration settings.
- Xóa user: khôi phục được trong 30 ngày.

**Tự kiểm tra:** Muốn tự động đưa mọi user phòng IT vào group thì cần license gì? Trên Free thì làm cách nào gần nhất?

## Ngày 3 — RBAC và custom role

**Mục tiêu:** chọn đúng role ở đúng scope; viết được custom role JSON. Toàn bộ phần này chạy được trên Entra ID Free.

**Các bước lab:**

1. Gán Reader cho `lab-reader` ở resource group, đăng nhập bằng user đó (cửa sổ ẩn danh) và thử tạo resource để thấy bị chặn.
2. Gán cùng role ở scope subscription, so sánh phạm vi nhìn thấy.
3. Gán cho chính mình Storage Blob Data Contributor rồi thử `--auth-mode login`.
4. Viết custom role VM Operator, gán cho `lab-ops`, thử start/stop VM.

```bash
SUB=$(az account show --query id -o tsv)
az group create -n rg-lab-d03
az role assignment create --assignee lab-reader@$DOMAIN --role "Reader" \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03
az role assignment list --assignee lab-reader@$DOMAIN --all -o table
az role definition list --name "Virtual Machine Contributor" --query "[].permissions"
```

`vm-operator.json`:

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
az role assignment create --assignee lab-ops@$DOMAIN --role "VM Operator" \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03
```

| Role | Quản lý resource | Gán quyền cho người khác |
| --- | --- | --- |
| Owner | Có | Có |
| Contributor | Có | Không |
| User Access Administrator | Không | Có |
| Reader | Chỉ xem | Không |

**Điểm hay thi:**

- Scope kế thừa từ trên xuống: management group → subscription → resource group → resource.
- `Actions` là control plane; `DataActions` là data plane. Owner không tự đọc được blob qua Entra ID, cần thêm Storage Blob Data Reader/Contributor.
- Entra roles (Global Administrator, User Administrator) quản lý directory; Azure RBAC roles quản lý resource. Hai hệ tách biệt.
- Đăng nhập VM bằng Entra ID cần Virtual Machine User Login hoặc Administrator Login.
- Thay đổi role có thể mất vài phút mới có hiệu lực; token cũ cần đăng nhập lại.

**Tự kiểm tra:** Role hẹp nhất cho phép start/stop VM trong một resource group là gì? Muốn một người gán quyền cho người khác nhưng không sửa resource thì dùng role nào?

## Ngày 4 — Azure Policy và resource lock

**Mục tiêu:** phân biệt các effect, chạy được remediation, hiểu lock chặn những gì.

**Các bước lab:**

1. Gán policy Allowed locations ở resource group, thử tạo resource ở region khác để thấy bị chặn.
2. Gán policy inherit tag từ resource group (effect modify) kèm managed identity, chạy remediation cho resource cũ.
3. Gom 2 policy thành initiative và gán.
4. Tạo lock ReadOnly rồi thử start VM, list key của storage account.

```bash
az group create -n rg-lab-d04 --tags env=lab
POLICY=$(az policy definition list \
  --query "[?displayName=='Allowed locations'].name" -o tsv)
az policy assignment create --name allowed-locs \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d04 --policy $POLICY \
  --params '{"listOfAllowedLocations":{"value":["southeastasia"]}}'

INHERIT=$(az policy definition list \
  --query "[?displayName=='Inherit a tag from the resource group'].name" -o tsv)
az policy assignment create --name inherit-env-tag \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d04 --policy $INHERIT \
  --params '{"tagName":{"value":"env"}}' \
  --mi-system-assigned --location southeastasia
# Managed identity cần role Tag Contributor (portal tự gán; CLI phải gán thủ công)
az policy remediation create --name fix-env-tag \
  --policy-assignment inherit-env-tag --resource-group rg-lab-d04
az policy state summarize --resource-group rg-lab-d04

az lock create --name read-only --lock-type ReadOnly --resource-group rg-lab-d04
az lock delete --name read-only --resource-group rg-lab-d04
```

| Effect | Tác dụng |
| --- | --- |
| deny | Chặn tạo/sửa resource không tuân thủ |
| audit | Cho tạo, đánh dấu non-compliant |
| append | Thêm field khi tạo/sửa |
| modify | Thêm/sửa tag hoặc property; sửa resource cũ qua remediation |
| deployIfNotExists | Deploy resource liên quan nếu thiếu (ví dụ diagnostic setting) |
| auditIfNotExists | Báo non-compliant nếu thiếu resource liên quan |
| disabled | Tắt policy |

**Điểm hay thi:**

- Policy chỉ chặn lúc tạo/sửa; resource có sẵn chỉ bị báo non-compliant cho đến khi remediation.
- Tag không tự kế thừa từ resource group; muốn vậy phải dùng policy effect modify.
- Lock áp dụng cho cả Owner. ReadOnly chặn cả thao tác POST: không start VM, không list storage key.
- Lock và policy kế thừa xuống scope con; có thể dùng exclusion để loại một scope khỏi policy.

**Tự kiểm tra:** Resource tạo trước khi gán policy deny có bị xóa không? Effect nào sửa được resource cũ?

## Ngày 5 — Management group, move resource, tags, cost

**Mục tiêu:** dựng cây management group, move resource giữa resource group, dùng tag để phân tích chi phí.

**Các bước lab:**

1. Tạo management group `mg-lab`, đưa subscription vào, gán một policy ở cấp này.
2. Tạo 2 resource group, tạo storage account và VNet ở nhóm A rồi move sang nhóm B.
3. Gắn tag `env`, `owner` và xem Cost Analysis group by tag.
4. Mở Azure Advisor, đọc các khuyến nghị cost.

```bash
az account management-group create --name mg-lab
az account management-group subscription add --name mg-lab --subscription $SUB

az group create -n rg-lab-d05a && az group create -n rg-lab-d05b
IDS=$(az resource list -g rg-lab-d05a --query "[].id" -o tsv)
az resource move --destination-group rg-lab-d05b --ids $IDS

az group update -n rg-lab-d05b --tags env=lab owner=khoa
az tag update --resource-id <resource-id> --operation Merge --tags costcenter=it
```

**Điểm hay thi:**

- Cây management group sâu tối đa 6 cấp (không tính root); mỗi subscription chỉ thuộc một management group.
- Move resource không đổi region; resource group nguồn và đích bị khóa ghi trong lúc move.
- VM phải move cùng disk, NIC; kiểm tra bằng Validate trên portal trước khi move.
- Tag: tối đa 50 cặp mỗi resource; không kế thừa mặc định.

**Tự kiểm tra:** Move VM sang resource group khác có làm đổi region không? Muốn áp policy cho nhiều subscription cùng lúc thì gán ở đâu?

## Ngày 6 — Ôn Identity & Governance

**Mục tiêu:** khóa điểm domain 20–25% này trước khi sang Networking.

**Các bước:**

1. Làm khoảng 40 câu practice của domain Identity & Governance (Microsoft Learn practice assessment miễn phí).
2. Ghi lại từng câu sai: sai vì không biết, hay sai vì đọc sót ràng buộc.
3. Lab lại đúng những thao tác ở câu sai.
4. Viết 1 trang ghi nhớ: bảng role (Ngày 3), bảng effect (Ngày 4), bảng giới hạn Entra ID Free (Ngày 2).

**Tự kiểm tra nhanh:**

- [ ] Phân biệt được Owner, Contributor, User Access Administrator
- [ ] Viết được custom role JSON không cần xem mẫu
- [ ] Nói được effect nào cần managed identity
- [ ] Biết ReadOnly lock chặn những thao tác nào
- [ ] Biết tính năng nào cần P1, tính năng nào cần P2

## Ngày 7 — VNet, NSG, ASG

**Mục tiêu:** dựng VNet có 2 subnet, hiểu thứ tự đánh giá NSG, viết rule theo ASG.

**Các bước lab:**

1. Tạo `vnet-hub` (10.0.0.0/16) với subnet `snet-web`, `snet-app`.
2. Tạo VM B1s trong `snet-web`, gắn NSG cả ở subnet lẫn NIC.
3. Cho phép RDP/SSH chỉ từ IP nhà bạn; xem effective rules.
4. Tạo ASG `asg-web`, gắn NIC vào và viết rule theo ASG.
5. Dùng IP flow verify kiểm tra một gói bị chặn và một gói được phép.

```bash
RG=rg-lab-d07 && az group create -n $RG
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-web --subnet-prefixes 10.0.1.0/24
az network vnet subnet create -g $RG --vnet-name vnet-hub -n snet-app \
  --address-prefixes 10.0.2.0/24

az network nsg create -g $RG -n nsg-web
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-ssh-myip \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 22 --source-address-prefixes <ip-nha-ban>
az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-web \
  --network-security-group nsg-web

az network asg create -g $RG -n asg-web
az network nic ip-config update -g $RG --nic-name vm1VMNic -n ipconfigvm1 \
  --application-security-groups asg-web
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-http-asg \
  --priority 200 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 --destination-asgs asg-web

az network nic list-effective-nsg -g $RG -n vm1VMNic
az network watcher test-ip-flow -g $RG --vm vm1 --direction Inbound \
  --protocol TCP --local 10.0.1.4:22 --remote <ip-nha-ban>:50000
```

**Điểm hay thi:**

- Azure giữ lại 5 IP mỗi subnet; /29 chỉ còn 3 IP dùng được.
- Priority 100–4096, số nhỏ xét trước; khớp rule đầu tiên là dừng.
- Inbound: NSG subnet trước, rồi NSG NIC. Outbound: ngược lại. Cả hai phải Allow.
- Rule mặc định (65000+): AllowVNetInBound, AllowAzureLoadBalancerInBound, DenyAllInBound; không xóa được.
- ASG chỉ gồm NIC trong cùng VNet.

**Tự kiểm tra:** NSG subnet cho phép port 80 nhưng NSG NIC không có rule nào cho port 80, traffic có vào được không?

## Ngày 8 — Peering, UDR, NVA

**Mục tiêu:** chứng minh peering không bắc cầu, rồi dùng UDR qua NVA để nối 2 spoke.

**Các bước lab:**

1. Tạo `vnet-hub` (10.0.0.0/16), `vnet-spoke1` (10.1.0.0/16), `vnet-spoke2` (10.2.0.0/16), mỗi VNet 1 VM B1s.
2. Peer hub với từng spoke (2 chiều). Ping từ spoke1 sang spoke2 để thấy thất bại.
3. Tạo VM NVA trong hub (10.0.3.4), bật IP forwarding ở NIC và trong OS.
4. Tạo route table cho spoke1 và spoke2 trỏ về NVA; ping lại.
5. Dùng Next hop xác nhận đường đi.

```bash
az network vnet peering create -g $RG -n hub-to-spoke1 --vnet-name vnet-hub \
  --remote-vnet vnet-spoke1 --allow-vnet-access --allow-forwarded-traffic
az network vnet peering create -g $RG -n spoke1-to-hub --vnet-name vnet-spoke1 \
  --remote-vnet vnet-hub --allow-vnet-access --allow-forwarded-traffic
# Lặp lại cho spoke2

az network route-table create -g $RG -n rt-spoke1
az network route-table route create -g $RG --route-table-name rt-spoke1 \
  -n to-spoke2 --address-prefix 10.2.0.0/16 \
  --next-hop-type VirtualAppliance --next-hop-ip-address 10.0.3.4
az network vnet subnet update -g $RG --vnet-name vnet-spoke1 -n default \
  --route-table rt-spoke1

az network nic update -g $RG -n nvaVMNic --ip-forwarding true
# Trong NVA (Linux): sudo sysctl -w net.ipv4.ip_forward=1

az network watcher show-next-hop -g $RG --vm vm-spoke1 \
  --source-ip 10.1.0.4 --dest-ip 10.2.0.4
```

**Điểm hay thi:**

- Peering không bắc cầu. Nối 2 spoke: peer trực tiếp, hoặc UDR qua NVA/Azure Firewall ở hub.
- Address space không được chồng nhau; global peering dùng được giữa các region.
- Gateway transit: Allow gateway transit ở hub, Use remote gateways ở spoke (cần hub có gateway).
- Route table gắn vào subnet. Next hop types: VirtualAppliance, VirtualNetworkGateway, VnetLocal, Internet, None.

**Tự kiểm tra:** Spoke1 và spoke2 đều peer với hub, VM spoke1 có ping được VM spoke2 không? Cần mấy thay đổi để được?

## Ngày 9 — DNS, service endpoint, private endpoint

**Mục tiêu:** phân biệt public/private DNS, và service endpoint với private endpoint.

**Các bước lab:**

1. Tạo public DNS zone và một A record (không cần sở hữu tên miền để lab).
2. Tạo private DNS zone `corp.internal`, link với VNet có bật autoregistration; tạo VM và xem record tự xuất hiện.
3. Bật service endpoint Microsoft.Storage cho subnet, giới hạn storage account chỉ nhận từ subnet đó.
4. Tạo private endpoint cho blob, kiểm tra `nslookup` từ VM trả về IP private.

```bash
az network dns zone create -g $RG -n khoalab.com
az network dns record-set a add-record -g $RG -z khoalab.com -n www -a 20.1.2.3

az network private-dns zone create -g $RG -n corp.internal
az network private-dns link vnet create -g $RG -z corp.internal -n link-hub \
  -v vnet-hub -e true

az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-app \
  --service-endpoints Microsoft.Storage
az storage account update -g $RG -n <sa> --default-action Deny
az storage account network-rule add -g $RG --account-name <sa> \
  --vnet-name vnet-hub --subnet snet-app

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
# Từ VM: nslookup <sa>.blob.core.windows.net
```

**Điểm hay thi:**

- Public zone phải được trỏ NS record ở nhà đăng ký tên miền về name server Azure.
- Một VNet chỉ link với một private zone có autoregistration; có thể link resolve-only với nhiều zone.
- Service endpoint: dịch vụ vẫn dùng IP public, không dùng được từ on-premises. Private endpoint: IP private trong VNet, dùng được qua VPN/ExpressRoute.

**Tự kiểm tra:** Công ty cần truy cập storage từ văn phòng qua VPN bằng IP private, chọn service endpoint hay private endpoint?

## Ngày 10 — Load Balancer và Application Gateway

**Mục tiêu:** dựng Standard Load Balancer cho 2 VM; làm path-based routing với Application Gateway. Xóa App Gateway ngay khi xong.

**Các bước lab:**

1. Tạo 2 VM B1s cài nginx (Custom Script extension) trong cùng subnet.
2. Tạo Standard LB, health probe, LB rule port 80, inbound NAT rule SSH cho từng VM.
3. Mở port 80 trong NSG (Standard LB mặc định đóng), truy cập IP của LB và tắt nginx một VM để thấy probe loại VM đó.
4. Tạo Application Gateway Standard\_v2 trong subnet riêng, cấu hình `/images/*` và `/api/*` về 2 backend pool.

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
az network nic ip-config address-pool add -g $RG --nic-name vm1VMNic \
  --ip-config-name ipconfigvm1 --lb-name lb-web --address-pool be

az network vnet subnet create -g $RG --vnet-name vnet-hub -n snet-agw \
  --address-prefixes 10.0.10.0/24
az network public-ip create -g $RG -n pip-agw --sku Standard
az network application-gateway create -g $RG -n agw --sku Standard_v2 \
  --capacity 1 --vnet-name vnet-hub --subnet snet-agw \
  --public-ip-address pip-agw --priority 100
# Path-based: az network application-gateway url-path-map create ... (portal dễ hơn)
az network application-gateway delete -g $RG -n agw
```

| Dịch vụ | Tầng | Phạm vi | Dùng khi |
| --- | --- | --- | --- |
| Load Balancer | L4 (TCP/UDP) | Region | Cân bằng TCP/UDP cho VM |
| Application Gateway | L7 (HTTP/S) | Region | Path-based routing, SSL termination, WAF |
| Front Door | L7 (HTTP/S) | Toàn cầu | Web app nhiều region, CDN, WAF toàn cầu |
| Traffic Manager | DNS | Toàn cầu | Định tuyến theo DNS, mọi giao thức |

**Điểm hay thi:**

- Basic Load Balancer đã ngừng hỗ trợ từ 30/9/2025; đề tập trung Standard.
- Standard LB mặc định chặn traffic vào, cần NSG cho phép. Backend phải cùng VNet; public IP phải Standard.
- Session persistence (client IP) cấu hình trong LB rule.
- Application Gateway cần subnet riêng, không chứa resource khác.

**Tự kiểm tra:** Cần định tuyến `/api` và `/images` về 2 nhóm server khác nhau trong một region, chọn dịch vụ nào?

## Ngày 11 — VPN Gateway, Bastion, Network Watcher

**Mục tiêu:** dựng Point-to-Site VPN, truy cập VM không public IP qua Bastion, dùng các công cụ Network Watcher. Xóa gateway và Bastion ngay khi xong.

**Các bước lab:**

1. Tạo `GatewaySubnet` và bấm deploy VPN Gateway trước (mất 30–45 phút).
2. Trong lúc chờ: tạo `AzureBastionSubnet`, deploy Bastion, RDP/SSH vào VM không có public IP.
3. Bật NSG/VNet flow logs, chạy Connection troubleshoot.
4. Khi gateway xong: cấu hình Point-to-Site (address pool, xác thực Entra ID hoặc certificate), kết nối thử từ máy bạn.

```bash
az network vnet subnet create -g $RG --vnet-name vnet-hub -n GatewaySubnet \
  --address-prefixes 10.0.255.0/27
az network public-ip create -g $RG -n pip-vpngw --sku Standard
az network vnet-gateway create -g $RG -n vpngw --vnet vnet-hub \
  --public-ip-addresses pip-vpngw --gateway-type Vpn --vpn-type RouteBased \
  --sku VpnGw1 --no-wait

az network vnet subnet create -g $RG --vnet-name vnet-hub -n AzureBastionSubnet \
  --address-prefixes 10.0.254.0/26
az network public-ip create -g $RG -n pip-bas --sku Standard
az network bastion create -g $RG -n bas --vnet-name vnet-hub \
  --public-ip-address pip-bas --sku Basic

az network watcher test-connectivity -g $RG --source-resource vm1 \
  --dest-address 10.1.0.4 --dest-port 443

az network bastion delete -g $RG -n bas
az network vnet-gateway delete -g $RG -n vpngw
```

| Công cụ Network Watcher | Dùng khi |
| --- | --- |
| IP flow verify | NSG có cho phép một gói tin cụ thể không |
| Next hop | Traffic đi theo route nào |
| Connection troubleshoot | Kiểm tra kết nối từ VM đến đích |
| Effective security rules | Xem rule NSG thực tế trên NIC |
| Flow logs | Ghi log traffic qua NSG/VNet |
| Packet capture | Bắt gói tin trên VM |

**Điểm hay thi:**

- Tên subnet bắt buộc: `GatewaySubnet` (khuyến nghị /27), `AzureBastionSubnet` (tối thiểu /26), `AzureFirewallSubnet` (/26).
- Point-to-Site: từng máy client. Site-to-Site: cả mạng on-prem, cần local network gateway và thiết bị VPN có IP public.
- Bastion: RDP/SSH qua trình duyệt, VM không cần public IP.

**Tự kiểm tra:** Admin cần RDP vào VM mà không mở port 3389 ra internet và không gán public IP, chọn gì?

## Ngày 12 — Viết hub-spoke bằng Bicep

**Mục tiêu:** tự viết từ đầu, không copy, một file Bicep dựng lại bài Ngày 8, trong 60 phút. Đây là bài luyện trực tiếp cho câu hotspot dạng template.

**Yêu cầu file `hub-spoke.bicep`:**

1. 3 VNet: hub 10.0.0.0/16, spoke1 10.1.0.0/16, spoke2 10.2.0.0/16.
2. Peering 2 chiều hub ↔ spoke1, hub ↔ spoke2.
3. NSG cho subnet spoke chỉ cho SSH từ IP truyền vào qua parameter.
4. Route table cho spoke1 và spoke2 trỏ next hop về 10.0.3.4.
5. Chạy `what-if` trước, rồi deploy, rồi kiểm tra bằng Next hop.

Các đoạn cú pháp hay quên:

```bicep
param myIp string

resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: 'nsg-spoke'
  location: resourceGroup().location
  properties: {
    securityRules: [
      {
        name: 'allow-ssh'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: myIp
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '22'
        }
      }
    ]
  }
}

resource rt 'Microsoft.Network/routeTables@2023-11-01' = {
  name: 'rt-spoke1'
  location: resourceGroup().location
  properties: {
    routes: [
      {
        name: 'to-spoke2'
        properties: {
          addressPrefix: '10.2.0.0/16'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: '10.0.3.4'
        }
      }
    ]
  }
}

// Peering là resource con của VNet
resource hubToSpoke1 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-11-01' = {
  parent: hub
  name: 'hub-to-spoke1'
  properties: {
    remoteVirtualNetwork: { id: spoke1.id }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
  }
}

// Gắn NSG và route table vào subnet trong khai báo VNet:
// subnets: [{ name: 'default', properties: {
//   addressPrefix: '10.1.0.0/24'
//   networkSecurityGroup: { id: nsg.id }
//   routeTable: { id: rt.id } } }]
```

```bash
az deployment group what-if -g rg-lab-d12 -f hub-spoke.bicep -p myIp=<ip-nha-ban>
az deployment group create -g rg-lab-d12 -f hub-spoke.bicep -p myIp=<ip-nha-ban>
```

**Tự kiểm tra:** Trong ARM JSON, peering spoke1-to-hub cần `dependsOn` những gì? Trong Bicep có cần không?

## Ngày 13 — Ôn Networking

**Mục tiêu:** khóa domain có nhiều câu hotspot nhất.

**Các bước:**

1. Làm khoảng 50 câu practice phần networking.
2. Lab lại các câu sai; ưu tiên NSG, peering, private endpoint.
3. Viết 1 trang ghi nhớ: bảng so sánh LB/App Gateway/Front Door/Traffic Manager (Ngày 10), bảng Network Watcher (Ngày 11), các tên subnet bắt buộc.

**Tự kiểm tra nhanh:**

- [ ] Giải thích được thứ tự đánh giá NSG inbound và outbound
- [ ] Biết cách nối 2 spoke khi peering không bắc cầu
- [ ] Phân biệt service endpoint và private endpoint
- [ ] Nhớ kích thước tối thiểu của GatewaySubnet, AzureBastionSubnet
- [ ] Chọn đúng dịch vụ cân bằng tải theo tầng và phạm vi

## Ngày 14 — Storage account, redundancy, tier, lifecycle

**Mục tiêu:** chọn đúng redundancy theo yêu cầu; vận hành tier, lifecycle, versioning, soft delete.

**Các bước lab:**

1. Tạo storage account GPv2 LRS, đổi sang GRS.
2. Gán cho mình Storage Blob Data Contributor, upload blob bằng `--auth-mode login`.
3. Chuyển blob sang Archive, rồi rehydrate về Hot.
4. Bật versioning, change feed, soft delete blob và container; xóa một blob rồi khôi phục.
5. Tạo lifecycle rule chuyển tier theo số ngày.

```bash
RG=rg-lab-d14 && az group create -n $RG
SA=stkhoalab$RANDOM
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --access-tier Hot --min-tls-version TLS1_2 --allow-blob-public-access false
az storage account update -g $RG -n $SA --sku Standard_GRS

az role assignment create --assignee $(az ad signed-in-user show --query id -o tsv) \
  --role "Storage Blob Data Contributor" \
  --scope $(az storage account show -g $RG -n $SA --query id -o tsv)

az storage container create --account-name $SA -n data --auth-mode login
az storage blob upload --account-name $SA -c data -f ./a.txt -n logs/a.txt --auth-mode login
az storage blob set-tier --account-name $SA -c data -n logs/a.txt --tier Archive --auth-mode login
az storage blob set-tier --account-name $SA -c data -n logs/a.txt --tier Hot \
  --rehydrate-priority High --auth-mode login

az storage account blob-service-properties update -g $RG --account-name $SA \
  --enable-versioning true --enable-change-feed true \
  --enable-delete-retention true --delete-retention-days 7 \
  --enable-container-delete-retention true --container-delete-retention-days 7

az storage account management-policy create -g $RG --account-name $SA --policy @policy.json
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

- Archive là offline, phải rehydrate (Standard tối đa khoảng 15 giờ) mới đọc được.
- Lưu tối thiểu: Cool 30 ngày, Cold 90 ngày, Archive 180 ngày; xóa hoặc chuyển sớm vẫn tính phí.
- Object replication cần versioning ở cả hai account và change feed ở account nguồn.
- Premium account không có tier Hot/Cool/Archive.

**Tự kiểm tra:** Yêu cầu vẫn đọc được dữ liệu khi cả region chính sập và chịu được mất một zone, chọn redundancy nào?

## Ngày 15 — SAS, firewall, Azure Files, AzCopy

**Mục tiêu:** phát hành và thu hồi được cả 3 loại SAS; giới hạn truy cập bằng firewall; dùng Azure Files và AzCopy.

**Các bước lab:**

1. Tạo user delegation SAS, mở URL trên trình duyệt.
2. Tạo stored access policy, phát SAS theo policy, xóa policy để thấy SAS hết hiệu lực.
3. Tạo account SAS bằng key, rotate key để thấy SAS chết.
4. Bật firewall chỉ cho IP nhà bạn.
5. Tạo file share, mount thử (nếu ISP không chặn port 445).
6. Copy và sync thư mục bằng AzCopy.

```bash
az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry 2026-10-31T00:00Z --auth-mode login --as-user

KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage container policy create --account-name $SA -c data -n read-policy \
  --permissions rl --expiry 2026-10-31T00:00Z --account-key $KEY
az storage container generate-sas --account-name $SA -n data \
  --policy-name read-policy --account-key $KEY
az storage container policy delete --account-name $SA -c data -n read-policy --account-key $KEY

az storage account keys renew -g $RG -n $SA --key primary
az storage account revoke-delegation-keys -g $RG -n $SA

az storage account update -g $RG -n $SA --default-action Deny
az storage account network-rule add -g $RG --account-name $SA --ip-address <ip-nha-ban>

az storage share-rm create -g $RG --storage-account $SA -n share1 --quota 100

azcopy login
azcopy copy "./data" "https://$SA.blob.core.windows.net/data" --recursive
azcopy sync "./data" "https://$SA.blob.core.windows.net/data" --delete-destination=true
```

| Loại SAS | Ký bằng | Cách thu hồi |
| --- | --- | --- |
| Account SAS | Account key | Rotate key |
| Service SAS (ad-hoc) | Account key | Rotate key |
| Service SAS + stored access policy | Account key | Xóa hoặc sửa policy |
| User delegation SAS | Entra ID | Revoke delegation keys; hiệu lực tối đa 7 ngày |

**Điểm hay thi:**

- Mỗi container tối đa 5 stored access policy.
- Azure Files dùng SMB port 445; Azure File Sync đồng bộ file server on-prem lên share.
- AzCopy và Storage Explorer hỗ trợ cả Entra ID lẫn SAS.

**Tự kiểm tra:** Đã phát SAS cho đối tác, cần thu hồi ngay mà không ảnh hưởng các ứng dụng khác dùng account key, làm cách nào?

## Ngày 16 — Virtual Machine

**Mục tiêu:** vận hành VM: availability, resize, disk, extension, move.

**Các bước lab:**

1. Tạo VM B1s trong zone 1, không public IP.
2. Xem các size resize được, resize lên B2s.
3. Gắn data disk 32 GB, format và mount trong OS.
4. Cài nginx bằng Custom Script extension.
5. Tạo availability set 2 FD/5 UD; thử thêm VM có sẵn vào (sẽ thất bại).
6. Move VM sang resource group khác.

```bash
RG=rg-lab-d16 && az group create -n $RG
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --zone 1 \
  --public-ip-address "" --nsg ""

az vm list-vm-resize-options -g $RG -n vm1 -o table
az vm resize -g $RG -n vm1 --size Standard_B2s
az vm disk attach -g $RG --vm-name vm1 --name data1 --new --size-gb 32 \
  --sku StandardSSD_LRS
az vm extension set -g $RG --vm-name vm1 --name CustomScript \
  --publisher Microsoft.Azure.Extensions \
  --settings '{"commandToExecute":"apt-get -y install nginx"}'

az vm availability-set create -g $RG -n avset1 \
  --platform-fault-domain-count 2 --platform-update-domain-count 5
az vm deallocate -g $RG -n vm1
```

**Điểm hay thi:**

- Availability set: tối đa 3 FD, 20 UD, SLA 99,95%. Availability zone: SLA 99,99%.
- Không thêm VM có sẵn vào availability set; phải tạo lại.
- Resize sang size không có trên cluster hiện tại phải deallocate. Stop trong OS vẫn tính phí compute.
- Move VM sang region khác: Azure Resource Mover hoặc Site Recovery.
- Ổ temporary mất dữ liệu khi deallocate/redeploy.

**Tự kiểm tra:** VM cần SLA 99,99% thì triển khai thế nào? Muốn ngừng tính phí compute mà giữ disk thì làm gì?

## Ngày 17 — VMSS và ARM/Bicep

**Mục tiêu:** cấu hình autoscale; đọc, sửa và deploy được ARM/Bicep template.

**Các bước lab:**

1. Tạo VMSS 2 instance, autoscale 1–4 theo CPU.
2. Export template từ resource group có sẵn, decompile sang Bicep.
3. Deploy `main.bicep` bên dưới bằng mode Incremental, sau đó thêm một resource thủ công rồi deploy mode Complete để thấy resource đó bị xóa.

```bash
RG=rg-lab-d17 && az group create -n $RG
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

`main.bicep`:

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
az deployment group create -g $RG -f main.bicep -p prefix=lab --mode Complete
az group export -n $RG > exported.json
az bicep decompile --file exported.json
```

**Điểm hay thi:**

- Incremental (mặc định) giữ resource không có trong template; Complete xóa chúng.
- ARM JSON: `$schema`, `contentVersion`, `parameters`, `variables`, `resources`, `outputs`. Hotspot hay hỏi `dependsOn`, `copy`, `[resourceGroup().location]`, `[parameters('x')]`.
- Bicep tự suy ra phụ thuộc khi tham chiếu property của resource khác.
- Autoscale cần cả rule scale out lẫn scale in; nếu chỉ có scale out thì số instance không giảm.

**Tự kiểm tra:** Deploy template mode Complete vào resource group có 3 resource, template chỉ khai báo 2. Kết quả là gì?

## Ngày 18 — App Service

**Mục tiêu:** nắm tier nào có tính năng gì; làm slot swap và scale. Phần bạn quen nhất, dành thời gian dư để ôn Networking.

**Các bước lab:**

1. Tạo plan S1 Linux và web app .NET 8; deploy một API mẫu.
2. Tạo slot `staging`, đặt một app setting dạng slot setting, swap và kiểm tra setting nào đi theo.
3. Scale out lên 2 instance, scale up lên P1V3 rồi về lại S1.
4. Cấu hình backup.

```bash
RG=rg-lab-d18 && az group create -n $RG
az appservice plan create -g $RG -n plan1 --sku S1 --is-linux
az webapp create -g $RG -p plan1 -n khoalab-api --runtime "DOTNETCORE:8.0"
az webapp deployment slot create -g $RG -n khoalab-api --slot staging
az webapp config appsettings set -g $RG -n khoalab-api --slot staging \
  --slot-settings ENV=staging
az webapp deployment slot swap -g $RG -n khoalab-api --slot staging --target-slot production
az appservice plan update -g $RG -n plan1 --number-of-workers 2
az appservice plan update -g $RG -n plan1 --sku P1V3
```

| Tier | Custom domain | Slot | Autoscale |
| --- | --- | --- | --- |
| Free / Shared | Shared có | Không | Không |
| Basic | Có | Không | Không (scale thủ công) |
| Standard | Có | 5 | Có |
| Premium | Có | 20 | Có |

**Điểm hay thi:**

- App trong cùng plan dùng chung tài nguyên; scale là scale cả plan.
- App setting đánh dấu deployment slot setting ở lại slot khi swap.
- Cần slot hoặc autoscale mà đang Basic → lên Standard (đáp án minimize cost).

**Tự kiểm tra:** Connection string của staging trỏ DB test, sau khi swap production có bị trỏ nhầm DB test không? Phụ thuộc vào cấu hình nào?

## Ngày 19 — ACR, ACI, Container Apps

**Mục tiêu:** build image lên ACR và chạy bằng ACI và Container Apps. Dùng chính một API .NET của bạn cho thực tế.

**Các bước lab:**

1. Tạo ACR Basic, build image từ Dockerfile bằng `az acr build`.
2. Chạy image bằng ACI với restart policy OnFailure.
3. Deploy cùng image lên Container Apps có ingress external; quan sát scale to zero.

```bash
RG=rg-lab-d19 && az group create -n $RG
az acr create -g $RG -n khoalabacr --sku Basic --admin-enabled true
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
- ACI restart policy: Always (mặc định), OnFailure, Never. Container group chạy chung host, chung IP.
- Container Apps scale theo HTTP/event, có scale to zero; ACI không tự scale.

**Tự kiểm tra:** Job batch chạy một lần rồi dừng, không muốn bị khởi động lại khi thành công, dùng restart policy nào?

## Ngày 20 — Alert, Log Analytics, KQL

**Mục tiêu:** tạo alert có action group; đẩy log về Log Analytics; viết KQL cơ bản.

**Các bước lab:**

1. Tạo VM B1s và action group gửi email.
2. Tạo metric alert CPU > 80%; chạy `stress` trong VM để alert bắn.
3. Tạo Log Analytics workspace, bật VM insights (Azure Monitor Agent + DCR) và diagnostic settings cho một resource.
4. Chạy 4 câu KQL bên dưới.

```bash
RG=rg-lab-d20 && az group create -n $RG
az monitor action-group create -g $RG -n ag-mail --short-name agmail \
  --action email khoa ban@example.com

VM_ID=$(az vm show -g $RG -n vm1 --query id -o tsv)
az monitor metrics alert create -g $RG -n cpu-high --scopes $VM_ID \
  --condition "avg Percentage CPU > 80" --window-size 5m \
  --evaluation-frequency 1m --action ag-mail

az monitor log-analytics workspace create -g $RG -n law-lab
LAW_ID=$(az monitor log-analytics workspace show -g $RG -n law-lab --query id -o tsv)
az monitor diagnostic-settings create --name to-law --resource <resource-id> \
  --workspace $LAW_ID --metrics '[{"category":"AllMetrics","enabled":true}]'
```

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

- Alert gồm scope, condition, action group (email, SMS, webhook, Logic App, Function, runbook).
- Activity log giữ 90 ngày; lâu hơn phải đẩy qua diagnostic settings.
- KQL: `where` trước, rồi `summarize`/`project`, cuối cùng `render`.

**Tự kiểm tra:** Cần biết ai đã xóa một VM tuần trước, xem ở đâu? Cần lưu thông tin đó 1 năm thì cấu hình gì?

## Ngày 21 — Backup và Site Recovery

**Mục tiêu:** backup và restore VM; biết vault nào backup được gì; hiểu Site Recovery ở mức khái niệm.

**Các bước lab:**

1. Tạo Recovery Services vault cùng region với VM, **tắt soft delete trước** để cuối ngày xóa được resource group.
2. Bật backup VM với DefaultPolicy, chạy backup ngay.
3. Restore một file bằng File Recovery, sau đó restore thành VM mới.
4. Đọc tài liệu Site Recovery: replicate, test failover, failover, failback.

```bash
RG=rg-lab-d21 && az group create -n $RG
az backup vault create -g $RG -n rsv-lab -l southeastasia
az backup vault backup-properties set -g $RG -n rsv-lab --soft-delete-feature-state Disable

az backup protection enable-for-vm -g $RG --vault-name rsv-lab --vm vm1 \
  --policy-name DefaultPolicy
az backup protection backup-now -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --retain-until 10-11-2026
az backup recoverypoint list -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM -o table

# Dọn: dừng bảo vệ và xóa dữ liệu backup trước khi xóa resource group
az backup protection disable -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --delete-backup-data true --yes
```

| Vault | Backup được |
| --- | --- |
| Recovery Services vault | Azure VM, SQL/SAP HANA trong VM, Azure Files, máy on-prem qua MARS agent |
| Backup vault | Azure Disks, Azure Blobs, Azure Database for PostgreSQL, AKS |

**Điểm hay thi:**

- Vault phải cùng region với VM.
- Restore VM: tạo VM mới, restore disk, hoặc replace existing.
- Xóa vault: dừng protection, xóa backup data, chờ hết soft delete 14 ngày (hoặc đã tắt trước).
- Site Recovery dùng cho DR (failover sang region khác); Backup dùng cho khôi phục dữ liệu theo thời điểm.

**Tự kiểm tra:** Cần chuyển toàn bộ VM sang region khác khi region chính sập trong vài phút, chọn Backup hay Site Recovery?

## Ngày 22 — Mock exam 1

**Mục tiêu:** làm thử trong điều kiện giống thi thật và tìm ra domain yếu.

- [ ] Làm full mock exam, bấm giờ 120 phút, có ít nhất một case study
- [ ] Tập mở Microsoft Learn trong lúc làm, chỉ để tra con số hoặc giới hạn
- [ ] Ghi câu sai theo domain, và lý do sai: không biết, hay đọc sót ràng buộc
- [ ] Chọn 2–3 chủ đề sai nhiều nhất cho ngày 23

## Ngày 23 — Lab lại và mock exam 2

**Mục tiêu:** sửa điểm yếu bằng tay, không chỉ đọc lại.

- [ ] Lab lại 2–3 chủ đề yếu, dùng đúng lệnh trong ngày tương ứng của guide
- [ ] Mock exam 2, mục tiêu trên 80%
- [ ] So sánh với mock 1: domain nào đã cải thiện, domain nào chưa

## Ngày 24 — Ôn tổng hợp và dọn subscription

**Mục tiêu:** không lab nữa; đọc lại bảng tổng hợp, dọn sạch subscription, nghỉ sớm.

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
| truy cập từ on-premises bằng IP private | Private endpoint |
| VM không public IP mà vẫn RDP/SSH | Azure Bastion |
| ngăn xóa nhầm, kể cả Owner | Resource lock CanNotDelete |
| bắt buộc tag/region cho resource mới | Azure Policy (deny hoặc modify) |
| tự động thêm user vào group theo thuộc tính | Dynamic group (cần Entra ID P1) |

### Checklist

- [ ] Đọc lại các bảng: role (Ngày 3), effect (Ngày 4), Entra ID Free (Ngày 2), cân bằng tải (Ngày 10), SAS (Ngày 15), App Service tier (Ngày 18), vault (Ngày 21)
- [ ] Chạy `az resource list -o table`, xóa mọi resource còn sót
- [ ] Kiểm tra lịch thi, giấy tờ tùy thân, phòng thi hoặc máy thi online
- [ ] Trong phòng thi: đọc dòng "You need to…" trước, gạch chân ràng buộc; case study đọc câu hỏi rồi mới mở tài liệu
