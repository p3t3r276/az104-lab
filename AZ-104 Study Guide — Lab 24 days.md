# AZ-104 Study Guide — Lab 24 ngày

Oct 3, 2026 · @Khoa

## Tổng quan và lịch 24 ngày

Mục tiêu: qua AZ-104 (700/1000) sau 24 ngày lab, khoảng 2–2,5 giờ mỗi tối, tổng chi phí dưới 200 USD credit. Networking (7 ngày) và Identity (5 ngày) chiếm khoảng một nửa thời lượng.

Cách dùng guide: mỗi ngày gồm mục tiêu, các bước lab, lệnh CLI/JSON/Bicep/KQL, câu tự kiểm tra và mục **Điểm hay thi**. Mỗi lab làm cả trên portal lẫn CLI, vì câu hotspot thường bắt điền tham số lệnh hoặc template. Quy tắc thiết kế: ngày N chỉ dùng kiến thức của các ngày trước nó (mục Dùng lại ghi rõ), và mỗi ngày tự đủ lệnh từ tạo resource group đến dọn dẹp, nên có thể làm lại bất kỳ ngày nào mà không cần ngày trước còn resource.

**Format đề:** khoảng 40–60 câu, 120 phút, điểm quy đổi. Dạng câu gồm multiple choice, drag-and-drop, hotspot, chuỗi Yes/No (không quay lại được) và 1–2 case study. Không có lab thực hành. Được mở Microsoft Learn trong giờ thi, nhưng dùng chung đồng hồ.

| Ngày | Domain | Lab chính | Dùng lại từ |
| --- | --- | --- | --- |
| 1 | Setup | CLI, resource group, budget, dọn dẹp, 2 user test | — |
| 2 | Identity | Users, guest, bulk CSV, group; P1/P2 học lý thuyết | 1 |
| 3 | Identity | RBAC 4 scope, custom role JSON | 1, 2 |
| 4 | Compute | VM cơ bản: tạo, SSH, resize, disk, extension, run-command; thử custom role | 1, 3 |
| 5 | Networking | VNet, subnet, NSG, ASG, IP flow verify | 4 |
| 6 | Networking | Peering, UDR, NVA, next hop | 4, 5 |
| 7 | Storage | Storage account, redundancy, blob, data-plane RBAC, tier, lifecycle | 3 |
| 8 | Storage | SAS, stored access policy, Azure Files, AzCopy, firewall | 4, 7 |
| 9 | Networking | Azure DNS, private DNS, service endpoint, private endpoint | 4, 5, 7, 8 |
| 10 | Networking | Standard Load Balancer, Application Gateway | 4, 5 |
| 11 | Networking | VPN Gateway P2S, Bastion, Network Watcher, flow logs | 4, 5, 7 |
| 12 | Networking | ARM/Bicep cơ bản, viết hub-spoke bằng Bicep | 5, 6, 7 |
| 13 | Ôn | Ôn Networking + Storage, bài tự dựng 45 phút | 4–12 |
| 14 | Governance | Azure Policy, managed identity, remediation, initiative | 3, 5, 7 |
| 15 | Governance | Lock, move, tags, management group, cost | 4, 5, 7, 14 |
| 16 | Ôn | Ôn Identity & Governance, bài tự dựng 45 phút | 2, 3, 14, 15 |
| 17 | Compute | Availability set/zone, VMSS, autoscale | 4, 5, 10 |
| 18 | Compute | App Service, slot, scale, backup | 7, 8, 17 |
| 19 | Compute | ACR, ACI, Container Apps | 18 |
| 20 | Monitor | Log Analytics, AMA, DCR, alert, KQL | 4, 14 |
| 21 | Monitor | Backup, restore, Site Recovery | 4, 7 |
| 22 | Nước rút | Mock exam 1 (120 phút) | tất cả |
| 23 | Nước rút | Lab lại chủ đề yếu + mock exam 2 | tất cả |
| 24 | Nước rút | Ôn bảng tổng hợp, dọn subscription | tất cả |

## Ngày 1 — Setup, resource group, chi phí

**Mục tiêu:** CLI trỏ đúng subscription, hiểu *resource group*, có budget alert, có script dọn dẹp, có 2 user test cho các ngày sau.

**Dùng lại:** không.

**Các bước lab:**

1. Đăng nhập, kiểm tra đúng tenant và subscription.
2. Đặt location mặc định, cài Bicep, tạo SSH key.
3. Tạo, xem, gắn tag, xóa *resource group*.
4. Tạo budget 200 USD kèm alert 50/80/100% và alert dự báo.
5. Tạo 2 user test `lab-reader`, `lab-ops`.
6. Tạo script dọn dẹp dùng cho mọi ngày.

```bash
# Step 1: Sign in and check the active tenant/subscription
az login
az account show --query "{sub:name, id:id, tenant:tenantId, user:user.name}" -o table

# Step 1: List every subscription you can see
az account list --query "[].{sub:name, id:id, isDefault:isDefault, tenant:tenantId}" -o table

# Step 1: Switch tenant/subscription if wrong
az login --tenant <tenant-id>
az account set --subscription "<subscription-id>"

# Step 2: Set default location (no experimental warning like az config)
az configure --defaults location=southeastasia

# Step 2: Install Bicep CLI
az bicep install

# Step 2: Create an SSH key pair for all VM labs (skip if ~/.ssh/id_rsa exists)
ssh-keygen -t rsa -b 4096 -f ~/.ssh/id_rsa -N ""

# Step 3: Create a resource group with a tag
RG=rg-lab-d01
az group create -n $RG -l southeastasia --tags env=lab

# Step 3: List and inspect resource groups
az group list -o table
az group show -n $RG

# Step 3: Replace tags on the resource group
az group update -n $RG --tags env=lab owner=khoa

# Step 3: Delete the resource group (deletes everything inside)
az group delete -n $RG --yes
```

File `budget.json` (thay email):

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
# Step 4: Create the budget via REST (az consumption budget create uses an old API)
SUB=$(az account show --query id -o tsv)
az rest --method put \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets/lab-200?api-version=2023-05-01" \
  --body @budget.json

# Step 4: Verify the budget
az rest --method get \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets?api-version=2023-05-01" \
  --query "value[].{name:name, amount:properties.amount}" -o table

# Step 4: If RBACAccessDenied - check your role on the subscription
az role assignment list --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope /subscriptions/$SUB --include-inherited -o table

# Step 4: If still failing - register providers, then retry (or use portal: Cost Management > Budgets)
az provider register --namespace Microsoft.Consumption
az provider register --namespace Microsoft.CostManagement

# Step 5: Get the tenant's default domain
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)

# Step 5: Create the two test users
az ad user create --display-name "Lab Reader" --user-principal-name lab-reader@$DOMAIN \
  --password '<mat-khau-tam>' --force-change-password-next-sign-in true
az ad user create --display-name "Lab Ops" --user-principal-name lab-ops@$DOMAIN \
  --password '<mat-khau-tam>' --force-change-password-next-sign-in true

# Step 6: Save a cleanup script for every lab day
cat > ~/cleanup.sh << 'EOF'
#!/bin/bash
az group list --query "[?starts_with(name,'rg-lab')].name" -o tsv \
  | xargs -I {} az group delete -n {} --yes --no-wait
az resource list --query "[].{name:name, type:type, rg:resourceGroup}" -o table
EOF
chmod +x ~/cleanup.sh

# Step 6: Run it at the end of every day
~/cleanup.sh
```

**Điểm hay thi:**

- Resource group chỉ là container logic; region của RG chỉ lưu metadata, resource bên trong có thể ở region khác.
- Mỗi resource thuộc đúng một RG; RG không lồng nhau; xóa RG là xóa toàn bộ resource bên trong.
- Budget chỉ gửi cảnh báo, không tự dừng resource. Role hẹp nhất để quản lý budget: **Cost Management Contributor**.
- `NetworkWatcherRG` do Azure tự tạo khi bạn tạo VNet; để nguyên, không tốn phí.

**Tự kiểm tra:** Alert `Forecasted` khác `Actual` thế nào? Storage account ở East US có nằm trong RG đặt ở Southeast Asia được không?

## Ngày 2 — Users, groups, guest

**Mục tiêu:** làm mọi thao tác user/group mà Entra ID Free cho phép bằng CLI và Microsoft Graph; học lý thuyết các tính năng cần P1/P2.

**Dùng lại:** Ngày 1 (đăng nhập, biến `DOMAIN`, `lab-reader`).

**Các bước lab:**

1. Tạo user, sửa thuộc tính `department`, `jobTitle`, `usageLocation` qua *Microsoft Graph*.
2. Bulk create 5 user từ file CSV.
3. Mời 1 *guest user* (B2B) bằng email cá nhân.
4. Tạo *security group* và *Microsoft 365 group* dạng Assigned; thêm member và owner.
5. Bulk add member theo phòng ban.
6. Xóa một user rồi khôi phục.
7. Đọc phần dynamic group và tự viết 5 rule ra giấy.

```bash
# Prep: tenant default domain
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)

# Step 1: Update user attributes (az ad user update has no --department)
az rest --method PATCH \
  --url "https://graph.microsoft.com/v1.0/users/lab-reader@$DOMAIN" \
  --headers "Content-Type=application/json" \
  --body '{"department":"IT","jobTitle":"Engineer","usageLocation":"VN"}'

# Step 1: Read user attributes
az rest --method GET \
  --url "https://graph.microsoft.com/v1.0/users/lab-reader@$DOMAIN?\$select=displayName,department,jobTitle,usageLocation"

# Step 1: List all users that have a specific attribute
az ad user list --filter "department eq 'IT'" --query "[].userPrincipalName" -o tsv

# Step 2: Prepare the CSV
cat > users.csv << 'EOF'
displayName,alias,department
Lab User A,lab-a,IT
Lab User B,lab-b,IT
Lab User C,lab-c,HR
Lab User D,lab-d,HR
Lab User E,lab-e,Sales
EOF

# Step 2: Bulk create users and set department
tail -n +2 users.csv | while IFS=, read -r NAME ALIAS DEPT; do
  az ad user create --display-name "$NAME" --user-principal-name "$ALIAS@$DOMAIN" \
    --password '<mat-khau-tam>' --force-change-password-next-sign-in true
  sleep 5
  az rest --method PATCH --url "https://graph.microsoft.com/v1.0/users/$ALIAS@$DOMAIN" \
    --headers "Content-Type=application/json" --body "{\"department\":\"$DEPT\"}"
done

# Step 3: Invite a guest user (B2B)
az rest --method POST --url "https://graph.microsoft.com/v1.0/invitations" \
  --headers "Content-Type=application/json" \
  --body '{"invitedUserEmailAddress":"<email-ca-nhan>","inviteRedirectUrl":"https://portal.azure.com","sendInvitationMessage":true}'

# Step 3: List guest users
az ad user list --filter "userType eq 'Guest'" \
  --query "[].{name:displayName, upn:userPrincipalName}" -o table

# Step 4: Create a security group
az ad group create --display-name IT-Team --mail-nickname itteam

# Step 4: Add a member and an owner
az ad group member add --group IT-Team \
  --member-id $(az ad user show --id lab-reader@$DOMAIN --query id -o tsv)
az ad group owner add --group IT-Team \
  --owner-object-id $(az ad user show --id lab-ops@$DOMAIN --query id -o tsv)

# Step 4: Create a Microsoft 365 group (only possible via Graph)
az rest --method POST --url "https://graph.microsoft.com/v1.0/groups" \
  --headers "Content-Type=application/json" \
  --body '{"displayName":"IT-M365","mailNickname":"itm365","mailEnabled":true,"securityEnabled":false,"groupTypes":["Unified"]}'

# Step 5: Bulk add every IT user to IT-Team (the Free-tier stand-in for a dynamic group)
for ID in $(az ad user list --filter "department eq 'IT'" --query "[].id" -o tsv); do
  az ad group member add --group IT-Team --member-id $ID
done

# Step 5: Verify membership
az ad group member list --group IT-Team --query "[].displayName" -o tsv

# Step 6: Delete a user
az ad user delete --id lab-e@$DOMAIN

# Step 6: Find it in deleted items
DELETED_ID=$(az rest --method GET \
  --url "https://graph.microsoft.com/v1.0/directory/deletedItems/microsoft.graph.user" \
  --query "value[?contains(userPrincipalName,'lab-e')].id" -o tsv)

# Step 6: Restore it (possible within 30 days)
az rest --method POST \
  --url "https://graph.microsoft.com/v1.0/directory/deletedItems/$DELETED_ID/restore"

# Cleanup: remove lab-a..lab-e and the groups (keep lab-reader, lab-ops)
for A in lab-a lab-b lab-c lab-d lab-e; do az ad user delete --id $A@$DOMAIN; done
az ad group delete --group IT-Team
az ad group delete --group IT-M365
```

### Giới hạn của Entra ID Free

Tenant của tài khoản Azure free chỉ có Entra ID Free. Các tính năng dưới đây không tạo được, nhưng vẫn xuất hiện trong đề.

| Tính năng | License cần | Trên Free làm gì thay |
| --- | --- | --- |
| Dynamic group (user hoặc device) | P1 | Assigned group + script add member theo thuộc tính (bước 5); học cú pháp rule |
| Gán license theo group | P1 | Học lý thuyết: lỗi thường gặp là thiếu `usageLocation` |
| SSPR cho user thường | P1 | Admin vẫn tự reset được; học số phương thức xác thực, registration, writeback |
| Conditional Access | P1 | Security defaults (bật MFA cơ bản, miễn phí) |
| Custom Entra role | P1 | Dùng built-in Entra role; custom **Azure RBAC** role vẫn miễn phí (Ngày 3) |
| Gán admin cho administrative unit, dynamic AU | P1 | Tạo AU thường, học lý thuyết phần gán quyền |
| PIM, Access reviews, Identity Protection | P2 | Học lý thuyết |

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
- Guest (B2B) đăng nhập bằng tài khoản của họ; có thể chặn mời guest trong External collaboration settings.
- User bị xóa khôi phục được trong 30 ngày.

**Tự kiểm tra:** Muốn tự động đưa mọi user phòng IT vào group thì cần license gì? Trên Free thì làm cách nào gần nhất?

## Ngày 3 — RBAC và custom role

**Mục tiêu:** chọn đúng role ở đúng scope; tự kiểm chứng quyền bằng cách đăng nhập như user khác; viết custom role JSON. Chạy được hoàn toàn trên Entra ID Free.

**Dùng lại:** Ngày 1 (resource group, tag), Ngày 2 (`lab-reader`, `lab-ops`).

**Các bước lab:**

1. Tạo 2 resource group, gán *Reader* cho `lab-reader` ở `rg-lab-d03a`.
2. Đăng nhập bằng `lab-reader` trong một CLI profile riêng; chỉ thấy `rg-lab-d03a`, sửa tag bị chặn.
3. Gán *Contributor* ở `rg-lab-d03a`: sửa tag được, nhưng gán quyền cho người khác vẫn bị chặn.
4. Gán *Reader* ở scope subscription, so sánh phạm vi nhìn thấy, rồi gỡ.
5. Đọc định nghĩa role: `Actions` và `DataActions`.
6. Tạo custom role *VM Operator* (thử quyền ở Ngày 4 khi đã có VM).

```bash
# Prep: variables
SUB=$(az account show --query id -o tsv)
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)

