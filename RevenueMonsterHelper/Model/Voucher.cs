namespace RevenueMonsterLibrary.Model;

public class IssueVoucher
{
    public string code { get; set; }
    public string qrUrl { get; set; }
}

public class IssueVoucherResult : ApiResponse<IssueVoucher>
{
}

public class VoidVoucherResult : ApiResponse<Voucher>
{
}

public class GetVoucherByCodeResult : ApiResponse<Voucher>
{
}

public class GetVoucherBatchesResult : ApiListResponse<Voucher>
{
}

public class GetVoucherBatchByKeyResult : ApiResponse<Voucher>
{
}

public class Voucher
{
    public Address address { get; set; }
    public long amount { get; set; }
    public string assignedAt { get; set; }
    public long balanceQuantity { get; set; }
    public string code { get; set; }
    public string createdAt { get; set; }
    public long discountRate { get; set; }
    public Expiry expiry { get; set; }
    public string id { get; set; }
    public string imageUrl { get; set; }
    public bool isDeviceRedeem { get; set; }
    public bool isShipping { get; set; }
    public string key { get; set; }
    public string label { get; set; }
    public string memberProfile { get; set; }
    public long minimumSpendAmount { get; set; }
    public string origin { get; set; }
    public string payload { get; set; }
    public string qrUrl { get; set; }
    public long quantity { get; set; }
    public string redeemedAt { get; set; }
    public string redemptionRuleKey { get; set; }
    public string status { get; set; }
    public string type { get; set; }
    public string updatedAt { get; set; }
    public string usedAt { get; set; }
    public long usedQuantity { get; set; }
    public string voucherBatchKey { get; set; }
}