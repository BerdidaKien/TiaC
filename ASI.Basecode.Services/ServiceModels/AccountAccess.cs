namespace ASI.Basecode.Services.ServiceModels
{
    /// <summary>What a signed-in account is currently allowed to do</summary>
    public enum AccountAccess
    {
        /// <summary>Missing, inactive or disabled: must be signed out</summary>
        Blocked,

        /// <summary>Active, but must replace the temporary password first</summary>
        MustSetUp,

        /// <summary>Active and ready to use the system</summary>
        Allowed
    }
}
