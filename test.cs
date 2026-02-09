using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace AutomationProject
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver();
            driver.Navigate().GoToUrl("https://www.wizzair.com/en-gb");
            driver.Manage().Window.Maximize();

            try
            {
                // Shadow DOM requires traversal from the host element
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                
                // 1. Find the Shadow Host
                var host = wait.Until(d => d.FindElement(By.CssSelector("#usercentrics-cmp-ui")));
                
                // 2. Get Shadow Root
                var shadow = host.GetShadowRoot();
                
                // 3. Find and Click the Deny Button inside the Shadow DOM
                var denyBtn = shadow.FindElement(By.CssSelector("#deny")); 
                denyBtn.Click();
                
                Console.WriteLine("Popup denied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Popup Error: " + ex.Message);
            }


            try
            {
                var originInput = wait.Until(d => d.FindElement(By.CssSelector("input[data-test='search-departure-station']")));
                originInput.Click();
                originInput.Clear();
                originInput.SendKeys("Tel-Aviv");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Origin Input Error: " + ex.Message);
            }


            

            driver.Quit();
        }
    }
}