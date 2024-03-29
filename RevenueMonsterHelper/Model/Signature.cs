﻿namespace RevenueMonsterLibrary.Model;

public class GenerateSignatureResult
{
    public string data { get; set; }
    public Error error { get; set; }
    public string sequenceData { get; set; }
    public string signature { get; set; }
}

public class VerifySignatureResult
{
    public Error error { get; set; }
    public bool isValid { get; set; }
}

public class GenerateSignatureRequestData
{
    public object data { get; set; }
    public string method { get; set; }
    public string nonceStr { get; set; }
    public string privateKey { get; set; }
    public string requestUrl { get; set; }
    public string signType { get; set; }
    public string timestamp { get; set; }
}

public class VerifySignatureRequestData
{
    public object data { get; set; }
    public string method { get; set; }
    public string nonceStr { get; set; }
    public string publicKey { get; set; }
    public string requestUrl { get; set; }
    public string signature { get; set; }
    public string signType { get; set; }
    public string timestamp { get; set; }
}