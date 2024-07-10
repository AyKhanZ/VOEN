using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication2;

public class TwoCaptchaSolver
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TwoCaptchaSolver(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<string> SolveCaptcha(string siteKey, string url)
    {
        var parameters = new Dictionary<string, string>
        {
            { "key", _apiKey },
            { "method", "userrecaptcha" },
            { "googlekey", siteKey },
            { "pageurl", url },
            { "json", "1" }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync("http://2captcha.com/in.php", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (responseString.Contains("OK|"))
        {
            var captchaId = responseString.Split('|')[1];
            var captchaResult = await PollForResult(captchaId);
            return captchaResult;
        }
        else
        {
            throw new Exception("Failed to submit captcha to 2Captcha");
        }
    }

    private async Task<string> PollForResult(string captchaId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "key", _apiKey },
            { "action", "get" },
            { "id", captchaId },
            { "json", "1" }
        };

        var response = await _httpClient.GetAsync($"http://2captcha.com/res.php?key={_apiKey}&action=get&id={captchaId}&json=1");
        var responseString = await response.Content.ReadAsStringAsync();

        if (responseString.Contains("OK|"))
        {
            return responseString.Split('|')[1];
        }
        else if (responseString.Contains("CAPCHA_NOT_READY"))
        {
            await Task.Delay(5000); // Подождать 5 секунд и повторить запрос
            return await PollForResult(captchaId);
        }
        else
        {
            throw new Exception("Failed to solve captcha with 2Captcha");
        }
    }
}