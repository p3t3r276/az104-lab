namespace AzAdminKit.Shared;

public class Az
{
    public static readonly TokenCredential Cred = new DefaultAzureCredential();
    public static readonly ArmClient Arm = new(Cred);

    public static async Task<ResourceGroupResource> RgAsync(string day)
    {
        var sub = await Arm.GetDefaultSubscriptionAsync();
        var data = new ResourceGroupData(AzureLocation.SoutheastAsia) { Tags = { ["course"] = "az104", ["day"] = day }};
        return (await sub.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, $"rg-az104-{day}", data)).Value;
    }
}