# Step 1: Create two resource groups
az group create -n rg-lab-d03a -l southeastasia
az group create -n rg-lab-d03b -l southeastasia

# Step 1: Assign Reader at resource group scope
az role assignment create --assignee lab-reader@$DOMAIN --role "Reader" \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03a

# Step 2 (NEW terminal): sign in as lab-reader in a separate CLI profile
export AZURE_CONFIG_DIR=$HOME/.azure-labreader
az login

# Step 2 (lab-reader): only rg-lab-d03a is visible
az group list -o table

# Step 2 (lab-reader): try to change a tag - expect AuthorizationFailed
az group update -n rg-lab-d03a --tags owner=reader

# Step 3 (admin terminal): grant Contributor on the same resource group
az role assignment create --assignee lab-reader@$DOMAIN --role "Contributor" \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03a

# Step 3 (lab-reader, wait 1-2 min): tag update now succeeds
az group update -n rg-lab-d03a --tags owner=reader

# Step 3 (lab-reader): Contributor cannot grant access - expect AuthorizationFailed
az role assignment create --assignee lab-ops@$DOMAIN --role "Reader" \
  --scope /subscriptions/$SUB/resourceGroups/rg-lab-d03a

# Step 4 (admin): grant Reader at subscription scope
az role assignment create --assignee lab-reader@$DOMAIN --role "Reader" \
  --scope /subscriptions/$SUB

# Step 4 (lab-reader): now both resource groups are visible
az group list -o table

# Step 4 (admin): remove the subscription-level assignment
az role assignment delete --assignee lab-reader@$DOMAIN --role "Reader" \
  --scope /subscriptions/$SUB

# Step 4 (admin): list all assignments of lab-reader
az role assignment list --assignee lab-reader@$DOMAIN --all -o table

# Step 5: Control-plane permissions of a built-in role
az role definition list --name "Virtual Machine Contributor" \
  --query "[].permissions[].actions" -o json

# Step 5: Data-plane permissions of a built-in role
az role definition list --name "Storage Blob Data Reader" \
  --query "[].permissions[].dataActions" -o json
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
# Step 6: Put your subscription id into the JSON
sed -i.bak "s|<subscription-id>|$SUB|" vm-operator.json

# Step 6: Create the custom role
az role definition create --role-definition @vm-operator.json

# Step 6: Verify (keep this role - used on Day 4)
az role definition list --custom-role-only true --query "[].roleName" -o tsv

# Cleanup (lab-reader terminal): go back to your admin profile
unset AZURE_CONFIG_DIR

