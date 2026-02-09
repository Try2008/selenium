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
            // --- מצב חמקן (Stealth) ---
            // WizzAir עלולים לזהות שאתה רובוט ולשנות את הדף. זה מנסה להסתיר את סלניום.
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            // options.AddArgument("--headless"); // במידה ותרצה לרוץ ברקע בעתיד

            IWebDriver driver = new ChromeDriver(options);
            
            // הסתרת דגל ה-webdriver ב-JS
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

            try
            {
                driver.Navigate().GoToUrl("https://www.wizzair.com/en-gb");

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
                
                Console.WriteLine("Waiting for site to load...");
                System.Threading.Thread.Sleep(5000);

                // נסגור עוגיות בצורה רובוסטית
                Console.WriteLine("Searching for cookie popup (Deep Scan)...");
                
                bool closed = false;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // ננסה במשך 20 שניות
                while (stopwatch.Elapsed.TotalSeconds < 20)
                {
                    if (CloseCookiesPopup(driver, wait))
                    {
                        closed = true;
                        Console.WriteLine("Cookie popup closed successfully!");
                        break;
                    }
                    System.Threading.Thread.Sleep(1000);
                }

                if (!closed)
                {
                    Console.WriteLine("\n--- DIAGNOSTIC RESULT ---");
                    Console.WriteLine("Popup not found. Running full structure dump to see what's on the page...");
                    DumpStructure(driver);
                    Console.WriteLine("-------------------------\n");
                    
                    // ננסה להמשיך בכל זאת, אולי הפופאפ לא קיים בכלל?
                }

                // --- שלב 2: איתור שדה הטקסט ---
                Console.WriteLine("Proceeding to text input...");
                System.Threading.Thread.Sleep(1000); 

                var originInput = wait.Until(d => d.FindElement(By.XPath("//input[@data-test-id='search-departure-station' or @placeholder='Origin']")));
                
                try {
                    originInput.Click();
                } catch {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", originInput);
                }
                
                originInput.Clear();
                originInput.SendKeys("Vienna");
                
                Console.WriteLine("Text entered successfully.");
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

        static void DumpStructure(IWebDriver driver)
        {
            try
            {
                // סריקת הדף הראשי
                driver.SwitchTo().DefaultContent();
                Console.WriteLine("Root Document:");
                ScanContext(driver);

                // סריקת iframes
                var iframes = driver.FindElements(By.TagName("iframe"));
                Console.WriteLine($"Found {iframes.Count} iframes.");
                
                for(int i=0; i<iframes.Count; i++)
                {
                    try
                    {
                        Console.WriteLine($"--- Switching to iframe {i} ---");
                        driver.SwitchTo().DefaultContent();
                        driver.SwitchTo().Frame(iframes[i]);
                        ScanContext(driver);
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine($"Error scanning iframe {i}: {ex.Message}");
                    }
                }
                
                driver.SwitchTo().DefaultContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dump Error: " + ex.Message);
            }
        }

        static void ScanContext(IWebDriver driver)
        {
            // בדיקת Usercentrics
            // לפי ה-HTML ששלחת, ה-Host הוא למעשה #usercentrics-cmp-ui
            var hosts = driver.FindElements(By.CssSelector("#usercentrics-cmp-ui"));
            if (hosts.Count > 0)
            {
                Console.WriteLine("  [V] Found #usercentrics-cmp-ui!");
                try 
                {
                    var host = hosts[0];
                    var shadow = host.GetShadowRoot();
                    Console.WriteLine("  [V] Accessed Shadow Root!");
                    
                    var btns = shadow.FindElements(By.CssSelector("button"));
                    Console.WriteLine($"  [i] Found {btns.Count} buttons inside Shadow DOM:");
                    foreach(var btn in btns)
                    {
                        try {
                            // הדפסת פרטי הכפתור כדי שנבין מה שמו
                            string id = btn.GetAttribute("id");
                            string cls = btn.GetAttribute("class");
                            string txt = btn.Text;
                            string testid = btn.GetAttribute("data-testid");
                            Console.WriteLine($"      -> Button: ID='{id}', Class='{cls}', Text='{txt}', TestID='{testid}'");
                        } catch {}
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("  [X] Error accessing Shadow: " + e.Message);
                }
            }
            else
            {
                // ננסה גם את הישן ליתר ביטחון
                var oldHosts = driver.FindElements(By.CssSelector("#usercentrics-root"));
                if (oldHosts.Count > 0) Console.WriteLine("  [V] Found #usercentrics-root (Old ID)!");
                else Console.WriteLine("  [ ] No usercentrics host found in this context.");
            }
        }

        static bool TryClickDenyInThisContext(IWebDriver driver, WebDriverWait wait)
        {
            try
            {
                // לפי ה-HTML שלך, ה-Shadow Host הוא: <aside id="usercentrics-cmp-ui">
                var hosts = driver.FindElements(By.CssSelector("#usercentrics-cmp-ui"));
                
                // fallback למקרה שזה בכל זאת ה-root הישן
                if (hosts.Count == 0) hosts = driver.FindElements(By.CssSelector("#usercentrics-root"));
                
                if (hosts.Count == 0) return false;

                var host = hosts[0];
                var shadow = host.GetShadowRoot();

                // עכשיו אנחנו בתוך ה-Shadow DOM.
                // הכפתור הוא: <button id="deny" class="deny uc-deny-button" ...>
                IWebElement btn = null;
                
                try { 
                    // ננסה לפי ID הכי פשוט
                    btn = shadow.FindElement(By.CssSelector("#deny")); 
                } catch {}

                if (btn == null) 
                {
                    try { 
                        // ננסה לפי Class
                        btn = shadow.FindElement(By.CssSelector(".uc-deny-button")); 
                    } catch {}
                }

                if (btn == null)
                {
                    try { 
                        // ננסה לפי Data Attribute
                        btn = shadow.FindElement(By.CssSelector("[data-action-type='deny']")); 
                    } catch {}
                }

                if (btn != null)
                {
                    Console.WriteLine("Found deny button! Clicking...");
                    // נחכה שיהיה מוצג
                    try { wait.Until(_ => btn.Displayed && btn.Enabled); } catch {}
                    
                    try {
                        btn.Click();
                    } catch {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
                    }
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static bool CloseCookiesPopup(IWebDriver driver, WebDriverWait wait)
        {
            driver.SwitchTo().DefaultContent();
            if (TryClickDenyInThisContext(driver, wait)) return true;

            var iframes = driver.FindElements(By.TagName("iframe"));
            foreach (var frame in iframes)
            {
                try
                {
                    driver.SwitchTo().DefaultContent();
                    driver.SwitchTo().Frame(frame);
                    if (TryClickDenyInThisContext(driver, wait)) {
                        driver.SwitchTo().DefaultContent();
                        return true;
                    }
                }
                catch {}
            }
            driver.SwitchTo().DefaultContent();
            return false;
        }
    }
}