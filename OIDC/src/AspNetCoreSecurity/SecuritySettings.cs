namespace PwC.MTS.Contract.Common
{
    public class SecuritySettings
    {

        // Main Security Settings
        public string SecurityPolicy { get; set; }

        public string SecurityPolicyOrigin { get; set; }
        public string SecurityAuthority { get; set; }
        public string SecurityAudience { get; set; }
        public string SecurityScope { get; set; }
        public string SecurityMetadataAddress { get; set; }
        public bool SecurityRequireHttpMetadata { get; set; }
        public string ClientId { get; set; }
        public string LoadBalanceUrl { get; set; }
        public string RedirectUri { get; set; }
        public bool EnableCorrelationCookieBuilder { get; set; }

        public SecuritySettings()
        {
            // Default Security Settings
            SecurityPolicy = "default";
            SecurityPolicyOrigin = "http://localhost:8080";
            SecurityAuthority = "https://qamobility.pwcinternal.com:4430/";
            SecurityAudience = "https://qamobility.pwcinternal.com:4430/resources";
            SecurityScope = "ia_api";
            SecurityRequireHttpMetadata = false;
        }
    }
}