# Cleanup (admin): delete today's resource groups (their role assignments go too)
az group delete -n rg-lab-d03a --yes --no-wait
az group delete -n rg-lab-d03b --yes --no-wait
```

| Role | Quản lý resource | Gán quyền cho người khác |
| --- | --- | --- |
| Owner | Có | Có |
| Contributor | Có | Không |
| User Access Administrator | Không | Có |
| Reader | Chỉ xem | Không |

**Điểm hay thi:**

- Scope kế thừa từ trên xuống: management group → subscription → resource group → resource.
- `Actions` là control plane; `DataActions` là data plane. Owner không tự đọc được blob qua Entra ID (làm thật ở Ngày 7).
- Entra roles (Global Administrator, User Administrator) quản lý directory; Azure RBAC roles quản lý resource. Hai hệ tách biệt.
- Thay đổi role mất vài phút mới có hiệu lực; có lúc phải đăng nhập lại để lấy token mới.

**Tự kiểm tra:** Muốn một người gán quyền cho người khác nhưng không sửa được resource thì dùng role nào? Contributor ở subscription có tạo được role assignment không?

## Ngày 4 — VM cơ bản

**Mục tiêu:** tạo và vận hành VM Linux: SSH, *run-command*, size, data disk, extension, stop và deallocate; thử custom role từ Ngày 3.

**Dùng lại:** Ngày 1 (resource group, SSH key), Ngày 3 (custom role *VM Operator*, CLI profile riêng).

**Các bước lab:**

1. Tạo VM Ubuntu B1s với cấu hình mặc định; xem CLI đã tạo thêm những resource nào.
2. SSH vào VM; chạy lệnh từ xa bằng *run-command*.
3. Xem size khả dụng, resize lên B2s.
4. Gắn data disk 32 GB, format và mount trong OS.
5. Cài nginx bằng *Custom Script extension*, mở port 80, truy cập từ máy bạn.
6. So sánh *stop* và *deallocate*.
7. Gán *VM Operator* cho `lab-ops`; đăng nhập bằng `lab-ops` thử start/stop (được) và resize (bị chặn).

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d04
SUB=$(az account show --query id -o tsv)
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)
az group create -n $RG -l $LOC

# Step 1: Create a VM with defaults
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys

# Step 1: See what az vm create made (VNet, NSG, public IP, NIC, OS disk)
az resource list -g $RG --query "[].{name:name, type:type}" -o table

# Step 2: Get the public IP and SSH in (type exit to leave)
IP=$(az vm show -d -g $RG -n vm1 --query publicIps -o tsv)
ssh azureuser@$IP

# Step 2: Run a command inside the VM without SSH
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "hostname; lsblk; df -h"

# Step 3: List B-series sizes in the region
az vm list-sizes -l $LOC \
  --query "[?starts_with(name,'Standard_B')].{name:name, cpu:numberOfCores, memMB:memoryInMB}" -o table

# Step 3: Sizes this VM can resize to without deallocating
az vm list-vm-resize-options -g $RG -n vm1 -o table

# Step 3: Resize
az vm resize -g $RG -n vm1 --size Standard_B2s

# Step 4: Attach a new 32 GB data disk
az vm disk attach -g $RG --vm-name vm1 --name data1 --new --size-gb 32 \
  --sku StandardSSD_LRS

# Step 4: Find the new disk name (usually sdc)
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript --scripts "lsblk"

# Step 4: Partition, format and mount it at /data
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript --scripts \
  "sudo parted /dev/sdc --script mklabel gpt mkpart primary ext4 0% 100% && sleep 2 && sudo mkfs.ext4 -F /dev/sdc1 && sudo mkdir -p /data && sudo mount /dev/sdc1 /data && df -h /data"

# Step 5: Install nginx with the Custom Script extension
az vm extension set -g $RG --vm-name vm1 --name CustomScript \
  --publisher Microsoft.Azure.Extensions \
  --settings '{"commandToExecute":"apt-get -y update && apt-get -y install nginx"}'

# Step 5: Open port 80 on the VM's NSG
az vm open-port -g $RG -n vm1 --port 80 --priority 1010

# Step 5: Test from your machine
curl http://$IP

# Step 6: Stop (OS off, compute still billed)
az vm stop -g $RG -n vm1
az vm get-instance-view -g $RG -n vm1 --query "instanceView.statuses[1].displayStatus" -o tsv

# Step 6: Deallocate (compute billing stops, disks kept)
az vm deallocate -g $RG -n vm1
az vm get-instance-view -g $RG -n vm1 --query "instanceView.statuses[1].displayStatus" -o tsv

# Step 7: Assign the Day 3 custom role to lab-ops on this resource group
az role assignment create --assignee lab-ops@$DOMAIN --role "VM Operator" \
  --scope /subscriptions/$SUB/resourceGroups/$RG

# Step 7 (NEW terminal): sign in as lab-ops
export AZURE_CONFIG_DIR=$HOME/.azure-labops
az login

# Step 7 (lab-ops): start works
az vm start -g rg-lab-d04 -n vm1

# Step 7 (lab-ops): resize is not in the role - expect AuthorizationFailed
az vm resize -g rg-lab-d04 -n vm1 --size Standard_B1s

# Cleanup (lab-ops terminal)
unset AZURE_CONFIG_DIR

# Cleanup (admin)
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- `az vm create` mặc định tạo kèm VNet, subnet, NSG (mở SSH 22), public IP, NIC, OS disk. Ngày 5 sẽ tự dựng từng phần.
- Stop trong OS hoặc `az vm stop` vẫn tính phí compute; chỉ *deallocate* mới dừng tính phí compute (disk vẫn tính).
- Resize sang size không có trên cluster hiện tại phải deallocate trước.
- Ổ temporary (`/dev/sdb` trên Linux, D: trên Windows) mất dữ liệu khi deallocate/redeploy; không dùng cho dữ liệu lâu dài.
- Run-command và Custom Script extension đều chạy lệnh trong VM mà không cần SSH/RDP.

**Tự kiểm tra:** Role hẹp nhất cho phép start/stop VM là gì? Muốn ngừng tính phí compute mà giữ disk thì làm gì?

## Ngày 5 — VNet, NSG, ASG

**Mục tiêu:** tự dựng phần mạng mà `az vm create` đã làm hộ ở Ngày 4; hiểu thứ tự đánh giá NSG ở subnet và NIC; viết rule theo ASG.

**Dùng lại:** Ngày 4 (tạo VM, Custom Script extension, run-command).

**Các bước lab:**

1. Tạo `vnet-hub` (10.0.0.0/16) với subnet `snet-web`, `snet-app`.
2. Tạo NSG `nsg-web` chỉ cho SSH từ IP nhà bạn, gắn vào `snet-web`.
3. Tạo `vm1` trong `snet-web`, có public IP, **không** có NSG ở NIC; SSH thử.
4. Cài nginx; mở port 80 ở NSG subnet, gắn thêm NSG ở NIC không có rule 80 → curl thất bại; thêm rule ở NIC → curl được.
5. Tạo *ASG* `asg-web`, gắn NIC vào, viết rule HTTPS theo ASG.
6. Xem effective rules; dùng *IP flow verify* với một IP được phép và một IP bị chặn.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d05
MYIP=$(curl -s https://api.ipify.org)
az group create -n $RG -l $LOC

# Step 1: Create the VNet with the first subnet
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-web --subnet-prefixes 10.0.1.0/24

# Step 1: Add a second subnet
az network vnet subnet create -g $RG --vnet-name vnet-hub -n snet-app \
  --address-prefixes 10.0.2.0/24

# Step 2: Create the subnet NSG
az network nsg create -g $RG -n nsg-web

# Step 2: Allow SSH only from your IP
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-ssh-myip \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 22 --source-address-prefixes $MYIP

# Step 2: Attach the NSG to snet-web
az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-web \
  --network-security-group nsg-web

# Step 3: Create vm1 in snet-web, no NIC-level NSG
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-hub --subnet snet-web --nsg ""

# Step 3: SSH works because the subnet NSG allows your IP
IP=$(az vm show -d -g $RG -n vm1 --query publicIps -o tsv)
ssh azureuser@$IP exit

# Step 4: Install nginx
az vm extension set -g $RG --vm-name vm1 --name CustomScript \
  --publisher Microsoft.Azure.Extensions \
  --settings '{"commandToExecute":"apt-get -y update && apt-get -y install nginx"}'

# Step 4: Allow HTTP on the subnet NSG
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-http \
  --priority 200 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 --source-address-prefixes Internet
curl -m 5 http://$IP

# Step 4: Add a NIC-level NSG that has no HTTP rule
az network nsg create -g $RG -n nsg-nic
az network nic update -g $RG -n vm1VMNic --network-security-group nsg-nic

# Step 4: curl now times out - both NSGs must allow
curl -m 5 http://$IP

# Step 4: Allow HTTP on the NIC NSG too, then curl works again
az network nsg rule create -g $RG --nsg-name nsg-nic -n allow-http \
  --priority 200 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 --source-address-prefixes Internet
curl -m 5 http://$IP

# Step 5: Create an ASG and put vm1's NIC in it
az network asg create -g $RG -n asg-web
az network nic ip-config update -g $RG --nic-name vm1VMNic -n ipconfigvm1 \
  --application-security-groups asg-web

# Step 5: Rule that targets the ASG instead of IP addresses
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-https-asg \
  --priority 300 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 443 --destination-asgs asg-web

# Step 6: Effective rules on the NIC (subnet + NIC NSGs combined)
az network nic list-effective-nsg -g $RG -n vm1VMNic

# Step 6: Make sure Network Watcher is enabled in the region
az network watcher configure -g NetworkWatcherRG -l $LOC --enabled true

# Step 6: IP flow verify - your IP on port 22 (Allow)
PRIV=$(az vm show -d -g $RG -n vm1 --query privateIps -o tsv)
az network watcher test-ip-flow -g $RG --vm vm1 --direction Inbound \
  --protocol TCP --local $PRIV:22 --remote $MYIP:50000

# Step 6: IP flow verify - another IP on port 22 (Deny, shows which rule)
az network watcher test-ip-flow -g $RG --vm vm1 --direction Inbound \
  --protocol TCP --local $PRIV:22 --remote 1.2.3.4:50000

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- Azure giữ lại 5 IP mỗi subnet (4 đầu + 1 cuối); /29 chỉ còn 3 IP dùng được.
- Priority 100–4096, số nhỏ xét trước; khớp rule đầu tiên là dừng.
- Inbound: NSG subnet xét trước, rồi NSG NIC. Outbound: ngược lại. Cả hai phải Allow.
- Rule mặc định (65000+): AllowVNetInBound, AllowAzureLoadBalancerInBound, DenyAllInBound; không xóa được.
- ASG chỉ gồm NIC trong cùng VNet; dùng để viết rule theo vai trò server thay vì IP.

**Tự kiểm tra:** NSG subnet cho phép port 80 nhưng NSG NIC không có rule nào cho port 80, traffic có vào được không? Vì sao?

## Ngày 6 — Peering, UDR, NVA

**Mục tiêu:** chứng minh *peering* không bắc cầu, rồi dùng *UDR* qua một VM làm *NVA* để nối 2 spoke.

**Dùng lại:** Ngày 4 (tạo VM, run-command), Ngày 5 (VNet, subnet).

**Các bước lab:**

1. Tạo `vnet-hub` (10.0.0.0/16, subnet `snet-nva`), `vnet-spoke1` (10.1.0.0/16), `vnet-spoke2` (10.2.0.0/16).
2. Tạo VM không public IP: `vm-spoke1`, `vm-spoke2`, và `vm-nva` với IP cố định 10.0.3.4.
3. Peer hub với từng spoke (mỗi cặp 2 chiều).
4. Ping từ spoke1 sang NVA (được) và sang spoke2 (thất bại).
5. Bật IP forwarding ở NIC của NVA và trong OS.
6. Tạo route table cho 2 spoke, next hop về NVA; ping lại.
7. Kiểm tra bằng *Next hop* và effective routes.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d06
az group create -n $RG -l $LOC

# Step 1: Create the three VNets
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-nva --subnet-prefixes 10.0.3.0/24
az network vnet create -g $RG -n vnet-spoke1 --address-prefix 10.1.0.0/16 \
  --subnet-name default --subnet-prefixes 10.1.0.0/24
az network vnet create -g $RG -n vnet-spoke2 --address-prefix 10.2.0.0/16 \
  --subnet-name default --subnet-prefixes 10.2.0.0/24

# Step 2: Create the NVA with a fixed private IP and no public IP
az vm create -g $RG -n vm-nva --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-hub --subnet snet-nva --private-ip-address 10.0.3.4 \
  --public-ip-address ""

# Step 2: Create one VM per spoke, no public IP
az vm create -g $RG -n vm-spoke1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-spoke1 --subnet default --public-ip-address ""
az vm create -g $RG -n vm-spoke2 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-spoke2 --subnet default --public-ip-address ""

# Step 3: Peer hub <-> spoke1 (both directions)
az network vnet peering create -g $RG -n hub-to-spoke1 --vnet-name vnet-hub \
  --remote-vnet vnet-spoke1 --allow-vnet-access --allow-forwarded-traffic
az network vnet peering create -g $RG -n spoke1-to-hub --vnet-name vnet-spoke1 \
  --remote-vnet vnet-hub --allow-vnet-access --allow-forwarded-traffic

# Step 3: Peer hub <-> spoke2 (both directions)
az network vnet peering create -g $RG -n hub-to-spoke2 --vnet-name vnet-hub \
  --remote-vnet vnet-spoke2 --allow-vnet-access --allow-forwarded-traffic
az network vnet peering create -g $RG -n spoke2-to-hub --vnet-name vnet-spoke2 \
  --remote-vnet vnet-hub --allow-vnet-access --allow-forwarded-traffic

# Step 3: Check peering state (Connected)
az network vnet peering list -g $RG --vnet-name vnet-hub \
  --query "[].{name:name, state:peeringState}" -o table

# Step 4: spoke1 -> NVA works
az vm run-command invoke -g $RG -n vm-spoke1 --command-id RunShellScript \
  --scripts "ping -c 3 10.0.3.4"

# Step 4: spoke1 -> spoke2 fails (peering is not transitive)
SPOKE2_IP=$(az vm show -d -g $RG -n vm-spoke2 --query privateIps -o tsv)
az vm run-command invoke -g $RG -n vm-spoke1 --command-id RunShellScript \
  --scripts "ping -c 3 -W 2 $SPOKE2_IP"

# Step 5: Enable IP forwarding on the NVA's NIC (Azure side)
az network nic update -g $RG -n vm-nvaVMNic --ip-forwarding true

# Step 5: Enable IP forwarding inside the NVA's OS
az vm run-command invoke -g $RG -n vm-nva --command-id RunShellScript \
  --scripts "sudo sysctl -w net.ipv4.ip_forward=1"

# Step 6: Route table for spoke1: traffic to spoke2 goes via the NVA
az network route-table create -g $RG -n rt-spoke1
az network route-table route create -g $RG --route-table-name rt-spoke1 \
  -n to-spoke2 --address-prefix 10.2.0.0/16 \
  --next-hop-type VirtualAppliance --next-hop-ip-address 10.0.3.4
az network vnet subnet update -g $RG --vnet-name vnet-spoke1 -n default \
  --route-table rt-spoke1

# Step 6: Route table for spoke2: return traffic to spoke1 via the NVA
az network route-table create -g $RG -n rt-spoke2
az network route-table route create -g $RG --route-table-name rt-spoke2 \
  -n to-spoke1 --address-prefix 10.1.0.0/16 \
  --next-hop-type VirtualAppliance --next-hop-ip-address 10.0.3.4
az network vnet subnet update -g $RG --vnet-name vnet-spoke2 -n default \
  --route-table rt-spoke2

# Step 6: Ping again - now works through the NVA
az vm run-command invoke -g $RG -n vm-spoke1 --command-id RunShellScript \
  --scripts "ping -c 3 $SPOKE2_IP"

# Step 7: Next hop from spoke1 to spoke2 (expect VirtualAppliance 10.0.3.4)
SPOKE1_IP=$(az vm show -d -g $RG -n vm-spoke1 --query privateIps -o tsv)
az network watcher show-next-hop -g $RG --vm vm-spoke1 \
  --source-ip $SPOKE1_IP --dest-ip $SPOKE2_IP

# Step 7: Effective routes on spoke1's NIC
az network nic show-effective-route-table -g $RG -n vm-spoke1VMNic -o table

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- Peering không bắc cầu. Nối 2 spoke: peer trực tiếp, hoặc UDR qua NVA/Azure Firewall ở hub.
- Address space hai VNet không được chồng nhau; global peering dùng được giữa các region.
- Gateway transit: *Allow gateway transit* ở hub, *Use remote gateways* ở spoke (hub phải có VPN/ExpressRoute gateway).
- Route table gắn vào subnet. Next hop types: VirtualAppliance, VirtualNetworkGateway, VnetLocal, Internet, None.
- NVA cần bật IP forwarding ở cả NIC lẫn trong OS.

**Tự kiểm tra:** Spoke1 và spoke2 đều peer với hub, VM spoke1 có ping được VM spoke2 không? Bạn đã cần những thay đổi nào để được?

## Ngày 7 — Storage account, blob, tier, lifecycle

**Mục tiêu:** tạo storage account, đổi redundancy, thao tác blob bằng Entra ID (data-plane RBAC), dùng tier, versioning, soft delete, lifecycle.

**Dùng lại:** Ngày 3 (gán role, phân biệt `Actions` và `DataActions`).

**Các bước lab:**

1. Tạo storage account GPv2 LRS, đổi sang GRS.
2. Thử upload bằng `--auth-mode login` khi chưa có data role (bị chặn); gán *Storage Blob Data Contributor* rồi thử lại.
3. So sánh với cách dùng account key.
4. Chuyển blob sang Cool, Archive; thử download blob Archive (lỗi); rehydrate.
5. Bật versioning, change feed, soft delete; ghi đè và xóa blob rồi khôi phục.
6. Tạo lifecycle rule.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d07
SA=stkhoalab$RANDOM
ME=$(az ad signed-in-user show --query id -o tsv)
az group create -n $RG -l $LOC

# Step 1: Create a GPv2 LRS storage account
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --access-tier Hot --min-tls-version TLS1_2 --allow-blob-public-access false

# Step 1: Change redundancy to GRS
az storage account update -g $RG -n $SA --sku Standard_GRS
az storage account show -g $RG -n $SA --query "{sku:sku.name, primary:primaryLocation, secondary:secondaryLocation}"

# Step 2: Try data-plane access as Owner without a data role - expect AuthorizationPermissionMismatch
az storage container create --account-name $SA -n data --auth-mode login

# Step 2: Grant yourself a data-plane role on the account
SA_ID=$(az storage account show -g $RG -n $SA --query id -o tsv)
az role assignment create --assignee $ME --role "Storage Blob Data Contributor" --scope $SA_ID

# Step 2 (wait 1-2 min): create container, upload, list, download
az storage container create --account-name $SA -n data --auth-mode login
echo "hello v1" > hello.txt
az storage blob upload --account-name $SA -c data -f hello.txt -n logs/hello.txt --auth-mode login
az storage blob list --account-name $SA -c data --auth-mode login --query "[].name" -o tsv
az storage blob download --account-name $SA -c data -n logs/hello.txt -f out.txt --auth-mode login

# Step 3: Same list using the account key (no RBAC involved)
KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage blob list --account-name $SA -c data --account-key $KEY --query "[].name" -o tsv

# Step 4: Move the blob to Cool, then Archive
az storage blob set-tier --account-name $SA -c data -n logs/hello.txt --tier Cool --auth-mode login
az storage blob set-tier --account-name $SA -c data -n logs/hello.txt --tier Archive --auth-mode login

# Step 4: Download an archived blob - expect BlobArchived error
az storage blob download --account-name $SA -c data -n logs/hello.txt -f out.txt --auth-mode login

# Step 4: Rehydrate to Hot (can take hours) and check status
az storage blob set-tier --account-name $SA -c data -n logs/hello.txt --tier Hot \
  --rehydrate-priority High --auth-mode login
az storage blob show --account-name $SA -c data -n logs/hello.txt --auth-mode login \
  --query "properties.{tier:blobTier, rehydrate:rehydrationStatus}"

# Step 5: Enable versioning, change feed, blob and container soft delete
az storage account blob-service-properties update -g $RG --account-name $SA \
  --enable-versioning true --enable-change-feed true \
  --enable-delete-retention true --delete-retention-days 7 \
  --enable-container-delete-retention true --container-delete-retention-days 7

# Step 5: Overwrite a blob twice and list its versions
echo "v1" > doc.txt
az storage blob upload --account-name $SA -c data -f doc.txt -n doc.txt --auth-mode login
echo "v2" > doc.txt
az storage blob upload --account-name $SA -c data -f doc.txt -n doc.txt --overwrite --auth-mode login
az storage blob list --account-name $SA -c data --include v --auth-mode login \
  --query "[?name=='doc.txt'].{name:name, version:versionId, current:isCurrentVersion}" -o table

# Step 5: Delete the blob, then undelete it
az storage blob delete --account-name $SA -c data -n doc.txt --auth-mode login
az storage blob undelete --account-name $SA -c data -n doc.txt --auth-mode login

# Step 6: Create the lifecycle policy (policy.json below) and read it back
az storage account management-policy create -g $RG --account-name $SA --policy @policy.json
az storage account management-policy show -g $RG --account-name $SA

# Cleanup
az group delete -n $RG --yes --no-wait
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

- Owner/Contributor là control plane; đọc/ghi blob bằng Entra ID cần Storage Blob Data Reader/Contributor/Owner.
- Archive là offline, phải rehydrate (Standard tối đa khoảng 15 giờ) mới đọc được.
- Lưu tối thiểu: Cool 30 ngày, Cold 90 ngày, Archive 180 ngày; xóa hoặc chuyển sớm vẫn tính phí.
- Object replication cần versioning ở cả hai account và change feed ở account nguồn.
- Premium account không có tier Hot/Cool/Archive.

**Tự kiểm tra:** Vì sao Owner vẫn lỗi khi dùng `--auth-mode login`? Yêu cầu vẫn đọc được dữ liệu khi region chính sập và chịu được mất một zone, chọn redundancy nào?

## Ngày 8 — SAS, Azure Files, AzCopy, firewall

**Mục tiêu:** phát hành và thu hồi cả 3 loại *SAS*; dùng Azure Files từ VM; copy dữ liệu bằng AzCopy; khóa account bằng firewall.

**Dùng lại:** Ngày 4 (VM, run-command), Ngày 7 (storage account, container, data role).

**Các bước lab:**

1. Dựng lại storage account, container, blob và data role như Ngày 7.
2. Tạo *user delegation SAS*, đọc blob bằng `curl`.
3. Tạo *stored access policy*, phát SAS theo policy; xóa policy để thấy SAS hết hiệu lực.
4. Tạo *account SAS* bằng key1; rotate key1 để thấy SAS chết.
5. Tạo file share, upload file, mount share từ một VM Linux.
6. Cài AzCopy, đăng nhập bằng Entra ID, copy và sync thư mục.
7. Bật firewall chỉ cho IP nhà bạn; thử từ VM (bị chặn) và từ máy bạn (được).

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d08
SA=stkhoalab$RANDOM
ME=$(az ad signed-in-user show --query id -o tsv)
MYIP=$(curl -s https://api.ipify.org)
EXP=$(date -u -d "+1 day" '+%Y-%m-%dT%H:%MZ')      # Linux
# EXP=$(date -u -v+1d '+%Y-%m-%dT%H:%MZ')          # macOS
az group create -n $RG -l $LOC

# Step 1: Storage account + data role + container + blob (same as Day 7)
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access false
az role assignment create --assignee $ME --role "Storage Blob Data Contributor" \
  --scope $(az storage account show -g $RG -n $SA --query id -o tsv)
az storage container create --account-name $SA -n data --auth-mode login
echo "hello" > hello.txt
az storage blob upload --account-name $SA -c data -f hello.txt -n hello.txt --auth-mode login
URL="https://$SA.blob.core.windows.net/data/hello.txt"

# Step 2: User delegation SAS (signed with your Entra ID)
SAS_UD=$(az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry $EXP --auth-mode login --as-user -o tsv)
curl "$URL?$SAS_UD"

# Step 3: Stored access policy on the container
KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage container policy create --account-name $SA -c data -n read-policy \
  --permissions rl --expiry $EXP --account-key $KEY

# Step 3: SAS that references the policy
SAS_POL=$(az storage container generate-sas --account-name $SA -n data \
  --policy-name read-policy --account-key $KEY -o tsv)
curl "$URL?$SAS_POL"

# Step 3: Delete the policy - the SAS stops working (403) without rotating keys
az storage container policy delete --account-name $SA -c data -n read-policy --account-key $KEY
curl "$URL?$SAS_POL"

# Step 4: Account SAS signed with key1
SAS_ACC=$(az storage account generate-sas --account-name $SA --account-key $KEY \
  --services b --resource-types sco --permissions rl --expiry $EXP -o tsv)
curl "$URL?$SAS_ACC"

# Step 4: Rotate key1 - the account SAS dies (403)
az storage account keys renew -g $RG -n $SA --key primary
curl "$URL?$SAS_ACC"

# Step 4: Revoke all user delegation SAS at once
az storage account revoke-delegation-keys -g $RG -n $SA

# Step 5: Create a file share and upload a file
KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage share-rm create -g $RG --storage-account $SA -n share1 --quota 100
az storage file upload --account-name $SA --share-name share1 --source hello.txt --account-key $KEY

# Step 5: Create a Linux VM and mount the share over SMB 3.1.1 (port 445)
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript --scripts \
  "apt-get -y install cifs-utils >/dev/null && mkdir -p /mnt/share1 && mount -t cifs //$SA.file.core.windows.net/share1 /mnt/share1 -o username=$SA,password=$KEY,vers=3.1.1,serverino && ls -l /mnt/share1"

# Step 6: Install AzCopy (macOS: brew install azcopy)
curl -sL https://aka.ms/downloadazcopy-v10-linux | tar -xz --strip-components=1 -C /tmp
sudo mv /tmp/azcopy /usr/local/bin/

# Step 6: Sign in to AzCopy with Entra ID (uses your data role)
azcopy login --tenant-id $(az account show --query tenantId -o tsv)

# Step 6: Copy a folder, then sync after changing it
mkdir -p sync-demo && echo "a" > sync-demo/a.txt && echo "b" > sync-demo/b.txt
azcopy copy "./sync-demo" "https://$SA.blob.core.windows.net/data" --recursive
rm sync-demo/b.txt
azcopy sync "./sync-demo" "https://$SA.blob.core.windows.net/data/sync-demo" --delete-destination=true

# Step 7: Firewall - deny by default, allow only your IP
az storage account update -g $RG -n $SA --default-action Deny
az storage account network-rule add -g $RG --account-name $SA --ip-address $MYIP

# Step 7: From your machine - still works (new SAS since keys were rotated)
SAS_UD=$(az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry $EXP --auth-mode login --as-user -o tsv)
curl "$URL?$SAS_UD"

# Step 7: From the VM - blocked (AuthorizationFailure)
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "curl -s '$URL?$SAS_UD'"

# Cleanup
az group delete -n $RG --yes --no-wait
```

