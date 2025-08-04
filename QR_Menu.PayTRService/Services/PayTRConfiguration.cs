namespace QR_Menu.PayTRService.Services
{
    
    public class PayTRConfiguration
    {
        
        public string BaseUrl { get; set; } = "https://www.paytr.com/";
      
        public string PayEndpoint { get; set; } = "odeme";
       
        public string CreateLinkEndpoint { get; set; } = "odeme/api/link/create";
    
       
        public string DeleteLinkEndpoint { get; set; } = "odeme/api/link/delete";
     
   
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
      

        public int MaxRetryAttempts { get; set; } = 3;
        
       
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    

        public int MerchantId { get; set; }
        
       
        public string MerchantKey { get; set; } = string.Empty;
        
        public string MerchantSalt { get; set; } = string.Empty;
       
        public bool TestMode { get; set; } = true;
        
        public string? SuccessUrl { get; set; }
        
       
        public string? FailUrl { get; set; }
        
       
        public string? CallbackUrl { get; set; }
       
        public bool DebugMode { get; set; } = false;
      
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(BaseUrl) &&
                   !string.IsNullOrEmpty(MerchantKey) &&
                   !string.IsNullOrEmpty(MerchantSalt) &&
                   MerchantId > 0;
        }
        
      
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrEmpty(BaseUrl))
                errors.Add("BaseUrl is required");
                
            if (string.IsNullOrEmpty(MerchantKey))
                errors.Add("MerchantKey is required");
                
            if (string.IsNullOrEmpty(MerchantSalt))
                errors.Add("MerchantSalt is required");
                
            if (MerchantId <= 0)
                errors.Add("MerchantId must be greater than 0");
                
            return errors;
        }
    }
} 