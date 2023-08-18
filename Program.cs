using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace LibraryCheckConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------------------------------------Check Library console app start!");

            // we need to set cultureInfo on Slovenian becouse of date format 25.5.2022
            CultureInfo = new CultureInfo("sl-SI");

            // build appsetings.json config builder
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var users = Config.GetSection("User").Get<List<LibraryUser>>();

            foreach (var user in users)
            {
                Console.WriteLine("--------------------------------------Checking for: " + user.Name);

                CheckLibrary(user);
            }

            Console.WriteLine("--------------------------------------Preverjanje KONČAO");
        }

        public static CultureInfo CultureInfo { get; set; }
        public static IConfiguration Config { get; set; }

        public static IWebDriver Driver { get; set; }

        public static void CheckLibrary(LibraryUser userData)
        {
            // options for headless
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            IWebDriver driver = new ChromeDriver(Config.GetSection("ChromeDriverLocation").Value, chromeOptions);
            Driver = driver;

            var bookTitleList = new List<string>();
            int minDiff = 100;

            try
            {
                // This will open up the URL
                driver.Url = Config.GetSection("CobissLoginUrl").Value;

                Thread.Sleep(500);

                IWebElement libField = Driver.FindElement(By.ClassName("select2-selection__rendered"));
                libField.Click();

                IWebElement libNameField = Driver.FindElement(By.ClassName("select2-search__field"));

                libNameField.SendKeys(userData.LibraryCode);    // siktrz
                libNameField.SendKeys(Keys.Enter);

                IWebElement memberIdField = Driver.FindElement(By.Name("memberId"));
                memberIdField.SendKeys(userData.MemberId);  // 0104232

                IWebElement passwordField = Driver.FindElement(By.Name("password"));
                passwordField.SendKeys(userData.Password);   // knjiga

                IWebElement submitButton = Driver.FindElement(By.ClassName("search"));
                submitButton.Click();

                Thread.Sleep(500);

                IWebElement yesButton = Driver.FindElement(By.Id("btnConfirmCookie"));
                yesButton.Click();

                Thread.Sleep(500);

                Driver.Url = "https://plus.cobiss.net/cobiss/si/sl/memberships";

                Thread.Sleep(300);

                // siktrz/104232
                string loanLink = userData.LibraryCode + "/" + userData.MemberId;    // siktrz/0104232
                Driver.Url = Config.GetSection("CobissProfileUrl").Value + loanLink + "/loan";  // https://plus.cobiss.net/cobiss/si/sl/mylib/siktrz/0104232/loan

                Thread.Sleep(300);

                // booksAtHome = driver.find_elements_by_xpath("//tbody [@id='extLoanStuleBody']/tr")
                var booksAtHome = Driver.FindElements(By.XPath("//tbody [@id='extLoanStuleBody']/tr"));

                DateTime now = DateTime.Now;

                foreach (var bookElement in booksAtHome)
                {
                    IWebElement titleCell = bookElement.FindElement(By.XPath(".//td[3]"));
                    string bookTitle = titleCell.Text;

                    // return_date = book.find_element_by_xpath(".//td[2]")
                    IWebElement dateCell = bookElement.FindElement(By.XPath(".//td[2]"));
                    string dateStr = dateCell.Text;

                    DateTime dueDate = DateTime.Parse(dateStr, CultureInfo);

                    int diff = (dueDate - now).Days + 1;

                    if (diff <= Int16.Parse(Config.GetSection("NotifyDaysUntil").Value))
                    {
                        if (minDiff > diff)
                        {
                            minDiff = diff;
                        }

                        bookTitleList.Add(bookTitle);
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }
            finally { 
                driver.Close();
                driver.Quit();
            }

            if (bookTitleList.Count < 1)
            {
                Console.WriteLine("--------------------------------------Ni knjig, ki bi kmalu pretekle...");
                return;
            }

            // login to google smtp client
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(Config.GetSection("GoogleSmptEmail").Value, Config.GetSection("GoogleSmptPass").Value),
                EnableSsl = true
            };

            string bookListHtml = "<ul>";
            foreach (var title in bookTitleList)
            {
                bookListHtml += ("<li>" + title + "</li>");
            }
            bookListHtml += "</ul>";

            string emailBody = @"
                <!DOCTYPE html>
                <html>
                    <head>
                        <meta http-equiv='content-type' content='text/html; charset=UTF-8' />
                        <title>Page Title</title>
                    </head>
                <body>

                    Za uporabnika: " + userData.Name + " do preteka še <b>" + minDiff + @"</b> dni! <br/>Knjige, ki potečejo: " + bookListHtml + @"<br/>
                    <a href='https://plus.cobiss.net/cobiss/si/sl/mylib/siktrz/104232/loan'>Link na COBISS</a>

                </body>
                </html>";

            // emails comma delimited
            string emails = string.Join(",", userData.NotificationEmails);

            // send emails
            var mailMessage = new MailMessage(Config.GetSection("GoogleSmptEmail").Value, emails, "Knjiznica API - VRNI!", emailBody);
            mailMessage.IsBodyHtml = true;

            client.Send(mailMessage);

            Console.WriteLine("--------------------------------------Email sent to: " + emails);

        }

        public class LibraryUser
        {

            public string Name { get; set; }
            public string LibraryCode { get; set; }
            public string MemberId { get; set; }
            public string Password { get; set; }
            public List<string> NotificationEmails { get; set; }
        }
    }
}