| Loại SAS | Ký bằng | Cách thu hồi |
| --- | --- | --- |
| Account SAS | Account key | Rotate key |
| Service SAS (ad-hoc) | Account key | Rotate key |
| Service SAS + stored access policy | Account key | Xóa hoặc sửa policy |
| User delegation SAS | Entra ID | Revoke delegation keys; hiệu lực tối đa 7 ngày |

**Điểm hay thi:**

- Mỗi container tối đa 5 stored access policy.
- Azure Files dùng SMB port 445; nhiều ISP chặn port này nên mount từ VM trong Azure là cách chắc chắn nhất. Azure File Sync đồng bộ file server on-prem lên share.
- AzCopy và Storage Explorer hỗ trợ cả Entra ID lẫn SAS.
- Firewall storage chặn mọi nguồn không có trong rule, kể cả VM Azure (trừ khi dùng service endpoint/private endpoint ở Ngày 9).

**Tự kiểm tra:** Đã phát SAS cho đối tác, cần thu hồi ngay mà không ảnh hưởng ứng dụng khác dùng account key, làm cách nào?

## Ngày 9 — DNS, service endpoint, private endpoint

**Mục tiêu:** phân biệt public/private DNS; cho VM trong VNet truy cập storage đã bật firewall bằng *service endpoint* rồi bằng *private endpoint*.

**Dùng lại:** Ngày 4 (VM, run-command), Ngày 5 (VNet, subnet), Ngày 7 (storage, data role), Ngày 8 (user delegation SAS, firewall).

**Các bước lab:**

1. Tạo public DNS zone, record A và CNAME; truy vấn trực tiếp name server Azure.
2. Tạo private DNS zone `corp.internal`, link với VNet có bật *autoregistration*; tạo VM và thấy record tự xuất hiện.
3. Tạo storage account, container, blob, SAS; bật firewall Deny.
4. Bật service endpoint `Microsoft.Storage` cho `snet-app`, thêm VNet rule; VM đọc được, máy bạn bị chặn.
5. Tạo private endpoint cho blob và private DNS zone `privatelink`; `nslookup` từ VM trả về IP private.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d09
SA=stkhoalab$RANDOM
ZONE=khoalab$RANDOM.com
ME=$(az ad signed-in-user show --query id -o tsv)
EXP=$(date -u -d "+1 day" '+%Y-%m-%dT%H:%MZ')      # Linux
# EXP=$(date -u -v+1d '+%Y-%m-%dT%H:%MZ')          # macOS
az group create -n $RG -l $LOC

# Step 1: Public DNS zone and records
az network dns zone create -g $RG -n $ZONE
az network dns record-set a add-record -g $RG -z $ZONE -n www -a 20.1.2.3
az network dns record-set cname set-record -g $RG -z $ZONE -n blog -c www.$ZONE

# Step 1: The 4 Azure name servers you would set at your registrar
az network dns zone show -g $RG -n $ZONE --query nameServers -o tsv

# Step 1: Query one Azure name server directly
NS1=$(az network dns zone show -g $RG -n $ZONE --query "nameServers[0]" -o tsv)
nslookup www.$ZONE $NS1

# Step 2: VNet with an app subnet and a private-endpoint subnet
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-app --subnet-prefixes 10.0.1.0/24
az network vnet subnet create -g $RG --vnet-name vnet-hub -n snet-pe \
  --address-prefixes 10.0.2.0/24

# Step 2: Private zone linked with auto-registration
az network private-dns zone create -g $RG -n corp.internal
az network private-dns link vnet create -g $RG -z corp.internal -n link-hub \
  -v vnet-hub -e true

# Step 2: Create a VM - its A record registers itself
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-hub --subnet snet-app
az network private-dns record-set a list -g $RG -z corp.internal -o table

# Step 2: Resolve it from inside the VNet
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "nslookup vm1.corp.internal"

# Step 3: Storage + data role + container + blob + SAS (Day 7-8)
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access false
SA_ID=$(az storage account show -g $RG -n $SA --query id -o tsv)
az role assignment create --assignee $ME --role "Storage Blob Data Contributor" --scope $SA_ID
az storage container create --account-name $SA -n data --auth-mode login
echo "hello" > hello.txt
az storage blob upload --account-name $SA -c data -f hello.txt -n hello.txt --auth-mode login
SAS=$(az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry $EXP --auth-mode login --as-user -o tsv)
URL="https://$SA.blob.core.windows.net/data/hello.txt"

# Step 3: Firewall deny by default
az storage account update -g $RG -n $SA --default-action Deny

# Step 4: Service endpoint on the VM's subnet
az network vnet subnet update -g $RG --vnet-name vnet-hub -n snet-app \
  --service-endpoints Microsoft.Storage

# Step 4: Allow that subnet in the storage firewall
az storage account network-rule add -g $RG --account-name $SA \
  --vnet-name vnet-hub --subnet snet-app

# Step 4: VM can read the blob
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "curl -s '$URL?$SAS'"

# Step 4: Your machine cannot (AuthorizationFailure)
curl "$URL?$SAS"

# Step 5: Private endpoint for the blob service
az network private-endpoint create -g $RG -n pe-blob --vnet-name vnet-hub \
  --subnet snet-pe --private-connection-resource-id $SA_ID \
  --group-id blob --connection-name pe-blob-conn

# Step 5: privatelink zone, link it, attach it to the endpoint
az network private-dns zone create -g $RG -n privatelink.blob.core.windows.net
az network private-dns link vnet create -g $RG \
  -z privatelink.blob.core.windows.net -n link-blob -v vnet-hub -e false
az network private-endpoint dns-zone-group create -g $RG \
  --endpoint-name pe-blob -n default \
  --private-dns-zone privatelink.blob.core.windows.net --zone-name blob

# Step 5: From the VM the storage name now resolves to 10.0.2.x
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "nslookup $SA.blob.core.windows.net"

# Step 5: From your machine it still resolves to a public IP
nslookup $SA.blob.core.windows.net

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- Public zone chỉ hoạt động khi nhà đăng ký tên miền trỏ NS record về 4 name server Azure.
- Một VNet chỉ link với một private zone có autoregistration; có thể link resolve-only với nhiều zone.
- Service endpoint: dịch vụ vẫn dùng IP public, chỉ mở cho subnet trong Azure, không dùng được từ on-premises.
- Private endpoint: dịch vụ có IP private trong VNet, dùng được qua VPN/ExpressRoute; cần private DNS zone `privatelink.*`.

**Tự kiểm tra:** Công ty cần truy cập storage từ văn phòng qua VPN bằng IP private, chọn service endpoint hay private endpoint?

## Ngày 10 — Load Balancer và Application Gateway

**Mục tiêu:** dựng *Standard Load Balancer* cho 2 VM, thấy health probe loại VM lỗi; làm path-based routing với *Application Gateway*. Xóa App Gateway ngay khi xong.

**Dùng lại:** Ngày 4 (VM, Custom Script extension, run-command), Ngày 5 (VNet, subnet, NSG).

**Các bước lab:**

