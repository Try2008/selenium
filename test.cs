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
            try
            {
                driver.Manage().Window.Maximize();
                driver.Navigate().GoToUrl("https://www.wizzair.com/en-gb");

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                // --- 1. Robust Cookie Popup Handling ---
                Console.WriteLine("Waiting for cookie popup...");
                try
                {
                    // Find Shadow Host
                    var host = wait.Until(d => d.FindElement(By.CssSelector("#usercentrics-cmp-ui")));
                    var shadow = host.GetShadowRoot();

                    // Wait for Deny button inside Shadow DOM
                    var denyBtn = wait.Until(d => {
                        try 
                        { 
                            var btn = shadow.FindElement(By.CssSelector("#deny"));
                            return btn.Displayed ? btn : null;
                        }
                        catch { return null; }
                    });

                    denyBtn.Click();
                    Console.WriteLine("Popup denied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Popup warning (might not be present): " + ex.Message);
                }

                // --- 2. Robust Input Handling ---
                Console.WriteLine("Inputting origin...");
                IWebElement originInput = null;
                
                try
                {
                    // Try Data Test ID (Most Reliable)
                    originInput = wait.Until(d => d.FindElement(By.CssSelector("input[data-test-id='search-departure-station']")));
                }
                catch
                {
                    try 
                    {
                        // Fallback to Placeholder if Data Test ID fails due to A/B testing
                        originInput = wait.Until(d => d.FindElement(By.XPath("//input[@placeholder='Origin']"))); 
                    }
                    catch { Console.WriteLine("Input field not found!"); }
                }

                if (originInput != null)
                {
                    originInput.Click();
                    originInput.Clear();
                    originInput.SendKeys("Vienna");
                    
                    System.Threading.Thread.Sleep(1500); // Allow suggestions to load
                    originInput.SendKeys(Keys.Enter);
                    Console.WriteLine("Origin selected: Vienna");
                    
                    System.Threading.Thread.Sleep(5000); // Keep browser open to see result
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical Error: " + ex.Message);
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}