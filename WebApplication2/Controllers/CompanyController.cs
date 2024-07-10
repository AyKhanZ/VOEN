using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebApplication2;
using HtmlAgilityPack;
using RestSharp;

namespace NewVoen.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class VoenController : ControllerBase
{
    //[HttpGet]
    //public async Task<IActionResult> GetVoenNew([FromHeader] string voen = "1805591121")
    //{
    //    VoenModel company = new();
    //    using (var client = new HttpClient())
    //    {
    //        string url = $"https://e-taxes.gov.az/ebyn/commersialChecker.jsp?name={voen}&tip=2&sub_mit=1";

    //        HttpResponseMessage response = await client.GetAsync(url);

    //        if (!response.IsSuccessStatusCode) return null!;

    //        var htmlContent = await response.Content.ReadAsStreamAsync();
    //        var doc = new HtmlDocument();
    //        doc.Load(htmlContent, System.Text.Encoding.UTF8);

    //        HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[@class='com']");

    //        if (table == null) return null!;

    //        foreach (HtmlNode row in table.SelectNodes(".//tr[position() > 1]"))
    //        {
    //            HtmlNodeCollection cells = row.SelectNodes(".//td");
    //            if (cells != null && cells.Count >= 10)
    //            {
    //                company.CompanyName = cells[0].InnerText.Trim();
    //                int startIndex = company.CompanyName.IndexOf('"');
    //                if (startIndex != -1)
    //                {
    //                    startIndex++;
    //                    int endIndex = company.CompanyName.IndexOf('"', startIndex);
    //                    if (endIndex != -1) company.CompanyName = company.CompanyName.Substring(startIndex, endIndex - startIndex).Trim();
    //                }
    //                company.Voen = cells[1].InnerText.Trim();
    //                company.LegalForm = cells[3].InnerText.Trim();
    //                company.LegalAddress = cells[4].InnerText.Trim();
    //                company.LegalRepresentative = cells[7].InnerText.Trim();
    //            }
    //        }
    //    }


    //    return Ok(company);
    //}

    private static readonly string ApiKey = "5eff7a0509edf4c26a2b174670242016";
    [HttpGet]
    public async Task<IActionResult> GetVoen([FromHeader] string voen = "1805591121")
    {
        VoenModel company = new();

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless"); // Запуск браузера в фоновом режиме

        using (var driver = new ChromeDriver(options))
        {
            string url = $"https://e-taxes.gov.az/ebyn/commersialChecker.jsp?name={voen}&tip=2&sub_mit=1";
            driver.Navigate().GoToUrl(url);

            // Найти элемент reCAPTCHA
            var captchaElement = driver.FindElement(By.XPath("//div[@class='g-recaptcha']"));
            if (captchaElement != null)
            {
                var siteKey = captchaElement.GetAttribute("data-sitekey");
                var pageUrl = driver.Url;

                // Решить капчу через 2Captcha
                var captchaSolution = await SolveCaptcha(siteKey, pageUrl);

                // Ввести капчу на сайте
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript($"document.getElementById('g-recaptcha-response').value='{captchaSolution}';");

                // Отправить форму
                driver.FindElement(By.XPath("//button[contains(text(), 'Проверить')]")).Click(); // Замените XPath на реальный путь кнопки отправки формы
            }

            // Подождать, пока страница загрузится
            await Task.Delay(5000);

            // Получить HTML-контент после решения капчи и загрузки страницы
            var pageSource = driver.PageSource;
            var doc = new HtmlDocument();
            doc.LoadHtml(pageSource);

            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[@class='com']");

            if (table == null) return null!;

            foreach (HtmlNode row in table.SelectNodes(".//tr[position() > 1]"))
            {
                HtmlNodeCollection cells = row.SelectNodes(".//td");
                if (cells != null && cells.Count >= 10)
                {
                    company.CompanyName = cells[0].InnerText.Trim();
                    int startIndex = company.CompanyName.IndexOf('"');
                    if (startIndex != -1)
                    {
                        startIndex++;
                        int endIndex = company.CompanyName.IndexOf('"', startIndex);
                        if (endIndex != -1) company.CompanyName = company.CompanyName.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                    company.Voen = cells[1].InnerText.Trim();
                    company.LegalForm = cells[3].InnerText.Trim();
                    company.LegalAddress = cells[4].InnerText.Trim();
                    company.LegalRepresentative = cells[7].InnerText.Trim();
                }
            }
        }

        return Ok(company);
    }

    private async Task<string> SolveCaptcha(string siteKey, string pageUrl)
    {
        var client = new RestClient("http://2captcha.com");
        var request = new RestRequest("in.php", Method.Post);
        request.AddParameter("key", ApiKey);
        request.AddParameter("method", "userrecaptcha");
        request.AddParameter("googlekey", siteKey);
        request.AddParameter("pageurl", pageUrl);

        var response = await client.ExecuteAsync(request);
        if (response.Content!.Contains("OK|"))
        {
            var captchaId = response.Content.Split('|')[1];
            // Ждем, пока капча не будет решена (может потребоваться некоторое время)
            await Task.Delay(15000); // Ждем 15 секунд, чтобы 2Captcha решил капчу

            // Проверяем статус решения капчи
            request = new RestRequest("res.php", Method.Get);
            request.AddParameter("key", ApiKey);
            request.AddParameter("action", "get");
            request.AddParameter("id", captchaId);

            var checkResponse = await client.ExecuteAsync(request);
            if (checkResponse.Content!.Contains("OK|"))
            {
                return checkResponse.Content.Split('|')[1];
            }
        }
        return null;
    }
}