1. Tạo VNet, subnet `snet-web`, NSG cho phép HTTP từ Internet và SSH từ IP bạn.
2. Tạo 2 VM, cài nginx trả về hostname ở `/`, `/api/`, `/images/`.
3. Tạo Standard LB: public IP, health probe, LB rule port 80; đưa 2 NIC vào backend pool.
4. Tạo inbound NAT rule SSH port 50001 → vm1:22.
5. Tắt nginx trên vm1, thấy LB chỉ trả về vm2.
6. Tạo Application Gateway Standard\_v2 trong subnet riêng, `/api/*` → vm1, `/images/*` → vm2; xóa ngay sau khi test.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d10
MYIP=$(curl -s https://api.ipify.org)
az group create -n $RG -l $LOC

# Step 1: VNet, web subnet, NSG
az network vnet create -g $RG -n vnet-web --address-prefix 10.0.0.0/16 \
  --subnet-name snet-web --subnet-prefixes 10.0.1.0/24
az network nsg create -g $RG -n nsg-web
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-http \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 --source-address-prefixes Internet
az network nsg rule create -g $RG --nsg-name nsg-web -n allow-ssh-myip \
  --priority 110 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 22 --source-address-prefixes $MYIP
az network vnet subnet update -g $RG --vnet-name vnet-web -n snet-web \
  --network-security-group nsg-web

# Step 2: Two VMs (Standard public IP kept for outbound apt), no NIC NSG
for VM in vm1 vm2; do
  az vm create -g $RG -n $VM --image Ubuntu2204 --size Standard_B1s \
    --admin-username azureuser --generate-ssh-keys \
    --vnet-name vnet-web --subnet snet-web --nsg ""
done

# Step 2: nginx answering with the hostname on /, /api/, /images/
for VM in vm1 vm2; do
  az vm extension set -g $RG --vm-name $VM --name CustomScript \
    --publisher Microsoft.Azure.Extensions \
    --settings '{"commandToExecute":"apt-get -y update && apt-get -y install nginx && mkdir -p /var/www/html/api /var/www/html/images && hostname | tee /var/www/html/index.html /var/www/html/api/index.html /var/www/html/images/index.html"}'
done

# Step 3: Standard public IP and Standard LB
az network public-ip create -g $RG -n pip-lb --sku Standard
az network lb create -g $RG -n lb-web --sku Standard --public-ip-address pip-lb \
  --frontend-ip-name fe --backend-pool-name be

# Step 3: Health probe and load-balancing rule
az network lb probe create -g $RG --lb-name lb-web -n hp80 --protocol Tcp --port 80
az network lb rule create -g $RG --lb-name lb-web -n http --protocol Tcp \
  --frontend-port 80 --backend-port 80 --frontend-ip-name fe \
  --backend-pool-name be --probe-name hp80

# Step 3: Put both NICs in the backend pool
for VM in vm1 vm2; do
  az network nic ip-config address-pool add -g $RG --nic-name ${VM}VMNic \
    --ip-config-name ipconfig$VM --lb-name lb-web --address-pool be
done

# Step 3: Call the LB several times - answers alternate vm1/vm2
LB_IP=$(az network public-ip show -g $RG -n pip-lb --query ipAddress -o tsv)
for i in 1 2 3 4 5 6; do curl -s http://$LB_IP; done

# Step 4: Inbound NAT rule LB:50001 -> vm1:22
az network lb inbound-nat-rule create -g $RG --lb-name lb-web -n ssh-vm1 \
  --protocol Tcp --frontend-port 50001 --backend-port 22 --frontend-ip-name fe
az network nic ip-config inbound-nat-rule add -g $RG --nic-name vm1VMNic \
  --ip-config-name ipconfigvm1 --lb-name lb-web --inbound-nat-rule ssh-vm1
ssh -p 50001 azureuser@$LB_IP hostname

# Step 5: Stop nginx on vm1 - after ~15s the probe removes it
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "systemctl stop nginx"
for i in 1 2 3 4; do curl -s http://$LB_IP; done

# Step 5: Start it again
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "systemctl start nginx"

# Step 6: Dedicated subnet and public IP for App Gateway
az network vnet subnet create -g $RG --vnet-name vnet-web -n snet-agw \
  --address-prefixes 10.0.10.0/24
az network public-ip create -g $RG -n pip-agw --sku Standard
IP1=$(az vm show -d -g $RG -n vm1 --query privateIps -o tsv)
IP2=$(az vm show -d -g $RG -n vm2 --query privateIps -o tsv)

# Step 6: Create App Gateway (default pool = both VMs, rule name rule1)
az network application-gateway create -g $RG -n agw --sku Standard_v2 \
  --capacity 1 --vnet-name vnet-web --subnet snet-agw \
  --public-ip-address pip-agw --priority 100 --servers $IP1 $IP2

# Step 6: One backend pool per path
az network application-gateway address-pool create -g $RG --gateway-name agw \
  -n pool-api --servers $IP1
az network application-gateway address-pool create -g $RG --gateway-name agw \
  -n pool-images --servers $IP2

# Step 6: Path map: /api/* -> pool-api, default -> both VMs
az network application-gateway url-path-map create -g $RG --gateway-name agw \
  -n pathmap --rule-name api --paths "/api/*" --address-pool pool-api \
  --http-settings appGatewayBackendHttpSettings \
  --default-address-pool appGatewayBackendPool \
  --default-http-settings appGatewayBackendHttpSettings

# Step 6: Add /images/* -> pool-images
az network application-gateway url-path-map rule create -g $RG --gateway-name agw \
  --path-map-name pathmap -n images --paths "/images/*" \
  --address-pool pool-images --http-settings appGatewayBackendHttpSettings

# Step 6: Switch the routing rule to path-based
az network application-gateway rule update -g $RG --gateway-name agw -n rule1 \
  --rule-type PathBasedRouting --url-path-map pathmap

# Step 6: Test - /api/ always vm1, /images/ always vm2
AGW_IP=$(az network public-ip show -g $RG -n pip-agw --query ipAddress -o tsv)
curl -s http://$AGW_IP/api/
curl -s http://$AGW_IP/images/

# Step 6: Delete App Gateway right away (expensive)
az network application-gateway delete -g $RG -n agw

# Cleanup
az group delete -n $RG --yes --no-wait
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

**Tự kiểm tra:** Cần định tuyến `/api` và `/images` về 2 nhóm server khác nhau trong một region, chọn dịch vụ nào? Probe thấy vm1 lỗi thì LB làm gì?

## Ngày 11 — VPN Gateway, Bastion, Network Watcher

**Mục tiêu:** kết nối máy bạn vào VNet bằng *Point-to-Site VPN*; vào VM không public IP qua *Bastion*; dùng các công cụ Network Watcher. Xóa gateway và Bastion ngay khi xong.

**Dùng lại:** Ngày 4 (VM, run-command), Ngày 5 (VNet, subnet, IP flow verify), Ngày 7 (storage account cho flow logs).

**Các bước lab:**

1. Tạo VNet với `snet-app`, `GatewaySubnet`, `AzureBastionSubnet`; bấm deploy VPN Gateway trước (30–45 phút).
2. Trong lúc chờ: tạo VM không public IP, deploy Bastion Basic, SSH vào VM trên portal.
3. Network Watcher: connection troubleshoot, VNet flow logs, packet capture.
4. Khi gateway xong: tạo certificate, cấu hình Point-to-Site, tải profile, kết nối và ping IP private của VM.
5. Xóa Bastion và gateway.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d11
SA=stkhoalab$RANDOM
az group create -n $RG -l $LOC

# Step 1: VNet and the three subnets (names are mandatory)
az network vnet create -g $RG -n vnet-hub --address-prefix 10.0.0.0/16 \
  --subnet-name snet-app --subnet-prefixes 10.0.1.0/24
az network vnet subnet create -g $RG --vnet-name vnet-hub -n GatewaySubnet \
  --address-prefixes 10.0.255.0/27
az network vnet subnet create -g $RG --vnet-name vnet-hub -n AzureBastionSubnet \
  --address-prefixes 10.0.254.0/26

# Step 1: Start the VPN gateway now (if VpnGw1AZ is rejected in your region, try VpnGw1)
az network public-ip create -g $RG -n pip-vpngw --sku Standard
az network vnet-gateway create -g $RG -n vpngw --vnet vnet-hub \
  --public-ip-addresses pip-vpngw --gateway-type Vpn --vpn-type RouteBased \
  --sku VpnGw1AZ --no-wait

# Step 2: VM without a public IP
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-hub --subnet snet-app --public-ip-address ""

# Step 2: Bastion Basic (connect in portal: VM > Connect > Bastion, SSH key ~/.ssh/id_rsa)
az network public-ip create -g $RG -n pip-bas --sku Standard
az network bastion create -g $RG -n bas --vnet-name vnet-hub \
  --public-ip-address pip-bas --sku Basic

# Step 3: Connection troubleshoot from vm1 to the internet
az network watcher test-connectivity -g $RG --source-resource vm1 \
  --dest-address www.microsoft.com --dest-port 443

# Step 3: Storage account for flow logs and packet capture
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2

# Step 3: VNet flow logs
az network watcher flow-log create -g $RG -l $LOC -n fl-hub \
  --vnet vnet-hub --storage-account $SA

# Step 3: Packet capture on vm1 for 60 seconds
az network watcher packet-capture create -g $RG --vm vm1 -n pc1 \
  --storage-account $SA --time-limit 60
az network watcher packet-capture show-status -l $LOC -n pc1

# Step 4: Wait until the gateway is Succeeded
az network vnet-gateway show -g $RG -n vpngw --query provisioningState -o tsv

# Step 4: Self-signed root cert and a client cert signed by it
openssl req -x509 -new -nodes -newkey rsa:2048 -keyout p2sroot.key \
  -out p2sroot.crt -days 365 -subj "/CN=P2SRoot"
openssl req -new -nodes -newkey rsa:2048 -keyout p2sclient.key \
  -out p2sclient.csr -subj "/CN=P2SClient"
openssl x509 -req -in p2sclient.csr -CA p2sroot.crt -CAkey p2sroot.key \
  -CAcreateserial -out p2sclient.crt -days 365

# Step 4: P2S address pool and protocol
az network vnet-gateway update -g $RG -n vpngw \
  --address-prefixes 172.16.201.0/24 --client-protocol OpenVPN

# Step 4: Upload the root cert public data (base64 DER, no headers)
ROOT_DATA=$(openssl x509 -in p2sroot.crt -outform der | base64 | tr -d '\n')
az network vnet-gateway root-cert create -g $RG --gateway-name vpngw \
  -n P2SRoot --public-cert-data "$ROOT_DATA"

# Step 4: Download the client profile (zip URL)
az network vnet-gateway vpn-client generate -g $RG -n vpngw -o tsv

# Step 4: In OpenVPN/vpnconfig.ovpn paste p2sclient.crt and p2sclient.key, connect, then:
ping $(az vm show -d -g $RG -n vm1 --query privateIps -o tsv)

# Step 5: Delete the expensive parts first
az network bastion delete -g $RG -n bas
az network vnet-gateway delete -g $RG -n vpngw

# Cleanup
az group delete -n $RG --yes --no-wait
```

| Công cụ Network Watcher | Dùng khi |
| --- | --- |
| IP flow verify | NSG có cho phép một gói tin cụ thể không |
| Next hop | Traffic đi theo route nào |
| Connection troubleshoot | Kiểm tra kết nối từ VM đến đích |
| Effective security rules | Xem rule NSG thực tế trên NIC |
| Flow logs | Ghi log traffic qua VNet (NSG flow logs cũ đang bị thay thế) |
| Packet capture | Bắt gói tin trên VM |

**Điểm hay thi:**

- Tên subnet bắt buộc: `GatewaySubnet` (khuyến nghị /27), `AzureBastionSubnet` (tối thiểu /26), `AzureFirewallSubnet` (/26).
- Point-to-Site: từng máy client, xác thực bằng certificate, Entra ID hoặc RADIUS. Site-to-Site: cả mạng on-prem, cần local network gateway và thiết bị VPN có IP public.
- Bastion: RDP/SSH qua trình duyệt, VM không cần public IP. SSH bằng CLI native (`az network bastion ssh`) cần SKU Standard trở lên.

**Tự kiểm tra:** Admin cần RDP vào VM mà không mở port 3389 ra internet và không gán public IP, chọn gì? P2S và S2S khác nhau ở thành phần nào?

## Ngày 12 — ARM/Bicep, viết hub-spoke

**Mục tiêu:** đọc, sửa, deploy ARM/Bicep; hiểu mode Incremental và Complete; tự viết hub-spoke của Ngày 6 bằng Bicep trong 60 phút.

**Dùng lại:** Ngày 5 (VNet, NSG), Ngày 6 (peering, route table), Ngày 7 (storage account).

**Các bước lab:**

1. Viết `main.bicep` (VNet + storage), chạy *what-if*, deploy mode Incremental, đọc output.
2. Tạo thêm 1 public IP bằng CLI, deploy lại mode Complete để thấy nó bị xóa.
3. Build Bicep ra ARM JSON và đọc cấu trúc; export template từ resource group, decompile ngược về Bicep.
4. Tự viết `hub-spoke.bicep` (không xem đáp án), what-if, deploy, kiểm tra peering và route.

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
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d12
MYIP=$(curl -s https://api.ipify.org)
az group create -n $RG -l $LOC

# Step 1: Preview changes
az deployment group what-if -g $RG -f main.bicep -p prefix=lab

# Step 1: Deploy (Incremental is the default)
az deployment group create -g $RG -f main.bicep -p prefix=lab --mode Incremental

# Step 1: Read the output
az deployment group show -g $RG -n main --query properties.outputs

# Step 2: Add a resource that is NOT in the template
az network public-ip create -g $RG -n pip-extra --sku Standard

# Step 2: Complete mode deletes it
az deployment group create -g $RG -f main.bicep -p prefix=lab --mode Complete
az resource list -g $RG --query "[].name" -o tsv

# Step 3: Build Bicep to ARM JSON and read it
az bicep build --file main.bicep
cat main.json

# Step 3: Export the live resource group and decompile to Bicep
az group export -n $RG > exported.json
az bicep decompile --file exported.json

# Step 4: Deploy your hub-spoke file
az deployment group what-if -g $RG -f hub-spoke.bicep -p myIp=$MYIP
az deployment group create -g $RG -f hub-spoke.bicep -p myIp=$MYIP

# Step 4: Verify peerings and routes
az network vnet peering list -g $RG --vnet-name vnet-hub \
  --query "[].{name:name, state:peeringState}" -o table
az network route-table route list -g $RG --route-table-name rt-vnet-spoke1 -o table

# Cleanup
az group delete -n $RG --yes --no-wait
```

Đáp án tham khảo `hub-spoke.bicep` (chỉ mở sau khi tự viết):

```bicep
param location string = resourceGroup().location
param myIp string
param nvaIp string = '10.0.3.4'

var spokes = [
  { name: 'vnet-spoke1', prefix: '10.1.0.0/16', subnet: '10.1.0.0/24', other: '10.2.0.0/16' }
  { name: 'vnet-spoke2', prefix: '10.2.0.0/16', subnet: '10.2.0.0/24', other: '10.1.0.0/16' }
]

resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: 'nsg-spoke'
  location: location
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

resource rts 'Microsoft.Network/routeTables@2023-11-01' = [for s in spokes: {
  name: 'rt-${s.name}'
  location: location
  properties: {
    routes: [
      {
        name: 'to-other-spoke'
        properties: {
          addressPrefix: s.other
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: nvaIp
        }
      }
    ]
  }
}]

resource hub 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: 'vnet-hub'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.0.0.0/16'] }
    subnets: [
      { name: 'snet-nva', properties: { addressPrefix: '10.0.3.0/24' } }
    ]
  }
}

resource spokeVnets 'Microsoft.Network/virtualNetworks@2023-11-01' = [for (s, i) in spokes: {
  name: s.name
  location: location
  properties: {
    addressSpace: { addressPrefixes: [s.prefix] }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: s.subnet
          networkSecurityGroup: { id: nsg.id }
          routeTable: { id: rts[i].id }
        }
      }
    ]
  }
}]

resource hubToSpoke 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-11-01' = [for (s, i) in spokes: {
  parent: hub
  name: 'hub-to-${s.name}'
  properties: {
    remoteVirtualNetwork: { id: spokeVnets[i].id }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
  }
}]

resource spokeToHub 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-11-01' = [for (s, i) in spokes: {
  parent: spokeVnets[i]
  name: '${s.name}-to-hub'
  properties: {
    remoteVirtualNetwork: { id: hub.id }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
  }
}]
```

**Điểm hay thi:**

- Incremental (mặc định) giữ resource không có trong template; Complete xóa chúng.
- ARM JSON gồm `$schema`, `contentVersion`, `parameters`, `variables`, `resources`, `outputs`. Hotspot hay hỏi `dependsOn`, `copy`, `[resourceGroup().location]`, `[parameters('x')]`.
- Vòng lặp `for` trong Bicep build ra `copy` trong ARM JSON (xem trong `main.json` nếu bạn build `hub-spoke.bicep`).
- Bicep tự suy ra phụ thuộc khi tham chiếu property của resource khác (`nsg.id`), nên không cần `dependsOn`.

**Tự kiểm tra:** Deploy mode Complete vào resource group có 3 resource, template chỉ khai báo 2, kết quả là gì? Mở `hub-spoke.json` sau khi build: peering spoke-to-hub có những `dependsOn` nào?

## Ngày 13 — Ôn Networking + Storage

**Mục tiêu:** khóa hai domain có nhiều câu hotspot nhất bằng practice và một bài tự dựng có bấm giờ.

**Dùng lại:** Ngày 4–12.

**Các bước:**

1. Làm khoảng 50 câu practice phần networking và storage (Microsoft Learn practice assessment).
2. Ghi từng câu sai: không biết, hay đọc sót ràng buộc.
3. Bài tự dựng 45 phút, không xem guide: VNet 2 subnet, NSG chỉ cho SSH từ IP bạn, VM không public IP, storage bật firewall Deny, private endpoint + DNS, VM đọc được blob qua SAS. So với đáp án bên dưới.
4. Viết 1 trang ghi nhớ: bảng cân bằng tải (Ngày 10), Network Watcher (Ngày 11), SAS (Ngày 8), redundancy (Ngày 7), tên subnet bắt buộc.

Đáp án tham khảo bài tự dựng:

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d13
SA=stkhoalab$RANDOM
ME=$(az ad signed-in-user show --query id -o tsv)
MYIP=$(curl -s https://api.ipify.org)
EXP=$(date -u -d "+1 day" '+%Y-%m-%dT%H:%MZ')      # Linux
# EXP=$(date -u -v+1d '+%Y-%m-%dT%H:%MZ')          # macOS
az group create -n $RG -l $LOC

# VNet with app and private-endpoint subnets
az network vnet create -g $RG -n vnet-lab --address-prefix 10.0.0.0/16 \
  --subnet-name snet-app --subnet-prefixes 10.0.1.0/24
az network vnet subnet create -g $RG --vnet-name vnet-lab -n snet-pe \
  --address-prefixes 10.0.2.0/24

# NSG: SSH only from your IP, on snet-app
az network nsg create -g $RG -n nsg-app
az network nsg rule create -g $RG --nsg-name nsg-app -n allow-ssh-myip \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 22 --source-address-prefixes $MYIP
az network vnet subnet update -g $RG --vnet-name vnet-lab -n snet-app \
  --network-security-group nsg-app

# VM without public IP
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys \
  --vnet-name vnet-lab --subnet snet-app --nsg "" --public-ip-address ""

# Storage, data role, blob, SAS, firewall Deny
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access false
SA_ID=$(az storage account show -g $RG -n $SA --query id -o tsv)
az role assignment create --assignee $ME --role "Storage Blob Data Contributor" --scope $SA_ID
az storage container create --account-name $SA -n data --auth-mode login
echo "hello" > hello.txt
az storage blob upload --account-name $SA -c data -f hello.txt -n hello.txt --auth-mode login
SAS=$(az storage container generate-sas --account-name $SA -n data \
  --permissions rl --expiry $EXP --auth-mode login --as-user -o tsv)
az storage account update -g $RG -n $SA --default-action Deny

# Private endpoint + DNS
az network private-endpoint create -g $RG -n pe-blob --vnet-name vnet-lab \
  --subnet snet-pe --private-connection-resource-id $SA_ID \
  --group-id blob --connection-name pe-blob-conn
az network private-dns zone create -g $RG -n privatelink.blob.core.windows.net
az network private-dns link vnet create -g $RG \
  -z privatelink.blob.core.windows.net -n link-blob -v vnet-lab -e false
az network private-endpoint dns-zone-group create -g $RG \
  --endpoint-name pe-blob -n default \
  --private-dns-zone privatelink.blob.core.windows.net --zone-name blob

# Verify from the VM: private IP + blob content
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript --scripts \
  "nslookup $SA.blob.core.windows.net; curl -s 'https://$SA.blob.core.windows.net/data/hello.txt?$SAS'"

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Tự kiểm tra nhanh:**

- [ ] Giải thích được thứ tự đánh giá NSG inbound và outbound
- [ ] Biết cách nối 2 spoke khi peering không bắc cầu
- [ ] Phân biệt service endpoint và private endpoint
- [ ] Thu hồi được từng loại SAS
- [ ] Chọn đúng dịch vụ cân bằng tải theo tầng và phạm vi

## Ngày 14 — Azure Policy

**Mục tiêu:** phân biệt các effect; thấy *deny* chặn lúc tạo; dùng *managed identity* và *remediation* để sửa resource cũ; gom policy thành *initiative*.

**Dùng lại:** Ngày 1 (tag resource group), Ngày 3 (gán role), Ngày 5 (VNet), Ngày 7 (storage account).

**Các bước lab:**

1. Tạo resource group có tag `env=lab`; tạo `vnet-old` **trước** khi gán policy (không có tag).
2. Gán *Allowed locations* (chỉ Southeast Asia) ở resource group; thử tạo storage ở East US (bị chặn), ở Southeast Asia (được).
3. Gán *Inherit a tag from the resource group* (effect modify) kèm managed identity; gán role cho identity; tạo `vnet-new` → tự có tag; `vnet-old` non-compliant → chạy remediation.
4. Gom 2 policy audit về storage thành initiative, gán và xem compliance.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d14
SUB=$(az account show --query id -o tsv)
SCOPE=/subscriptions/$SUB/resourceGroups/$RG
az group create -n $RG -l $LOC --tags env=lab

# Step 1: A resource created BEFORE any policy (no tags)
az network vnet create -g $RG -n vnet-old --address-prefix 10.0.0.0/16

# Step 2: Find the built-in Allowed locations policy
POLICY=$(az policy definition list \
  --query "[?displayName=='Allowed locations'].name" -o tsv)

# Step 2: Assign it at resource group scope
az policy assignment create --name allowed-locs --scope $SCOPE --policy $POLICY \
  --params '{"listOfAllowedLocations":{"value":["southeastasia"]}}'

# Step 2 (wait ~5 min): East US is denied - RequestDisallowedByPolicy
az storage account create -g $RG -n stdeny$RANDOM -l eastus --sku Standard_LRS

# Step 2: Southeast Asia is allowed
az storage account create -g $RG -n stok$RANDOM -l $LOC --sku Standard_LRS

# Step 3: Find the built-in inherit-tag policy
INHERIT=$(az policy definition list \
  --query "[?displayName=='Inherit a tag from the resource group'].name" -o tsv)

# Step 3: Assign it with a system-assigned managed identity
az policy assignment create --name inherit-env-tag --scope $SCOPE --policy $INHERIT \
  --params '{"tagName":{"value":"env"}}' \
  --mi-system-assigned --location $LOC

# Step 3: The identity needs Tag Contributor (portal does this for you, CLI does not)
PRINCIPAL=$(az policy assignment show --name inherit-env-tag --scope $SCOPE \
  --query identity.principalId -o tsv)
az role assignment create --assignee-object-id $PRINCIPAL \
  --assignee-principal-type ServicePrincipal --role "Tag Contributor" --scope $SCOPE

# Step 3 (wait ~5 min): a new resource gets the tag automatically
az network vnet create -g $RG -n vnet-new --address-prefix 10.1.0.0/16
az network vnet show -g $RG -n vnet-new --query tags

# Step 3: Force a compliance scan and list states
az policy state trigger-scan --resource-group $RG
az policy state list --resource-group $RG \
  --query "[].{resource:resourceId, state:complianceState, assignment:policyAssignmentName}" -o table

# Step 3: Remediate the old resource
az policy remediation create --name fix-env-tag \
  --policy-assignment inherit-env-tag --resource-group $RG
az policy remediation show --name fix-env-tag --resource-group $RG \
  --query provisioningState -o tsv
az network vnet show -g $RG -n vnet-old --query tags

# Step 4: Two built-in audit policies for storage
ID1=$(az policy definition list \
  --query "[?displayName=='Secure transfer to storage accounts should be enabled'].id" -o tsv)
ID2=$(az policy definition list \
  --query "[?displayName=='Storage accounts should prevent shared key access'].id" -o tsv)

# Step 4: Create the initiative (policy set)
az policy set-definition create -n lab-storage-baseline \
  --display-name "Lab storage baseline" \
  --definitions "[{\"policyDefinitionId\":\"$ID1\"},{\"policyDefinitionId\":\"$ID2\"}]"

# Step 4: Assign the initiative and check compliance after a scan
az policy assignment create --name storage-baseline --scope $SCOPE \
  --policy-set-definition lab-storage-baseline
az policy state trigger-scan --resource-group $RG
az policy state summarize --resource-group $RG

# Cleanup: resource group (assignments go with it) and the initiative definition
az group delete -n $RG --yes --no-wait
az policy set-definition delete -n lab-storage-baseline
```

| Effect | Tác dụng |
| --- | --- |
| deny | Chặn tạo/sửa resource không tuân thủ |
| audit | Cho tạo, đánh dấu non-compliant |
| append | Thêm field khi tạo/sửa |
| modify | Thêm/sửa tag hoặc property; sửa resource cũ qua remediation (cần managed identity) |
| deployIfNotExists | Deploy resource liên quan nếu thiếu (cần managed identity) |
| auditIfNotExists | Báo non-compliant nếu thiếu resource liên quan |
| disabled | Tắt policy |

**Điểm hay thi:**

- Policy chỉ chặn lúc tạo/sửa; resource có sẵn chỉ bị báo non-compliant cho đến khi remediation.
- Allowed locations không áp cho chính resource group; muốn giới hạn region của RG dùng policy *Allowed locations for resource groups*.
- Tag không tự kế thừa từ resource group; muốn vậy phải dùng policy effect modify.
- Có thể loại một scope con khỏi assignment bằng exclusion (`--not-scopes`).
- RBAC kiểm soát *ai* được làm; Policy kiểm soát resource *phải trông thế nào*.

**Tự kiểm tra:** Resource tạo trước khi gán policy deny có bị xóa không? Effect nào sửa được resource cũ, và nó cần gì để chạy?

## Ngày 15 — Lock, move, tags, management group, cost

**Mục tiêu:** hiểu *resource lock* chặn những gì; move resource giữa resource group; quản lý tag; dựng *management group*; xem chi phí theo tag.

**Dùng lại:** Ngày 4 (VM), Ngày 5 (VNet), Ngày 7 (storage), Ngày 14 (scope, kế thừa).

**Các bước lab:**

1. Tạo `rg-lab-d15a` có VM và storage, `rg-lab-d15b` trống.
2. Lock *CanNotDelete*: thử xóa storage (bị chặn); gỡ lock.
3. Lock *ReadOnly*: thử start/stop VM, list storage key, sửa tag (đều bị chặn); gỡ lock.
4. Validate rồi move storage và một VNet riêng sang `rg-lab-d15b`.
5. Tag: Merge, Replace, Delete; tìm resource theo tag.
6. Tạo management group, đưa subscription vào, xem cây; rồi gỡ ra.
7. Xem khuyến nghị cost của Advisor; truy vấn chi phí tháng này theo tag.

```bash
# Prep: variables and resource groups
LOC=southeastasia
SUB=$(az account show --query id -o tsv)
SA=stkhoalab$RANDOM
az group create -n rg-lab-d15a -l $LOC --tags env=lab
az group create -n rg-lab-d15b -l $LOC --tags env=lab

# Step 1: A VM, a storage account and a standalone VNet in group A
az vm create -g rg-lab-d15a -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys
az storage account create -g rg-lab-d15a -n $SA --sku Standard_LRS --kind StorageV2
az network vnet create -g rg-lab-d15a -n vnet-move --address-prefix 10.50.0.0/16

# Step 2: CanNotDelete lock on group A
az lock create --name no-delete --lock-type CanNotDelete --resource-group rg-lab-d15a

# Step 2: Delete is blocked (ScopeLocked), even for Owner
az storage account delete -g rg-lab-d15a -n $SA --yes

# Step 2: Remove the lock
az lock delete --name no-delete --resource-group rg-lab-d15a

# Step 3: ReadOnly lock on group A
az lock create --name read-only --lock-type ReadOnly --resource-group rg-lab-d15a

# Step 3: All of these are blocked (POST/PUT operations)
az vm stop -g rg-lab-d15a -n vm1
az storage account keys list -g rg-lab-d15a -n $SA
az group update -n rg-lab-d15a --tags env=lab owner=khoa

# Step 3: List and remove the lock
az lock list --resource-group rg-lab-d15a -o table
az lock delete --name read-only --resource-group rg-lab-d15a

# Step 4: Validate the move first (empty output = OK)
SA_ID=$(az storage account show -g rg-lab-d15a -n $SA --query id -o tsv)
VNET_ID=$(az network vnet show -g rg-lab-d15a -n vnet-move --query id -o tsv)
az rest --method POST \
  --url "https://management.azure.com/subscriptions/$SUB/resourceGroups/rg-lab-d15a/validateMoveResources?api-version=2021-04-01" \
  --body "{\"resources\":[\"$SA_ID\",\"$VNET_ID\"],\"targetResourceGroup\":\"/subscriptions/$SUB/resourceGroups/rg-lab-d15b\"}"

# Step 4: Move both, then check location did not change
az resource move --destination-group rg-lab-d15b --ids $SA_ID $VNET_ID
az resource list -g rg-lab-d15b --query "[].{name:name, location:location}" -o table

# Step 5: Merge adds/updates tags, keeps the others
SA_ID=$(az storage account show -g rg-lab-d15b -n $SA --query id -o tsv)
az tag update --resource-id $SA_ID --operation Merge --tags env=lab costcenter=it

# Step 5: Replace overwrites the whole tag set
az tag update --resource-id $SA_ID --operation Replace --tags owner=khoa

# Step 5: Delete removes specific tags
az tag update --resource-id $SA_ID --operation Delete --tags owner=khoa

# Step 5: Find resources by tag
az tag update --resource-id $SA_ID --operation Merge --tags env=lab
az resource list --tag env=lab --query "[].{name:name, rg:resourceGroup}" -o table

# Step 6: Create a management group and move the subscription under it
az account management-group create --name mg-lab --display-name "Lab MG"
az account management-group subscription add --name mg-lab --subscription $SUB

# Step 6: Show the hierarchy
az account management-group show --name mg-lab --expand --recurse

# Step 6: Move the subscription back and delete the management group
az account management-group subscription remove --name mg-lab --subscription $SUB
az account management-group delete --name mg-lab

# Step 7: Advisor cost recommendations
az advisor recommendation list --category Cost -o table

# Step 7: Month-to-date cost grouped by the env tag
az rest --method POST \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.CostManagement/query?api-version=2023-03-01" \
  --body '{"type":"ActualCost","timeframe":"MonthToDate","dataset":{"granularity":"None","aggregation":{"totalCost":{"name":"Cost","function":"Sum"}},"grouping":[{"type":"TagKey","name":"env"}]}}'

# Cleanup
az group delete -n rg-lab-d15a --yes --no-wait
az group delete -n rg-lab-d15b --yes --no-wait
```

**Điểm hay thi:**

- Lock áp dụng cho mọi người, kể cả Owner; gỡ lock cần quyền `Microsoft.Authorization/locks/*` (Owner, User Access Administrator).
- ReadOnly chặn cả thao tác POST: không start/stop VM, không list storage key, không sửa tag.
- Lock kế thừa từ scope cha xuống resource con.
- Move không đổi region; resource group nguồn và đích bị khóa ghi trong lúc move. VM phải move kèm disk, NIC.
- Cây management group sâu tối đa 6 cấp (không tính root); mỗi subscription chỉ thuộc một management group.
- Tag tối đa 50 cặp mỗi resource; không kế thừa mặc định (xem Ngày 14).

**Tự kiểm tra:** Move VM sang resource group khác có đổi region không? Muốn áp policy cho nhiều subscription cùng lúc thì gán ở đâu? Lock nào chặn cả việc stop VM?

## Ngày 16 — Ôn Identity & Governance

**Mục tiêu:** khóa domain 20–25% này bằng practice và một bài tự dựng có bấm giờ.

**Dùng lại:** Ngày 2, 3, 14, 15.

**Các bước:**

1. Làm khoảng 40 câu practice của domain Identity & Governance.
2. Ghi từng câu sai: không biết, hay đọc sót ràng buộc.
3. Bài tự dựng 45 phút, không xem guide: tạo user `lab-auditor` và group `Auditors`; cho group quyền Reader ở một resource group; chặn mọi region ngoài Southeast Asia ở resource group đó; khóa CanNotDelete; gắn tag. Đăng nhập bằng `lab-auditor` để kiểm chứng. So với đáp án bên dưới.
4. Viết 1 trang ghi nhớ: bảng role (Ngày 3), bảng effect (Ngày 14), bảng Entra ID Free (Ngày 2), các loại lock (Ngày 15).

Đáp án tham khảo bài tự dựng:

```bash
# Prep: variables
LOC=southeastasia
RG=rg-lab-d16
SUB=$(az account show --query id -o tsv)
SCOPE=/subscriptions/$SUB/resourceGroups/$RG
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)

# Resource group with a tag
az group create -n $RG -l $LOC --tags env=lab owner=khoa

# User and group, user added to group
az ad user create --display-name "Lab Auditor" --user-principal-name lab-auditor@$DOMAIN \
  --password '<mat-khau-tam>' --force-change-password-next-sign-in true
az ad group create --display-name Auditors --mail-nickname auditors
az ad group member add --group Auditors \
  --member-id $(az ad user show --id lab-auditor@$DOMAIN --query id -o tsv)

# Reader for the group (not the user) on the resource group
az role assignment create --assignee $(az ad group show --group Auditors --query id -o tsv) \
  --assignee-principal-type Group --role "Reader" --scope $SCOPE

# Allowed locations at the resource group
POLICY=$(az policy definition list --query "[?displayName=='Allowed locations'].name" -o tsv)
az policy assignment create --name allowed-locs --scope $SCOPE --policy $POLICY \
  --params '{"listOfAllowedLocations":{"value":["southeastasia"]}}'

# CanNotDelete lock
az lock create --name no-delete --lock-type CanNotDelete --resource-group $RG

# Verify as lab-auditor in a separate profile (sees the RG, cannot change tags)
export AZURE_CONFIG_DIR=$HOME/.azure-labauditor
az login
az group list -o table
az group update -n rg-lab-d16 --tags env=changed
unset AZURE_CONFIG_DIR

# Cleanup: lock first, then RG, user, group
az lock delete --name no-delete --resource-group $RG
az group delete -n $RG --yes --no-wait
az ad user delete --id lab-auditor@$DOMAIN
az ad group delete --group Auditors
```

**Tự kiểm tra nhanh:**

- [ ] Phân biệt được Owner, Contributor, User Access Administrator
- [ ] Viết được custom role JSON không cần xem mẫu
- [ ] Nói được effect nào cần managed identity
- [ ] Biết ReadOnly lock chặn những thao tác nào
- [ ] Biết tính năng Entra nào cần P1, tính năng nào cần P2

## Ngày 17 — Availability set, zone, VMSS

**Mục tiêu:** đặt VM vào *availability set* và *availability zone*; tạo *VMSS* có autoscale theo CPU và thấy nó scale out.

**Dùng lại:** Ngày 4 (VM, run-command), Ngày 5 (VNet), Ngày 10 (load balancer, health probe).

**Các bước lab:**

1. Kiểm tra quota vCPU (free trial thường giới hạn khoảng 4 vCPU mỗi region, nên làm tuần tự và xóa VM giữa các bước).
2. Tạo availability set 2 FD/5 UD, đặt 2 VM vào, xem FD/UD của từng VM.
3. Tạo VM thứ 3 ngoài set, thử đưa vào set (thất bại); xóa 3 VM.
4. Xem zone khả dụng, tạo 2 VM ở zone 1 và zone 2; xóa.
5. Tạo VMSS 2 instance (CLI tạo kèm load balancer), scale thủ công, cấu hình autoscale 1–4 theo CPU.
6. Chạy tải CPU trên instance, theo dõi scale out.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d17
az group create -n $RG -l $LOC

# Step 1: Regional vCPU quota and current usage
az vm list-usage -l $LOC --query "[?contains(name.value,'cores')].{name:name.localizedValue, used:currentValue, limit:limit}" -o table

# Step 2: Availability set
az vm availability-set create -g $RG -n avset1 \
  --platform-fault-domain-count 2 --platform-update-domain-count 5

# Step 2: Two VMs in the set
for VM in vm1 vm2; do
  az vm create -g $RG -n $VM --image Ubuntu2204 --size Standard_B1s \
    --admin-username azureuser --generate-ssh-keys \
    --availability-set avset1 --public-ip-address ""
done

# Step 2: Fault/update domain of each VM
for VM in vm1 vm2; do
  az vm get-instance-view -g $RG -n $VM \
    --query "{vm:name, fd:instanceView.platformFaultDomain, ud:instanceView.platformUpdateDomain}"
done

# Step 3: A VM created outside the set
az vm create -g $RG -n vm3 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --public-ip-address ""

# Step 3: Try to put it into the set - fails (must recreate the VM)
AVSET_ID=$(az vm availability-set show -g $RG -n avset1 --query id -o tsv)
az vm update -g $RG -n vm3 --set availabilitySet.id=$AVSET_ID

# Step 3: Free the quota
az vm delete -g $RG --ids $(az vm list -g $RG --query "[].id" -o tsv) --yes

# Step 4: Zones offered for B1s in this region
az vm list-skus -l $LOC --size Standard_B1s --zone \
  --query "[].locationInfo[].zones" -o tsv

# Step 4: One VM per zone, then delete them
az vm create -g $RG -n vm-z1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --zone 1 --public-ip-address ""
az vm create -g $RG -n vm-z2 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --zone 2 --public-ip-address ""
az vm list -g $RG --query "[].{name:name, zone:zones[0]}" -o table
az vm delete -g $RG --ids $(az vm list -g $RG --query "[].id" -o tsv) --yes

# Step 5: VMSS with 2 instances (creates its own VNet and load balancer)
az vmss create -g $RG -n vmss1 --image Ubuntu2204 --vm-sku Standard_B1s \
  --instance-count 2 --admin-username azureuser --generate-ssh-keys \
  --orchestration-mode Uniform --upgrade-policy-mode Automatic
az vmss list-instances -g $RG -n vmss1 -o table

# Step 5: Manual scale to 3, then back to 2
az vmss scale -g $RG -n vmss1 --new-capacity 3
az vmss scale -g $RG -n vmss1 --new-capacity 2

# Step 5: Autoscale profile 1-4 instances
az monitor autoscale create -g $RG --resource vmss1 \
  --resource-type Microsoft.Compute/virtualMachineScaleSets \
  --name as-vmss1 --min-count 1 --max-count 4 --count 2

# Step 5: Scale-out and scale-in rules
az monitor autoscale rule create -g $RG --autoscale-name as-vmss1 \
  --condition "Percentage CPU > 70 avg 5m" --scale out 1
az monitor autoscale rule create -g $RG --autoscale-name as-vmss1 \
  --condition "Percentage CPU < 30 avg 5m" --scale in 1

# Step 6: Load the CPU on every instance for 15 minutes
for ID in $(az vmss list-instances -g $RG -n vmss1 --query "[].instanceId" -o tsv); do
  az vmss run-command invoke -g $RG -n vmss1 --instance-id $ID \
    --command-id RunShellScript \
    --scripts "apt-get -y install stress-ng >/dev/null && nohup stress-ng --cpu 1 --timeout 900 >/dev/null 2>&1 &"
done

# Step 6: After ~10 minutes the instance count grows
az vmss list-instances -g $RG -n vmss1 -o table
az monitor autoscale show -g $RG -n as-vmss1 --query "{min:profiles[0].capacity.minimum, max:profiles[0].capacity.maximum}"

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- Availability set: tối đa 3 FD, 20 UD, SLA 99,95%. Availability zone: SLA 99,99%.
- Không thêm VM có sẵn vào availability set; phải tạo lại VM.
- VMSS Uniform: các instance giống nhau, scale theo rule; Flexible: trộn được nhiều size, gần giống VM đơn lẻ.
- Autoscale cần cả rule scale out lẫn scale in; nếu chỉ có scale out thì số instance không giảm.
- Move VM sang region khác: Azure Resource Mover hoặc Site Recovery.

**Tự kiểm tra:** VM cần SLA 99,99% thì triển khai thế nào? VMSS đang 4 instance, CPU còn 10% nhưng không giảm, nguyên nhân hay gặp là gì?

## Ngày 18 — App Service

**Mục tiêu:** deploy API .NET 8 lên App Service; thấy tier quyết định tính năng (slot, autoscale, backup); làm slot swap với slot setting.

**Dùng lại:** Ngày 7 (storage), Ngày 8 (SAS bằng account key), Ngày 17 (autoscale).

**Các bước lab:**

1. Tạo API mẫu `LabApi` bằng `dotnet new webapi`, đóng gói zip.
2. Tạo plan B1 Linux, web app .NET 8, deploy zip, gọi `/weatherforecast`.
3. Thử tạo deployment slot trên B1 (bị chặn); scale up lên S1 rồi tạo slot `staging`.
4. Đặt 1 setting thường và 1 *slot setting* ở staging; swap; xem setting nào đi theo.
5. Scale out thủ công lên 2, sau đó cấu hình autoscale cho plan.
6. Backup app vào blob container bằng SAS URL.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d18
APP=khoalab-api-$RANDOM
SA=stkhoalab$RANDOM
EXP=$(date -u -d "+7 days" '+%Y-%m-%dT%H:%MZ')     # Linux
# EXP=$(date -u -v+7d '+%Y-%m-%dT%H:%MZ')          # macOS
az group create -n $RG -l $LOC

# Step 1: Sample .NET 8 API and a zip package
dotnet new webapi -n LabApi -o LabApi
dotnet publish LabApi -c Release -o publish
(cd publish && zip -r ../app.zip .)

# Step 2: Basic Linux plan and web app
az appservice plan create -g $RG -n plan1 --sku B1 --is-linux
az webapp create -g $RG -p plan1 -n $APP --runtime "DOTNETCORE:8.0"

# Step 2: Deploy and call the API
az webapp deploy -g $RG -n $APP --src-path app.zip --type zip
curl https://$APP.azurewebsites.net/weatherforecast

# Step 3: Slots are not available on Basic - expect an error
az webapp deployment slot create -g $RG -n $APP --slot staging

# Step 3: Scale up to Standard S1, then create the slot
az appservice plan update -g $RG -n plan1 --sku S1
az webapp deployment slot create -g $RG -n $APP --slot staging
az webapp deploy -g $RG -n $APP --slot staging --src-path app.zip --type zip

# Step 4: Normal setting (moves with swap) and slot setting (sticky)
az webapp config appsettings set -g $RG -n $APP --slot staging --settings FEATURE=new
az webapp config appsettings set -g $RG -n $APP --slot staging --slot-settings ENV=staging

# Step 4: Swap staging into production
az webapp deployment slot swap -g $RG -n $APP --slot staging --target-slot production

# Step 4: Production now has FEATURE=new but not ENV=staging
az webapp config appsettings list -g $RG -n $APP --query "[].{name:name, value:value}" -o table

# Step 5: Scale out manually to 2 instances
az appservice plan update -g $RG -n plan1 --number-of-workers 2

# Step 5: Autoscale for the plan 1-3 instances on CPU
az monitor autoscale create -g $RG --resource plan1 \
  --resource-type Microsoft.Web/serverfarms --name as-plan1 \
  --min-count 1 --max-count 3 --count 1
az monitor autoscale rule create -g $RG --autoscale-name as-plan1 \
  --condition "CpuPercentage > 70 avg 5m" --scale out 1
az monitor autoscale rule create -g $RG --autoscale-name as-plan1 \
  --condition "CpuPercentage < 30 avg 5m" --scale in 1

# Step 6: Storage container and a write SAS for backups
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2
KEY=$(az storage account keys list -g $RG -n $SA --query "[0].value" -o tsv)
az storage container create --account-name $SA -n backups --account-key $KEY
BK_SAS=$(az storage container generate-sas --account-name $SA -n backups \
  --permissions rwdl --expiry $EXP --account-key $KEY -o tsv)

# Step 6: Run a backup and list it
az webapp config backup create -g $RG --webapp-name $APP --backup-name bk1 \
  --container-url "https://$SA.blob.core.windows.net/backups?$BK_SAS"
az webapp config backup list -g $RG --webapp-name $APP -o table

# Cleanup
az group delete -n $RG --yes --no-wait
```

| Tier | Custom domain | Slot | Autoscale |
| --- | --- | --- | --- |
| Free / Shared | Shared có | Không | Không |
| Basic | Có | Không | Không (scale thủ công) |
| Standard | Có | 5 | Có |
| Premium | Có | 20 | Có |

**Điểm hay thi:**

- App trong cùng plan dùng chung tài nguyên; scale là scale cả plan.
- Slot setting ở lại slot khi swap; setting thường đi theo code.
- Cần slot hoặc autoscale mà đang Basic → lên Standard (đáp án minimize cost).
- Custom domain cần record CNAME/TXT xác minh; TLS binding cần Basic trở lên.

**Tự kiểm tra:** Connection string của staging trỏ DB test, sau khi swap production có bị trỏ nhầm DB test không? Phụ thuộc vào cấu hình nào?

## Ngày 19 — ACR, ACI, Container Apps

**Mục tiêu:** đóng gói `LabApi` thành image, đẩy lên *ACR*, chạy bằng *ACI* và *Container Apps*; thấy khác biệt về scale.

**Dùng lại:** Ngày 18 (`LabApi`, `dotnet publish`).

**Các bước lab:**

1. Tạo lại `LabApi` nếu chưa có; viết Dockerfile multi-stage.
2. Tạo ACR Basic; build image trên ACR (hoặc build local rồi push nếu ACR Tasks bị chặn trên free trial).
3. Chạy image bằng ACI với restart policy OnFailure; xem log; gọi API.
4. Deploy cùng image lên Container Apps có ingress external; đặt scale 0–3 theo HTTP.

```bash
# Prep: variables, providers and resource group
LOC=southeastasia
RG=rg-lab-d19
ACR=khoalabacr$RANDOM
az provider register --namespace Microsoft.ContainerInstance
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
az group create -n $RG -l $LOC

# Step 1: Sample API (skip if LabApi from Day 18 still exists)
dotnet new webapi -n LabApi -o LabApi

# Step 1: Multi-stage Dockerfile (.NET 8 images listen on 8080)
cat > LabApi/Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "LabApi.dll"]
EOF

# Step 2: Create the registry with the admin user enabled
az acr create -g $RG -n $ACR --sku Basic --admin-enabled true

# Step 2: Build in the cloud with ACR Tasks
az acr build -r $ACR -t labapi:v1 LabApi

# Step 2: Fallback if ACR Tasks is blocked - build locally and push
az acr login -n $ACR
docker build -t $ACR.azurecr.io/labapi:v1 LabApi
docker push $ACR.azurecr.io/labapi:v1

# Step 2: Check the repository and tag
az acr repository show-tags -n $ACR --repository labapi -o table

# Step 2: Registry credentials for ACI/Container Apps
ACR_USER=$(az acr credential show -n $ACR --query username -o tsv)
ACR_PASS=$(az acr credential show -n $ACR --query "passwords[0].value" -o tsv)

# Step 3: Run the image on ACI
az container create -g $RG -n aci-api --image $ACR.azurecr.io/labapi:v1 \
  --os-type Linux --cpu 1 --memory 1.5 --ports 8080 --ip-address Public \
  --dns-name-label aci-$ACR \
  --registry-login-server $ACR.azurecr.io \
  --registry-username $ACR_USER --registry-password $ACR_PASS \
  --restart-policy OnFailure

# Step 3: State, logs, call the API
az container show -g $RG -n aci-api --query "{state:instanceView.state, fqdn:ipAddress.fqdn}"
az container logs -g $RG -n aci-api
curl http://aci-$ACR.$LOC.azurecontainer.io:8080/weatherforecast

# Step 3: Delete the ACI container
az container delete -g $RG -n aci-api --yes

# Step 4: Container Apps CLI extension
az extension add --name containerapp --upgrade

# Step 4: Create environment + app in one command
az containerapp up -n ca-api -g $RG -l $LOC --image $ACR.azurecr.io/labapi:v1 \
  --registry-server $ACR.azurecr.io --registry-username $ACR_USER \
  --registry-password $ACR_PASS --ingress external --target-port 8080

# Step 4: Scale to zero when idle, up to 3 replicas on HTTP load
az containerapp update -n ca-api -g $RG --min-replicas 0 --max-replicas 3 \
  --scale-rule-name http --scale-rule-type http --scale-rule-http-concurrency 10

# Step 4: Call the app
FQDN=$(az containerapp show -n ca-api -g $RG --query properties.configuration.ingress.fqdn -o tsv)
curl https://$FQDN/weatherforecast

# Cleanup
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- ACR SKU: Basic, Standard, Premium. Geo-replication và private endpoint chỉ có ở Premium.
- ACI restart policy: Always (mặc định), OnFailure, Never. Container group chạy chung host, chung IP.
- Container Apps scale theo HTTP/event, có scale to zero; ACI không tự scale.
- Thay admin user bằng managed identity + role *AcrPull* là cách khuyến nghị cho production.

**Tự kiểm tra:** Job batch chạy một lần rồi dừng, không muốn khởi động lại khi thành công, dùng restart policy nào? Cần scale về 0 khi không có request thì chọn ACI hay Container Apps?

## Ngày 20 — Log Analytics, alert, KQL

**Mục tiêu:** thu metric/log của VM về *Log Analytics* bằng *Azure Monitor Agent* + *DCR*; đẩy Activity log về workspace; tạo metric alert gửi email; viết KQL cơ bản.

**Dùng lại:** Ngày 4 (VM, extension, run-command), Ngày 14 (managed identity).

**Các bước lab:**

1. Tạo Log Analytics workspace.
2. Tạo VM có system-assigned managed identity; cài Azure Monitor Agent.
3. Tạo DCR thu CPU/memory gửi về workspace; gắn DCR vào VM.
4. Đẩy Activity log của subscription về workspace.
5. Tạo action group email và metric alert CPU > 80%; tạo tải CPU để alert bắn.
6. Chạy 4 câu KQL từ CLI.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d20
az group create -n $RG -l $LOC

# Step 1: Workspace
az monitor log-analytics workspace create -g $RG -n law-lab -l $LOC
LAW_ID=$(az monitor log-analytics workspace show -g $RG -n law-lab --query id -o tsv)
LAW_GUID=$(az monitor log-analytics workspace show -g $RG -n law-lab --query customerId -o tsv)

# Step 2: VM with a system-assigned managed identity
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys --assign-identity
VM_ID=$(az vm show -g $RG -n vm1 --query id -o tsv)

# Step 2: Azure Monitor Agent
az vm extension set -g $RG --vm-name vm1 --name AzureMonitorLinuxAgent \
  --publisher Microsoft.Azure.Monitor --enable-auto-upgrade true
```

File `dcr.json` (thay `<LAW_ID>` bằng giá trị của `$LAW_ID`):

```json
{
  "location": "southeastasia",
  "properties": {
    "dataSources": {
      "performanceCounters": [{
        "name": "perf",
        "streams": ["Microsoft-Perf"],
        "samplingFrequencyInSeconds": 60,
        "counterSpecifiers": ["Processor(*)\\% Processor Time", "Memory(*)\\Available MBytes Memory"]
      }]
    },
    "destinations": {
      "logAnalytics": [{ "name": "law", "workspaceResourceId": "<LAW_ID>" }]
    },
    "dataFlows": [{ "streams": ["Microsoft-Perf"], "destinations": ["law"] }]
  }
}
```

```bash
# Step 3: Put the workspace id into the file
sed -i.bak "s|<LAW_ID>|$LAW_ID|" dcr.json

# Step 3: Create the DCR and associate it with the VM
az monitor data-collection rule create -g $RG -n dcr-perf -l $LOC --rule-file dcr.json
DCR_ID=$(az monitor data-collection rule show -g $RG -n dcr-perf --query id -o tsv)
az monitor data-collection rule association create --name dcra-vm1 \
  --rule-id $DCR_ID --resource $VM_ID

# Step 4: Send the subscription Activity log to the workspace
az monitor diagnostic-settings subscription create --name act-to-law \
  --location $LOC --workspace $LAW_ID \
  --logs '[{"category":"Administrative","enabled":true}]'

# Step 5: Action group that emails you
az monitor action-group create -g $RG -n ag-mail --short-name agmail \
  --action email khoa <email-cua-ban>

# Step 5: Metric alert CPU > 80% over 5 minutes
az monitor metrics alert create -g $RG -n cpu-high --scopes $VM_ID \
  --condition "avg Percentage CPU > 80" --window-size 5m \
  --evaluation-frequency 1m --action ag-mail

# Step 5: Burn CPU for 15 minutes to fire the alert
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "apt-get -y install stress-ng >/dev/null && nohup stress-ng --cpu 1 --timeout 900 >/dev/null 2>&1 &"

# Step 6 (after ~10 min): VMs that stopped sending heartbeat
az monitor log-analytics query -w $LAW_GUID --analytics-query \
  "Heartbeat | summarize LastSeen = max(TimeGenerated) by Computer | where LastSeen < ago(5m)" -o table

# Step 6: Average CPU per 5 minutes
az monitor log-analytics query -w $LAW_GUID --analytics-query \
  "Perf | where ObjectName == 'Processor' and CounterName == '% Processor Time' | summarize avg(CounterValue) by Computer, bin(TimeGenerated, 5m) | order by TimeGenerated desc" -o table

# Step 6: Who deleted or changed what
az monitor log-analytics query -w $LAW_GUID --analytics-query \
  "AzureActivity | where OperationNameValue has 'write' or OperationNameValue has 'delete' | project TimeGenerated, Caller, ResourceGroup, OperationNameValue | order by TimeGenerated desc | take 20" -o table

# Step 6: Lowest free memory per VM in the last hour
az monitor log-analytics query -w $LAW_GUID --analytics-query \
  "Perf | where TimeGenerated > ago(1h) and CounterName == 'Available MBytes Memory' | summarize min(CounterValue) by Computer" -o table

# Cleanup: the subscription diagnostic setting is NOT deleted with the RG
az monitor diagnostic-settings subscription delete --name act-to-law --yes
az group delete -n $RG --yes --no-wait
```

**Điểm hay thi:**

- Alert gồm scope, condition, action group (email, SMS, webhook, Logic App, Function, runbook).
- Activity log giữ 90 ngày; muốn lâu hơn phải đẩy qua diagnostic settings.
- Azure Monitor Agent cần Data Collection Rule để biết thu gì và gửi đi đâu; agent cũ (Log Analytics agent) đã ngừng hỗ trợ.
- KQL: `where` trước, rồi `summarize`/`project`, cuối cùng `render`/`order by`.

**Tự kiểm tra:** Cần biết ai đã xóa một VM tuần trước, xem ở đâu? Cần giữ thông tin đó 1 năm thì cấu hình gì?

## Ngày 21 — Backup và Site Recovery

**Mục tiêu:** backup VM vào *Recovery Services vault*, khôi phục từng file và khôi phục thành VM mới; biết vault nào backup được gì; hiểu Site Recovery ở mức khái niệm.

**Dùng lại:** Ngày 4 (VM, run-command, SSH), Ngày 7 (storage account làm staging khi restore).

**Các bước lab:**

1. Tạo VM, ghi một file quan trọng vào VM.
2. Tạo vault cùng region, **tắt soft delete** để cuối ngày xóa được resource group.
3. Bật backup với DefaultPolicy, chạy backup ngay, chờ job xong (lần đầu có thể mất 30–60 phút).
4. Xóa file trong VM; khôi phục file bằng *File Recovery* (mount recovery point).
5. Khôi phục thành VM mới `vm1-restored`.
6. Dừng bảo vệ, xóa dữ liệu backup, xóa resource group.

```bash
# Prep: variables and resource group
LOC=southeastasia
RG=rg-lab-d21
SA=stkhoalab$RANDOM
RETAIN=$(date -u -d "+30 days" '+%d-%m-%Y')      # Linux
# RETAIN=$(date -u -v+30d '+%d-%m-%Y')            # macOS
az group create -n $RG -l $LOC

# Step 1: VM and an important file
az vm create -g $RG -n vm1 --image Ubuntu2204 --size Standard_B1s \
  --admin-username azureuser --generate-ssh-keys
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "echo 'quan trong' > /home/azureuser/important.txt"

# Step 2: Vault in the same region, soft delete off for the lab
az backup vault create -g $RG -n rsv-lab -l $LOC
az backup vault backup-properties set -g $RG -n rsv-lab --soft-delete-feature-state Disable

# Step 3: Protect the VM with the default policy
az backup protection enable-for-vm -g $RG --vault-name rsv-lab --vm vm1 \
  --policy-name DefaultPolicy

# Step 3: On-demand backup
az backup protection backup-now -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --retain-until $RETAIN

# Step 3: Watch the job, then wait for it
az backup job list -g $RG --vault-name rsv-lab -o table
JOB=$(az backup job list -g $RG --vault-name rsv-lab --query "[0].name" -o tsv)
az backup job wait -g $RG --vault-name rsv-lab -n $JOB

# Step 3: Latest recovery point
RP=$(az backup recoverypoint list -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --query "[0].name" -o tsv)

# Step 4: Lose the file
az vm run-command invoke -g $RG -n vm1 --command-id RunShellScript \
  --scripts "rm /home/azureuser/important.txt"

# Step 4: Download the File Recovery script for that recovery point
az backup restore files mount-rp -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --rp-name $RP

# Step 4: Copy the script to the VM and run it (follow its prompts, note the mount path)
IP=$(az vm show -d -g $RG -n vm1 --query publicIps -o tsv)
scp ./*.py azureuser@$IP:~/
ssh azureuser@$IP "sudo python3 ~/*.py"

# Step 4: Copy important.txt back from the mounted volume, then unmount
az backup restore files unmount-rp -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --rp-name $RP

# Step 5: Staging storage account for the restore
az storage account create -g $RG -n $SA --sku Standard_LRS --kind StorageV2

# Step 5: Restore as a new VM in the same VNet/subnet (names from az vm create defaults)
az backup restore restore-disks -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --rp-name $RP --storage-account $SA \
  --target-resource-group $RG --target-vm-name vm1-restored \
  --target-vnet-name vm1VNET --target-vnet-resource-group $RG \
  --target-subnet-name vm1Subnet

# Step 5: Wait for the restore job, then check the new VM
JOB=$(az backup job list -g $RG --vault-name rsv-lab --query "[0].name" -o tsv)
az backup job wait -g $RG --vault-name rsv-lab -n $JOB
az vm list -g $RG -o table

# Step 6: Stop protection and delete backup data
az backup protection disable -g $RG --vault-name rsv-lab \
  --container-name vm1 --item-name vm1 --backup-management-type AzureIaasVM \
  --delete-backup-data true --yes

# Cleanup
az group delete -n $RG --yes --no-wait
```

| Vault | Backup được |
| --- | --- |
| Recovery Services vault | Azure VM, SQL/SAP HANA trong VM, Azure Files, máy on-prem qua MARS agent |
| Backup vault | Azure Disks, Azure Blobs, Azure Database for PostgreSQL, AKS |

**Điểm hay thi:**

- Vault phải cùng region với VM.
- Restore VM: tạo VM mới, restore disk, hoặc replace existing. Restore từng file dùng File Recovery (mount recovery point).
- Xóa vault: dừng protection, xóa backup data, chờ hết soft delete 14 ngày (hoặc đã tắt trước).
- Site Recovery dùng cho DR: replicate VM sang region khác, test failover, failover, failback. Backup dùng cho khôi phục dữ liệu theo thời điểm.

**Tự kiểm tra:** Cần chuyển toàn bộ VM sang region khác trong vài phút khi region chính sập, chọn Backup hay Site Recovery? Vì sao xóa resource group có vault đôi khi thất bại?

## Ngày 22 — Mock exam 1

**Mục tiêu:** làm thử trong điều kiện giống thi thật và tìm ra domain yếu. Không có lab, không có lệnh mới.

- [ ] Làm full mock exam, bấm giờ 120 phút, có ít nhất một case study
- [ ] Tập mở Microsoft Learn trong lúc làm, chỉ để tra con số hoặc giới hạn
- [ ] Ghi câu sai theo domain và lý do sai: không biết, hay đọc sót ràng buộc
- [ ] Chọn 2–3 chủ đề sai nhiều nhất cho ngày 23, ghi kèm số ngày tương ứng trong guide

## Ngày 23 — Lab lại và mock exam 2

**Mục tiêu:** sửa điểm yếu bằng tay, không chỉ đọc lại. Lệnh dùng lại nguyên khối của ngày tương ứng; mỗi ngày đã tự đủ lệnh nên chạy lại độc lập được.

- [ ] Lab lại 2–3 chủ đề yếu bằng đúng khối lệnh của ngày đó (bắt đầu từ Prep, kết thúc bằng Cleanup)
- [ ] Mock exam 2, mục tiêu trên 80%
- [ ] So sánh với mock 1: domain nào đã cải thiện, domain nào chưa

## Ngày 24 — Ôn tổng hợp và dọn subscription

**Mục tiêu:** không lab nữa; đọc lại bảng tổng hợp, dọn sạch những thứ không nằm trong resource group, nghỉ sớm.

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
| User bị xóa khôi phục được trong | 30 ngày |

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

### Dọn những thứ không nằm trong resource group

```bash
# Prep: variables
SUB=$(az account show --query id -o tsv)
DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains \
  --query "value[?isDefault].id" -o tsv)

# Any lab resource groups left
~/cleanup.sh

# Custom role from Day 3
az role definition delete --name "VM Operator"

# Subscription-level diagnostic settings (Day 20)
az monitor diagnostic-settings subscription list -o table

# Management groups (Day 15)
az account management-group list -o table

# Policy assignments or initiatives left at subscription scope (Day 14)
az policy assignment list --scope /subscriptions/$SUB --query "[].name" -o tsv
az policy set-definition list --query "[?policyType=='Custom'].name" -o tsv

# Test users and their CLI profiles (after the exam)
for U in lab-reader lab-ops; do az ad user delete --id $U@$DOMAIN; done
rm -rf ~/.azure-labreader ~/.azure-labops ~/.azure-labauditor

# Budget stays - remove it only when you stop using the subscription
az rest --method delete \
  --url "https://management.azure.com/subscriptions/$SUB/providers/Microsoft.Consumption/budgets/lab-200?api-version=2023-05-01"
```

### Checklist

- [ ] Đọc lại các bảng: Entra ID Free (Ngày 2), role (Ngày 3), redundancy (Ngày 7), SAS (Ngày 8), cân bằng tải (Ngày 10), Network Watcher (Ngày 11), effect (Ngày 14), App Service tier (Ngày 18), vault (Ngày 21)
- [ ] Chạy khối dọn dẹp ở trên, kiểm tra `az resource list -o table` trống
- [ ] Kiểm tra lịch thi, giấy tờ tùy thân, phòng thi hoặc máy thi online
- [ ] Trong phòng thi: đọc dòng "You need to…" trước, gạch chân ràng buộc; case study đọc câu hỏi rồi mới mở tài liệu
