using API_Server.Services;
using NetStudy.Services;

public class HybridEncryptionService
{
    private readonly RsaService _rsaService;
    private readonly AesService _aesService;

    public HybridEncryptionService(RsaService rsaService, AesService aesService)
    {
        _rsaService = rsaService;
        _aesService = aesService;
    }

    
}
