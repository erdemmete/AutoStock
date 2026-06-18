public enum AuditActionType
{
    Create = 1,
    Update = 2,
    SetPassive = 3,
    SetActive = 4,

    Complete = 10,
    Cancel = 11,
    Reopen = 12,

    Add = 20,
    Remove = 21,

    Issue = 30,
    ReceivePayment = 31,

    StockIn = 40,
    StockOut = 41,
    StockAdjustment = 42,

    LoginSuccess = 50,
    LoginFailed = 51,
    Logout = 52,

    Retire = 70,
    Revoke = 71
}
