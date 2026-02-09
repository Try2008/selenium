using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace AutomationProject
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver = InitDriver();
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                NavigateToSite(driver);
                HandleCookiePopup(driver, wait);

                // Origin
                SelectCity(driver, wait, "Vienna", "input[data-test-id='search-departure-station']", "Origin");
                
                // Destination
                SelectCity(driver, wait, "Barcelona", "input[data-test-id='search-arrival-station']", "Destination");
                // Note: WizzAir often uses 'search-arrival-station' for destination. 
                // Fallback to 'placeholder' if ID fails.

                // Dates
                SelectFlexibleDates(driver, wait);

                // Sort
                SelectSortByPrice(driver, wait);

                Console.WriteLine("Process completed successfully. Waiting 5 seconds...");
                System.Threading.Thread.Sleep(5000); 
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

        static IWebDriver InitDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            return new ChromeDriver(options);
        }

        static void NavigateToSite(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://www.wizzair.com/en-gb");
            Console.WriteLine("Navigated to WizzAir.");
        }

        static void HandleCookiePopup(IWebDriver driver, WebDriverWait wait)
        {
            Console.WriteLine("Waiting for cookie popup...");
            try
            {
                var host = wait.Until(d => d.FindElement(By.CssSelector("#usercentrics-cmp-ui")));
                var shadow = host.GetShadowRoot();
                var denyBtn = wait.Until(d => {
                    try { 
                        var btn = shadow.FindElement(By.CssSelector("#deny"));
                        return btn.Displayed ? btn : null;
                    } catch { return null; }
                });
                denyBtn.Click();
                Console.WriteLine("Popup denied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Popup warning: " + ex.Message);
            }
        }

        static void SelectCity(IWebDriver driver, WebDriverWait wait, string city, string cssSelector, string fieldName)
        {
            Console.WriteLine($"Selecting {fieldName}: {city}...");
            IWebElement input = null;
            
            try
            {
                input = wait.Until(d => d.FindElement(By.CssSelector(cssSelector)));
            }
            catch
            {
                try 
                {
                    // Fallback to Placeholder if ID fails
                    input = wait.Until(d => d.FindElement(By.XPath($"//input[@placeholder='{fieldName}']"))); 
                }
                catch 
                { 
                    Console.WriteLine($"{fieldName} input field not found!"); 
                    return; 
                }
            }

            input.Click();
            input.Clear();
            input.SendKeys(city);
            
            // Wait for suggestions and find the best match
            try
            {
                var suggestion = wait.Until(d => 
                {
                    var options = d.FindElements(By.CssSelector("div[data-test='locations-container'] label[data-test='flight-search-panel-location-label']"));
                    
                    // Fallback if container structure is slightly different
                    if (options.Count == 0)
                        options = d.FindElements(By.CssSelector("div[data-test='flight-search-locations'] label[data-test='flight-search-panel-location-label']"));
                    
                    // Specific fallback for "anywhere" or similar global options if needed, but we focus on locations
                    if (options.Count == 0)
                         options = d.FindElements(By.CssSelector("div.ds-locations-container label.ds-locations-container__location"));


                    foreach (var opt in options)
                    {
                        try
                        {
                            var nameEl = opt.FindElement(By.CssSelector(".ds-locations-container__location__name, strong"));
                            string text = nameEl.Text.Trim();
                            
                            // Check for exact match (ignoring case) OR if the city is contained (e.g. "Barcelona" in "Barcelona El Prat")
                            if (text.Equals(city, StringComparison.OrdinalIgnoreCase) || 
                                text.StartsWith(city, StringComparison.OrdinalIgnoreCase))
                            {
                                return opt;
                            }
                        }
                        catch { /* Ignore stale or missing elements in loop */ }
                    }
                    return null;
                });

                suggestion.Click();
                Console.WriteLine($"{fieldName} '{city}' selected via suggestion.");
            }
            catch (WebDriverTimeoutException)
            {
                // If suggestion click fails, try Enter key as fallback
                Console.WriteLine($"Suggestion for '{city}' not clicked (timeout). Trying Enter key...");
                input.SendKeys(Keys.Enter);
            }
        }

        static void SelectFlexibleDates(IWebDriver driver, WebDriverWait wait)
        {
            Console.WriteLine("Selecting 'Flexible on dates'...");
            try
            {
                var flexButton = wait.Until(d => d.FindElement(By.XPath("//*[contains(text(), 'Flexible on dates')]")));
                flexButton.Click();
                Console.WriteLine("'Flexible on dates' selected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not select 'Flexible on dates': " + ex.Message);
            }
        }


        static void SelectSortByPrice(IWebDriver driver, WebDriverWait wait)
        {
            Console.WriteLine("Opening sort dropdown...");
            try
            {
                // Click the dropdown trigger first
                var sortTrigger = wait.Until(d => d.FindElement(By.CssSelector("button[data-test='sort']")));
                sortTrigger.Click();

                // Wait for the dropdown options to appear and select 'Price low to high'
                var sortOption = wait.Until(d => d.FindElement(By.XPath("//span[contains(text(), 'Price low to high')]")));
                sortOption.Click();
                
                Console.WriteLine("Sort 'Price low to high' selected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not select sort option: " + ex.Message);
            }
        }

    }
}