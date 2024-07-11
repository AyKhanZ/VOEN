using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using HtmlAgilityPack;
using OpenQA.Selenium.Edge;

namespace NewVoen.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class VoenController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVoen([FromHeader] string voen = "9900014901")
    {
        VoenModel company = new();
        using (var client = new HttpClient())
        {
            string url = $"https://e-taxes.gov.az/ebyn/commersialChecker.jsp?name={voen}&tip=2&sub_mit=1";

            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null!;

            var htmlContent = await response.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(htmlContent, System.Text.Encoding.UTF8);


            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[@class='com']");
            if (table == null) return null!;
             
            //var captchaElement = doc.DocumentNode.SelectSingleNode("//element[@id='rc-imageselect']");
            var captchaElement = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'g-recaptcha')]");
            if (captchaElement != null) BadRequest("Capcha!!!!!!");

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



    [HttpGet]
    public async Task<IActionResult> GetVoenNew([FromHeader] string voen = "1805591121")
    {
        VoenModel company = new();

        // Инициализация WebDriver
        IWebDriver driver = new ChromeDriver();
        try
        {
            // Открываем URL
            string url = $"https://e-taxes.gov.az/ebyn/commersialChecker.jsp?name={voen}&tip=2&sub_mit=1";
            driver.Navigate().GoToUrl(url);

            // Проверка на наличие капчи
            //var captchaElement = driver.FindElements(By.Id("rc-imageselect"));
            var captchaElement = driver.FindElements(By.XPath("//table[@id='rc-imageselect']"));
            if (captchaElement.Count > 0)
            {
                // Закрываем Chrome
                driver.Quit();

                // Инициализация WebDriver для Edge
                driver = new EdgeDriver();
                driver.Navigate().GoToUrl(url); 
            }

            // Получаем HTML-контент страницы
            var pageSource = driver.PageSource;
            var doc = new HtmlDocument();
            doc.LoadHtml(pageSource);

            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[@class='com']");
            if (table == null) return NotFound("Table not found");

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
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
        finally
        {
            // Закрываем браузер
            driver.Quit();
        }

        return Ok(company);
    }

    private bool IsCaptchaPresent(IWebDriver driver)
    {
        try
        { 
            var captchaElement = driver.FindElement(By.Id("rc-imageselect"));
            return captchaElement != null;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
}